using System.Collections;
using System.Linq;
using NUnit.Framework;
using Umbral.Core;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;

namespace Umbral.App.Tests
{
    /// <summary>Reglas de comodidad del GDD sobre el rig compartido, dentro del grey-box.
    /// Límite conocido: sin subsistema XR en CI no se observa la altura real de cámara en modo sentado; el test solo cubre la configuración.</summary>
    public class RigTests
    {
        private static IEnumerator LoadGreybox()
        {
            var load = SceneManager.LoadSceneAsync("Apartment_Greybox");
            while (!load.isDone) yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator SoloHayTeletransporteGiroPorPasosYGravedad()
        {
            yield return LoadGreybox();
            var enabled = Object.FindObjectsByType<LocomotionProvider>().Where(p => p.enabled)
                .Select(p => p.GetType().Name).OrderBy(n => n).ToArray();
            CollectionAssert.AreEquivalent(new[] { "GravityProvider", "SnapTurnProvider", "TeleportationProvider" }, enabled,
                "No debe haber movimiento continuo, agarre, escalada, salto ni giro suave");
        }

        [UnityTest]
        public IEnumerator ElGiroEsDe30Grados()
        {
            yield return LoadGreybox();
            var turns = Object.FindObjectsByType<SnapTurnProvider>(FindObjectsInactive.Include);
            Assert.IsNotEmpty(turns);
            foreach (var turn in turns)
            {
                Assert.AreEqual(ComfortSettings.SnapTurnDegrees, turn.turnAmount);
                Assert.IsFalse(turn.enableTurnAround, "El giro de 180° rompe la regla de pasos de 30°");
            }
        }

        [UnityTest]
        public IEnumerator TeletransportarOscureceYMueve()
        {
            yield return LoadGreybox();
            var origin = Object.FindFirstObjectByType<XROrigin>();
            var teleport = Object.FindFirstObjectByType<TeleportationProvider>();
            var fade = Object.FindFirstObjectByType<ComfortFade>();
            var target = new Vector3(5.5f, 0f, 2.0f); // en la cocina

            Assert.AreEqual(0f, fade.Alpha, 0.001f, "Empieza sin fundido");
            teleport.QueueTeleportRequest(new TeleportRequest
            {
                destinationPosition = target,
                matchOrientation = MatchOrientation.None,
            });

            var maxAlpha = 0f;
            var timeout = Time.realtimeSinceStartup + 4f;
            while (Time.realtimeSinceStartup < timeout)
            {
                maxAlpha = Mathf.Max(maxAlpha, fade.Alpha);
                var cam = origin.Camera.transform.position;
                if (Vector2.Distance(new Vector2(cam.x, cam.z), new Vector2(target.x, target.z)) < 0.05f && fade.Alpha < 0.001f) break;
                yield return null;
            }

            var final = origin.Camera.transform.position;
            Assert.AreEqual(target.x, final.x, 0.05f);
            Assert.AreEqual(target.z, final.z, 0.05f);
            Assert.Greater(maxAlpha, 0.95f, "La pantalla debe llegar a negro durante el teletransporte");
            Assert.AreEqual(0f, fade.Alpha, 0.001f, "Y volver a verse después");
        }

        [UnityTest]
        public IEnumerator AmbasManosPuedenTeletransportarseYGirar()
        {
            yield return LoadGreybox();
            var managers = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include)
                .Where(m => m.GetType().Name == "ControllerInputActionManager").ToArray();
            Assert.AreEqual(2, managers.Length, "Un manager por mano");
            foreach (var m in managers)
            {
                var prop = m.GetType().GetProperty("smoothMotionEnabled");
                Assert.IsNotNull(prop, "smoothMotionEnabled no existe (¿cambió la API del sample?)");
                Assert.IsFalse((bool)prop.GetValue(m), $"{m.name}: smooth motion apagaría teletransporte y giro en esa mano");
            }
        }

        [UnityTest]
        public IEnumerator DesactivarElFundidoNoDejaLaPantallaNegra()
        {
            yield return LoadGreybox();
            var fade = Object.FindFirstObjectByType<ComfortFade>();
            fade.FadeTo(1f, 0.05f);
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(1f, fade.Alpha, 0.01f);

            fade.gameObject.SetActive(false);
            fade.gameObject.SetActive(true);
            Assert.AreEqual(0f, fade.Alpha, 0.001f, "Al reactivarse no puede seguir oscuro");
        }

        [UnityTest]
        public IEnumerator SiFallaElCambioDeLuzLaPantallaVuelve()
        {
            yield return LoadGreybox();
            var fade = Object.FindFirstObjectByType<ComfortFade>();
            LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("boom"));
            fade.FadeThrough(() => throw new System.InvalidOperationException("boom"));
            yield return new WaitForSeconds(0.8f);
            Assert.AreEqual(0f, fade.Alpha, 0.001f);
        }

        [UnityTest]
        public IEnumerator ModoSentadoUsaTrackingDeDispositivoConAlturaDePie()
        {
            yield return LoadGreybox();
            var origin = Object.FindFirstObjectByType<XROrigin>();
            var seated = Object.FindFirstObjectByType<SeatedMode>();

            seated.SetSeated(true, ComfortSettings.StandingEyeHeight);
            Assert.AreEqual(XROrigin.TrackingOriginMode.Device, origin.RequestedTrackingOriginMode);
            Assert.AreEqual(ComfortSettings.StandingEyeHeight, origin.CameraYOffset, 0.001f);

            seated.SetSeated(false, ComfortSettings.StandingEyeHeight);
            Assert.AreEqual(XROrigin.TrackingOriginMode.Floor, origin.RequestedTrackingOriginMode);
        }
    }
}

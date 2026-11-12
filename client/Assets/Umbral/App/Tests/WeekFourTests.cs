using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Umbral.App.Tests
{
    /// <summary>Semana 4: cinta métrica, botones de la muñeca y reloj solar del grey-box. No requieren GPU.</summary>
    public class WeekFourTests
    {
        private static IEnumerator LoadGreybox()
        {
            var load = SceneManager.LoadSceneAsync("Apartment_Greybox");
            while (!load.isDone) yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator ElGreyboxTieneRelojSolarConCuatroParadasHorneadas()
        {
            yield return LoadGreybox();
            var clock = Object.FindFirstObjectByType<SolarClock>();
            Assert.IsNotNull(clock, "Falta SolarClock en Apartment_Greybox");
            Assert.AreEqual(4, clock.Count, "Faltan paradas: corré Umbral > Bake Greybox Solar después de regenerar la escena");
            Assert.AreEqual(1, clock.Current, "Debe arrancar en mediodía");
            foreach (var l in Object.FindObjectsByType<Light>())
                Assert.AreEqual(LightmapBakeType.Baked, l.lightmapBakeType, $"La luz '{l.name}' debe ser horneada (no Mixed ni Realtime)");
        }

        [UnityTest]
        [Category("Spike")]
        public IEnumerator LasCuatroParadasSeVenDistintasEnElGreybox()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("Requiere GPU para renderizar (no usar -nographics)");
            yield return LoadGreybox();
            var clock = Object.FindFirstObjectByType<SolarClock>();
            var cam = new GameObject("TestCam", typeof(Camera)).GetComponent<Camera>();
            cam.transform.position = new Vector3(3.5f, 1.6f, 3.0f);
            cam.transform.rotation = Quaternion.Euler(10f, 90f, 0f);
            var rt = new RenderTexture(256, 144, 24);
            cam.targetTexture = rt;
            cam.enabled = false;

            var brightness = new float[4];
            for (var i = 0; i < 4; i++)
            {
                clock.Apply(i);
                yield return null;
                cam.Render();
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                RenderTexture.active = prev;
                double sum = 0;
                foreach (var p in tex.GetPixels32()) sum += 0.2126 * p.r + 0.7152 * p.g + 0.0722 * p.b;
                brightness[i] = (float)(sum / (rt.width * rt.height) / 255.0);
                Object.Destroy(tex);
            }
            Debug.Log($"GREYBOX_SOLAR brillo manana={brightness[0]:0.000} mediodia={brightness[1]:0.000} tarde={brightness[2]:0.000} noche={brightness[3]:0.000}");
            for (var i = 0; i < 4; i++)
            for (var j = i + 1; j < 4; j++)
                Assert.Greater(Mathf.Abs(brightness[i] - brightness[j]), 0.005f, $"Las paradas {i} y {j} se ven iguales");
            Assert.Less(brightness[3], Mathf.Min(brightness[0], brightness[1], brightness[2]), "La noche debe ser la más oscura");
            Object.Destroy(cam.gameObject);
        }

        [UnityTest]
        public IEnumerator LosBotonesDelRelojCambianLaParadaYSeRestauraElAmbiente()
        {
            yield return LoadGreybox();
            var clock = Object.FindFirstObjectByType<SolarClock>();
            var buttons = Object.FindObjectsByType<WristButton>(FindObjectsInactive.Include);
            Assert.AreEqual(5, buttons.Length, "4 paradas + cinta");

            WristButton Named(string n) { foreach (var b in buttons) if (b.name == n) return b; return null; }
            var ambientNoon = RenderSettings.ambientLight;
            Named("Button_Reloj_19:00").Press();
            var timeout = Time.realtimeSinceStartup + 5f;
            while (clock.Current != 3 && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.AreEqual(3, clock.Current);
            Assert.Less(RenderSettings.ambientLight.grayscale, ambientNoon.grayscale, "De noche el ambiente debe ser más oscuro");

            var fade = Object.FindFirstObjectByType<ComfortFade>(FindObjectsInactive.Include);
            timeout = Time.realtimeSinceStartup + 5f;
            while (fade.Busy && Time.realtimeSinceStartup < timeout) yield return null; // los botones ignoran pulsaciones durante un fundido
            Named("Button_Reloj_12:00").Press();
            timeout = Time.realtimeSinceStartup + 5f;
            while (clock.Current != 1 && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.AreEqual(1, clock.Current);
            Assert.AreEqual(ambientNoon, RenderSettings.ambientLight, "Al volver a las 12:00 vuelve el ambiente de mediodía");
        }

        [UnityTest]
        public IEnumerator LaCintaMideDosPuntosYSeReinicia()
        {
            yield return LoadGreybox();
            var tape = Object.FindFirstObjectByType<TapeMeasure>(FindObjectsInactive.Include);
            Assert.IsNotNull(tape);

            tape.Toggle();
            Assert.IsTrue(tape.Active);
            tape.AddPoint(new Vector3(1f, 0f, 1f));
            Assert.AreEqual("Punto 1", tape.LabelText);
            tape.AddPoint(new Vector3(1f, 2f, 3f));
            Assert.AreEqual(Mathf.RoundToInt(new Vector3(0f, 2f, 2f).magnitude * 100f), tape.LastCentimeters);
            Assert.AreEqual("283 cm", tape.LabelText);

            tape.AddPoint(Vector3.zero); // tercer punto: empieza otra medida
            Assert.AreEqual(-1, tape.LastCentimeters);

            tape.Toggle();
            Assert.IsFalse(tape.Active);
            Assert.AreEqual(-1, tape.LastCentimeters);
        }

        [UnityTest]
        public IEnumerator EnBootLosBotonesNoRompenSinRelojNiCinta()
        {
            var load = SceneManager.LoadSceneAsync("Boot");
            while (!load.isDone) yield return null;
            yield return null;
            foreach (var b in Object.FindObjectsByType<WristButton>(FindObjectsInactive.Include)) b.Press(); // no debe lanzar
            Assert.Pass();
        }
    }
}

using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Umbral.Core;
using Umbral.Net;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace Umbral.App.Tests
{
    /// <summary>Flujo de escenas de la semana 5: lobby, maqueta y llegada. Sin visor ni simulador: los eventos de interacción se disparan a mano.</summary>
    public class WeekFiveTests
    {
        [SetUp]
        public void SetUp() => Session.Reset();

        [TearDown]
        public void TearDown() => Session.Reset();

        private static IEnumerator Load(string scene)
        {
            var load = SceneManager.LoadSceneAsync(scene);
            while (!load.isDone) yield return null;
            yield return null;
        }

        private static IEnumerator WaitFor(System.Func<bool> condition, float seconds = 15f)
        {
            var timeout = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < timeout) yield return null;
        }

        private static async Task<Catalog> LoadCatalog() => await CatalogClient.LoadAsync("");

        [UnityTest]
        public IEnumerator UnaCacheViejaNoTapaElCatalogoNuevo()
        {
            var path = Path.Combine(Application.persistentDataPath, "catalog.json");
            var backup = File.Exists(path) ? File.ReadAllText(path) : null;
            try
            {
                File.WriteAllText(path, "{\"version\":1,\"validUntil\":\"2020-01-01\",\"units\":[{\"id\":\"A-301\",\"floor\":3,\"areaM2\":58,\"basePrice\":1}],\"surfaces\":[]}");
                var task = LoadCatalog();
                yield return WaitFor(() => task.IsCompleted);
                Assert.IsTrue(task.IsCompletedSuccessfully);
                Assert.Greater(task.Result.version, 1, "Gana el seed de versión mayor");
                var statuses = task.Result.units.Select(CatalogQueries.StatusOf).Distinct().ToArray();
                CollectionAssert.IsSupersetOf(statuses, new[] { UnitStatus.Available, UnitStatus.Reserved, UnitStatus.Sold });
            }
            finally
            {
                if (backup != null) File.WriteAllText(path, backup); else File.Delete(path);
            }
        }

        [UnityTest]
        public IEnumerator ElTutorialDelLobbyVaEnOrdenYSeCompletaUnaVez()
        {
            yield return Load("Lobby");
            var director = Object.FindFirstObjectByType<TutorialDirector>();
            var teleport = Object.FindFirstObjectByType<TeleportationProvider>();
            var origin = Object.FindFirstObjectByType<XROrigin>();
            var marker = GameObject.Find("Marca_Teletransporte").transform.position;
            var aim = GameObject.Find("Objetivo_Apuntar").GetComponent<XRSimpleInteractable>();
            var grab = GameObject.Find("Cubo_Agarrar").GetComponent<XRGrabInteractable>();
            var done = 0;
            director.Steps.AllDone += () => done++;

            // Teletransportarse antes de apuntar no cuenta
            teleport.QueueTeleportRequest(new TeleportRequest { destinationPosition = marker, matchOrientation = MatchOrientation.None });
            yield return WaitFor(() => Vector2.Distance(new Vector2(origin.Camera.transform.position.x, origin.Camera.transform.position.z), new Vector2(marker.x, marker.z)) < 0.05f);
            yield return null;
            Assert.AreEqual(TutorialStep.Aim, director.Steps.Current);

            grab.selectEntered.Invoke(new SelectEnterEventArgs()); // agarrar fuera de turno tampoco
            Assert.AreEqual(TutorialStep.Aim, director.Steps.Current);

            aim.hoverEntered.Invoke(new HoverEnterEventArgs());
            Assert.AreEqual(TutorialStep.Teleport, director.Steps.Current);
            yield return null; // Update ve que ya estamos sobre la marca
            Assert.AreEqual(TutorialStep.Grab, director.Steps.Current);

            grab.selectEntered.Invoke(new SelectEnterEventArgs());
            Assert.IsTrue(director.Steps.Completed);
            grab.selectEntered.Invoke(new SelectEnterEventArgs());
            Assert.AreEqual(1, done);
        }

        [UnityTest]
        public IEnumerator SentadoDelLobbyViajaALaMaquetaYElLobbyNoMarcaUnaUnidad()
        {
            yield return Load("Lobby");
            GameObject.Find("Boton_Sentado").GetComponent<SeatedButton>().Press();
            Assert.IsTrue(Session.Flow.Seated);

            GameObject.Find("Boton_Maqueta").GetComponent<FlowButton>().Press();
            yield return WaitFor(() => SceneManager.GetActiveScene().name == "Maqueta");
            yield return null;
            yield return null;

            Assert.AreEqual(SessionStage.Maqueta, Session.Flow.Stage);
            Assert.AreEqual(XROrigin.TrackingOriginMode.Device, Object.FindFirstObjectByType<XROrigin>().RequestedTrackingOriginMode, "Sigue sentado en la escena siguiente");
        }

        [UnityTest]
        public IEnumerator LaMaquetaMuestraElEstadoYSoloDejaElegirLasDisponibles()
        {
            yield return Load("Maqueta");
            var board = Object.FindFirstObjectByType<MaquetaBoard>();
            yield return WaitFor(() => board.Loaded);
            Assert.IsTrue(board.Loaded);

            var blocks = Object.FindObjectsByType<UnitBlock>();
            Assert.IsNotEmpty(blocks);
            foreach (var b in blocks)
                Assert.AreEqual(b.Status == UnitStatus.Available, b.Selectable, $"{b.UnitId}: solo las disponibles se eligen");
            CollectionAssert.IsSupersetOf(blocks.Select(b => b.Status).Distinct().ToArray(), new[] { UnitStatus.Available, UnitStatus.Reserved, UnitStatus.Sold });

            var sold = blocks.First(b => b.Status == UnitStatus.Sold);
            sold.Press();
            yield return new WaitForSeconds(0.5f);
            Assert.IsNull(Session.Flow.SelectedUnitId);
            Assert.AreEqual("Maqueta", SceneManager.GetActiveScene().name, "Una unidad vendida no cambia de escena");
        }

        [UnityTest]
        public IEnumerator ElegirUnaUnidadLlevaALaPuertaConElPrecioDeEsaUnidad()
        {
            yield return Load("Maqueta");
            var board = Object.FindFirstObjectByType<MaquetaBoard>();
            yield return WaitFor(() => board.Loaded);
            var fade = Object.FindFirstObjectByType<ComfortFade>();

            Object.FindObjectsByType<UnitBlock>().First(b => b.UnitId == "A-1201").Press();
            var sawFade = false;
            yield return WaitFor(() => SceneManager.GetActiveScene().name == "Apartment_Greybox");
            fade = Object.FindFirstObjectByType<ComfortFade>();
            sawFade = fade.Alpha > 0.5f; // la escena abierta por el flujo empieza en negro: no hay destello
            Assert.IsTrue(sawFade, "Debe empezar oscura y aclarar");
            yield return WaitFor(() => fade.Alpha < 0.001f);
            Assert.AreEqual(0f, fade.Alpha, 0.001f);

            Assert.AreEqual(SessionStage.Llegada, Session.Flow.Stage);
            var host = Object.FindFirstObjectByType<ConfiguratorHost>();
            var configurator = host.Ready.Result;
            Assert.AreEqual("A-1201", configurator.UnitId);

            var wrist = Object.FindFirstObjectByType<WristPrice>(FindObjectsInactive.Include);
            for (var tr = wrist.transform; tr != null; tr = tr.parent) tr.gameObject.SetActive(true);
            var label = wrist.GetComponentInChildren<Text>();
            var expected = PriceFormat.Bolivianos(PriceCalculator.Total(configurator.Catalog, "A-1201", CatalogQueries.DefaultSelection(configurator.Catalog)));
            yield return WaitFor(() => label.text.Contains("A-1201"));
            StringAssert.Contains("A-1201", label.text);
            StringAssert.Contains(expected, label.text);

            var xro = Object.FindFirstObjectByType<XROrigin>();
            var cam = xro.Camera.transform.position;
            Assert.Less(Vector2.Distance(new Vector2(cam.x, cam.z), new Vector2(1.45f, 1.2f)), 0.6f, "Llega dentro de la puerta de entrada");
        }

        [UnityTest]
        public IEnumerator LaEscenaDeLlegadaSinFlujoSigueMostrandoA301()
        {
            yield return Load("Apartment_Greybox");
            var host = Object.FindFirstObjectByType<ConfiguratorHost>();
            yield return WaitFor(() => host.Ready.IsCompleted);
            Assert.AreEqual("A-301", host.Ready.Result.UnitId);
        }
    }
}

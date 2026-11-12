using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Umbral.Core;
using Umbral.Net;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Umbral.App.Tests
{
    /// <summary>Arreglos de la revisión de la semana 5.</summary>
    public class WeekFiveFixesTests
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

        [Test]
        public void UnaCacheCorruptaSeTrataComoSinCache()
        {
            var path = Path.Combine(Application.persistentDataPath, "test-cache-corrupta.json");
            try
            {
                File.WriteAllText(path, "{\"version\":2,\"units\":[{\"id\":\"A-30");
                Assert.IsNull(CatalogCache.TryRead(path));
                File.WriteAllText(path, "no es json");
                Assert.IsNull(CatalogCache.TryRead(path));
                Assert.IsNull(CatalogCache.TryRead(path + ".no-existe"));
            }
            finally { File.Delete(path); }
        }

        [Test]
        public void LaCacheSeEscribeEntera_YNoQuedaElTemporal()
        {
            var path = Path.Combine(Application.persistentDataPath, "test-cache-atomica.json");
            try
            {
                CatalogCache.Write(path, "{\"version\":1,\"validUntil\":\"x\",\"units\":[],\"surfaces\":[]}");
                CatalogCache.Write(path, "{\"version\":5,\"validUntil\":\"x\",\"units\":[],\"surfaces\":[]}");
                Assert.AreEqual(5, CatalogCache.TryRead(path).version);
                Assert.IsFalse(File.Exists(path + ".tmp"));
            }
            finally { File.Delete(path); File.Delete(path + ".tmp"); }
        }

        [UnityTest]
        public IEnumerator ConUnaCacheCorruptaElCatalogoSaleDelSeed()
        {
            var path = Path.Combine(Application.persistentDataPath, "catalog.json");
            var backup = File.Exists(path) ? File.ReadAllText(path) : null;
            try
            {
                File.WriteAllText(path, "{\"version\":2,\"units\":[{\"id\":\"A-30");
                var load = CatalogClient.LoadAsync("");
                var done = false; Catalog result = null; System.Exception error = null;
                Run(load, c => { result = c; done = true; }, e => { error = e; done = true; });
                yield return WaitFor(() => done);
                Assert.IsNull(error);
                Assert.IsNotNull(result);
                Assert.IsNotEmpty(result.units);
            }
            finally
            {
                if (backup != null) File.WriteAllText(path, backup); else File.Delete(path);
            }
        }

        private static async void Run(Awaitable<Catalog> load, System.Action<Catalog> ok, System.Action<System.Exception> fail)
        {
            try { ok(await load); }
            catch (System.Exception e) { fail(e); }
        }

        [UnityTest]
        public IEnumerator LosBloquesDeLaMaquetaDejanMasDeDobleDelRadioDeAgarreEntreVecinos()
        {
            yield return Load("Maqueta");
            var blocks = Object.FindObjectsByType<UnitBlock>().OrderBy(b => b.transform.position.y).ToArray();
            Assert.GreaterOrEqual(blocks.Length, 2);
            for (var i = 1; i < blocks.Length; i++)
            {
                var below = blocks[i - 1].GetComponent<Renderer>().bounds;
                var above = blocks[i].GetComponent<Renderer>().bounds;
                var gap = above.min.y - below.max.y;
                Assert.Greater(gap, 0.2f, $"{blocks[i - 1].UnitId} y {blocks[i].UnitId}: el radio de agarre cercano es 0.1 m");
            }
        }

        [UnityTest]
        public IEnumerator ConUnCambioDeEscenaEnCursoUnaSegundaUnidadNoSeElige()
        {
            yield return Load("Maqueta");
            var board = Object.FindFirstObjectByType<MaquetaBoard>();
            yield return WaitFor(() => board.Loaded);
            var blocks = Object.FindObjectsByType<UnitBlock>().Where(b => b.Status == UnitStatus.Available).ToArray();
            Assert.GreaterOrEqual(blocks.Length, 2);

            SceneFlow.Busy = true; // hay una transición en marcha
            blocks[1].Press();
            Assert.IsNull(Session.Flow.SelectedUnitId, "No debe cambiar la unidad a mitad de la transición");
            SceneFlow.Busy = false;
            Assert.AreEqual("Maqueta", SceneManager.GetActiveScene().name);
        }

        [UnityTest]
        public IEnumerator LaLlegadaCompensaElDesplazamientoYElGiroDelVisor()
        {
            yield return Load("Apartment_Greybox");
            var origin = Object.FindFirstObjectByType<XROrigin>();
            var arrival = Object.FindFirstObjectByType<ArrivalPoint>();
            // El usuario está a 0.8 m del centro de su área segura y girado 90°.
            origin.Camera.transform.localPosition = new Vector3(0.8f, 1.6f, 0.3f);
            origin.Camera.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            arrival.Place();
            yield return null;

            var cam = origin.Camera.transform;
            Assert.Less(Vector2.Distance(new Vector2(cam.position.x, cam.position.z), new Vector2(arrival.transform.position.x, arrival.transform.position.z)), 0.05f);
            var yaw = Mathf.DeltaAngle(cam.eulerAngles.y, arrival.transform.eulerAngles.y);
            Assert.Less(Mathf.Abs(yaw), 1f, "Mira hacia adentro, no hacia una pared");
        }
    }
}

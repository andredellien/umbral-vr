using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Umbral.Core;
using Umbral.Net;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Umbral.App.Tests
{
    /// <summary>Semana 7: telemetría con cola local y reservas.</summary>
    public class WeekSevenTests
    {
        private string dir;
        private TelemetryQueue queue;

        [SetUp]
        public void SetUp()
        {
            Session.Reset();
            dir = Path.Combine(Application.temporaryCachePath, "wk7-" + System.Guid.NewGuid());
            Directory.CreateDirectory(dir);
            queue = new TelemetryQueue(Path.Combine(dir, "telemetry.jsonl"));
            Telemetry.Configure(queue);
        }

        [TearDown]
        public void TearDown()
        {
            Session.Reset();
            Telemetry.Configure(null);
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }

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

        private static TelemetryEvent Ev(string type = "x") => new TelemetryEvent { id = System.Guid.NewGuid().ToString(), type = type };

        [Test]
        public void LaColaGuardaEnOrdenSaltaLineasRotasYQuitaPorId()
        {
            var a = Ev("a"); var b = Ev("b"); var c = Ev("c");
            queue.Append(a);
            File.AppendAllText(Path.Combine(dir, "telemetry.jsonl"), "{esto no es json\n");
            queue.Append(b);
            queue.Append(c);

            Assert.AreEqual(new[] { a.id, b.id, c.id }, queue.ReadBatch(10).Select(e => e.id).ToArray());
            Assert.AreEqual(2, queue.ReadBatch(2).Count, "El lote respeta el máximo");

            queue.Remove(new[] { a.id, b.id });
            Assert.AreEqual(new[] { c.id }, queue.ReadBatch(10).Select(e => e.id).ToArray());
            Assert.AreEqual(1, queue.Count);
        }

        [Test]
        public void LaColaSobreviveAUnReinicio()
        {
            var e = Ev("session_start");
            queue.Append(e);
            var again = new TelemetryQueue(Path.Combine(dir, "telemetry.jsonl")); // otra instancia = app reabierta
            Assert.AreEqual(e.id, again.ReadBatch(5)[0].id);
        }

        [UnityTest]
        public IEnumerator SiElEnvioFallaLosEventosSeQuedanYElReintentoEsperaMas()
        {
            queue.Append(Ev()); queue.Append(Ev());
            var sent = 0;
            var uploader = new TelemetryUploader(queue, json => { sent++; return Fail(); });

            var t = Flush(uploader, 100f);
            yield return WaitFor(() => t.done);
            Assert.IsFalse(t.ok);
            Assert.AreEqual(2, queue.Count, "Un fallo no pierde eventos");
            Assert.IsFalse(uploader.Due(100f + 1f), "Espera antes de reintentar");
            Assert.IsTrue(uploader.Due(100f + 3f));

            var t2 = Flush(uploader, 103f);
            yield return WaitFor(() => t2.done);
            Assert.AreEqual(2, uploader.Failures);
            Assert.IsFalse(uploader.Due(103f + 3f), "La segunda espera es mayor (4 s)");
            Assert.IsTrue(uploader.Due(103f + 5f));
            Assert.AreEqual(2, sent);
        }

        [UnityTest]
        public IEnumerator ElReintentoManda_LosMismosIds_YAlConfirmarSeVaciaLaCola()
        {
            queue.Append(Ev()); queue.Append(Ev());
            var bodies = new System.Collections.Generic.List<string>();
            var accept = false;
            var uploader = new TelemetryUploader(queue, json => { bodies.Add(json); return Reply(accept); });

            var t1 = Flush(uploader, 0f);
            yield return WaitFor(() => t1.done);
            accept = true;
            var t2 = Flush(uploader, 10f);
            yield return WaitFor(() => t2.done);

            Assert.IsTrue(t2.ok);
            Assert.AreEqual(0, queue.Count);
            Assert.AreEqual(2, bodies.Count);
            Assert.AreEqual(bodies[0], bodies[1], "El mismo lote con los mismos ids: el servidor lo deduplica");
            StringAssert.StartsWith("{\"events\":[", bodies[0]);
        }

        private class Result { public bool done, ok; }

        private static Result Flush(TelemetryUploader u, float now)
        {
            var r = new Result();
            Run(u, now, r);
            return r;
        }

        private static async void Run(TelemetryUploader u, float now, Result r)
        {
            r.ok = await u.FlushAsync(now);
            r.done = true;
        }

        private static async Awaitable<bool> Fail() { await Awaitable.NextFrameAsync(); return false; }
        private static async Awaitable<bool> Reply(bool ok) { await Awaitable.NextFrameAsync(); return ok; }

        [UnityTest]
        public IEnumerator LaSesionRegistraEtapaAcabadoYHerramientas()
        {
            yield return Load("Apartment_Greybox");
            var host = Object.FindFirstObjectByType<ConfiguratorHost>();
            yield return WaitFor(() => host.Ready.IsCompleted);
            var swatches = Object.FindObjectsByType<FinishSwatch>();
            yield return null;

            // cambiar a una opción distinta de la incluida
            var configurator = host.Ready.Result;
            var target = swatches.First(s => configurator.SelectedOptionId(s.SurfaceId) != s.OptionId);
            target.Choose();
            var events = queue.ReadBatch(50);
            var chosen = events.FirstOrDefault(e => e.type == TelemetryTypes.FinishChosen);
            Assert.IsNotNull(chosen);
            Assert.AreEqual(target.SurfaceId, chosen.surface);
            Assert.AreEqual(target.OptionId, chosen.option);
            Assert.IsFalse(string.IsNullOrEmpty(chosen.id));
            Assert.AreEqual(events.Select(e => e.id).Distinct().Count(), events.Count, "Cada evento tiene su propio id");

            Object.FindFirstObjectByType<SolarClock>().Select(0);
            Assert.IsTrue(queue.ReadBatch(50).Any(e => e.type == TelemetryTypes.ToolUsed && e.tool == "solar"));
        }

        [UnityTest]
        public IEnumerator PasarDeEtapaRegistraStageEntered()
        {
            yield return Load("Lobby");
            queue = new TelemetryQueue(Path.Combine(dir, "stages.jsonl"));
            Telemetry.Configure(queue);

            SceneFlow.GoTo(SessionStage.Maqueta);
            yield return WaitFor(() => SceneManager.GetActiveScene().name == "Maqueta");
            yield return null;

            var stage = queue.ReadBatch(10).FirstOrDefault(e => e.type == TelemetryTypes.StageEntered);
            Assert.IsNotNull(stage);
            Assert.AreEqual("Maqueta", stage.stage);
        }

        [UnityTest]
        public IEnumerator ElGreyboxTieneElBotonDeReserva()
        {
            yield return Load("Apartment_Greybox");
            var button = Object.FindFirstObjectByType<ReserveButton>();
            Assert.IsNotNull(button);
            Assert.IsNotNull(button.GetComponent<XRSimpleInteractable>());
        }

        // Con el servidor real (UMBRAL_SERVER=http://localhost:3000 y datos limpios): reserva A-1201 y muestra la ficha con el total del servidor.
        [UnityTest]
        public IEnumerator InteropReservaConElServidorReal()
        {
            var server = System.Environment.GetEnvironmentVariable("UMBRAL_SERVER");
            if (string.IsNullOrEmpty(server)) Assert.Ignore("Definí UMBRAL_SERVER para la prueba contra el servidor real");
            SessionHost.ServerUrl = server;

            Session.Flow.SelectUnit(new Catalog { units = new[] { new UnitInfo { id = "A-1201" } }, surfaces = new SurfaceInfo[0] }, "A-1201");
            yield return Load("Apartment_Greybox");
            var host = Object.FindFirstObjectByType<ConfiguratorHost>();
            yield return WaitFor(() => host.Ready.IsCompleted);
            var configurator = host.Ready.Result;
            Assert.AreEqual("A-1201", configurator.UnitId);
            configurator.Select("floor", "floor-wood");

            var button = Object.FindFirstObjectByType<ReserveButton>();
            button.Press();
            yield return WaitFor(() => button.LastOutcome != null);
            Assert.AreEqual(ReservationStatus.Ok, button.LastOutcome.Status, button.LastOutcome.Message);
            var expected = PriceCalculator.Total(configurator.Catalog, "A-1201", CatalogQueriesSelection(configurator));
            Assert.AreEqual(expected, button.LastOutcome.Reservation.total, "El servidor y el cliente calculan lo mismo");
            StringAssert.Contains(PriceFormat.Bolivianos(expected), button.LastText);

            // una segunda reserva de la misma unidad: el servidor responde 409
            var again = new ReservationOutcome();
            var task = ReservationClient.ReserveAsync(server, "A-1201", Ficha.SelectionsOf(configurator));
            var done = false;
            Wait(task, o => { again = o; done = true; });
            yield return WaitFor(() => done);
            Assert.AreEqual(ReservationStatus.Unavailable, again.Status);
            SessionHost.ServerUrl = "http://localhost:3000";
        }

        private static System.Collections.Generic.Dictionary<string, string> CatalogQueriesSelection(Configurator c) =>
            Ficha.SelectionsOf(c).ToDictionary(s => s.surface, s => s.option);

        private static async void Wait(Awaitable<ReservationOutcome> t, System.Action<ReservationOutcome> done) => done(await t);
    }
}

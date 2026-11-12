using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Umbral.Core;
using Umbral.Net;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

namespace Umbral.App.Tests
{
    [Serializable] internal class StateEnvelope { public string @event; public SessionSnapshot data; }
    [Serializable] internal class CommandEnvelope { public string @event; public SessionCommand data; }

    /// <summary>Transporte en memoria: guarda lo enviado y deja inyectar mensajes del relay.</summary>
    internal class FakeTransport : ISessionTransport
    {
        public readonly List<string> Sent = new List<string>();
        public string Url;
        public bool IsOpen { get; private set; }
        public event Action<string> Received;
        public event Action Closed;

        public Task ConnectAsync(string url) { Url = url; IsOpen = true; return Task.CompletedTask; }
        public void Send(string json) => Sent.Add(json);
        public void Close() { if (!IsOpen) return; IsOpen = false; Closed?.Invoke(); }
        public void Receive(string json) => Received?.Invoke(json);
        public int Count(string eventName) => Sent.Count(s => s.Contains("\"event\":\"" + eventName + "\""));
    }

    /// <summary>Semana 6: sala por código, estado espejo, pausa y pantalla del asesor. Sin servidor: relay falso en memoria.</summary>
    public class WeekSixTests
    {
        private readonly List<FakeTransport> made = new List<FakeTransport>();
        private FakeTransport Fake => made[made.Count - 1];

        [SetUp]
        public void SetUp()
        {
            Session.Reset();
            made.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            Session.Reset();
            SessionHost.Restart(); // vuelve al transporte real
        }

        private IEnumerator StartHost()
        {
            SessionHost.Restart(() => { var t = new FakeTransport(); made.Add(t); return t; });
            yield return WaitFor(() => made.Count > 0 && made[0].Count("host") > 0);
            Fake.Receive("{\"event\":\"hosted\",\"data\":{\"code\":\"" + SessionHost.Code + "\"}}");
        }

        private static IEnumerator Load(string scene)
        {
            var load = SceneManager.LoadSceneAsync(scene);
            while (!load.isDone) yield return null;
            yield return null;
        }

        private static IEnumerator WaitFor(Func<bool> condition, float seconds = 15f)
        {
            var timeout = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < timeout) yield return null;
        }

        private static IEnumerator Wait(float seconds)
        {
            var end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end) yield return null;
        }

        private static StateEnvelope LastState(FakeTransport t) =>
            JsonUtility.FromJson<StateEnvelope>(t.Sent.Last(s => s.Contains("\"event\":\"state\"")));

        // --- Protocolo ---

        [Test]
        public void ElEnlaceUsaElFormatoEventoYDatos()
        {
            var t = new FakeTransport();
            var link = new SessionLink(t);
            link.Host("1234");
            link.SendCommand(SessionCommand.Pause());
            StringAssert.Contains("\"event\":\"host\"", t.Sent[0]);
            StringAssert.Contains("\"code\":\"1234\"", t.Sent[0]);
            StringAssert.Contains("\"event\":\"command\"", t.Sent[1]);
            StringAssert.Contains("\"kind\":\"pause\"", t.Sent[1]);
        }

        [Test]
        public void ElEnlaceInterpretaLosMensajesDelRelay()
        {
            var t = new FakeTransport();
            var link = new SessionLink(t);
            string hosted = null, error = null;
            SessionSnapshot state = null;
            SessionCommand command = null;
            var left = 0;
            link.Hosted += c => hosted = c;
            link.ErrorReceived += m => error = m;
            link.StateReceived += s => state = s;
            link.CommandReceived += c => command = c;
            link.AdvisorLeft += () => left++;

            t.Receive("{\"event\":\"hosted\",\"data\":{\"code\":\"4321\"}}");
            t.Receive("{\"event\":\"error\",\"data\":{\"message\":\"Código en uso\"}}");
            t.Receive("{\"event\":\"state\",\"data\":{\"stage\":2,\"unitId\":\"A-301\",\"total\":520000,\"paused\":true}}");
            t.Receive("{\"event\":\"command\",\"data\":{\"kind\":\"jump\",\"stage\":1}}");
            t.Receive("{\"event\":\"advisor-left\"}");
            t.Receive("no es json");

            Assert.AreEqual("4321", hosted);
            Assert.AreEqual("Código en uso", error);
            Assert.AreEqual(2, state.stage);
            Assert.AreEqual(520000, state.total);
            Assert.IsTrue(state.paused);
            Assert.AreEqual("jump", command.kind);
            Assert.AreEqual(1, command.stage);
            Assert.AreEqual(1, left);
        }

        [TestCase("http://localhost:3000", "ws://localhost:3000/session")]
        [TestCase("http://192.168.1.5:3000/", "ws://192.168.1.5:3000/session")]
        [TestCase("https://umbral.example.com", "wss://umbral.example.com/session")]
        public void LaUrlDelRelayVieneDeLaDelServidor(string http, string expected) =>
            Assert.AreEqual(expected, SessionUrl.FromServer(http));

        // --- Anfitrión (visor) ---

        [UnityTest]
        public IEnumerator ElVisorRegistraSuCodigoYLoMantieneAlReconectar()
        {
            yield return Load("Lobby");
            yield return StartHost();
            var code = SessionHost.Code;
            StringAssert.IsMatch(@"^\d{4}$", code);
            StringAssert.Contains(code, made[0].Sent.First(s => s.Contains("\"event\":\"host\"")));
            Assert.IsTrue(SessionHost.Connected);

            Fake.Close(); // se cae la conexión: reintenta con espera y registra el mismo código
            yield return WaitFor(() => made.Count > 1 && made[1].Count("host") > 0, 5f);
            Assert.GreaterOrEqual(made.Count, 2);
            StringAssert.Contains(code, made[1].Sent.First(s => s.Contains("\"event\":\"host\"")));
        }

        [UnityTest]
        public IEnumerator SiElCodigoEstaEnUso_ElVisorGeneraOtro()
        {
            yield return Load("Lobby");
            SessionHost.Restart(() => { var t = new FakeTransport(); made.Add(t); return t; });
            yield return WaitFor(() => made.Count > 0 && made[0].Count("host") > 0);
            var first = SessionHost.Code;
            Fake.Receive("{\"event\":\"error\",\"data\":{\"message\":\"Código en uso\"}}");
            Assert.AreEqual(2, Fake.Count("host"));
            Assert.AreNotEqual(first, SessionHost.Code, "Un código nuevo (probabilidad de colisión 1/10000)");
        }

        [UnityTest]
        public IEnumerator ElVisorPublicaElEstadoSoloCuandoCambia()
        {
            yield return Load("Apartment_Greybox");
            yield return StartHost();
            var host = UnityEngine.Object.FindFirstObjectByType<ConfiguratorHost>();
            yield return WaitFor(() => host.Ready.IsCompleted);
            yield return WaitFor(() => Fake.Count("state") > 0, 5f);

            var s = LastState(Fake).data;
            Assert.AreEqual(SessionHost.Code, s.code);
            Assert.AreEqual("A-301", s.unitId);
            Assert.AreEqual(host.Ready.Result.Total, s.total);
            Assert.AreEqual(host.Ready.Result.Catalog.surfaces.Length, s.choices.Length);

            yield return Wait(0.8f);
            var before = Fake.Count("state");
            yield return Wait(1.0f);
            Assert.AreEqual(before, Fake.Count("state"), "Sin cambios no se reenvía");

            var surface = host.Ready.Result.Catalog.surfaces[0];
            host.Ready.Result.Select(surface.id, surface.options.Last().id);
            yield return WaitFor(() => Fake.Count("state") > before, 3f);
            Assert.AreEqual(before + 1, Fake.Count("state"));
            Assert.AreEqual(host.Ready.Result.Total, LastState(Fake).data.total);

            Fake.Receive("{\"event\":\"advisor-joined\"}"); // el asesor que llega recibe el estado completo
            yield return WaitFor(() => Fake.Count("state") > before + 1, 3f);
            Assert.AreEqual(before + 2, Fake.Count("state"));
        }

        // --- Comandos ---

        [UnityTest]
        public IEnumerator UnSaltoDelAsesorCambiaDeEscenaConFundido()
        {
            yield return Load("Lobby");
            yield return StartHost();
            Fake.Receive("{\"event\":\"command\",\"data\":{\"kind\":\"jump\",\"stage\":2}}");
            yield return WaitFor(() => SceneManager.GetActiveScene().name == "Apartment_Greybox", 15f);
            Assert.AreEqual("Apartment_Greybox", SceneManager.GetActiveScene().name);
            Assert.AreEqual(SessionStage.Llegada, Session.Flow.Stage);
        }

        [UnityTest]
        public IEnumerator EnPausa_ElVisorQuedaEnNegroSinInteractoresYElCartelSeVeSobreElNegro()
        {
            yield return Load("Apartment_Greybox");
            yield return StartHost();
            var fade = UnityEngine.Object.FindFirstObjectByType<ComfortFade>();
            var interactors = UnityEngine.Object.FindObjectsByType<XRBaseInteractor>(FindObjectsInactive.Include);
            var providers = UnityEngine.Object.FindObjectsByType<LocomotionProvider>(FindObjectsInactive.Include);
            Assert.IsNotEmpty(interactors);
            var wasEnabled = interactors.Concat<Behaviour>(providers).Where(b => b.enabled).ToArray();

            Fake.Receive("{\"event\":\"command\",\"data\":{\"kind\":\"pause\"}}");
            yield return WaitFor(() => fade.Alpha > 0.99f, 3f);
            Assert.Greater(fade.Alpha, 0.99f);
            Assert.IsTrue(wasEnabled.All(b => !b.enabled), "Interactores y locomoción apagados");
            var label = GameObject.Find("PauseLabel");
            Assert.IsNotNull(label);
            Assert.Greater(label.GetComponent<MeshRenderer>().material.renderQueue, 4000, "El cartel se dibuja después del fundido");

            // Pausa: los botones no hacen nada, ni siquiera por código, y el asesor no puede saltar sin reanudar.
            var solar = UnityEngine.Object.FindFirstObjectByType<SolarClock>();
            var stop = solar.Current;
            foreach (var b in UnityEngine.Object.FindObjectsByType<WristButton>(FindObjectsInactive.Include)) b.Press();
            yield return Wait(0.5f);
            Assert.AreEqual(stop, solar.Current);
            Fake.Receive("{\"event\":\"command\",\"data\":{\"kind\":\"jump\",\"stage\":0}}");
            yield return Wait(0.5f);
            Assert.AreEqual("Apartment_Greybox", SceneManager.GetActiveScene().name);

            Fake.Receive("{\"event\":\"command\",\"data\":{\"kind\":\"resume\"}}");
            yield return WaitFor(() => fade.Alpha < 0.001f, 3f);
            Assert.Less(fade.Alpha, 0.001f);
            Assert.IsTrue(wasEnabled.All(b => b.enabled), "Se restituyen exactamente los que estaban activos");
            Assert.IsNull(GameObject.Find("PauseLabel"));
        }

        [UnityTest]
        public IEnumerator SiSeVaElAsesorOSeCaeElCanal_LaPausaSeLevantaSola()
        {
            yield return Load("Apartment_Greybox");
            yield return StartHost();
            var fade = UnityEngine.Object.FindFirstObjectByType<ComfortFade>();

            Session.Control.SetPaused(true);
            yield return WaitFor(() => fade.Alpha > 0.99f, 3f);
            Fake.Receive("{\"event\":\"advisor-left\"}");
            Assert.IsFalse(Session.Control.Paused);
            yield return WaitFor(() => fade.Alpha < 0.001f, 3f);
            Assert.Less(fade.Alpha, 0.001f);

            Session.Control.SetPaused(true);
            yield return WaitFor(() => fade.Alpha > 0.99f, 3f);
            Fake.Close();
            yield return null;
            Assert.IsFalse(Session.Control.Paused);
            yield return WaitFor(() => fade.Alpha < 0.001f, 3f);
            Assert.Less(fade.Alpha, 0.001f);
        }

        [UnityTest]
        public IEnumerator LaPausaEsperaAQueTermineUnFundidoEnCurso()
        {
            yield return Load("Apartment_Greybox");
            yield return StartHost();
            var fade = UnityEngine.Object.FindFirstObjectByType<ComfortFade>();
            fade.FadeThrough(() => { }); // fundido del reloj solar en curso (0,15 s + 0,20 s)
            Session.Control.SetPaused(true);
            yield return null;
            Assert.IsNull(GameObject.Find("PauseLabel"), "Aún hay un fundido: la pausa espera");
            yield return WaitFor(() => GameObject.Find("PauseLabel") != null, 3f);
            Assert.IsNotNull(GameObject.Find("PauseLabel"));
        }

        [UnityTest]
        public IEnumerator ElCodigoDeSalaSeVeEnElLobbyYEnLaMuneca()
        {
            yield return Load("Lobby");
            var label = UnityEngine.Object.FindFirstObjectByType<RoomCodeLabel>();
            Assert.IsNotNull(label);
            StringAssert.Contains(SessionHost.Code, label.GetComponent<TextMesh>().text);

            yield return Load("Apartment_Greybox");
            var wrist = UnityEngine.Object.FindFirstObjectByType<WristPrice>(FindObjectsInactive.Include);
            for (var tr = wrist.transform; tr != null; tr = tr.parent) tr.gameObject.SetActive(true); // los controles del rig están apagados sin dispositivos
            var text = wrist.GetComponentInChildren<Text>();
            yield return WaitFor(() => text.text.Contains("Sala"), 5f);
            StringAssert.Contains("Sala " + SessionHost.Code, text.text);
        }

#if UNITY_EDITOR
        // --- Pantalla del asesor ---

        private static IEnumerator LoadAdvisor()
        {
            var op = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Scenes/Asesor.unity", new LoadSceneParameters(LoadSceneMode.Single));
            while (!op.isDone) yield return null;
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator LaPantallaDelAsesorRefleja_YManda_ComandosPorElRelay()
        {
            yield return LoadAdvisor();
            var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            Assert.AreEqual(RenderMode.ScreenSpaceCamera, canvas.renderMode);
            Assert.IsNotNull(UnityEngine.Object.FindFirstObjectByType<UnityEngine.InputSystem.UI.InputSystemUIInputModule>(), "El Input System nuevo necesita su módulo de UI");
            Assert.IsNull(UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.StandaloneInputModule>());

            var view = UnityEngine.Object.FindFirstObjectByType<AdvisorView>();
            var fake = new FakeTransport();
            var link = new SessionLink(fake);
            view.Bind(link);
            fake.Receive("{\"event\":\"joined\",\"data\":{\"code\":\"1234\"}}");
            Assert.IsTrue(view.Joined);
            StringAssert.Contains("1234", view.StatusText);

            var catalog = CatalogQueries.PickFresher(null, JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("catalog_seed").text));
            var cfg = new Configurator(catalog, "A-301");
            cfg.Select(catalog.surfaces[0].id, catalog.surfaces[0].options.Last().id);
            var snapshot = SessionSnapshot.Build("1234", new SessionFlow(), cfg, 1, false);
            fake.Receive(JsonUtility.ToJson(new StateEnvelope { @event = "state", data = snapshot }));
            StringAssert.Contains("Unidad: A-301", view.StateText);
            StringAssert.Contains(PriceFormat.Bolivianos(cfg.Total), view.StateText);

            ClickButton("Pausar");
            StringAssert.Contains("\"kind\":\"pause\"", fake.Sent.Last());
            ClickButton("Maqueta");
            StringAssert.Contains("\"kind\":\"jump\"", fake.Sent.Last());
            StringAssert.Contains("\"stage\":1", fake.Sent.Last());
            ClickButton("Reanudar"); // sin "paused" en el último estado, Reanudar queda inactivo: no se envía
            StringAssert.Contains("\"stage\":1", fake.Sent.Last());

            fake.Receive("{\"event\":\"host-left\"}");
            Assert.IsFalse(view.Joined);
            ClickButton("Lobby");
            StringAssert.Contains("\"stage\":1", fake.Sent.Last(), "Sin sala no se envía nada");
        }

        private static void ClickButton(string name)
        {
            var b = UnityEngine.Object.FindObjectsByType<Button>().First(x => x.name == name);
            if (b.interactable) b.onClick.Invoke();
        }
#endif

        // --- Interoperabilidad con el servidor real (solo si UMBRAL_SERVER está definido) ---

        [UnityTest]
        [Category("Interop")]
        public IEnumerator ClienteCSharpYServidorReal_RetransmitenEstadoYComandos()
        {
            var server = Environment.GetEnvironmentVariable("UMBRAL_SERVER");
            if (string.IsNullOrEmpty(server)) Assert.Ignore("Definí UMBRAL_SERVER (p. ej. http://localhost:3000) con el servidor corriendo");

            var code = new System.Random().Next(1000, 9999).ToString();
            var host = new SessionLink(new WebSocketTransport());
            var advisor = new SessionLink(new WebSocketTransport());
            SessionSnapshot gotState = null;
            SessionCommand gotCommand = null;
            string hosted = null, joined = null, error = null;
            host.Hosted += c => hosted = c;
            host.CommandReceived += c => gotCommand = c;
            advisor.Joined += c => joined = c;
            advisor.StateReceived += s => gotState = s;
            advisor.ErrorReceived += m => error = m;

            var url = SessionUrl.FromServer(server);
            var t1 = host.ConnectAsync(url);
            var t2 = advisor.ConnectAsync(url);
            yield return WaitFor(() => t1.IsCompleted && t2.IsCompleted);
            Assert.IsTrue(t1.IsCompletedSuccessfully && t2.IsCompletedSuccessfully, "No se pudo conectar a " + url);

            host.Host(code);
            yield return WaitFor(() => hosted != null);
            Assert.AreEqual(code, hosted);
            host.PublishState(new SessionSnapshot { code = code, stage = 2, unitId = "A-301", total = 520000 });
            advisor.Join(code);
            yield return WaitFor(() => joined != null && gotState != null);
            Assert.AreEqual(code, joined);
            Assert.AreEqual(520000, gotState.total, "El que llega tarde recibe el último estado");

            advisor.SendCommand(SessionCommand.Pause());
            yield return WaitFor(() => gotCommand != null);
            Assert.AreEqual("pause", gotCommand.kind);

            advisor.PublishState(new SessionSnapshot()); // rol equivocado
            yield return WaitFor(() => error != null);
            StringAssert.Contains("visor", error);

            host.Close();
            advisor.Close();
        }
    }
}

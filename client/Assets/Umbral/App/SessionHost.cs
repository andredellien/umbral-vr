using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Umbral.Core;
using Umbral.Net;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

namespace Umbral.App
{
    /// <summary>
    /// El visor como anfitrión de la sala: publica el estado de la sesión al relay y ejecuta las órdenes del asesor (pausa, salto de etapa).
    /// Vive entre escenas (cada escena trae su propio rig). Sin servidor, el comprador usa la app igual: solo se reintenta con espera creciente.
    /// </summary>
    public class SessionHost : MonoBehaviour
    {
        public static string ServerUrl = "http://localhost:3000"; // el mismo servidor del catálogo
        public static SessionHost Instance { get; private set; }

        /// <summary>Cómo se crea el canal con el relay. Las pruebas lo cambian por uno falso.</summary>
        public static Func<ISessionTransport> TransportFactory = () => new WebSocketTransport();

        /// <summary>Código de sala de 4 dígitos: se genera una vez y se mantiene aunque la conexión se caiga.</summary>
        public static string Code { get; private set; } = NewCode();

        public static bool Connected => Instance != null && Instance.connected;

        private SessionLink link;
        private bool connected;
        private string lastJson;
        private float publishTimer;
        private bool warned;
        private bool destroyed;

        // Pausa: negro + interactores y locomoción apagados. Se espera a que termine un fundido en curso.
        private bool pauseApplied;
        private readonly List<Behaviour> disabledByPause = new List<Behaviour>();
        private GameObject pauseLabel;
        private float sweepTimer;

        public static void Ensure()
        {
            if (Instance != null) return;
            var go = new GameObject("SessionHost");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<SessionHost>();
        }

        /// <summary>Reinicia el anfitrión (nueva conexión y nuevo código). Para pruebas.</summary>
        public static void Restart(Func<ISessionTransport> factory = null)
        {
            if (Instance != null) DestroyImmediate(Instance.gameObject);
            TransportFactory = factory ?? (() => new WebSocketTransport());
            Code = NewCode();
            Ensure();
        }

        private static string NewCode() => new System.Random().Next(0, 10000).ToString("D4");

        private void Start() => _ = Connect();

        private void OnDestroy()
        {
            destroyed = true; // el cierre del canal de un anfitrión destruido no debe tocar la sesión
            if (Instance == this) Instance = null;
            link?.Close();
        }

        private async Task Connect()
        {
            var delay = 1f;
            while (this != null)
            {
                var closed = false;
                try
                {
                    var l = new SessionLink(TransportFactory());
                    l.Hosted += _ => { connected = true; warned = false; lastJson = null; };
                    l.ErrorReceived += OnError;
                    l.CommandReceived += OnCommand;
                    l.AdvisorJoined += () => lastJson = null;     // al asesor que llega se le manda el estado completo
                    l.AdvisorLeft += ResumeIfPaused;              // sin asesor nadie puede reanudar: no dejar al comprador en negro
                    l.Closed += () => { closed = true; ResumeIfPaused(); }; // sin canal no hay quien reanude
                    link = l;
                    await l.ConnectAsync(SessionUrl.FromServer(ServerUrl));
                    delay = 1f;
                    l.Host(Code);
                    while (this != null && !closed) await Awaitable.WaitForSecondsAsync(0.25f);
                }
                catch (Exception e)
                {
                    if (!warned) Debug.LogWarning($"[Session] Sin relay ({e.GetType().Name}): la sesión sigue sin tablet."); // aviso, no error: sin servidor es un caso normal
                    warned = true;
                }
                if (this == null) return; // destruido (p. ej. Restart): no tocar el estado de la sesión que ya es de otro anfitrión
                connected = false;
                link = null; // si se perdió el canal, Closed ya levantó la pausa
                await Awaitable.WaitForSecondsAsync(delay);
                delay = Mathf.Min(delay * 2f, 15f);
            }
        }

        private void OnError(string message)
        {
            if (connected || link == null) return;
            Code = NewCode(); // código en uso: otro visor lo tiene. Se prueba con uno nuevo.
            link.Host(Code);
        }

        private void OnCommand(SessionCommand command) =>
            Session.Control.Apply(command, stage => SceneFlow.GoTo(stage));

        private void ResumeIfPaused()
        {
            if (!destroyed) Session.Control.SetPaused(false);
        }

        private void Update()
        {
            UpdatePause();
            publishTimer -= Time.unscaledDeltaTime;
            if (publishTimer > 0f) return;
            publishTimer = 0.25f;
            Publish();
        }

        private void Publish()
        {
            if (!connected || link == null) return;
            var snapshot = BuildSnapshot();
            var json = JsonUtility.ToJson(snapshot);
            if (json == lastJson) return; // solo cuando algo cambió
            lastJson = json;
            link.PublishState(snapshot);
        }

        public static SessionSnapshot BuildSnapshot()
        {
            var host = FindFirstObjectByType<ConfiguratorHost>();
            var configurator = host != null && host.Ready != null && host.Ready.IsCompletedSuccessfully ? host.Ready.Result : null;
            var solar = FindFirstObjectByType<SolarClock>();
            return SessionSnapshot.Build(Code, Session.Flow, configurator, solar != null ? solar.Current : -1, Session.Control.Paused);
        }

        // --- Pausa ---

        private void UpdatePause()
        {
            var want = Session.Control.Paused;
            if (want && !pauseApplied)
            {
                var fade = FindFirstObjectByType<ComfortFade>();
                if (fade == null || fade.Busy) return; // esperar a que termine el fundido en curso (teletransporte, reloj, escena)
                EnterPause(fade);
            }
            else if (!want && pauseApplied)
            {
                ExitPause();
            }
            else if (pauseApplied && (sweepTimer -= Time.unscaledDeltaTime) <= 0f)
            {
                sweepTimer = 0.5f;
                DisableInput(); // un control que recupera el tracking durante la pausa no debe poder seleccionar nada
            }
        }

        private void EnterPause(ComfortFade fade)
        {
            pauseApplied = true;
            fade.FadeTo(1f, 0.3f);
            DisableInput();
            ShowLabel();
        }

        private void ExitPause()
        {
            pauseApplied = false;
            foreach (var b in disabledByPause) if (b != null) b.enabled = true;
            disabledByPause.Clear();
            if (pauseLabel != null) Destroy(pauseLabel);
            var fade = FindFirstObjectByType<ComfortFade>();
            if (fade != null) fade.FadeTo(0f, ComfortSettings.FadeInSeconds);
        }

        private void DisableInput()
        {
            foreach (var i in FindObjectsByType<XRBaseInteractor>(FindObjectsInactive.Include)) Disable(i);
            foreach (var l in FindObjectsByType<LocomotionProvider>(FindObjectsInactive.Include)) Disable(l);
            foreach (var t in FindObjectsByType<TapeMeasure>(FindObjectsInactive.Include)) Disable(t);
        }

        private void Disable(Behaviour b)
        {
            if (!b.enabled) return;
            b.enabled = false;
            disabledByPause.Add(b);
        }

        // El cartel se coloca una vez delante de la cabeza al empezar la pausa y no la sigue (la interfaz no va fija frente a los ojos).
        // Cola de render por encima del fundido (4000) para que no quede tapado por el negro.
        private void ShowLabel()
        {
            var cam = Camera.main;
            var origin = cam != null ? cam.transform : null;
            var forward = origin != null ? Vector3.ProjectOnPlane(origin.forward, Vector3.up).normalized : Vector3.forward;
            if (forward == Vector3.zero) forward = Vector3.forward;
            var pos = origin != null ? origin.position + forward * 2f : new Vector3(0f, 1.5f, 2f);

            pauseLabel = new GameObject("PauseLabel", typeof(TextMesh), typeof(MeshRenderer));
            pauseLabel.transform.position = pos;
            pauseLabel.transform.rotation = Quaternion.LookRotation(forward);
            var text = pauseLabel.GetComponent<TextMesh>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.font = font;
            text.text = "Sesion en pausa\nTu asesor continua en un momento";
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = 0.02f;
            text.fontSize = 64;
            text.color = Color.white;
            var renderer = pauseLabel.GetComponent<MeshRenderer>();
            renderer.material = new Material(font.material) { renderQueue = 4500 };
        }
    }
}

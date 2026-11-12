using System;
using Umbral.Core;
using Umbral.Net;
using UnityEngine;
using UnityEngine.UI;

namespace Umbral.App
{
    /// <summary>
    /// Pantalla del asesor (tablet o editor): se une a la sala con el código del visor, refleja el estado de la sesión
    /// y manda pausa, reanudar y salto de etapa. Es estado, no video (GDD 7.3 recortado al slice).
    /// </summary>
    public class AdvisorView : MonoBehaviour
    {
        [SerializeField] private InputField codeInput;
        [SerializeField] private Button connectButton;
        [SerializeField] private Text statusText;
        [SerializeField] private Text stateText;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button[] stageButtons; // Lobby, Maqueta, Llegada, en el orden de SessionStage

        private SessionLink link;
        private Catalog catalog;
        private bool joined;

        public bool Joined => joined;
        public string StatusText => statusText != null ? statusText.text : "";
        public string StateText => stateText != null ? stateText.text : "";

        public void Setup(InputField input, Button connect, Text status, Text state, Button pause, Button resume, Button[] stages)
        {
            codeInput = input;
            connectButton = connect;
            statusText = status;
            stateText = state;
            pauseButton = pause;
            resumeButton = resume;
            stageButtons = stages;
        }

        private async void Start()
        {
            SetControlsEnabled(false);
            Show("Escribe el codigo que muestra el visor");
            connectButton.onClick.AddListener(Connect);
            pauseButton.onClick.AddListener(() => Send(SessionCommand.Pause()));
            resumeButton.onClick.AddListener(() => Send(SessionCommand.Resume()));
            for (var i = 0; i < stageButtons.Length; i++)
            {
                var stage = (SessionStage)i;
                stageButtons[i].onClick.AddListener(() => Send(SessionCommand.Jump(stage)));
            }
            try { catalog = await CatalogClient.LoadAsync(SessionHost.ServerUrl); }
            catch (Exception) { /* sin catálogo se muestran los ids */ }
        }

        /// <summary>Conecta al relay real y se une a la sala del campo de código.</summary>
        public async void Connect()
        {
            var code = codeInput.text.Trim();
            Show("Conectando...");
            try
            {
                var l = new SessionLink(new WebSocketTransport());
                Bind(l);
                await l.ConnectAsync(SessionUrl.FromServer(SessionHost.ServerUrl));
                l.Join(code);
            }
            catch (Exception)
            {
                Show("Sin servidor");
            }
        }

        /// <summary>Engancha la vista a un enlace ya creado (el transporte lo pone quien llama; las pruebas usan uno falso).</summary>
        public void Bind(SessionLink value)
        {
            link = value;
            joined = false;
            link.Joined += code => { joined = true; SetControlsEnabled(true); Show("Conectado a la sala " + code); };
            link.StateReceived += OnState;
            link.ErrorReceived += message => Show(message);
            link.HostLeft += () => { joined = false; SetControlsEnabled(false); Show("El visor se desconecto"); };
            link.Closed += () => { joined = false; SetControlsEnabled(false); Show("Conexion perdida"); };
        }

        private void OnState(SessionSnapshot snapshot)
        {
            if (snapshot == null) return;
            stateText.text = snapshot.Describe(catalog);
            pauseButton.interactable = joined && !snapshot.paused;
            resumeButton.interactable = joined && snapshot.paused;
        }

        private void Send(SessionCommand command)
        {
            if (joined) link.SendCommand(command);
        }

        private void Show(string status)
        {
            if (statusText != null) statusText.text = status;
        }

        private void SetControlsEnabled(bool value)
        {
            if (pauseButton == null) return;
            pauseButton.interactable = value;
            resumeButton.interactable = value;
            foreach (var b in stageButtons) b.interactable = value;
        }
    }
}

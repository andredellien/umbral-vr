using Umbral.Core;
using Umbral.Net;
using UnityEngine;

namespace Umbral.App
{
    /// <summary>
    /// Vive entre escenas: registra inicio y fin de sesión y manda la cola al servidor cada pocos segundos, con espera
    /// creciente si falla. Sin servidor la sesión sigue igual: los eventos quedan en disco para la próxima vez.
    /// </summary>
    public class TelemetryHost : MonoBehaviour
    {
        public static TelemetryHost Instance { get; private set; }

        private const float CheckSeconds = 3f;
        private TelemetryUploader uploader;
        private TelemetryQueue uploaderQueue;
        private float timer;
        private bool endLogged;

        public TelemetryUploader Uploader => uploader;

        public static void Ensure()
        {
            if (Instance != null) return;
            var go = new GameObject("TelemetryHost");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<TelemetryHost>();
        }

        private void Awake()
        {
            EnsureUploader();
            Telemetry.Log(TelemetryTypes.SessionStart);
            Telemetry.Log(TelemetryTypes.StageEntered); // la etapa con la que arranca (Lobby, o la escena suelta que se abrió)
        }

        // Si la cola cambia (pruebas), el envío sigue a la cola vigente.
        private void EnsureUploader()
        {
            var queue = Telemetry.EnsureQueue();
            if (uploader != null && uploaderQueue == queue) return;
            uploaderQueue = queue;
            uploader = new TelemetryUploader(queue, json => TelemetryUploader.HttpPost(SessionHost.ServerUrl, json));
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < CheckSeconds) return;
            timer = 0f;
            EnsureUploader();
            if (uploader.Due(Time.realtimeSinceStartup)) _ = uploader.FlushAsync(Time.realtimeSinceStartup);
        }

        // Quest: pausa = el visor se quitó o se fue al menú. Cuenta como fin de la sesión; el envío ocurre en el próximo arranque si no alcanza ahora.
        private void OnApplicationPause(bool paused)
        {
            if (paused) EndSession();
            else endLogged = false;
        }

        private void OnApplicationQuit() => EndSession();

        private void EndSession()
        {
            if (endLogged) return;
            endLogged = true;
            Telemetry.Log(TelemetryTypes.SessionEnd);
            EnsureUploader();
            _ = uploader.FlushAsync(Time.realtimeSinceStartup);
        }
    }
}

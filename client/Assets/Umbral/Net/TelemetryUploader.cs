using System;
using System.Linq;
using Umbral.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace Umbral.Net
{
    /// <summary>Envía la cola al servidor por lotes. Si falla, espera cada vez más (Backoff); el evento queda en el archivo.</summary>
    public class TelemetryUploader
    {
        public const int BatchSize = 100;

        private readonly TelemetryQueue queue;
        private readonly Func<string, Awaitable<bool>> send; // recibe el JSON del lote; true = el servidor lo aceptó
        private bool flushing;
        private float nextAttempt;

        public int Failures { get; private set; }

        public TelemetryUploader(TelemetryQueue queue, Func<string, Awaitable<bool>> send)
        {
            this.queue = queue;
            this.send = send;
        }

        public bool Due(float now) => !flushing && now >= nextAttempt;

        /// <summary>Manda un lote. Devuelve true si no queda nada pendiente o el lote se confirmó.</summary>
        public async Awaitable<bool> FlushAsync(float now)
        {
            if (flushing) return false;
            flushing = true;
            try
            {
                var batch = queue.ReadBatch(BatchSize);
                if (batch.Count == 0) { Failures = 0; return true; }
                var json = JsonUtility.ToJson(new TelemetryBatch { events = batch.ToArray() });
                var ok = false;
                try { ok = await send(json); }
                catch (Exception e) { Debug.LogWarning($"[Telemetry] Envío fallido: {e.Message}"); }
                if (ok)
                {
                    queue.Remove(batch.Select(e => e.id));
                    Failures = 0;
                    nextAttempt = now;
                    return true;
                }
                Failures++;
                nextAttempt = now + Backoff.DelaySeconds(Failures);
                return false;
            }
            finally
            {
                flushing = false;
            }
        }

        public static async Awaitable<bool> HttpPost(string baseUrl, string json)
        {
            using var request = new UnityWebRequest(baseUrl.TrimEnd('/') + "/events", "POST")
            {
                uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = 5,
            };
            request.SetRequestHeader("Content-Type", "application/json");
            await request.SendWebRequest();
            return request.result == UnityWebRequest.Result.Success;
        }
    }
}

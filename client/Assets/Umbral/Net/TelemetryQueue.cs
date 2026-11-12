using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Umbral.Core;
using UnityEngine;

namespace Umbral.Net
{
    /// <summary>
    /// Cola local de eventos: un archivo JSONL (una línea por evento). Sobrevive a cierres de la app y a la falta de red;
    /// los eventos solo se quitan cuando el servidor confirmó el lote. Se usa solo desde el hilo principal.
    /// </summary>
    public class TelemetryQueue
    {
        private readonly string path;

        public TelemetryQueue(string path) => this.path = path;

        public void Append(TelemetryEvent e)
        {
            try { File.AppendAllText(path, JsonUtility.ToJson(e) + "\n"); }
            catch (Exception ex) { Debug.LogWarning($"[Telemetry] No se pudo guardar el evento: {ex.Message}"); } // la telemetría nunca rompe la sesión
        }

        /// <summary>Hasta <paramref name="max"/> eventos, los más viejos primero. Las líneas rotas se saltan.</summary>
        public List<TelemetryEvent> ReadBatch(int max)
        {
            var list = new List<TelemetryEvent>();
            try
            {
                if (!File.Exists(path)) return list;
                foreach (var line in File.ReadLines(path))
                {
                    if (list.Count >= max) break;
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    try
                    {
                        var e = JsonUtility.FromJson<TelemetryEvent>(line);
                        if (e != null && !string.IsNullOrEmpty(e.id) && !string.IsNullOrEmpty(e.type)) list.Add(e);
                    }
                    catch (ArgumentException) { /* línea a medio escribir */ }
                }
            }
            catch (Exception ex) { Debug.LogWarning($"[Telemetry] No se pudo leer la cola: {ex.Message}"); }
            return list;
        }

        public int Count => File.Exists(path) ? File.ReadLines(path).Count(l => !string.IsNullOrWhiteSpace(l)) : 0;

        /// <summary>Quita los eventos con esos ids reescribiendo el archivo de forma atómica.</summary>
        public void Remove(IEnumerable<string> ids)
        {
            try
            {
                if (!File.Exists(path)) return;
                var gone = new HashSet<string>(ids);
                var keep = new List<string>();
                foreach (var line in File.ReadLines(path))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    try
                    {
                        var e = JsonUtility.FromJson<TelemetryEvent>(line);
                        if (e != null && gone.Contains(e.id)) continue;
                    }
                    catch (ArgumentException) { continue; } // una línea rota no se conserva
                    keep.Add(line);
                }
                var tmp = path + ".tmp";
                File.WriteAllLines(tmp, keep);
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
            }
            catch (Exception ex) { Debug.LogWarning($"[Telemetry] No se pudo limpiar la cola: {ex.Message}"); }
        }
    }
}

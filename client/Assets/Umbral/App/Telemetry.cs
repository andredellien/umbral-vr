using System;
using System.IO;
using Umbral.Core;
using Umbral.Net;
using UnityEngine;

namespace Umbral.App
{
    /// <summary>Punto único para registrar eventos. Escribe en la cola local; el envío es aparte y nunca bloquea la sesión.</summary>
    public static class Telemetry
    {
        public static string SessionId { get; private set; } = Guid.NewGuid().ToString();
        public static TelemetryQueue Queue { get; private set; }

        public static TelemetryQueue EnsureQueue()
        {
            return Queue ??= new TelemetryQueue(Path.Combine(Application.persistentDataPath, "telemetry.jsonl"));
        }

        /// <summary>Cola y sesión nuevas. Para pruebas.</summary>
        public static void Configure(TelemetryQueue queue)
        {
            Queue = queue;
            SessionId = Guid.NewGuid().ToString();
        }

        public static TelemetryEvent Log(string type, Action<TelemetryEvent> fill = null)
        {
            var e = new TelemetryEvent
            {
                id = Guid.NewGuid().ToString(),
                type = type,
                sessionId = SessionId,
                at = DateTime.UtcNow.ToString("o"),
                stage = Session.Flow.Stage.ToString(),
                unitId = Session.Flow.SelectedUnitId,
            };
            fill?.Invoke(e);
            EnsureQueue().Append(e);
            return e;
        }

        public static void Tool(string tool, string detail = null) =>
            Log(TelemetryTypes.ToolUsed, e => { e.tool = tool; e.detail = detail; });
    }
}

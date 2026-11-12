using System;

namespace Umbral.Core
{
    /// <summary>Tipos de evento de telemetría (GDD sección 15 mide, sobre todo, las sesiones que llegan a la escena final).</summary>
    public static class TelemetryTypes
    {
        public const string SessionStart = "session_start";
        public const string SessionEnd = "session_end";
        public const string StageEntered = "stage_entered";
        public const string FinishChosen = "finish_chosen";
        public const string ToolUsed = "tool_used";
        public const string ReservationMade = "reservation_made";
    }

    /// <summary>
    /// Un evento. Un solo tipo plano (JsonUtility no hace polimorfismo); el servidor lo guarda tal cual.
    /// El id (GUID) es la clave con la que el servidor descarta un reintento.
    /// </summary>
    [Serializable]
    public class TelemetryEvent
    {
        public string id;
        public string type;
        public string sessionId;
        public string at;      // ISO 8601 UTC
        public string stage;   // stage_entered, session_*
        public string unitId;
        public string surface; // finish_chosen
        public string option;
        public string tool;    // tool_used: "solar" o "tape"
        public string detail;  // p. ej. la parada del sol o los cm medidos
    }

    [Serializable]
    public class TelemetryBatch
    {
        public TelemetryEvent[] events;
    }

    /// <summary>Espera creciente entre reintentos de envío: 2, 4, 8... hasta 60 s.</summary>
    public static class Backoff
    {
        public static float DelaySeconds(int failures)
        {
            if (failures <= 0) return 0f;
            var seconds = 2f * (float)Math.Pow(2, Math.Min(failures - 1, 10));
            return Math.Min(seconds, 60f);
        }
    }
}

using System;

namespace Umbral.Core
{
    /// <summary>Etapas de la sesión que cubre el slice hasta la semana 5. Reveal y llave llegan después (semana 8).</summary>
    public enum SessionStage { Lobby, Maqueta, Llegada }

    /// <summary>
    /// Estado de la sesión: en qué etapa va, qué unidad eligió el comprador y si está sentado.
    /// C# puro. La etapa se puede saltar por índice (la tablet del asesor lo usará en la semana 6).
    /// </summary>
    public class SessionFlow
    {
        public SessionStage Stage { get; private set; } = SessionStage.Lobby;
        public string SelectedUnitId { get; private set; }
        public bool Seated { get; set; }

        public event Action<SessionStage> StageChanged;

        public static int StageCount => Enum.GetValues(typeof(SessionStage)).Length;

        /// <summary>Avanza a la etapa siguiente; devuelve false si ya es la última.</summary>
        public bool Next()
        {
            if ((int)Stage + 1 >= StageCount) return false;
            JumpTo((SessionStage)((int)Stage + 1));
            return true;
        }

        public void JumpTo(int index)
        {
            if (index < 0 || index >= StageCount) throw new ArgumentOutOfRangeException(nameof(index));
            JumpTo((SessionStage)index);
        }

        public void JumpTo(SessionStage stage)
        {
            if (stage == Stage) return;
            Stage = stage;
            StageChanged?.Invoke(stage);
        }

        /// <summary>Elige la unidad si existe y está disponible. Vendida o reservada: false y el estado no cambia.</summary>
        public bool SelectUnit(Catalog catalog, string unitId)
        {
            UnitInfo unit;
            try { unit = CatalogQueries.FindUnit(catalog, unitId); }
            catch (ArgumentException) { return false; }
            if (CatalogQueries.StatusOf(unit) != UnitStatus.Available) return false;
            SelectedUnitId = unitId;
            return true;
        }
    }
}

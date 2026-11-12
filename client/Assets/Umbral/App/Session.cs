using Umbral.Core;

namespace Umbral.App
{
    /// <summary>Estado de la sesión que sobrevive al cambio de escena (cada escena trae su propio rig).</summary>
    public static class Session
    {
        public static SessionFlow Flow { get; private set; } = new SessionFlow();

        /// <summary>Pausa y órdenes del asesor.</summary>
        public static SessionControl Control { get; private set; } = new SessionControl();

        /// <summary>La escena nueva se abre por el flujo: debe empezar en negro y aclarar, sin destello.</summary>
        public static bool EnteredByFlow { get; set; }

        public static void Reset()
        {
            Flow = new SessionFlow();
            Control = new SessionControl();
            EnteredByFlow = false;
            SceneFlow.Busy = false;
        }

        public static bool ConsumeEntryFade()
        {
            var v = EnteredByFlow;
            EnteredByFlow = false;
            return v;
        }

        /// <summary>La unidad elegida en la maqueta si existe en el catálogo; si no, la de respaldo (Boot y pruebas abren una escena suelta).</summary>
        public static string ResolveUnit(Catalog catalog, string fallback)
        {
            var id = Flow.SelectedUnitId;
            if (string.IsNullOrEmpty(id)) return fallback;
            try { CatalogQueries.FindUnit(catalog, id); return id; }
            catch (System.ArgumentException) { return fallback; }
        }
    }
}

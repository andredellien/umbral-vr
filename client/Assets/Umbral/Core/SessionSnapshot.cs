using System;
using System.Collections.Generic;

namespace Umbral.Core
{
    [Serializable]
    public class Choice
    {
        public string surface;
        public string option;
    }

    /// <summary>
    /// Lo que la tablet del asesor ve de la sesión del comprador (espejo de estado, no de video).
    /// Campos públicos y arrays para que JsonUtility lo serialice. C# puro.
    /// </summary>
    [Serializable]
    public class SessionSnapshot
    {
        public string code;
        public int stage;
        public string unitId;
        public Choice[] choices = new Choice[0];
        public long total;
        public int solarStop = -1; // -1: la escena no tiene reloj solar
        public bool seated;
        public bool paused;

        /// <summary>Arma el estado actual. Sin configurador (lobby, maqueta) no hay unidad ni acabados y el total es 0.</summary>
        public static SessionSnapshot Build(string code, SessionFlow flow, Configurator configurator, int solarStop, bool paused)
        {
            var s = new SessionSnapshot
            {
                code = code,
                stage = (int)flow.Stage,
                unitId = configurator != null ? configurator.UnitId : flow.SelectedUnitId,
                solarStop = solarStop,
                seated = flow.Seated,
                paused = paused,
            };
            if (configurator != null)
            {
                var list = new List<Choice>();
                foreach (var surface in configurator.Catalog.surfaces)
                    list.Add(new Choice { surface = surface.id, option = configurator.SelectedOptionId(surface.id) });
                s.choices = list.ToArray();
                s.total = configurator.Total;
            }
            return s;
        }

        public static string StageName(int stage)
        {
            switch ((SessionStage)stage)
            {
                case SessionStage.Lobby: return "Lobby";
                case SessionStage.Maqueta: return "Maqueta";
                case SessionStage.Llegada: return "Llegada";
                default: return "Etapa " + stage;
            }
        }

        /// <summary>Texto para el panel del asesor. Con catálogo muestra los nombres; sin él, los ids.</summary>
        public string Describe(Catalog catalog)
        {
            var lines = new List<string>
            {
                "Etapa: " + StageName(stage) + (paused ? "  (EN PAUSA)" : ""),
                "Unidad: " + (string.IsNullOrEmpty(unitId) ? "sin elegir" : unitId),
                "Postura: " + (seated ? "sentado" : "de pie"),
            };
            foreach (var c in choices ?? new Choice[0])
                lines.Add(NameOf(catalog, c));
            if (!string.IsNullOrEmpty(unitId)) lines.Add("Total: " + PriceFormat.Bolivianos(total));
            lines.Add("Sol: " + (solarStop >= 0 && solarStop < SolarStops.Hours.Length ? SolarStops.Label(solarStop) : "-"));
            return string.Join("\n", lines);
        }

        private static string NameOf(Catalog catalog, Choice c)
        {
            try
            {
                var surface = CatalogQueries.FindSurface(catalog, c.surface);
                var option = CatalogQueries.FindOption(catalog, c.surface, c.option);
                return surface.name + ": " + option.name;
            }
            catch (Exception) // sin catálogo o id desconocido: mostrar los ids tal cual
            {
                return c.surface + ": " + c.option;
            }
        }
    }
}

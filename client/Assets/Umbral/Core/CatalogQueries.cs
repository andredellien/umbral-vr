using System;
using System.Collections.Generic;

namespace Umbral.Core
{
    /// <summary>Búsquedas sobre el catálogo. C# puro, se prueba sin abrir el editor.</summary>
    public static class CatalogQueries
    {
        public static UnitInfo FindUnit(Catalog catalog, string unitId)
        {
            foreach (var unit in catalog.units)
                if (unit.id == unitId) return unit;
            throw new ArgumentException($"Unidad desconocida '{unitId}'");
        }

        public static SurfaceInfo FindSurface(Catalog catalog, string surfaceId)
        {
            foreach (var surface in catalog.surfaces)
                if (surface.id == surfaceId) return surface;
            throw new ArgumentException($"Superficie desconocida '{surfaceId}'");
        }

        public static OptionInfo FindOption(Catalog catalog, string surfaceId, string optionId)
        {
            var surface = FindSurface(catalog, surfaceId);
            foreach (var option in surface.options)
                if (option.id == optionId) return option;
            throw new ArgumentException($"Opción desconocida '{optionId}' en la superficie '{surfaceId}'");
        }

        /// <summary>surfaceId -> optionId con la primera opción de cada superficie.</summary>
        public static Dictionary<string, string> DefaultSelection(Catalog catalog)
        {
            var selection = new Dictionary<string, string>();
            foreach (var surface in catalog.surfaces)
                if (surface.options != null && surface.options.Length > 0)
                    selection[surface.id] = surface.options[0].id;
            return selection;
        }
    }
}

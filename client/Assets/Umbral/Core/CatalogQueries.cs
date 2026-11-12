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

        /// <summary>Estado de venta de la unidad. Vacío, nulo o desconocido cuenta como disponible (catálogos viejos no traen el campo).</summary>
        public static UnitStatus StatusOf(UnitInfo unit)
        {
            switch (unit.status)
            {
                case "sold": return UnitStatus.Sold;
                case "reserved": return UnitStatus.Reserved;
                default: return UnitStatus.Available;
            }
        }

        /// <summary>Entre la caché en disco y el seed del build gana el de versión mayor; a igual versión, la caché (puede venir del servidor).</summary>
        public static Catalog PickFresher(Catalog cached, Catalog seed)
        {
            if (cached == null) return seed;
            if (seed == null) return cached;
            return seed.version > cached.version ? seed : cached;
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

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Umbral.Core
{
    /// <summary>
    /// Estado de la configuración de una unidad: qué opción hay elegida en cada superficie y cuánto cuesta.
    /// Fuente única para el precio de la muñeca y los acabados visibles. C# puro.
    /// </summary>
    public class Configurator
    {
        private readonly Dictionary<string, string> selection;

        public Catalog Catalog { get; }
        public string UnitId { get; }

        /// <summary>Se dispara solo cuando la elección cambia de verdad.</summary>
        public event Action Changed;

        public Configurator(Catalog catalog, string unitId)
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            CatalogQueries.FindUnit(catalog, unitId); // valida la unidad
            UnitId = unitId;
            selection = CatalogQueries.DefaultSelection(catalog);
        }

        public UnitInfo Unit => CatalogQueries.FindUnit(Catalog, UnitId);

        public long Total => PriceCalculator.Total(Catalog, UnitId, selection);

        public string SelectedOptionId(string surfaceId)
        {
            CatalogQueries.FindSurface(Catalog, surfaceId);
            return selection[surfaceId];
        }

        /// <summary>Reemplaza la elección de la superficie. Si el id no existe lanza y el estado queda igual.</summary>
        public void Select(string surfaceId, string optionId)
        {
            CatalogQueries.FindOption(Catalog, surfaceId, optionId);
            if (selection.TryGetValue(surfaceId, out var current) && current == optionId) return;
            selection[surfaceId] = optionId;
            Changed?.Invoke();
        }
    }

    /// <summary>Formato de precios fijo (no depende del idioma del visor): "Bs 520,000".</summary>
    public static class PriceFormat
    {
        public static string Bolivianos(long amount) =>
            "Bs " + amount.ToString("N0", CultureInfo.InvariantCulture);

        /// <summary>Diferencia frente a la opción incluida: "Incluido" o "+Bs 4,200".</summary>
        public static string Delta(long delta) =>
            delta == 0 ? "Incluido" : "+" + Bolivianos(delta);
    }
}

using System;
using System.Collections.Generic;

namespace Umbral.Core
{
    /// <summary>
    /// Precio que ve el comprador: base de la unidad + diferencias de los acabados elegidos.
    /// Sin descuentos ni plan de pago (ADR-001 #6). C# puro para probarlo en CI sin abrir el editor.
    /// </summary>
    public static class PriceCalculator
    {
        /// <param name="selection">surfaceId -> optionId. Una superficie sin elección usa su opción incluida (delta 0).</param>
        public static long Total(Catalog catalog, string unitId, IReadOnlyDictionary<string, string> selection)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));

            long total = CatalogQueries.FindUnit(catalog, unitId).basePrice;

            if (selection != null)
            {
                foreach (var pair in selection)
                    total += CatalogQueries.FindOption(catalog, pair.Key, pair.Value).delta;
            }

            return total;
        }
    }
}

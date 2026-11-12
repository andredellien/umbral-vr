using System.Collections.Generic;
using UnityEngine;

namespace Umbral.App
{
    /// <summary>
    /// Color de cada opción del catálogo. Es solo visual y vive en el cliente: el catálogo del servidor no lleva colores.
    /// Una prueba exige que toda opción del catálogo tenga color.
    /// </summary>
    public static class FinishPalette
    {
        private static readonly Dictionary<string, Color> Colors = new Dictionary<string, Color>
        {
            { "floor-ceramic", new Color(0.78f, 0.74f, 0.68f) },
            { "floor-porcelain-xl", new Color(0.88f, 0.88f, 0.90f) },
            { "floor-wood", new Color(0.55f, 0.38f, 0.22f) },
            { "counter-laminate", new Color(0.85f, 0.85f, 0.82f) },
            { "counter-granite", new Color(0.25f, 0.25f, 0.27f) },
            { "counter-quartz", new Color(0.93f, 0.93f, 0.95f) },
            { "wall-white", new Color(0.95f, 0.95f, 0.94f) },
            { "wall-warm-grey", new Color(0.72f, 0.69f, 0.66f) },
            { "wall-accent", new Color(0.12f, 0.30f, 0.38f) },
        };

        public static bool TryGet(string optionId, out Color color) => Colors.TryGetValue(optionId, out color);

        public static Color ColorFor(string optionId) =>
            Colors.TryGetValue(optionId, out var c) ? c : Color.magenta; // magenta = opción sin color asignado
    }
}

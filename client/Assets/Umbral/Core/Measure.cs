using System;
using System.Globalization;

namespace Umbral.Core
{
    /// <summary>Cinta métrica: distancia entre dos puntos en centímetros enteros. C# puro.</summary>
    public static class Measure
    {
        /// <param name="dx">Diferencia en metros entre los dos puntos, por eje.</param>
        public static int Centimeters(float dx, float dy, float dz)
        {
            var meters = Math.Sqrt((double)dx * dx + (double)dy * dy + (double)dz * dz);
            if (double.IsNaN(meters) || double.IsInfinity(meters)) return 0;
            return (int)Math.Round(meters * 100.0, MidpointRounding.AwayFromZero);
        }

        public static string Format(int centimeters) =>
            centimeters.ToString(CultureInfo.InvariantCulture) + " cm";
    }

    /// <summary>Las cuatro paradas del reloj solar (GDD: entre las 6:00 y las 19:00). URP no mezcla lightmaps, así que son paradas fijas.</summary>
    public static class SolarStops
    {
        public static readonly int[] Hours = { 7, 12, 16, 19 }; // mañana, mediodía, tarde, noche
        public static readonly string[] Names = { "Mañana", "Mediodía", "Tarde", "Noche" };

        public static string Label(int index) => Hours[index].ToString(CultureInfo.InvariantCulture) + ":00";
    }
}

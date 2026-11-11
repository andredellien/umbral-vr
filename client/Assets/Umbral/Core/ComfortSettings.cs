using System;

namespace Umbral.Core
{
    /// <summary>Reglas de comodidad VR del GDD (sección 9). C# puro para fijarlas con tests.</summary>
    public static class ComfortSettings
    {
        public const float SnapTurnDegrees = 30f;      // giros por pasos, nunca suaves
        public const float FadeOutSeconds = 0.15f;     // fundido breve al teletransportarse
        public const float FadeInSeconds = 0.20f;
        public const float StandingEyeHeight = 1.65f;  // altura de ojos de referencia para el modo sentado
        public const float MinEyeHeight = 1.0f;
        public const float MaxEyeHeight = 2.1f;

        /// <summary>Limita la altura de ojos calibrada a un rango razonable (evita valores de un tracking roto).</summary>
        public static float ClampEyeHeight(float meters)
        {
            if (float.IsNaN(meters) || float.IsInfinity(meters)) return StandingEyeHeight;
            return Math.Min(MaxEyeHeight, Math.Max(MinEyeHeight, meters));
        }
    }
}

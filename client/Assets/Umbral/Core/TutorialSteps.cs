using System;

namespace Umbral.Core
{
    public enum TutorialStep { Aim, Teleport, Grab }

    /// <summary>Tutorial de tres pasos del lobby, en orden: apuntar, teletransportarse, agarrar. Un paso fuera de turno no cuenta.</summary>
    public class TutorialSteps
    {
        public static readonly int Count = Enum.GetValues(typeof(TutorialStep)).Length;

        private int done;

        /// <summary>Paso que toca ahora; solo válido mientras <see cref="Completed"/> sea false.</summary>
        public TutorialStep Current => (TutorialStep)done;
        public bool Completed => done >= Count;
        public int DoneCount => done;

        public event Action<TutorialStep> StepDone;
        public event Action AllDone; // se dispara una sola vez

        public bool Complete(TutorialStep step)
        {
            if (Completed || step != Current) return false;
            done++;
            StepDone?.Invoke(step);
            if (Completed) AllDone?.Invoke();
            return true;
        }
    }
}

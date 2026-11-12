using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Umbral.App
{
    /// <summary>Botón 3D de la muñeca: una parada del reloj solar o el interruptor de la cinta métrica.</summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class WristButton : MonoBehaviour
    {
        public enum Kind { SolarStop, TapeToggle }

        [SerializeField] private Kind kind;
        [SerializeField] private int stopIndex;

        private XRSimpleInteractable interactable;

        public void Setup(Kind value, int index)
        {
            kind = value;
            stopIndex = index;
        }

        private void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();
            interactable.selectEntered.AddListener(_ => Press());
        }

        private void OnDestroy() => interactable.selectEntered.RemoveAllListeners();

        /// <summary>Lo llama el interactor y también las pruebas. Sin reloj o cinta en la escena (Boot) no hace nada.</summary>
        public void Press()
        {
            if (kind == Kind.SolarStop) FindFirstObjectByType<SolarClock>()?.Select(stopIndex);
            else FindFirstObjectByType<TapeMeasure>(FindObjectsInactive.Include)?.Toggle();
        }
    }
}

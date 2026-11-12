using Umbral.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Umbral.App
{
    /// <summary>Botón 3D que lleva a otra etapa de la sesión.</summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class FlowButton : MonoBehaviour
    {
        [SerializeField] private SessionStage target;
        private XRSimpleInteractable interactable;

        public void Setup(SessionStage stage) => target = stage;

        private void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();
            interactable.selectEntered.AddListener(_ => Press());
        }

        private void OnDestroy() => interactable.selectEntered.RemoveAllListeners();

        public void Press()
        {
            if (!Session.Control.Paused) SceneFlow.GoTo(target);
        }
    }
}

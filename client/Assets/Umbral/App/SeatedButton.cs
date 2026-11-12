using Umbral.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Umbral.App
{
    /// <summary>Botón 3D del lobby: de pie o sentado (reusa <see cref="SeatedMode"/> del rig).</summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class SeatedButton : MonoBehaviour
    {
        [SerializeField] private bool seated;
        private XRSimpleInteractable interactable;

        public void Setup(bool value) => seated = value;

        private void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();
            interactable.selectEntered.AddListener(_ => Press());
        }

        private void OnDestroy() => interactable.selectEntered.RemoveAllListeners();

        public void Press() => FindFirstObjectByType<SeatedMode>()?.SetSeated(seated, ComfortSettings.StandingEyeHeight);
    }
}

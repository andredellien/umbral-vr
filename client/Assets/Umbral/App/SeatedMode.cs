using Umbral.Core;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace Umbral.App
{
    /// <summary>
    /// Modo sentado: el espacio se ve como lo vería alguien de pie. Usa tracking en modo Device con CameraYOffset
    /// (en modo Floor, XROrigin pone el Camera Offset en 0 y pisaría cualquier ajuste manual).
    /// </summary>
    public class SeatedMode : MonoBehaviour
    {
        [SerializeField] private XROrigin origin;
        [SerializeField] private bool startSeated;

        public bool Seated { get; private set; }

        private void Start() => SetSeated(startSeated || Session.Flow.Seated, ComfortSettings.StandingEyeHeight); // la elección del lobby viaja entre escenas

        public void SetSeated(bool seated, float eyeHeight)
        {
            Seated = seated;
            Session.Flow.Seated = seated;
            origin.RequestedTrackingOriginMode = seated
                ? XROrigin.TrackingOriginMode.Device
                : XROrigin.TrackingOriginMode.Floor;
            origin.CameraYOffset = ComfortSettings.ClampEyeHeight(eyeHeight);
        }
    }
}

using Umbral.Core;
using Umbral.Net;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Umbral.App
{
    /// <summary>
    /// Botón de reserva (ficha mínima). Pide la reserva al servidor con lo que el comprador eligió y muestra la ficha o el motivo.
    /// El total que ve sale de la respuesta del servidor, no de un cálculo local. Sin servidor: avisa y no pasa nada más.
    /// </summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class ReserveButton : MonoBehaviour
    {
        [SerializeField] private TextMesh board;

        private XRSimpleInteractable interactable;
        private bool busy;

        public string LastText => board != null ? board.text : null;
        public ReservationOutcome LastOutcome { get; private set; }

        public void Setup(TextMesh text) => board = text;

        private void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();
            interactable.selectEntered.AddListener(_ => Press());
        }

        private void OnDestroy() => interactable.selectEntered.RemoveAllListeners();

        public async void Press()
        {
            if (busy || Session.Control.Paused) return;
            var host = FindFirstObjectByType<ConfiguratorHost>();
            if (host == null) return;
            busy = true;
            try
            {
                var configurator = await host.Ready;
                if (this == null) return;
                Show("Reservando...");
                var outcome = await ReservationClient.ReserveAsync(SessionHost.ServerUrl, configurator.UnitId, Ficha.SelectionsOf(configurator));
                if (this == null) return;
                LastOutcome = outcome;
                if (outcome.Status == ReservationStatus.Ok)
                {
                    Show(Ficha.Text(configurator.Catalog, outcome.Reservation));
                    Telemetry.Log(TelemetryTypes.ReservationMade, e => { e.unitId = configurator.UnitId; e.detail = outcome.Reservation.total.ToString(); });
                }
                else Show(outcome.Message);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Reserva] {e.Message}");
                Show("No se pudo reservar");
            }
            finally
            {
                busy = false;
            }
        }

        private void Show(string text)
        {
            if (board != null) board.text = text;
        }
    }
}

using UnityEngine;

namespace Umbral.App
{
    /// <summary>Muestra el código de sala del visor para que el asesor se una desde la tablet.</summary>
    [RequireComponent(typeof(TextMesh))]
    public class RoomCodeLabel : MonoBehaviour
    {
        private TextMesh text;
        private float timer;

        private void Awake()
        {
            text = GetComponent<TextMesh>();
            Refresh();
        }

        private void Update()
        {
            timer -= Time.unscaledDeltaTime;
            if (timer > 0f) return;
            timer = 0.5f;
            Refresh();
        }

        private void Refresh() =>
            text.text = "Codigo de sala: " + SessionHost.Code + (SessionHost.Connected ? "" : "\n(sin conexion con la tablet)");
    }
}

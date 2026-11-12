using System.Globalization;
using Umbral.Core;
using Umbral.Net;
using UnityEngine;
using UnityEngine.UI;

namespace Umbral.App
{
    /// <summary>Muestra en la muñeca el precio de una unidad. Carga el catálogo (servidor -> caché -> seed).</summary>
    public class WristPrice : MonoBehaviour
    {
        [SerializeField] private Text label;
        [SerializeField] private string serverUrl = "http://localhost:3000";
        [SerializeField] private string unitId = "A-301";

        private Configurator configurator;

        private async void Start()
        {
            label.text = "Cargando...";
            try
            {
                // Con un ConfiguratorHost en la escena el precio sigue las elecciones; sin él (Boot) muestra el precio base.
                var host = FindFirstObjectByType<ConfiguratorHost>();
                var c = host != null
                    ? await host.Ready
                    : new Configurator(await CatalogClient.LoadAsync(serverUrl), unitId);
                if (this == null) return;
                configurator = c;
                configurator.Changed += Show;
                Show();
            }
            catch (System.Exception e)
            {
                label.text = "Sin catálogo";
                Debug.LogError($"[WristPrice] {e.Message}");
            }
        }

        private void OnDestroy()
        {
            if (configurator != null) configurator.Changed -= Show;
        }

        private void Show()
        {
            var unit = configurator.Unit;
            label.text = $"{unit.id}\n{unit.areaM2.ToString("0.#", CultureInfo.InvariantCulture)} m²\n" +
                         $"{PriceFormat.Bolivianos(configurator.Total)}\nVigente hasta {configurator.Catalog.validUntil}";
        }
    }
}

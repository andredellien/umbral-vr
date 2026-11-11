using System.Collections.Generic;
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

        private async void Start()
        {
            label.text = "Cargando...";
            try
            {
                var catalog = await CatalogClient.LoadAsync(serverUrl);
                var unit = CatalogQueries.FindUnit(catalog, unitId);
                var total = PriceCalculator.Total(catalog, unitId, CatalogQueries.DefaultSelection(catalog));
                label.text = $"{unit.id}\n{unit.areaM2:0.#} m²\nBs {total:N0}\nVigente hasta {catalog.validUntil}";
            }
            catch (System.Exception e)
            {
                label.text = "Sin catálogo";
                Debug.LogError($"[WristPrice] {e.Message}");
            }
        }
    }
}

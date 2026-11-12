using System.Threading.Tasks;
using Umbral.Core;
using Umbral.Net;
using UnityEngine;

namespace Umbral.App
{
    /// <summary>Dueño del <see cref="Configurator"/> de la escena: carga el catálogo y lo comparte con muñeca, muestras y superficies.</summary>
    public class ConfiguratorHost : MonoBehaviour
    {
        [SerializeField] private string serverUrl = "http://localhost:3000";
        [SerializeField] private string unitId = "A-301";

        /// <summary>Se completa cuando el catálogo está cargado; falla si no hay servidor, caché ni seed.</summary>
        public Task<Configurator> Ready { get; private set; }

        private void Awake()
        {
            Ready = LoadAsync();
        }

        private async Task<Configurator> LoadAsync()
        {
            var catalog = await CatalogClient.LoadAsync(serverUrl);
            return new Configurator(catalog, unitId);
        }
    }
}

using Umbral.Core;
using Umbral.Net;
using UnityEngine;

namespace Umbral.App
{
    /// <summary>Carga el catálogo y lo aplica a todas las unidades de la maqueta.</summary>
    public class MaquetaBoard : MonoBehaviour
    {
        [SerializeField] private string serverUrl = "http://localhost:3000";

        public bool Loaded { get; private set; }

        private async void Start()
        {
            try
            {
                var catalog = await CatalogClient.LoadAsync(serverUrl);
                if (this == null) return;
                foreach (var block in GetComponentsInChildren<UnitBlock>(true)) block.Apply(catalog);
                Loaded = true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Maqueta] {e.Message}");
            }
        }
    }
}

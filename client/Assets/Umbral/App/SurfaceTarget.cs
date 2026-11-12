using Umbral.Core;
using UnityEngine;

namespace Umbral.App
{
    /// <summary>Pinta los renderers de este objeto (y sus hijos) con el color de la opción elegida para una superficie.</summary>
    public class SurfaceTarget : MonoBehaviour
    {
        [SerializeField] private string surfaceId;
        [SerializeField] private ConfiguratorHost host;

        private Configurator configurator;
        private MaterialPropertyBlock block;

        public void Setup(string surface, ConfiguratorHost owner)
        {
            surfaceId = surface;
            host = owner;
        }

        private async void Start()
        {
            try
            {
                var c = await host.Ready;
                if (this == null) return;
                configurator = c;
                configurator.Changed += Apply;
                Apply();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SurfaceTarget:{surfaceId}] {e.Message}");
            }
        }

        private void OnDestroy()
        {
            if (configurator != null) configurator.Changed -= Apply;
        }

        private void Apply()
        {
            block ??= new MaterialPropertyBlock();
            var color = FinishPalette.ColorFor(configurator.SelectedOptionId(surfaceId));
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            foreach (var r in GetComponentsInChildren<Renderer>()) r.SetPropertyBlock(block);
        }
    }
}

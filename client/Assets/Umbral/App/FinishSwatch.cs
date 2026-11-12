using System.Text;
using Umbral.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Umbral.App
{
    /// <summary>Muestra 3D que el comprador apunta y aprieta para elegir un acabado. Se resalta cuando es la elegida.</summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class FinishSwatch : MonoBehaviour
    {
        [SerializeField] private string surfaceId;
        [SerializeField] private string optionId;
        [SerializeField] private ConfiguratorHost host;
        [SerializeField] private TextMesh label;

        private const float SelectedScale = 1.3f;

        private Configurator configurator;
        private MaterialPropertyBlock block;
        private Vector3 baseScale;
        private XRSimpleInteractable interactable;

        public string OptionId => optionId;

        public void Setup(string surface, string option, ConfiguratorHost owner, TextMesh text)
        {
            surfaceId = surface;
            optionId = option;
            host = owner;
            label = text;
        }

        private void Awake()
        {
            baseScale = transform.localScale;
            interactable = GetComponent<XRSimpleInteractable>();
            interactable.selectEntered.AddListener(OnSelectEntered);
        }

        private void OnDestroy()
        {
            if (interactable != null) interactable.selectEntered.RemoveListener(OnSelectEntered);
            if (configurator != null) configurator.Changed -= Refresh;
        }

        private void OnSelectEntered(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs _) => Choose();

        /// <summary>Elige esta opción. Lo llama el interactor y también las pruebas.</summary>
        public void Choose() => configurator?.Select(surfaceId, optionId);

        private async void Start()
        {
            try
            {
                var c = await host.Ready;
                if (this == null) return;
                configurator = c;
                var option = CatalogQueries.FindOption(c.Catalog, surfaceId, optionId);
                if (label != null) label.text = Wrap(option.name, 12) + "\n" + PriceFormat.Delta(option.delta);
                configurator.Changed += Refresh;
                Refresh();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FinishSwatch:{optionId}] {e.Message}");
            }
        }

        private void Refresh()
        {
            block ??= new MaterialPropertyBlock();
            var selected = configurator.SelectedOptionId(surfaceId) == optionId;
            var color = FinishPalette.ColorFor(optionId);
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            GetComponent<Renderer>().SetPropertyBlock(block);
            transform.localScale = baseScale * (selected ? SelectedScale : 1f);
        }

        /// <summary>Parte el texto en renglones de a lo sumo <paramref name="width"/> letras, cortando en espacios.</summary>
        public static string Wrap(string text, int width)
        {
            var sb = new StringBuilder();
            var line = 0;
            foreach (var word in text.Split(' '))
            {
                if (line > 0 && line + 1 + word.Length > width)
                {
                    sb.Append('\n');
                    line = 0;
                }
                if (line > 0) { sb.Append(' '); line++; }
                sb.Append(word);
                line += word.Length;
            }
            return sb.ToString();
        }
    }
}

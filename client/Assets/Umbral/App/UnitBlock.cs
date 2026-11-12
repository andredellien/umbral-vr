using Umbral.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Umbral.App
{
    /// <summary>Una unidad de la maqueta. Color y texto salen del catálogo cargado; solo las disponibles se pueden elegir.</summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class UnitBlock : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        // Disponible en color, reservada en ámbar, vendida en gris (GDD sección 8).
        public static Color ColorFor(UnitStatus status)
        {
            switch (status)
            {
                case UnitStatus.Reserved: return new Color(0.95f, 0.65f, 0.15f);
                case UnitStatus.Sold: return new Color(0.45f, 0.45f, 0.45f);
                default: return new Color(0.25f, 0.72f, 0.48f);
            }
        }

        [SerializeField] private string unitId;
        [SerializeField] private TextMesh label;

        private XRSimpleInteractable interactable;
        private Catalog catalog;

        public string UnitId => unitId;
        public UnitStatus Status { get; private set; }
        public bool Selectable => interactable != null && interactable.enabled;

        public void Setup(string id, TextMesh text)
        {
            unitId = id;
            label = text;
        }

        private void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();
            interactable.selectEntered.AddListener(_ => Press());
        }

        private void OnDestroy() => interactable.selectEntered.RemoveAllListeners();

        /// <summary>Aplica el catálogo: una unidad que ya no existe se oculta; las vendidas y reservadas no se pueden elegir.</summary>
        public void Apply(Catalog loaded)
        {
            catalog = loaded;
            UnitInfo unit;
            try { unit = CatalogQueries.FindUnit(loaded, unitId); }
            catch (System.ArgumentException) { gameObject.SetActive(false); if (label != null) label.gameObject.SetActive(false); return; }

            Status = CatalogQueries.StatusOf(unit);
            var block = new MaterialPropertyBlock();
            var renderer = GetComponent<Renderer>();
            renderer.GetPropertyBlock(block);
            var color = ColorFor(Status);
            block.SetColor(BaseColor, color);
            block.SetColor(ColorId, color);
            renderer.SetPropertyBlock(block);

            interactable.enabled = Status == UnitStatus.Available;
            if (label != null)
                label.text = $"{unit.id} - Piso {unit.floor}\n{StatusText(Status)}\n{PriceFormat.Bolivianos(unit.basePrice)}";
        }

        private static string StatusText(UnitStatus s) =>
            s == UnitStatus.Sold ? "Vendida" : s == UnitStatus.Reserved ? "Reservada" : "Disponible";

        /// <summary>Lo llama el interactor y también las pruebas.</summary>
        public void Press()
        {
            // Con un cambio de escena en curso o en pausa no se elige nada: así el precio no cambia a mitad de la transición.
            if (catalog == null || SceneFlow.Busy || Session.Control.Paused || !Session.Flow.SelectUnit(catalog, unitId)) return;
            SceneFlow.GoTo(SessionStage.Llegada);
        }
    }
}

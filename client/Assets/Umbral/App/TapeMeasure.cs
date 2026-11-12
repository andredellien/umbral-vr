using Umbral.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Umbral.App
{
    /// <summary>
    /// Cinta métrica (GDD: "se tocan dos puntos"): con la cinta activa, el gatillo marca la posición del control.
    /// El segundo punto cierra la medida y muestra los centímetros; un tercer gatillo empieza otra.
    /// Los puntos son de mundo, así que sobreviven a un teletransporte.
    /// </summary>
    public class TapeMeasure : MonoBehaviour
    {
        [SerializeField] private InputActionProperty activate;
        [SerializeField] private Transform tip;
        [SerializeField] private Material lineMaterial;

        private Vector3? first, second;
        private LineRenderer line;
        private TextMesh label;

        public bool Active { get; private set; }
        public int LastCentimeters { get; private set; } = -1;
        public string LabelText => label != null ? label.text : "";

        public void Setup(InputActionProperty action, Transform point, Material material)
        {
            activate = action;
            tip = point;
            lineMaterial = material;
        }

        // Los controles del rig están inactivos hasta que se rastrean, así que Awake puede no haber corrido: se crea al primer uso.
        private void EnsureVisuals()
        {
            if (line != null) return;
            line = new GameObject("TapeLine", typeof(LineRenderer)).GetComponent<LineRenderer>();
            line.transform.SetParent(null);
            line.positionCount = 2;
            line.startWidth = line.endWidth = 0.01f;
            line.sharedMaterial = lineMaterial;
            line.useWorldSpace = true;
            line.enabled = false;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var go = new GameObject("TapeLabel", typeof(TextMesh), typeof(MeshRenderer));
            label = go.GetComponent<TextMesh>();
            label.font = font;
            label.anchor = TextAnchor.LowerCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.012f;
            label.fontSize = 64;
            label.color = Color.yellow;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            go.SetActive(false);
        }

        private void OnEnable()
        {
            if (activate.action == null) return;
            activate.action.Enable();
            activate.action.performed += OnActivate;
        }

        private void OnDisable()
        {
            if (activate.action != null) activate.action.performed -= OnActivate;
        }

        private void OnDestroy()
        {
            if (line != null) Destroy(line.gameObject);
            if (label != null) Destroy(label.gameObject);
        }

        private void OnActivate(InputAction.CallbackContext _)
        {
            if (Active && tip != null) AddPoint(tip.position);
        }

        private void LateUpdate()
        {
            // El texto siempre mira al visitante.
            if (label != null && label.gameObject.activeSelf && Camera.main != null)
                label.transform.rotation = Quaternion.LookRotation(label.transform.position - Camera.main.transform.position);
        }

        public void Toggle()
        {
            EnsureVisuals();
            Active = !Active;
            if (!Active) Clear();
        }

        public void AddPoint(Vector3 point)
        {
            EnsureVisuals();
            if (first == null || second != null)
            {
                first = point;
                second = null;
                LastCentimeters = -1;
                line.enabled = false;
                label.text = "Punto 1"; // confirma que el primer gatillo se registró
                label.transform.position = point + Vector3.up * 0.05f;
                label.gameObject.SetActive(true);
                return;
            }

            second = point;
            var d = point - first.Value;
            LastCentimeters = Measure.Centimeters(d.x, d.y, d.z);
            line.SetPosition(0, first.Value);
            line.SetPosition(1, point);
            line.enabled = true;
            label.text = Measure.Format(LastCentimeters);
            label.transform.position = (first.Value + point) / 2f + Vector3.up * 0.05f;
            label.gameObject.SetActive(true);
        }

        private void Clear()
        {
            first = second = null;
            LastCentimeters = -1;
            line.enabled = false;
            label.gameObject.SetActive(false);
        }
    }
}

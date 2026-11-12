using Umbral.Core;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Umbral.App
{
    /// <summary>
    /// Tutorial de tres pasos del lobby: apuntar, teletransportarse, agarrar. El texto está en un tablero de la escena
    /// (no fijo frente a los ojos). El área segura la define Meta (Guardian): aquí solo se indica como instrucción.
    /// </summary>
    public class TutorialDirector : MonoBehaviour
    {
        [SerializeField] private XRSimpleInteractable aimTarget;
        [SerializeField] private Transform teleportMarker;
        [SerializeField] private XRGrabInteractable grabCube;
        [SerializeField] private TextMesh board;
        [SerializeField] private float markerRadius = 0.5f;

        public TutorialSteps Steps { get; } = new TutorialSteps();

        public void Setup(XRSimpleInteractable aim, Transform marker, XRGrabInteractable grab, TextMesh text)
        {
            aimTarget = aim;
            teleportMarker = marker;
            grabCube = grab;
            board = text;
        }

        private void Awake()
        {
            aimTarget.hoverEntered.AddListener(_ => Steps.Complete(TutorialStep.Aim));
            grabCube.selectEntered.AddListener(_ => Steps.Complete(TutorialStep.Grab));
            Steps.StepDone += _ => Refresh();
        }

        private void Start() => Refresh();

        private void Update()
        {
            if (Steps.Completed || Steps.Current != TutorialStep.Teleport) return;
            var origin = FindFirstObjectByType<XROrigin>();
            if (origin == null || origin.Camera == null) return;
            var cam = origin.Camera.transform.position;
            var m = teleportMarker.position;
            if (Vector2.Distance(new Vector2(cam.x, cam.z), new Vector2(m.x, m.z)) < markerRadius)
                Steps.Complete(TutorialStep.Teleport);
        }

        private void Refresh()
        {
            if (board == null) return;
            if (Steps.Completed)
            {
                board.text = "Listo.\nElegi como estas (de pie o sentado)\ny segui a la maqueta.";
                return;
            }
            switch (Steps.Current)
            {
                case TutorialStep.Aim:
                    board.text = "Antes de empezar: confirma tu area segura (Guardian de Meta).\n\n1 de 3: apunta el rayo al circulo azul."; break;
                case TutorialStep.Teleport:
                    board.text = "2 de 3: empuja el stick hacia adelante,\napunta al circulo verde del piso y sueltalo."; break;
                default:
                    board.text = "3 de 3: agarra el cubo con el boton lateral (grip)."; break;
            }
        }
    }
}

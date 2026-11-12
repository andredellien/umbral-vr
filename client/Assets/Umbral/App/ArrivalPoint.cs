using Unity.XR.CoreUtils;
using UnityEngine;

namespace Umbral.App
{
    /// <summary>
    /// Punto de llegada de la escena: al cargarla, la cámara del usuario queda aquí (en la puerta, mirando adentro).
    /// Se mueve la cámara y no el origen del rig, para compensar el desplazamiento y el giro del visor dentro del área segura.
    /// No depende de la posición del prefab del rig en la escena, que un override desactualizado puede dejar en el origen.
    /// </summary>
    public class ArrivalPoint : MonoBehaviour
    {
        private void Start() => Place();

        public void Place()
        {
            var origin = FindFirstObjectByType<XROrigin>();
            if (origin == null) return;
            var target = transform.position;
            var yaw = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            if (origin.Camera == null)
            {
                origin.transform.SetPositionAndRotation(target, yaw);
                return;
            }

            origin.MatchOriginUpCameraForward(Vector3.up, yaw * Vector3.forward); // gira alrededor de la cámara
            var camera = origin.Camera.transform.position;
            origin.MoveCameraToWorldLocation(new Vector3(target.x, camera.y, target.z));
            var o = origin.transform.position;
            origin.transform.position = new Vector3(o.x, target.y, o.z); // el piso queda a la altura del punto
        }
    }
}

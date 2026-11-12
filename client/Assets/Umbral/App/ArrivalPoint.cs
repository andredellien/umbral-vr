using Unity.XR.CoreUtils;
using UnityEngine;

namespace Umbral.App
{
    /// <summary>
    /// Punto de llegada de la escena: al cargarla, el XR Origin se coloca aquí (en la puerta, mirando adentro).
    /// No depende de la posición del prefab del rig en la escena, que un override desactualizado puede dejar en el origen.
    /// </summary>
    public class ArrivalPoint : MonoBehaviour
    {
        private void Start()
        {
            var origin = FindFirstObjectByType<XROrigin>();
            if (origin == null) return;
            origin.transform.SetPositionAndRotation(transform.position, Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
        }
    }
}

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Umbral.App.Tests
{
    public class BootSceneTests
    {
        [UnityTest]
        public IEnumerator MuestraPrecioDeA301SinServidor()
        {
            var load = SceneManager.LoadSceneAsync("Boot");
            while (!load.isDone) yield return null;

            // El XRInputModalityManager del rig apaga los controles hasta detectar uno rastreado (sin dispositivos en CI no hay).
            // Simulamos "control izquierdo detectado" activando la rama que contiene el panel.
            var wrist = Object.FindFirstObjectByType<WristPrice>(FindObjectsInactive.Include);
            for (var tr = wrist.transform; tr != null; tr = tr.parent) tr.gameObject.SetActive(true);
            var label = wrist.GetComponentInChildren<Text>();
            var timeout = Time.realtimeSinceStartup + 15f;
            while ((label.text == "" || label.text == "Cargando...") && Time.realtimeSinceStartup < timeout) yield return null; // Start corre al activarse

            Debug.Log("WRISTPRICE_TEXT=" + label.text.Replace("\n", " | "));
            StringAssert.Contains("A-301", label.text);
            StringAssert.IsMatch(@"Bs \d{1,3}(,\d{3})*", label.text); // formato fijo; el monto depende del catálogo de la corrida
        }
    }
}

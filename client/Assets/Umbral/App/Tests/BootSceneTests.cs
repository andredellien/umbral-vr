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

            var label = Object.FindFirstObjectByType<WristPrice>().GetComponentInChildren<Text>();
            var timeout = Time.realtimeSinceStartup + 15f;
            while (label.text == "Cargando..." && Time.realtimeSinceStartup < timeout) yield return null;

            Debug.Log("WRISTPRICE_TEXT=" + label.text.Replace("\n", " | "));
            StringAssert.Contains("A-301", label.text);
            StringAssert.Contains("Bs", label.text);
        }
    }
}

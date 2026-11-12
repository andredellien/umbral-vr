using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Umbral.App.Tests
{
    /// <summary>Capturas de las escenas para los informes. Solo corre con UMBRAL_SHOTS=carpeta de salida.</summary>
    public class ShotsTests
    {
        private static IEnumerator Shot(string scene, string file, Vector3 pos, Vector3 look, string outDir)
        {
            SceneManager.LoadScene(scene);
            for (var i = 0; i < 40; i++) yield return null;

            var cam = new GameObject("ShotCam", typeof(Camera)).GetComponent<Camera>();
            cam.transform.position = pos;
            cam.transform.LookAt(look);
            cam.fieldOfView = 70;
            var rt = new RenderTexture(1600, 900, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            Directory.CreateDirectory(outDir);
            File.WriteAllBytes(Path.Combine(outDir, file), tex.EncodeToPNG());
            Object.Destroy(tex);
            Object.Destroy(cam.gameObject);
            rt.Release();
        }

        [UnityTest]
        [Category("Shots")]
        public IEnumerator CapturasSemana5()
        {
            var outDir = System.Environment.GetEnvironmentVariable("UMBRAL_SHOTS");
            if (string.IsNullOrEmpty(outDir)) Assert.Ignore("Definí UMBRAL_SHOTS para sacar capturas");
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) Assert.Ignore("Requiere GPU");

            yield return Shot("Lobby", "s5-lobby.png", new Vector3(0f, 1.7f, 0.5f), new Vector3(0f, 1.3f, 6f), outDir);
            yield return Shot("Maqueta", "s5-maqueta.png", new Vector3(0.6f, 1.7f, 0.4f), new Vector3(0f, 1.1f, 3f), outDir);
            yield return Shot("Apartment_Greybox", "s5-llegada.png", new Vector3(1.45f, 1.6f, 0.4f), new Vector3(3.6f, 1.2f, 4f), outDir);
        }
    }
}

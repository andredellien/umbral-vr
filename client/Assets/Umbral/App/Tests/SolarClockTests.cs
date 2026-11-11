#if UNITY_EDITOR
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Umbral.App.Tests
{
    /// <summary>Spike del reloj solar: cambia entre 4 sets de lightmaps horneados y verifica que la luz cambia.</summary>
    public class SolarClockTests
    {
        private const string ScenePath = "Assets/Spikes/Solar/Spike_Solar.unity";
        private static readonly string[] Names = { "manana", "mediodia", "tarde", "noche" };

        [UnityTest]
        [Category("Spike")]
        public IEnumerator CambiaLaLuzEntreLasCuatroParadas()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("Requiere GPU para renderizar (no usar -nographics)");

            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; // dejar que corra Start() de SolarClock antes de medir
            var clock = Object.FindFirstObjectByType<SolarClock>();
            var cam = Camera.main;
            var rt = new RenderTexture(512, 288, 24);
            cam.targetTexture = rt;
            cam.enabled = false;

            var outDir = System.Environment.GetEnvironmentVariable("UMBRAL_SPIKE_OUT");
            var brightness = new float[clock.Count];
            var swapMs = new double[clock.Count];
            var memMb = new float[clock.Count];

            for (var i = 0; i < clock.Count; i++)
            {
                var t0 = Time.realtimeSinceStartupAsDouble;
                clock.Apply(i);
                swapMs[i] = (Time.realtimeSinceStartupAsDouble - t0) * 1000.0;
                yield return null;
                cam.Render();

                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                RenderTexture.active = prev;

                var px = tex.GetPixels32();
                double sum = 0;
                foreach (var p in px) sum += 0.2126 * p.r + 0.7152 * p.g + 0.0722 * p.b;
                brightness[i] = (float)(sum / px.Length / 255.0);

                long bytes = 0;
                foreach (var lm in LightmapSettings.lightmaps) bytes += Profiler.GetRuntimeMemorySizeLong(lm.lightmapColor);
                memMb[i] = bytes / 1048576f;

                if (!string.IsNullOrEmpty(outDir))
                {
                    Directory.CreateDirectory(outDir);
                    File.WriteAllBytes(Path.Combine(outDir, $"solar-{Names[i]}.png"), tex.EncodeToPNG());
                }
                Object.Destroy(tex);
            }

            var report = "";
            for (var i = 0; i < clock.Count; i++)
                report += $" [{Names[i]} brillo={brightness[i]:0.000} lightmaps={memMb[i]:0.00}MB cambio={swapMs[i]:0.0}ms]";
            Debug.Log("SOLAR_RESULT" + report);

            for (var i = 0; i < clock.Count; i++)
            for (var j = i + 1; j < clock.Count; j++)
                Assert.Greater(Mathf.Abs(brightness[i] - brightness[j]), 0.005f,
                    $"Las paradas '{Names[i]}' y '{Names[j]}' se ven iguales");
            Assert.Less(brightness[3], brightness[1], "La noche debe ser más oscura que el mediodía");
        }
    }
}
#endif

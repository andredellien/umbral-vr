using System.Collections;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace Umbral.App.Tests
{
    /// <summary>
    /// Spike de presupuesto (GDD: 100 draw calls / 500k triángulos). Mide con ProfilerRecorder,
    /// que funciona en el editor, en batchmode con GPU y en builds de desarrollo (Quest).
    /// </summary>
    public class BudgetTests
    {
        [UnityTest]
        [Category("Spike")]
        public IEnumerator EscenaEnElLimiteDelPresupuesto()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("Sin GPU (-nographics): el spike de presupuesto requiere render");

            using var drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            using var triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            using var setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");

            var cam = new GameObject("Cam", typeof(Camera)).GetComponent<Camera>();
            cam.transform.position = new Vector3(3.6f, 4.6f, -8f);
            cam.transform.LookAt(new Vector3(3.6f, 4.6f, 3f));

            cam.targetTexture = new RenderTexture(1920, 1080, 24);
            cam.enabled = false; // render manual: una sola pasada por frame, sin contar doble
            var go = new GameObject("Stress", typeof(BudgetStress));
            var stress = go.GetComponent<BudgetStress>();
            for (var i = 0; i < 15; i++)
            {
                yield return null;
                cam.Render();
            }

            Debug.Log($"BUDGET objetos={stress.objects} tris_generados={stress.SpawnedTriangles} " +
                      $"drawCalls={drawCalls.LastValue} tris_render={triangles.LastValue} setPass={setPass.LastValue} " +
                      $"gpu={SystemInfo.graphicsDeviceName}");

            Assert.That(stress.SpawnedTriangles, Is.InRange(450_000, 550_000));
            Assert.Greater(drawCalls.LastValue, 0, "Sin render no hay métricas (no usar -nographics)");
            Object.Destroy(go);
            Object.Destroy(cam.gameObject);
        }
    }
}

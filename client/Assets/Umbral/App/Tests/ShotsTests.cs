using System.Collections;
using System.IO;
using NUnit.Framework;
using Umbral.Core;
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

        private static void Snap(Camera cam, string file, string outDir)
        {
            var rt = new RenderTexture(1600, 900, 24);
            var previous = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = previous;
            Directory.CreateDirectory(outDir);
            File.WriteAllBytes(Path.Combine(outDir, file), tex.EncodeToPNG());
            Object.Destroy(tex);
            rt.Release();
        }

        [UnityTest]
        [Category("Shots")]
        public IEnumerator CapturasSemana6()
        {
            var outDir = System.Environment.GetEnvironmentVariable("UMBRAL_SHOTS");
            if (string.IsNullOrEmpty(outDir)) Assert.Ignore("Definí UMBRAL_SHOTS para sacar capturas");
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) Assert.Ignore("Requiere GPU");
            Session.Reset();

            // Lobby legible desde donde se para el comprador (el círculo verde)
            yield return Shot("Lobby", "s6-lobby.png", new Vector3(0f, 1.7f, 3.2f), new Vector3(0f, 1.6f, 5.7f), outDir);

            // Pantalla del asesor reflejando una sesión (estado armado desde el catálogo, por un relay falso)
#if UNITY_EDITOR
            var op = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Asesor.unity", new LoadSceneParameters(LoadSceneMode.Single));
            while (!op.isDone) yield return null;
            for (var i = 0; i < 3; i++) yield return null;
            var view = Object.FindFirstObjectByType<AdvisorView>();
            var fake = new FakeTransport();
            view.Bind(new Umbral.Net.SessionLink(fake));
            var catalog = JsonUtility.FromJson<Umbral.Core.Catalog>(Resources.Load<TextAsset>("catalog_seed").text);
            var cfg = new Umbral.Core.Configurator(catalog, "A-1201");
            cfg.Select("floor", "floor-wood");
            cfg.Select("counter", "counter-quartz");
            var flow = new Umbral.Core.SessionFlow();
            flow.JumpTo(Umbral.Core.SessionStage.Llegada);
            var snap = Umbral.Core.SessionSnapshot.Build("4821", flow, cfg, 2, false);
            fake.Receive("{\"event\":\"joined\",\"data\":{\"code\":\"4821\"}}");
            fake.Receive(JsonUtility.ToJson(new StateEnvelope { @event = "state", data = snap }));
            Canvas.ForceUpdateCanvases();
            yield return null;
            yield return null;
            Snap(Object.FindFirstObjectByType<Canvas>().worldCamera, "s6-asesor.png", outDir);
#endif

            // Visor en pausa: se renderiza con la cámara del propio rig, así entran el fundido y el cartel
            yield return Shot("Apartment_Greybox", "s6-pausa-previa.png", new Vector3(1.45f, 1.6f, 0.4f), new Vector3(3.6f, 1.2f, 4f), outDir);
            Session.Control.SetPaused(true);
            var fade = Object.FindFirstObjectByType<Umbral.App.ComfortFade>();
            var rigCamera = fade.GetComponentInParent<Camera>();
            var timeout = Time.realtimeSinceStartup + 3f;
            while (Time.realtimeSinceStartup < timeout) yield return null;
            Snap(rigCamera, "s6-pausa.png", outDir);
            Session.Control.SetPaused(false);
        }

        private static void Snap(string file, Vector3 pos, Vector3 look, string outDir)
        {
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

        private static IEnumerator WaitFor(System.Func<bool> condition, float seconds = 15f)
        {
            var timeout = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < timeout) yield return null;
        }

        // Requiere el servidor real (UMBRAL_SERVER): reserva A-301 con acabados elegidos y captura la ficha; después la maqueta con las reservadas en ámbar.
        [UnityTest]
        [Category("Shots")]
        public IEnumerator CapturasSemana7()
        {
            var outDir = System.Environment.GetEnvironmentVariable("UMBRAL_SHOTS");
            var server = System.Environment.GetEnvironmentVariable("UMBRAL_SERVER");
            if (string.IsNullOrEmpty(outDir) || string.IsNullOrEmpty(server)) Assert.Ignore("Definí UMBRAL_SHOTS y UMBRAL_SERVER");
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) Assert.Ignore("Requiere GPU");
            Session.Reset();
            SessionHost.ServerUrl = server;

            SceneManager.LoadScene("Apartment_Greybox");
            for (var i = 0; i < 40; i++) yield return null;
            var host = Object.FindFirstObjectByType<ConfiguratorHost>();
            yield return WaitFor(() => host.Ready.IsCompleted);
            var configurator = host.Ready.Result;
            configurator.Select("floor", "floor-wood");
            configurator.Select("counter", "counter-quartz");
            var button = Object.FindFirstObjectByType<ReserveButton>();
            button.Press();
            yield return WaitFor(() => button.LastOutcome != null);
            for (var i = 0; i < 5; i++) yield return null;
            Snap("s7-ficha.png", new Vector3(3.9f, 1.5f, 1.4f), new Vector3(3.9f, 1.3f, 3.0f), outDir);

            Session.Reset();
            SceneManager.LoadScene("Maqueta");
            var board = Object.FindFirstObjectByType<MaquetaBoard>();
            for (var i = 0; i < 10; i++) yield return null;
            board = Object.FindFirstObjectByType<MaquetaBoard>();
            yield return WaitFor(() => board.Loaded);
            for (var i = 0; i < 20; i++) yield return null;
            Snap("s7-maqueta-reservada.png", new Vector3(0.6f, 1.7f, 0.4f), new Vector3(0f, 1.2f, 3f), outDir);
            SessionHost.ServerUrl = "http://localhost:3000";
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

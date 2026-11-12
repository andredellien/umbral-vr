using System;
using Umbral.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Umbral.App
{
    /// <summary>Cambio de etapa con fundido a negro (GDD sección 9). Sin vuelo de cámara: el usuario nunca ve un movimiento que no generó.</summary>
    public static class SceneFlow
    {
        public static bool Busy { get; set; }

        public static string SceneName(SessionStage stage)
        {
            switch (stage)
            {
                case SessionStage.Lobby: return "Lobby";
                case SessionStage.Maqueta: return "Maqueta";
                default: return "Apartment_Greybox"; // la llegada es la unidad A-301 con el rig en la puerta
            }
        }

        /// <summary>Oscurece, carga la escena de la etapa y la nueva escena aclara sola. Si la carga falla, la pantalla vuelve a verse.</summary>
        public static async void GoTo(SessionStage stage)
        {
            if (Busy) return;
            Busy = true;
            var fade = UnityEngine.Object.FindFirstObjectByType<ComfortFade>();
            try
            {
                if (fade != null)
                {
                    fade.FadeTo(1f, ComfortSettings.FadeOutSeconds);
                    var waited = 0f;
                    while (fade != null && fade.Alpha < 0.999f && waited < 1f)
                    {
                        waited += Time.unscaledDeltaTime;
                        await Awaitable.NextFrameAsync();
                    }
                }
                Session.Flow.JumpTo(stage);
                Session.EnteredByFlow = true;
                var load = SceneManager.LoadSceneAsync(SceneName(stage));
                if (load == null) throw new InvalidOperationException($"No se pudo cargar la escena '{SceneName(stage)}'");
                await load;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Session.EnteredByFlow = false;
                if (fade != null) fade.FadeTo(0f, ComfortSettings.FadeInSeconds);
            }
            finally
            {
                Busy = false;
            }
        }
    }
}

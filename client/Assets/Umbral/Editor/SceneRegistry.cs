using System.Linq;
using UnityEditor;

namespace Umbral.EditorTools
{
    /// <summary>Lista de escenas del build, en orden: la sesión arranca en el lobby (índice 0) y sigue por el flujo (maqueta, llegada). Boot queda como escena de prueba.</summary>
    public static class SceneRegistry
    {
        private static readonly string[] Scenes = { "Assets/Scenes/Lobby.unity", "Assets/Scenes/Maqueta.unity", "Assets/Scenes/Apartment_Greybox.unity", "Assets/Scenes/Boot.unity" };

        public static void Register()
        {
            EditorBuildSettings.scenes = Scenes
                .Where(s => System.IO.File.Exists(s))
                .Select(s => new EditorBuildSettingsScene(s, true))
                .ToArray();
        }
    }
}

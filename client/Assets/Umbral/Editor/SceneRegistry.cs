using System.Linq;
using UnityEditor;

namespace Umbral.EditorTools
{
    /// <summary>Lista de escenas del build, en orden. Por ahora el build arranca en el grey-box (nada carga escenas todavía, llega con el flujo de escenas de la semana 5); Boot queda como escena de prueba.</summary>
    public static class SceneRegistry
    {
        private static readonly string[] Scenes = { "Assets/Scenes/Apartment_Greybox.unity", "Assets/Scenes/Boot.unity" };

        public static void Register()
        {
            EditorBuildSettings.scenes = Scenes
                .Where(s => System.IO.File.Exists(s))
                .Select(s => new EditorBuildSettingsScene(s, true))
                .ToArray();
        }
    }
}

using UnityEngine;

namespace Umbral.App
{
    /// <summary>Un momento del día horneado: las texturas de lightmap de todos los renderers de la escena.</summary>
    [CreateAssetMenu(menuName = "Umbral/Lightmap Set")]
    public class LightmapSet : ScriptableObject
    {
        public string label;
        public Texture2D[] colors;

        public LightmapData[] ToData()
        {
            var data = new LightmapData[colors.Length];
            for (var i = 0; i < colors.Length; i++) data[i] = new LightmapData { lightmapColor = colors[i] };
            return data;
        }
    }
}

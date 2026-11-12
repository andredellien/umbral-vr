using UnityEngine;

namespace Umbral.App
{
    /// <summary>Un momento del día horneado: las texturas de lightmap de todos los renderers de la escena.</summary>
    [CreateAssetMenu(menuName = "Umbral/Lightmap Set")]
    public class LightmapSet : ScriptableObject
    {
        public string label;
        public Texture2D[] colors;
        public Color ambient = new Color(0.5f, 0.55f, 0.6f); // luz ambiente para objetos dinámicos (muestras, paletas)
        public Color skyTint = Color.gray;                    // tinte del panorama de la ventana
        public float skyExposure = 1f;

        public LightmapData[] ToData()
        {
            var data = new LightmapData[colors.Length];
            for (var i = 0; i < colors.Length; i++) data[i] = new LightmapData { lightmapColor = colors[i] };
            return data;
        }
    }
}

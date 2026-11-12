using UnityEngine;

namespace Umbral.App
{
    /// <summary>
    /// Reloj solar por paradas (ADR-001 #5): cambia entre sets de lightmaps horneados.
    /// El fundido a negro lo hace el componente de confort compartido con el teletransporte; aquí solo se intercambia la luz.
    /// Requiere que todos los sets se horneen con la misma geometría (mismos índices y escalas de lightmap).
    /// </summary>
    public class SolarClock : MonoBehaviour
    {
        [SerializeField] private LightmapSet[] sets;
        [SerializeField] private int startIndex = 1; // mediodía (7, 12, 16 y 19 h)
        [SerializeField] private ComfortFade fade;   // opcional: cambia la luz con fundido a negro

        public int Count => sets == null ? 0 : sets.Length;
        public int Current { get; private set; } = -1;

        private Material sky;

        private void Start()
        {
            if (fade == null) fade = FindFirstObjectByType<ComfortFade>(FindObjectsInactive.Include);
            if (RenderSettings.skybox != null)
            {
                sky = new Material(RenderSettings.skybox); // copia: cambiar el original quedaría guardado en el proyecto
                RenderSettings.skybox = sky;
            }
            Apply(startIndex);
        }

        public void Apply(int index)
        {
            if (sets == null || sets.Length == 0) return;
            index = Mathf.Clamp(index, 0, sets.Length - 1);
            if (sets[index] == null || sets[index].colors == null) return;
            var set = sets[index];
            LightmapSettings.lightmaps = set.ToData();
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = set.ambient;
            if (sky != null)
            {
                if (sky.HasProperty("_Tint")) sky.SetColor("_Tint", set.skyTint);
                if (sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", set.skyExposure);
            }
            Current = index;
        }

        /// <summary>Cambia de parada con fundido (si hay ComfortFade); si no, de golpe.</summary>
        public void Select(int index)
        {
            if (index == Current) return;
            if (fade != null && fade.Busy) return; // no pisar un fundido en curso (p. ej. el del teletransporte)
            if (fade != null) fade.FadeThrough(() => Apply(index)); // la corrutina vive en ComfortFade, no aquí
            else Apply(index);
        }

        public void SetSets(LightmapSet[] value) => sets = value;
    }
}

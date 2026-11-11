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
        [SerializeField] private int startIndex = 1; // media mañana, como en el GDD
        [SerializeField] private ComfortFade fade;   // opcional: cambia la luz con fundido a negro

        public int Count => sets.Length;
        public int Current { get; private set; } = -1;

        private void Start() => Apply(startIndex);

        public void Apply(int index)
        {
            if (sets == null || sets.Length == 0) return;
            index = Mathf.Clamp(index, 0, sets.Length - 1);
            if (sets[index] == null || sets[index].colors == null) return;
            LightmapSettings.lightmaps = sets[index].ToData();
            Current = index;
        }

        /// <summary>Cambia de parada con fundido (si hay ComfortFade); si no, de golpe.</summary>
        public void Select(int index)
        {
            if (fade != null) fade.FadeThrough(() => Apply(index)); // la corrutina vive en ComfortFade, no aquí
            else Apply(index);
        }

        public void SetSets(LightmapSet[] value) => sets = value;
    }
}

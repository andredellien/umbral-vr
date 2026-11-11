using System;
using System.Collections;
using Umbral.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace Umbral.App
{
    /// <summary>
    /// Fundido a negro único para todo el proyecto (teletransporte, reloj solar, escenas). Es un quad pegado a la cámara:
    /// un Canvas Screen Space no se renderiza en el visor. Se apaga el renderer cuando alpha = 0 para no costar nada.
    /// Regla de seguridad: el usuario nunca debe quedar en negro. Hay un solo fundido activo a la vez, un reset al
    /// desactivarse y una red de seguridad en Update si el teletransporte ya terminó y seguimos oscuros.
    /// </summary>
    public class ComfortFade : MonoBehaviour
    {
        [SerializeField] private Renderer quad;
        [SerializeField] private TeleportationProvider teleport;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private MaterialPropertyBlock block;
        private Coroutine running;
        private bool teleportHold; // el negro actual lo pidió el teletransporte (y es él quien debe liberarlo)

        public float Alpha { get; private set; }

        private void Awake()
        {
            block = new MaterialPropertyBlock();
            Apply(0f);
        }

        private void OnEnable()
        {
            if (teleport == null) teleport = FindFirstObjectByType<TeleportationProvider>();
            if (teleport != null) teleport.locomotionStateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            if (teleport != null) teleport.locomotionStateChanged -= OnStateChanged;
            // Desactivar mata las corrutinas: dejar el quad a medias dejaría la pantalla oscura al reactivarse.
            running = null;
            teleportHold = false;
            Apply(0f);
        }

        // Preparing = el teletransporte espera su delay. Oscurecemos algo más rápido que el delay para estar en negro
        // cuando se mueve, y volvemos recién en Ended.
        private void OnStateChanged(LocomotionProvider provider, LocomotionState state)
        {
            if (state == LocomotionState.Preparing)
            {
                teleportHold = true;
                FadeTo(1f, Mathf.Max(teleport.delayTime * 0.8f, 0.01f));
            }
            else if (state == LocomotionState.Ended)
            {
                teleportHold = false;
                FadeTo(0f, ComfortSettings.FadeInSeconds);
            }
        }

        // Red de seguridad: si el teletransporte pidió el negro pero ya no está en curso (p. ej. se desactivó el provider), aclarar.
        // Solo aplica a negros pedidos por el teletransporte: un fundido manual (escena, reloj solar) no se toca.
        private void Update()
        {
            if (teleportHold && running == null && Alpha > 0.001f && teleport != null && teleport.locomotionState == LocomotionState.Idle)
            {
                teleportHold = false;
                FadeTo(0f, ComfortSettings.FadeInSeconds);
            }
        }

        public void FadeTo(float target, float seconds) => Run(Fade(target, seconds));

        /// <summary>Oscurece, ejecuta <paramref name="atBlack"/> con la pantalla en negro y vuelve a mostrar, pase lo que pase.</summary>
        public void FadeThrough(Action atBlack) => Run(Through(atBlack));

        private void Run(IEnumerator routine)
        {
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(Wrap(routine));
        }

        private IEnumerator Wrap(IEnumerator routine)
        {
            yield return routine;
            running = null;
        }

        private IEnumerator Through(Action atBlack)
        {
            yield return Fade(1f, ComfortSettings.FadeOutSeconds);
            try { atBlack?.Invoke(); }
            catch (Exception e) { Debug.LogException(e); } // un fallo al cambiar la luz no puede dejar la pantalla negra
            yield return Fade(0f, ComfortSettings.FadeInSeconds);
        }

        private IEnumerator Fade(float target, float seconds)
        {
            var start = Alpha;
            for (var t = 0f; t < seconds; t += Time.deltaTime) // mismo reloj (escalado) que el delay del teletransporte
            {
                Apply(Mathf.Lerp(start, target, t / seconds));
                yield return null;
            }
            Apply(target);
        }

        private void Apply(float alpha)
        {
            Alpha = alpha;
            quad.enabled = alpha > 0.001f;
            quad.GetPropertyBlock(block);
            block.SetColor(BaseColor, new Color(0f, 0f, 0f, alpha));
            quad.SetPropertyBlock(block);
        }
    }
}

using UnityEngine;

namespace Ruinas
{
    /// <summary>
    /// Orbe luminoso que cresce rápido, pulsa enquanto dura e some (carga do obelisco, bola de fogo da explosão).
    /// Raio e duração vêm de VfxParams; cor opcional.
    /// </summary>
    public class OrbFx : MonoBehaviour, IVfxBehaviour
    {
        static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        public Renderer sphere;
        public Light orbLight;
        public float growTime = 0.08f;
        public float fadeTime = 0.15f;
        public float defaultRadius = 1.2f;
        public float defaultHold = 0.3f;
        public float intensity = 1.2f;
        public float pulseAmount = 0.08f;
        public float endScale = 1f;
        public Color defaultColor = new Color(2.2f, 0.6f, 2.8f);

        MaterialPropertyBlock mpb;
        float t, radius, hold, lightBase;
        Color color;
        bool running;

        public void OnVfxSpawn(PooledVfx vfx)
        {
            if (mpb == null) mpb = new MaterialPropertyBlock();
            radius = vfx.Params.radius > 0f ? vfx.Params.radius : defaultRadius;
            hold = vfx.Params.duration > 0f ? vfx.Params.duration : defaultHold;
            color = vfx.Params.hasColor ? vfx.Params.color : defaultColor;
            if (orbLight != null && lightBase <= 0f) lightBase = orbLight.intensity;
            t = 0f;
            running = true;
            Apply();
        }

        public void OnVfxStop() { }

        void Update()
        {
            if (!running) return;
            t += Time.deltaTime;
            Apply();
            if (t > growTime + hold + fadeTime) running = false;
        }

        void Apply()
        {
            float grow = Mathf.Clamp01(t / Mathf.Max(0.001f, growTime));
            grow = 1f - (1f - grow) * (1f - grow);
            float after = Mathf.Clamp01((t - growTime - hold) / Mathf.Max(0.001f, fadeTime));
            float fade = 1f - after;
            float pulse = 1f + pulseAmount * Mathf.Sin(t * 26f);
            float r = radius * grow * pulse * Mathf.Lerp(1f, endScale, after);
            float flashScale = LevelContext.Current != null ? LevelContext.Current.Vfx.FlashScale : 1f;
            if (sphere != null)
            {
                sphere.transform.localScale = Vector3.one * Mathf.Max(0.01f, r * 2f);
                sphere.GetPropertyBlock(mpb);
                mpb.SetFloat(IntensityId, intensity * fade * flashScale);
                mpb.SetColor(ColorId, color);
                sphere.SetPropertyBlock(mpb);
                sphere.enabled = fade > 0.001f;
            }
            if (orbLight != null) orbLight.intensity = lightBase * fade * flashScale;
        }
    }
}

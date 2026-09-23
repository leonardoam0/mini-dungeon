using UnityEngine;

namespace Ruinas
{
    /// <summary>Onda de choque rente ao chão: anel que cresce até o raio do efeito e desvanece.</summary>
    public class RingFx : MonoBehaviour, IVfxBehaviour
    {
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int IntensityId = Shader.PropertyToID("_Intensity");

        public Renderer ring;
        public float duration = 0.38f;
        public float defaultRadius = 2.5f;
        public float intensity = 1.6f;

        MaterialPropertyBlock mpb;
        float t;
        float radius;
        Color color = Color.white;

        public void OnVfxSpawn(PooledVfx vfx)
        {
            if (mpb == null) mpb = new MaterialPropertyBlock();
            t = 0f;
            radius = vfx.Params.radius > 0f ? vfx.Params.radius : defaultRadius;
            color = vfx.Params.hasColor ? vfx.Params.color : Color.white;
            Apply();
        }

        public void OnVfxStop() { }

        void Update()
        {
            t += Time.deltaTime;
            Apply();
        }

        void Apply()
        {
            if (ring == null) return;
            float u = Mathf.Clamp01(t / duration);
            float r = Mathf.Lerp(0.2f, radius, 1f - (1f - u) * (1f - u));
            ring.transform.localScale = new Vector3(r * 2.2f, r * 2.2f, 1f);
            float flash = LevelContext.Current != null ? LevelContext.Current.Vfx.FlashScale : 1f;
            ring.GetPropertyBlock(mpb);
            mpb.SetColor(ColorId, color);
            mpb.SetFloat(IntensityId, intensity * (1f - u) * flash);
            ring.SetPropertyBlock(mpb);
            ring.enabled = u < 1f;
        }
    }
}

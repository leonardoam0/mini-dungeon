using UnityEngine;

namespace Ruinas
{
    /// <summary>Clarão breve voltado para a câmera (impactos). Respeita a opção de reduzir flashes.</summary>
    public class FlashFx : MonoBehaviour, IVfxBehaviour
    {
        static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        public Renderer quad;
        public float duration = 0.09f;
        public float startScale = 0.4f;
        public float endScale = 1.3f;
        public float intensity = 2.2f;

        MaterialPropertyBlock mpb;
        float t;
        Color color = new Color(1f, 0.95f, 0.85f);
        float scaleMul = 1f;

        public void OnVfxSpawn(PooledVfx vfx)
        {
            if (mpb == null) mpb = new MaterialPropertyBlock();
            t = 0f;
            if (vfx.Params.hasColor) color = vfx.Params.color;
            scaleMul = vfx.Params.radius > 0f ? vfx.Params.radius : 1f;
            Apply();
        }

        public void OnVfxStop() { }

        void LateUpdate()
        {
            t += Time.deltaTime;
            Apply();
        }

        void Apply()
        {
            if (quad == null) return;
            float u = Mathf.Clamp01(t / duration);
            var cam = Camera.main;
            if (cam != null) quad.transform.rotation = cam.transform.rotation;
            quad.transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, u) * scaleMul;
            float flashScale = LevelContext.Current != null ? LevelContext.Current.Vfx.FlashScale : 1f;
            quad.GetPropertyBlock(mpb);
            mpb.SetFloat(IntensityId, intensity * (1f - u) * flashScale);
            mpb.SetColor(ColorId, color);
            quad.SetPropertyBlock(mpb);
            quad.enabled = u < 1f;
        }
    }
}

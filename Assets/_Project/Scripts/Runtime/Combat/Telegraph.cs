using UnityEngine;

namespace Ruinas
{
    /// <summary>
    /// Aviso no chão: anel (opcionalmente tracejado) cujo disco se preenche até o instante do golpe.
    /// Combina forma e movimento com o som de preparação — não depende só de cor.
    /// </summary>
    public class Telegraph : MonoBehaviour
    {
        static readonly int FillId = Shader.PropertyToID("_Fill");
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int DashesId = Shader.PropertyToID("_Dashes");

        MeshRenderer meshRenderer;
        MaterialPropertyBlock mpb;
        float duration;
        float t;
        Color color;
        bool fill;
        bool active;
        VfxPool pool;

        public bool Active => active;

        public void Init(VfxPool owner, Mesh quad, Material material)
        {
            pool = owner;
            var mf = gameObject.AddComponent<MeshFilter>();
            mf.sharedMesh = quad;
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            mpb = new MaterialPropertyBlock();
            gameObject.layer = Layers.Vfx;
        }

        public void Show(Vector3 position, float radius, float seconds, Color c, bool dashed, bool fillDisc)
        {
            transform.position = position;
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            transform.localScale = Vector3.one * radius * 2.08f;
            duration = Mathf.Max(0.05f, seconds);
            t = 0f;
            color = c;
            fill = fillDisc;
            active = true;
            mpb.SetFloat(DashesId, dashed ? 14f : 0f);
            Apply();
        }

        void Apply()
        {
            float u = Mathf.Clamp01(t / duration);
            float pulse = 0.85f + 0.15f * Mathf.Sin(t * 18f);
            var c = color;
            c.a *= pulse * Mathf.Clamp01((duration + 0.12f - t) / 0.12f);
            mpb.SetColor(ColorId, c);
            mpb.SetFloat(FillId, fill ? u : 0f);
            meshRenderer.SetPropertyBlock(mpb);
        }

        void Update()
        {
            if (!active) return;
            t += Time.deltaTime;
            Apply();
            if (t >= duration + 0.12f) Hide();
        }

        public void Hide()
        {
            if (!active) return;
            active = false;
            pool?.ReleaseTelegraph(this);
        }
    }
}

using UnityEngine;

namespace Ruinas
{
    /// <summary>
    /// Pulso magenta: esfera translúcida que se expande rápido (centro claro, borda saturada),
    /// arcos elétricos curtos e dissipação breve. O raio vem de VfxParams.radius (o mesmo do dano).
    /// </summary>
    public class PulseSphereFx : MonoBehaviour, IVfxBehaviour
    {
        static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        public Renderer sphere;
        public LineRenderer[] arcs;
        public Light pulseLight;
        public float growTime = 0.3f;
        public float holdTime = 0.12f;
        public float fadeTime = 0.38f;
        public float defaultRadius = 5f;

        MaterialPropertyBlock mpb;
        float t;
        float radius;
        float arcTimer;
        bool running;
        Color baseColor = new Color(2.2f, 0.55f, 2.6f, 1f);
        uint seed = 1;

        public float CurrentRadius => sphere != null ? sphere.transform.localScale.x * 0.5f : 0f;

        public void OnVfxSpawn(PooledVfx vfx)
        {
            if (mpb == null) mpb = new MaterialPropertyBlock();
            radius = vfx.Params.radius > 0f ? vfx.Params.radius : defaultRadius;
            if (vfx.Params.hasColor) baseColor = vfx.Params.color;
            t = 0f;
            arcTimer = 0f;
            running = true;
            seed = DeterministicVfx.Enabled ? DeterministicVfx.NextSpawnSeed("arcos") | 1u : (uint)(Time.frameCount * 7919 + 17);
            Apply();
        }

        public void OnVfxStop() { }

        float Rand()
        {
            seed ^= seed << 13; seed ^= seed >> 17; seed ^= seed << 5;
            return (seed & 0xFFFF) / 65535f;
        }

        void Update()
        {
            if (!running) return;
            t += Time.deltaTime;
            Apply();
            arcTimer -= Time.deltaTime;
            if (arcTimer <= 0f)
            {
                arcTimer = 0.045f;
                JitterArcs();
            }
            if (t > growTime + holdTime + fadeTime) running = false;
        }

        void Apply()
        {
            float grow = Mathf.Clamp01(t / growTime);
            grow = 1f - (1f - grow) * (1f - grow) * (1f - grow);
            float r = Mathf.Lerp(0.3f, radius, grow);
            float fade = 1f - Mathf.Clamp01((t - growTime - holdTime) / fadeTime);
            float flashScale = LevelContext.Current != null ? LevelContext.Current.Vfx.FlashScale : 1f;
            if (sphere != null)
            {
                sphere.transform.localScale = Vector3.one * r * 2f;
                sphere.GetPropertyBlock(mpb);
                // Translúcida: no vídeo a esfera é rosa-clara e deixa ver a cena através dela.
                mpb.SetFloat(IntensityId, (0.22f + 0.55f * fade) * fade * flashScale);
                mpb.SetColor(ColorId, baseColor);
                sphere.SetPropertyBlock(mpb);
                sphere.enabled = fade > 0.001f;
            }
            if (pulseLight != null) pulseLight.intensity = 6f * fade * flashScale;
            if (arcs != null)
                foreach (var a in arcs) if (a != null) a.enabled = fade > 0.15f;
        }

        void JitterArcs()
        {
            if (arcs == null) return;
            float r = CurrentRadius;
            foreach (var a in arcs)
            {
                if (a == null) continue;
                int n = a.positionCount;
                float ang = Rand() * Mathf.PI * 2f;
                float elev = Mathf.Lerp(-0.1f, 0.9f, Rand());
                Vector3 dir = new Vector3(Mathf.Cos(ang) * Mathf.Cos(elev), Mathf.Sin(elev), Mathf.Sin(ang) * Mathf.Cos(elev));
                Vector3 start = dir * r * Mathf.Lerp(0.2f, 0.5f, Rand()) + Vector3.up * 0.8f;
                Vector3 end = dir * r * 0.98f + Vector3.up * 0.8f;
                for (int i = 0; i < n; i++)
                {
                    float u = n > 1 ? i / (float)(n - 1) : 0f;
                    Vector3 p = Vector3.Lerp(start, end, u);
                    if (i > 0 && i < n - 1) p += new Vector3(Rand() - 0.5f, Rand() - 0.5f, Rand() - 0.5f) * 0.45f;
                    a.SetPosition(i, p);
                }
            }
        }
    }
}

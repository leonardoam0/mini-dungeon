using UnityEngine;

namespace Ruinas
{
    /// <summary>
    /// Feixe vertical (coluna magenta do obelisco, brilho de loot). Envelope de subida/queda;
    /// com duração negativa permanece até ser parado.
    /// </summary>
    public class BeamFx : MonoBehaviour, IVfxBehaviour
    {
        static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        public Renderer[] beams;
        public Light beamLight;
        public float riseTime = 0.15f;
        public float fallTime = 0.35f;
        public float defaultHeight = 10f;
        public float width = 0.9f;
        public float baseIntensity = 1f;
        public float pulse = 0.15f;

        MaterialPropertyBlock mpb;
        float t;
        float duration;
        bool stopping;
        float stopT;
        float level;
        Color color = new Color(2.2f, 0.5f, 2.6f, 1f);
        float lightBase = 3f;
        bool lightCaptured;

        public void OnVfxSpawn(PooledVfx vfx)
        {
            if (mpb == null) mpb = new MaterialPropertyBlock();
            if (beamLight != null && !lightCaptured) { lightBase = beamLight.intensity; lightCaptured = true; }
            t = 0f;
            stopT = 0f;
            stopping = false;
            duration = vfx.Params.duration;
            if (vfx.Params.hasColor) color = vfx.Params.color;
            float h = vfx.Params.height > 0f ? vfx.Params.height : defaultHeight;
            if (beams != null)
                foreach (var b in beams)
                    if (b != null) b.transform.localScale = new Vector3(width, h, width);
            Apply(0f);
        }

        public void OnVfxStop()
        {
            stopping = true;
            stopT = 0f;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            t += dt;
            float target;
            if (stopping)
            {
                stopT += dt;
                target = 1f - Mathf.Clamp01(stopT / fallTime);
                level = Mathf.Min(level, target);
            }
            else
            {
                level = Mathf.Clamp01(t / Mathf.Max(0.01f, riseTime));
                if (duration > 0f && t > duration - fallTime)
                    level = Mathf.Min(level, Mathf.Clamp01((duration - t) / fallTime));
            }
            Apply(level * (1f + pulse * Mathf.Sin(t * 11f)));
        }

        void Apply(float k)
        {
            if (beams != null)
            {
                foreach (var b in beams)
                {
                    if (b == null) continue;
                    b.GetPropertyBlock(mpb);
                    mpb.SetFloat(IntensityId, baseIntensity * k);
                    mpb.SetColor(ColorId, color);
                    b.SetPropertyBlock(mpb);
                    b.enabled = k > 0.001f;
                }
            }
            if (beamLight != null)
            {
                beamLight.color = new Color(Mathf.Clamp01(color.r / 2.6f), Mathf.Clamp01(color.g / 2.6f), Mathf.Clamp01(color.b / 2.6f));
                beamLight.intensity = lightBase * k;
            }
        }
    }
}

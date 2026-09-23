using UnityEngine;

namespace Ruinas
{
    /// <summary>
    /// Contorno luminoso rente ao piso da arena. A cor/intensidade acompanha o estado do encontro:
    /// ciano-branco quando ativo, deslocando para verde perto do fim (observado entre ~6 e 9 s no vídeo).
    /// </summary>
    public class PerimeterGlow : MonoBehaviour
    {
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int IntensityId = Shader.PropertyToID("_Intensity");

        public Renderer[] strips;
        public Light[] lights;
        public Color cyan = new Color(0.62f, 1f, 0.94f);
        public Color green = new Color(0.45f, 1f, 0.38f);
        public float dormantIntensity = 0.5f;
        public float activeIntensity = 3.2f;

        MaterialPropertyBlock mpb;
        Color current;
        float intensity;
        Color targetColor;
        float targetIntensity;
        float flicker;
        float[] lightBase;

        /// <summary>Quando verdadeiro, a cor só muda por SnapColor (roteiro de referência); o estado altera apenas a intensidade.</summary>
        public bool ScriptedColor { get; set; }

        void Awake()
        {
            mpb = new MaterialPropertyBlock();
            current = targetColor = cyan;
            intensity = targetIntensity = dormantIntensity;
            if (lights != null)
            {
                lightBase = new float[lights.Length];
                for (int i = 0; i < lights.Length; i++) lightBase[i] = lights[i] != null ? lights[i].intensity : 1f;
            }
            Apply();
        }

        public void SetState(EncounterState s, float progress, float shiftAt)
        {
            var keep = targetColor;
            ApplyState(s, progress, shiftAt);
            if (ScriptedColor) targetColor = keep;
        }

        void ApplyState(EncounterState s, float progress, float shiftAt)
        {
            switch (s)
            {
                case EncounterState.Dormant:
                    targetColor = cyan; targetIntensity = dormantIntensity; break;
                case EncounterState.Activating:
                    targetColor = cyan; targetIntensity = activeIntensity * 0.8f; flicker = 1.2f; break;
                case EncounterState.Active:
                    targetColor = progress >= shiftAt ? green : cyan; targetIntensity = activeIntensity; break;
                case EncounterState.Resolving:
                    targetColor = green; targetIntensity = activeIntensity * 0.7f; break;
                case EncounterState.Cleared:
                    targetColor = green; targetIntensity = dormantIntensity * 0.8f; break;
            }
        }

        /// <summary>Força cor imediata (eventos do roteiro de referência).</summary>
        public void SnapColor(bool greenish)
        {
            targetColor = greenish ? green : cyan;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            current = Color.Lerp(current, targetColor, 1f - Mathf.Exp(-dt * 2.5f));
            intensity = Mathf.Lerp(intensity, targetIntensity, 1f - Mathf.Exp(-dt * 4f));
            if (flicker > 0f) flicker -= dt;
            Apply();
        }

        void Apply()
        {
            float vt = DeterministicVfx.VisualTime;
            float f = flicker > 0f ? 0.75f + 0.25f * Mathf.Sin(vt * 40f) : 1f;
            float pulse = 1f + 0.06f * Mathf.Sin(vt * 2.2f);
            if (strips != null)
            {
                foreach (var r in strips)
                {
                    if (r == null) continue;
                    r.GetPropertyBlock(mpb);
                    mpb.SetColor(ColorId, current);
                    mpb.SetFloat(IntensityId, intensity * f * pulse);
                    r.SetPropertyBlock(mpb);
                }
            }
            if (lights != null)
            {
                for (int i = 0; i < lights.Length; i++)
                {
                    if (lights[i] == null) continue;
                    lights[i].color = current;
                    lights[i].intensity = lightBase[i] * intensity / Mathf.Max(0.01f, activeIntensity) * f;
                }
            }
        }
    }
}

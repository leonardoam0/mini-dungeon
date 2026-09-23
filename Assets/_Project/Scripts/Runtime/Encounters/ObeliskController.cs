using UnityEngine;

namespace Ruinas
{
    /// <summary>
    /// Marco central da arena: topo emissivo, sinais laterais, luz local e partículas ascendentes.
    /// Os visuais seguem o estado do encontro; o feixe magenta periódico é só apresentação (o vídeo não
    /// comprova que o obelisco ataque) — os ataques vêm de outras entidades.
    /// </summary>
    public class ObeliskController : MonoBehaviour
    {
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int IntensityId = Shader.PropertyToID("_Intensity");

        public Renderer top;
        public Renderer[] eyes;
        public Light topLight;
        public ParticleSystem motes;
        public ParticleSystem risingSparks;
        public float beamInterval = 6.5f;

        [Header("Cores")]
        public Color dormantTop = new Color(0.25f, 0.45f, 0.45f);
        public Color activeTop = new Color(1.6f, 2.0f, 1.95f);
        public Color clearedTop = new Color(0.55f, 2.1f, 0.6f);
        public Color eyesActive = new Color(1.4f, 1.9f, 1.9f);
        public Color eyesAlert = new Color(1.8f, 0.5f, 2.2f);
        public Color eyesCleared = new Color(0.4f, 1.6f, 0.45f);
        public Color chargeTop = new Color(2.3f, 0.85f, 2.9f);
        public Color chargeEyes = new Color(2.4f, 0.7f, 2.8f);

        EncounterState state = EncounterState.Dormant;
        MaterialPropertyBlock mpb;
        Color topColor, topTarget, eyeColor, eyeTarget;
        float lightTarget, lightLevel;
        float beamTimer;
        float stateT;
        float chargeUntil = -1f;

        public EncounterState State => state;
        public bool Charged => Time.time < chargeUntil;
        Vector3 TopCenter => top != null ? top.bounds.center : transform.position + Vector3.up * 4.8f;

        void Awake()
        {
            mpb = new MaterialPropertyBlock();
            topColor = topTarget = dormantTop;
            eyeColor = eyeTarget = dormantTop * 0.5f;
            lightLevel = lightTarget = 0.4f;
            beamTimer = beamInterval * 0.5f;
            Apply();
        }

        public void SetState(EncounterState s)
        {
            state = s;
            stateT = 0f;
            switch (s)
            {
                case EncounterState.Dormant:
                    topTarget = dormantTop; eyeTarget = dormantTop * 0.5f; lightTarget = 0.4f;
                    SetEmission(motes, false); SetEmission(risingSparks, false);
                    break;
                case EncounterState.Activating:
                    topTarget = activeTop; eyeTarget = eyesAlert; lightTarget = 2.5f;
                    SetEmission(motes, true);
                    break;
                case EncounterState.Active:
                    topTarget = activeTop; eyeTarget = eyesActive; lightTarget = 3.2f;
                    SetEmission(motes, true); SetEmission(risingSparks, false);
                    break;
                case EncounterState.Resolving:
                    topTarget = clearedTop; eyeTarget = eyesCleared; lightTarget = 3.6f;
                    SetEmission(risingSparks, true); SetEmission(motes, false);
                    break;
                case EncounterState.Cleared:
                    topTarget = clearedTop; eyeTarget = eyesCleared; lightTarget = 1.8f;
                    SetEmission(risingSparks, false); SetEmission(motes, true);
                    break;
            }
        }

        static void SetEmission(ParticleSystem ps, bool on)
        {
            if (ps == null) return;
            if (on && !ps.isEmitting) ps.Play(true);
            else if (!on && ps.isEmitting) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        /// <summary>Carga violeta: topo e sinais roxos com um orbe luminoso ao redor do topo (antecede o feixe).</summary>
        public void Charge(float duration)
        {
            chargeUntil = Mathf.Max(chargeUntil, Time.time + duration);
            topColor = chargeTop;
            eyeColor = chargeEyes;
            var ctx = LevelContext.Current;
            if (ctx == null) return;
            Vector3 p = TopCenter;
            ctx.Vfx.Spawn("orb_violet", p, Quaternion.identity, duration + 0.15f, new VfxParams { radius = 1.25f, duration = duration });
            Services.Audio?.Play("pulse_charge", p);
        }

        /// <summary>Coluna magenta sobre o topo (evento visual); o topo permanece violeta enquanto dura.</summary>
        public void EmitBeam(float duration = 1.4f)
        {
            var ctx = LevelContext.Current;
            if (ctx == null) return;
            Vector3 p = TopCenter;
            ctx.Vfx.Spawn("beam_magenta", p, Quaternion.identity, duration, new VfxParams { height = 11f, duration = duration, color = new Color(2.2f, 0.45f, 2.7f), hasColor = true });
            ctx.Vfx.Spawn("magic_flame", p + Vector3.up * 0.2f, Quaternion.identity, duration);
            Services.Audio?.Play("obelisk_beam", p);
            chargeUntil = Mathf.Max(chargeUntil, Time.time + duration);
            eyeColor = chargeEyes;
        }

        /// <summary>Limpa efeitos temporários (reinício da arena).</summary>
        public void ResetVisuals()
        {
            chargeUntil = -1f;
            beamTimer = beamInterval * 0.5f;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            stateT += dt;
            bool charged = Charged;
            topColor = Color.Lerp(topColor, charged ? chargeTop : topTarget, 1f - Mathf.Exp(-dt * (charged ? 12f : 4f)));
            eyeColor = Color.Lerp(eyeColor, charged ? chargeEyes : eyeTarget, 1f - Mathf.Exp(-dt * (charged ? 12f : 4f)));
            lightLevel = Mathf.Lerp(lightLevel, lightTarget, 1f - Mathf.Exp(-dt * 2f));
            if (state == EncounterState.Active && beamInterval > 0f)
            {
                beamTimer -= dt;
                if (beamTimer <= 0f)
                {
                    beamTimer = beamInterval;
                    EmitBeam();
                }
            }
            Apply();
        }

        void Apply()
        {
            float pulse = state == EncounterState.Active ? 1f + 0.12f * Mathf.Sin(DeterministicVfx.VisualTime * 3.1f) : 1f;
            if (top != null)
            {
                top.GetPropertyBlock(mpb);
                mpb.SetColor(EmissionId, topColor * pulse);
                mpb.SetColor(ColorId, topColor * pulse);
                mpb.SetFloat(IntensityId, 1f);
                top.SetPropertyBlock(mpb);
            }
            if (eyes != null)
            {
                foreach (var e in eyes)
                {
                    if (e == null) continue;
                    e.GetPropertyBlock(mpb);
                    mpb.SetColor(ColorId, eyeColor * pulse);
                    mpb.SetFloat(IntensityId, 1f);
                    e.SetPropertyBlock(mpb);
                }
            }
            if (topLight != null)
            {
                topLight.intensity = lightLevel * pulse;
                var c = topColor;
                float m = Mathf.Max(0.001f, Mathf.Max(c.r, Mathf.Max(c.g, c.b)));
                topLight.color = new Color(c.r / m, c.g / m, c.b / m);
            }
        }
    }
}

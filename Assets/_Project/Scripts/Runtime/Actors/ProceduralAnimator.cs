using UnityEngine;

namespace Ruinas
{
    /// <summary>
    /// Animação por poses-chave sobre o rig rígido. A locomoção avança pela distância percorrida
    /// (sem deslizamento de pés) e os clips de ação (golpes, disparo, esquiva...) sobrepõem o corpo
    /// inteiro ou só a parte superior. A autoridade de movimento é sempre o motor, nunca a animação.
    /// </summary>
    public class ProceduralAnimator : MonoBehaviour
    {
        public CharacterRig rig;
        public PoseLibrary library;
        public bool quadruped;

        [Header("Locomoção")]
        public float strideLength = 1.55f;
        public float legSwing = 36f;
        public float armSwing = 30f;
        public float bobHeight = 0.045f;
        public float runLean = 5f;
        public float idleBreath = 1.2f;
        public float armRest = 4f;

        public float LocalTimeScale { get; set; } = 1f;
        /// <summary>Elevação visual extra do modelo (arco do salto); o colisor e a câmera não sobem.</summary>
        public float ExtraLift { get; set; }
        public string CurrentClipId => clipActive && clip != null ? clip.id : null;
        public float ClipTime => clipTime;
        public bool ClipActive => clipActive;

        float speed;
        float speedNorm;
        float phase;
        float idleTime;

        PoseClip clip;
        float clipTime;
        float clipSpeed = 1f;
        bool clipActive;
        bool holdAtEnd;
        float weight;
        float fadeOut = -1f;
        PoseSample frozenClipPose;

        float flash;
        Color flashColor = Color.white;
        MaterialPropertyBlock mpb;
        static readonly int HitFlashId = Shader.PropertyToID("_HitFlash");
        static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");

        void Awake()
        {
            if (rig == null) rig = GetComponent<CharacterRig>();
            if (rig != null) rig.CaptureRest();
            if (library == null && Services.Database != null) library = Services.Database.poses;
            mpb = new MaterialPropertyBlock();
        }

        public void SetLocomotion(Vector3 worldVelocity, float maxSpeed)
        {
            worldVelocity.y = 0f;
            speed = worldVelocity.magnitude;
            speedNorm = maxSpeed > 0.01f ? Mathf.Clamp01(speed / maxSpeed) : 0f;
        }

        public bool Play(string id, float speedMul = 1f, bool hold = false, bool restart = true)
        {
            if (library == null) return false;
            var c = library.Get(id);
            if (c == null) return false;
            if (!restart && clipActive && clip == c) return true;
            clip = c;
            clipTime = 0f;
            clipSpeed = Mathf.Max(0.01f, speedMul);
            clipActive = true;
            holdAtEnd = hold;
            fadeOut = -1f;
            if (weight < 0.01f) weight = 0f;
            return true;
        }

        public void StopClip(bool immediate = false)
        {
            if (!clipActive) return;
            if (immediate)
            {
                clipActive = false;
                weight = 0f;
                return;
            }
            frozenClipPose = PoseMath.Sample(clip, clipTime);
            fadeOut = clip != null ? Mathf.Max(0.01f, clip.blendOut) : 0.1f;
        }

        public bool IsPlaying(string id) => clipActive && clip != null && clip.id == id;

        public void Flash(float amount = 0.85f, Color? color = null)
        {
            flash = Mathf.Max(flash, amount);
            flashColor = color ?? Color.white;
        }

        public void ResetPose()
        {
            clipActive = false;
            weight = 0f;
            fadeOut = -1f;
            flash = 0f;
            LocalTimeScale = 1f;
            ApplyFlash();
        }

        void LateUpdate()
        {
            if (rig == null) return;
            float dt = Time.deltaTime * LocalTimeScale;

            PoseSample loco = Locomotion(dt);
            PoseSample final = loco;

            if (clipActive && clip != null)
            {
                PoseSample cp;
                if (fadeOut > 0f)
                {
                    cp = frozenClipPose;
                    weight -= dt / fadeOut;
                    if (weight <= 0f)
                    {
                        weight = 0f;
                        clipActive = false;
                        fadeOut = -1f;
                    }
                }
                else
                {
                    clipTime += dt * clipSpeed;
                    float blendIn = Mathf.Max(0.001f, clip.blendIn);
                    weight = Mathf.Min(1f, weight + dt / blendIn);
                    cp = PoseMath.Sample(clip, clipTime);
                    if (!clip.loop && clipTime >= clip.duration && !holdAtEnd)
                    {
                        frozenClipPose = cp;
                        fadeOut = Mathf.Max(0.01f, clip.blendOut);
                    }
                }

                if (clipActive || weight > 0f)
                {
                    float w = Mathf.SmoothStep(0f, 1f, weight);
                    final = clip.upperBodyOnly ? PoseSample.LerpUpper(loco, cp, w) : PoseSample.Lerp(loco, cp, w);
                }
            }

            ApplyPose(final);

            if (flash > 0f)
            {
                flash = Mathf.Max(0f, flash - Time.deltaTime * 7f);
                ApplyFlash();
            }
        }

        PoseSample Locomotion(float dt)
        {
            var s = new PoseSample();
            idleTime += dt;
            phase += (speed * dt / Mathf.Max(0.2f, strideLength)) * Mathf.PI * 2f;
            if (phase > Mathf.PI * 200f) phase -= Mathf.PI * 200f;
            float sn = Mathf.Sin(phase);
            float m = speedNorm;
            float breath = Mathf.Sin(idleTime * 2.1f);

            if (quadruped)
            {
                s.armL = new Vector3(-legSwing * sn * m, 0, 0);
                s.legR = new Vector3(-legSwing * sn * m, 0, 0);
                s.armR = new Vector3(legSwing * sn * m, 0, 0);
                s.legL = new Vector3(legSwing * sn * m, 0, 0);
                s.head = new Vector3(Mathf.Sin(phase * 2f) * 4f * m + breath * 1.5f * (1f - m), 0, 0);
                s.rootOffset = new Vector3(0, Mathf.Abs(Mathf.Cos(phase)) * bobHeight * m, 0);
                return s;
            }

            s.legL = new Vector3(-legSwing * sn * m, 0, 0);
            s.legR = new Vector3(legSwing * sn * m, 0, 0);
            s.armL = new Vector3(armSwing * sn * m + breath * 1.2f * (1f - m), 0, -armRest - breath * idleBreath * (1f - m));
            s.armR = new Vector3(-armSwing * sn * m - breath * 1.2f * (1f - m), 0, armRest + breath * idleBreath * (1f - m));
            s.body = new Vector3(runLean * m + breath * 0.6f * (1f - m), 0, 0);
            s.head = new Vector3(-runLean * 0.5f * m, Mathf.Sin(idleTime * 0.7f) * 3f * (1f - m), 0);
            s.rootOffset = new Vector3(0, (0.5f + 0.5f * Mathf.Cos(phase * 2f)) * bobHeight * m, 0);
            return s;
        }

        void ApplyPose(in PoseSample s)
        {
            if (rig.body) rig.body.localRotation = rig.bodyRest * Quaternion.Euler(s.body);
            if (rig.head) rig.head.localRotation = rig.headRest * Quaternion.Euler(s.head);
            if (rig.armL) rig.armL.localRotation = rig.armLRest * Quaternion.Euler(s.armL);
            if (rig.armR) rig.armR.localRotation = rig.armRRest * Quaternion.Euler(s.armR);
            if (rig.legL) rig.legL.localRotation = rig.legLRest * Quaternion.Euler(s.legL);
            if (rig.legR) rig.legR.localRotation = rig.legRRest * Quaternion.Euler(s.legR);
            if (rig.model)
            {
                rig.model.localPosition = rig.modelRestPos + s.rootOffset + Vector3.up * ExtraLift;
                rig.model.localRotation = rig.modelRestRot * Quaternion.Euler(s.rootTilt);
            }
        }

        void ApplyFlash()
        {
            if (rig == null || rig.renderers == null) return;
            foreach (var r in rig.renderers)
            {
                if (r == null) continue;
                if (flash <= 0f)
                {
                    // Sem bloco de propriedades o renderer volta a ser agrupado pelo SRP Batcher.
                    r.SetPropertyBlock(null);
                    continue;
                }
                r.GetPropertyBlock(mpb);
                mpb.SetFloat(HitFlashId, flash);
                mpb.SetColor(FlashColorId, flashColor);
                r.SetPropertyBlock(mpb);
            }
        }
    }
}

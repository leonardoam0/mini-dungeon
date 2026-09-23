using System;
using UnityEngine;

namespace Ruinas
{
    public enum PlayerActionState { Free, Melee, Ranged, Dodge, Artifact, HitReact, Stunned, Defeated }

    /// <summary>
    /// Estados explícitos de ação do jogador: livre, golpe (preparação → janela ativa → recuperação),
    /// disparo, esquiva, uso de artefato, reação, atordoamento e derrota. Tempos vêm dos dados
    /// (AttackDefinition / ItemDefinition / PlayerDefinition).
    /// </summary>
    public class PlayerActionStateMachine : MonoBehaviour
    {
        PlayerController pc;
        Actor actor;
        PlayerMotor motor;
        HitboxController hitbox;
        ProceduralAnimator anim;
        PlayerDefinition def;
        TrailRenderer trail;

        public PlayerActionState State { get; private set; } = PlayerActionState.Free;
        public float StateTime => t;
        float t;

        // Corpo a corpo
        AttackDefinition atk;
        int comboIndex;
        bool windowOpened, windowClosed, swingPlayed, queued;
        Vector3 queuedDir, attackDir;
        int eventId;
        float comboGrace;

        // Distância
        bool fired;
        float releaseAt;
        Vector3 rangedDir;
        float fireCooldown;

        // Esquiva / poção / artefato
        float dodgeCooldown;
        public float PotionCooldown { get; private set; }
        public float PotionCooldownTotal => def != null ? def.potionCooldown : 1f;
        int potionFrame = -1;
        float lockDuration;

        public event Action PotionUsed;
        public event Action PotionDenied;
        public event Action<string> Denied;

        public bool AllowsMovement => State == PlayerActionState.Free;
        public bool IsAttacking => State == PlayerActionState.Melee || State == PlayerActionState.Ranged;
        public AttackDefinition CurrentAttack => State == PlayerActionState.Melee ? atk : null;
        public int ComboIndex => comboIndex;

        public void Init(PlayerController controller, PlayerDefinition definition)
        {
            pc = controller;
            def = definition;
            actor = pc.Actor;
            motor = pc.Motor;
            hitbox = pc.Hitbox;
            anim = pc.Anim;
            actor.Damaged += OnDamaged;
        }

        void OnDestroy()
        {
            if (actor != null) actor.Damaged -= OnDamaged;
        }

        public void SetTrail(TrailRenderer tr) => trail = tr;

        void SetTrailEmitting(bool on)
        {
            if (trail == null) return;
            if (on) trail.Clear();
            trail.emitting = on;
        }

        // ------------------------------------------------------------ Corpo a corpo

        public bool RequestMelee(Vector3 dir)
        {
            if (State == PlayerActionState.Defeated || State == PlayerActionState.Stunned) return false;
            if (State == PlayerActionState.Melee)
            {
                if (atk != null && t >= atk.comboWindowStart && t <= atk.comboWindowEnd + 0.1f)
                {
                    queued = true;
                    queuedDir = dir;
                }
                return false;
            }
            if (State == PlayerActionState.Ranged && !fired) CancelToFree();
            if (State != PlayerActionState.Free) return false;
            StartMelee(comboGrace > 0f ? comboIndex + 1 : 0, dir);
            return true;
        }

        void StartMelee(int index, Vector3 dir)
        {
            var weapon = pc.Equipment.Melee;
            var combo = weapon != null && weapon.def.combo != null && weapon.def.combo.Length > 0 ? weapon.def.combo : pc.FistCombo;
            if (combo == null || combo.Length == 0) return;
            comboIndex = ((index % combo.Length) + combo.Length) % combo.Length;
            atk = combo[comboIndex];
            State = PlayerActionState.Melee;
            t = 0f;
            windowOpened = windowClosed = swingPlayed = queued = false;
            dir.y = 0f;
            attackDir = dir.sqrMagnitude > 1e-4f ? dir.normalized : transform.forward;
            eventId = LevelContext.Current != null ? LevelContext.Current.Combat.NewEventId() : 0;
            motor.SetMoveIntent(Vector3.zero);
            motor.FaceDirection(attackDir, true);
            if (atk.lunge > 0f) motor.Lunge(attackDir, atk.lunge, Mathf.Max(0.05f, atk.activeStart));
            anim?.Play(atk.poseClip, 1f);
        }

        float MeleeDamage()
        {
            var w = pc.Equipment.Melee;
            float baseDmg = w != null ? w.def.meleeDamage * w.PowerMultiplier : 4f;
            return baseDmg * (atk != null ? atk.damageMultiplier : 1f);
        }

        void UpdateMelee(float dt)
        {
            if (atk == null) { State = PlayerActionState.Free; return; }
            t += dt;
            if (!swingPlayed && t >= atk.activeStart - 0.07f)
            {
                swingPlayed = true;
                Services.Audio?.Play(atk.swingSfx, transform.position);
                if (atk.trail) SetTrailEmitting(true);
            }
            if (!windowOpened && t >= atk.activeStart)
            {
                windowOpened = true;
                hitbox.Begin(atk, MeleeDamage(), eventId, attackDir, pc.Equipment.MeleeStatuses, DamageFlags.CanCrit, DamageKind.Melee);
            }
            if (windowOpened && !windowClosed && t >= atk.activeEnd)
            {
                windowClosed = true;
                hitbox.End();
                SetTrailEmitting(false);
            }
            if (queued && t >= atk.chainAt && windowClosed)
            {
                StartMelee(comboIndex + 1, queuedDir);
                return;
            }
            if (t >= atk.duration)
            {
                EndMelee();
                State = PlayerActionState.Free;
                comboGrace = 0.28f;
            }
        }

        void EndMelee()
        {
            hitbox.End();
            SetTrailEmitting(false);
            queued = false;
        }

        // ------------------------------------------------------------ Distância

        public bool RequestRanged(Vector3 dir)
        {
            if (State == PlayerActionState.Ranged)
            {
                rangedDir = dir.sqrMagnitude > 1e-4f ? dir : rangedDir;
                return false;
            }
            if (State != PlayerActionState.Free) return false;
            if (fireCooldown > 0f) return false;
            var w = pc.Equipment.Ranged;
            if (w == null) { Deny("Sem arma à distância equipada"); return false; }
            if (pc.Inventory.Arrows <= 0) { Deny("Sem flechas"); return false; }
            State = PlayerActionState.Ranged;
            t = 0f;
            fired = false;
            rangedDir = dir;
            motor.SetMoveIntent(Vector3.zero);
            motor.FaceDirection(dir, true);
            anim?.Play("bow_draw", 0.3f / Mathf.Max(0.05f, w.def.drawTime));
            Services.Audio?.Play("bow_draw", transform.position);
            return true;
        }

        void UpdateRanged(float dt)
        {
            t += dt;
            var w = pc.Equipment.Ranged;
            if (w == null) { State = PlayerActionState.Free; return; }
            motor.FaceDirection(rangedDir);
            if (!fired && t >= w.def.drawTime)
            {
                fired = true;
                releaseAt = t;
                Fire(w);
            }
            if (fired && t >= releaseAt + 0.16f)
            {
                State = PlayerActionState.Free;
                fireCooldown = w.def.fireCooldown;
            }
        }

        void Fire(ItemInstance w)
        {
            var ctx = LevelContext.Current;
            if (ctx == null || !pc.Inventory.UseArrow()) return;
            Vector3 dir = rangedDir;
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-4f) dir = transform.forward;
            dir.Normalize();
            Vector3 origin = transform.position + Vector3.up * 1.15f + dir * 0.5f;
            int count = Mathf.Max(1, w.def.projectilesPerShot);
            for (int i = 0; i < count; i++)
            {
                float spread = count > 1 ? Mathf.Lerp(-w.def.spreadDegrees, w.def.spreadDegrees, i / (float)(count - 1)) : 0f;
                Vector3 d = Quaternion.Euler(0f, spread, 0f) * dir;
                ctx.Projectiles.Spawn(w.def.projectile, actor, actor.team, origin, d, w.def.rangedDamage * w.PowerMultiplier, 1f, null, pc.Equipment.ExtraPierce);
            }
            anim?.Play("bow_release", 1f);
        }

        // ------------------------------------------------------------ Esquiva, poção, artefatos

        public bool TryDodge(Vector3 dir)
        {
            if (State == PlayerActionState.Defeated || State == PlayerActionState.Stunned || State == PlayerActionState.Dodge) return false;
            if (dodgeCooldown > 0f) return false;
            if (State == PlayerActionState.Melee && atk != null && t < atk.dodgeCancelAfter) return false;
            if (State == PlayerActionState.Artifact && t < lockDuration * 0.6f) return false;
            if (State == PlayerActionState.Melee) EndMelee();
            State = PlayerActionState.Dodge;
            t = 0f;
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-4f) dir = transform.forward;
            motor.StartDodge(dir.normalized, def.dodgeDistance, def.dodgeDuration);
            if (def.dodgeInvulnerable && def.dodgeInvulnerableTime > 0f) actor.Receiver.GrantInvulnerability(def.dodgeInvulnerableTime);
            anim?.Play("roll", 0.34f / Mathf.Max(0.05f, def.dodgeDuration));
            Services.Audio?.Play("roll", transform.position);
            LevelContext.Current?.Vfx.Spawn("dust_puff", transform.position, Quaternion.identity);
            return true;
        }

        public bool TryPotion()
        {
            if (potionFrame == Time.frameCount) return false;
            potionFrame = Time.frameCount;
            if (State == PlayerActionState.Defeated) return false;
            if (PotionCooldown > 0f || actor.Receiver.Health >= actor.Receiver.MaxHealth - 0.5f)
            {
                PotionDenied?.Invoke();
                Services.Audio?.PlayUi("denied");
                return false;
            }
            var ctx = LevelContext.Current;
            float amount = actor.Receiver.MaxHealth * def.potionHealFraction;
            ctx?.Combat.Heal(actor, amount, "potion");
            PotionCooldown = def.potionCooldown;
            anim?.Play("drink", 1f);
            Services.Audio?.Play("heal", transform.position);
            ctx?.Vfx.SpawnAttached("heal_swirl", transform, Vector3.up * 0.1f, 1.2f);
            PotionUsed?.Invoke();
            return true;
        }

        public bool CanUseArtifact()
        {
            switch (State)
            {
                case PlayerActionState.Free: return true;
                case PlayerActionState.Ranged: return !fired;
                case PlayerActionState.Melee: return atk != null && t >= atk.dodgeCancelAfter;
                default: return false;
            }
        }

        public void LockForArtifact(float seconds, string clip)
        {
            if (State == PlayerActionState.Melee) EndMelee();
            State = PlayerActionState.Artifact;
            t = 0f;
            lockDuration = Mathf.Max(0.05f, seconds);
            motor.SetMoveIntent(Vector3.zero);
            if (!string.IsNullOrEmpty(clip)) anim?.Play(clip, 1f);
        }

        void Deny(string reason)
        {
            Denied?.Invoke(reason);
            Services.Audio?.PlayUi("denied");
        }

        // ------------------------------------------------------------ Reações

        void OnDamaged(Actor a, DamageResult r)
        {
            if (State == PlayerActionState.Defeated || r.killed) return;
            if ((r.request.flags & DamageFlags.NoReaction) != 0) return;
            if (r.request.stagger >= 3f && State != PlayerActionState.Dodge)
            {
                CancelCurrent();
                State = PlayerActionState.HitReact;
                t = 0f;
                anim?.Play("hit_heavy", 1f);
            }
            else if (State == PlayerActionState.Free)
            {
                anim?.Play("hit_light", 1f);
            }
            LevelContext.Current?.CameraRig?.AddShake(0.08f + Mathf.Min(0.12f, r.amount / Mathf.Max(1f, actor.Receiver.MaxHealth)));
        }

        void CancelCurrent()
        {
            if (State == PlayerActionState.Melee) EndMelee();
            if (motor.IsForced && !motor.IsLeaping) motor.CancelForced();
        }

        void CancelToFree()
        {
            CancelCurrent();
            State = PlayerActionState.Free;
        }

        public void SetDefeated()
        {
            CancelCurrent();
            motor.CancelForced();
            State = PlayerActionState.Defeated;
            t = 0f;
            anim?.Play("death", 1f, true);
        }

        public void ResetState(bool resetCooldowns)
        {
            CancelCurrent();
            State = PlayerActionState.Free;
            t = 0f;
            comboGrace = 0f;
            fireCooldown = 0f;
            dodgeCooldown = 0f;
            if (resetCooldowns) PotionCooldown = 0f;
            anim?.ResetPose();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (PotionCooldown > 0f) PotionCooldown = Mathf.Max(0f, PotionCooldown - dt);
            if (dodgeCooldown > 0f) dodgeCooldown -= dt;
            if (fireCooldown > 0f) fireCooldown -= dt;
            if (comboGrace > 0f) comboGrace -= dt;

            if (State == PlayerActionState.Defeated) return;

            if (actor.Stats.Stunned && State != PlayerActionState.Stunned)
            {
                CancelCurrent();
                State = PlayerActionState.Stunned;
                t = 0f;
            }

            if (actor.Hitstop > 0f) return;

            switch (State)
            {
                case PlayerActionState.Melee: UpdateMelee(dt); break;
                case PlayerActionState.Ranged: UpdateRanged(dt); break;
                case PlayerActionState.Dodge:
                    t += dt;
                    if (!motor.IsForced && t >= def.dodgeDuration + def.dodgeRecovery)
                    {
                        State = PlayerActionState.Free;
                        dodgeCooldown = def.dodgeCooldown;
                    }
                    break;
                case PlayerActionState.Artifact:
                    t += dt;
                    if (t >= lockDuration && !motor.IsLeaping) State = PlayerActionState.Free;
                    break;
                case PlayerActionState.HitReact:
                    t += dt;
                    if (t >= 0.32f) State = PlayerActionState.Free;
                    break;
                case PlayerActionState.Stunned:
                    t += dt;
                    if (!actor.Stats.Stunned) State = PlayerActionState.Free;
                    break;
            }
        }
    }
}

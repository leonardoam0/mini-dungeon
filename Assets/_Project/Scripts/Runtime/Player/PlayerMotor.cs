using System;
using UnityEngine;
using UnityEngine.AI;

namespace Ruinas
{
    /// <summary>
    /// Autoridade ÚNICA de locomoção do jogador (sem root motion): aceleração, gravidade, giro, movimentos
    /// forçados (esquiva, salto do artefato, avanço do golpe), empurrão e recuperação de quedas.
    /// Tudo em função de deltaTime (independente da taxa de quadros).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour, IKnockbackTarget
    {
        enum Forced { None, Dodge, Leap, Lunge }

        CharacterController cc;
        Actor actor;
        PlayerDefinition def;

        Vector3 velocity;
        float verticalVelocity;
        Vector3 knockback;
        Vector3 desiredDir;
        float desiredSpeedMul;
        Vector3 faceDir;
        bool snapFacing;

        Forced forced;
        Vector3 forcedDir;
        float forcedDistance;
        float forcedDuration;
        float forcedT;
        float forcedTravelled;
        float leapHeight;
        Action onForcedDone;

        float safeTimer;
        public float KillY = -30f;

        public Vector3 Velocity => velocity;
        public bool Grounded { get; private set; }
        public bool IsForced => forced != Forced.None;
        public bool IsLeaping => forced == Forced.Leap;
        public float MaxSpeed => actor != null ? actor.Stats.MoveSpeed : 4.6f;
        public Vector3 LastSafePosition { get; private set; }
        /// <summary>Altura visual do salto (aplicada ao modelo, não ao colisor).</summary>
        public float VisualLift { get; private set; }

        public void Init(PlayerDefinition definition)
        {
            def = definition;
            cc = GetComponent<CharacterController>();
            actor = GetComponent<Actor>();
            actor.Knockback = this;
            faceDir = transform.forward;
            LastSafePosition = transform.position;
        }

        public void SetMoveIntent(Vector3 worldDir, float speedMul = 1f)
        {
            worldDir.y = 0f;
            desiredDir = worldDir.sqrMagnitude > 1f ? worldDir.normalized : worldDir;
            desiredSpeedMul = speedMul;
            if (desiredDir.sqrMagnitude > 0.01f && forced == Forced.None) faceDir = desiredDir.normalized;
        }

        public void FaceDirection(Vector3 dir, bool instant = false)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-4f) return;
            faceDir = dir.normalized;
            if (instant) transform.rotation = Quaternion.LookRotation(faceDir, Vector3.up);
        }

        public void StartDodge(Vector3 dir, float distance, float duration) => StartForced(Forced.Dodge, dir, distance, duration, 0f, null);

        public void StartLeap(Vector3 dir, float distance, float duration, float height, Action onLand) => StartForced(Forced.Leap, dir, distance, duration, height, onLand);

        public void Lunge(Vector3 dir, float distance, float duration)
        {
            if (forced != Forced.None || distance <= 0.01f) return;
            StartForced(Forced.Lunge, dir, distance, duration, 0f, null);
        }

        void StartForced(Forced kind, Vector3 dir, float distance, float duration, float height, Action done)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-4f) dir = transform.forward;
            forced = kind;
            forcedDir = dir.normalized;
            forcedDistance = distance;
            forcedDuration = Mathf.Max(0.05f, duration);
            forcedT = 0f;
            forcedTravelled = 0f;
            leapHeight = height;
            onForcedDone = done;
            faceDir = forcedDir;
            snapFacing = kind != Forced.Lunge;
        }

        public void CancelForced()
        {
            forced = Forced.None;
            VisualLift = 0f;
            onForcedDone = null;
        }

        public void ApplyKnockback(Vector3 v)
        {
            v.y = 0f;
            knockback += v;
            if (knockback.magnitude > 12f) knockback = knockback.normalized * 12f;
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            cc.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            cc.enabled = true;
            velocity = Vector3.zero;
            knockback = Vector3.zero;
            verticalVelocity = 0f;
            CancelForced();
            faceDir = rotation * Vector3.forward;
            LastSafePosition = position;
        }

        public void Stop()
        {
            velocity = Vector3.zero;
            desiredDir = Vector3.zero;
            knockback = Vector3.zero;
        }

        void Update()
        {
            if (cc == null || !cc.enabled) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            bool frozen = actor != null && actor.Hitstop > 0f;

            Vector3 horizontal;
            if (forced != Forced.None)
            {
                if (!frozen) forcedT += dt;
                float u = Mathf.Clamp01(forcedT / forcedDuration);
                float eased = forced == Forced.Dodge ? 1f - (1f - u) * (1f - u) : forced == Forced.Leap ? u : Mathf.Sin(u * Mathf.PI * 0.5f);
                float targetDist = forcedDistance * eased;
                float step = Mathf.Max(0f, targetDist - forcedTravelled);
                horizontal = frozen ? Vector3.zero : forcedDir * (step / dt);
                forcedTravelled = targetDist;
                VisualLift = forced == Forced.Leap ? Mathf.Sin(u * Mathf.PI) * leapHeight : 0f;
                velocity = forced == Forced.Lunge ? Vector3.zero : forcedDir * (forcedDistance / forcedDuration);
                if (u >= 1f)
                {
                    var done = onForcedDone;
                    forced = Forced.None;
                    VisualLift = 0f;
                    onForcedDone = null;
                    velocity *= 0.3f;
                    done?.Invoke();
                }
            }
            else
            {
                float maxSpeed = MaxSpeed * Mathf.Max(0f, desiredSpeedMul);
                Vector3 target = desiredDir * maxSpeed;
                float rate = target.sqrMagnitude > velocity.sqrMagnitude ? def.acceleration : def.deceleration;
                if (actor != null && actor.Stats.Stunned) target = Vector3.zero;
                velocity = frozen ? velocity : Vector3.MoveTowards(velocity, target, rate * dt);
                horizontal = frozen ? Vector3.zero : velocity;
            }

            horizontal += knockback;
            knockback = Vector3.MoveTowards(knockback, Vector3.zero, 26f * dt);

            if (Grounded && verticalVelocity < 0f) verticalVelocity = -3f;
            else verticalVelocity -= def.gravity * dt;

            var flags = cc.Move((horizontal + Vector3.up * verticalVelocity) * dt);
            Grounded = (flags & CollisionFlags.Below) != 0 || cc.isGrounded;
            if (Grounded && verticalVelocity < 0f) verticalVelocity = -3f;

            if (faceDir.sqrMagnitude > 0.01f)
            {
                var targetRot = Quaternion.LookRotation(faceDir, Vector3.up);
                transform.rotation = snapFacing && forced != Forced.None
                    ? targetRot
                    : Quaternion.RotateTowards(transform.rotation, targetRot, def.turnSpeed * dt);
            }

            TrackSafety(dt);
        }

        void TrackSafety(float dt)
        {
            if (transform.position.y < KillY)
            {
                // Queda para fora do cenário: volta ao último ponto seguro com pequena penalidade.
                var ctx = LevelContext.Current;
                if (ctx != null && actor != null && !actor.IsDead)
                    ctx.Combat.Apply(new DamageRequest { target = actor.Receiver, amount = actor.Receiver.MaxHealth * 0.1f, kind = DamageKind.Fall, flags = DamageFlags.NoReaction | DamageFlags.IgnoreArmor, sourceTag = "fall" });
                Teleport(LastSafePosition, transform.rotation);
                return;
            }

            safeTimer -= dt;
            if (safeTimer > 0f || !Grounded || forced != Forced.None) return;
            safeTimer = 0.4f;
            if (NavMesh.SamplePosition(transform.position, out var hit, 0.6f, NavMesh.AllAreas))
                LastSafePosition = hit.position;
        }
    }
}

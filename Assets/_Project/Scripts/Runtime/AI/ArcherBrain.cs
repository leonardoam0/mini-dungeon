using UnityEngine;
using UnityEngine.AI;

namespace Ruinas
{
    /// <summary>
    /// Atirador: procura posição com linha de visão na distância preferida, recua se o jogador chega perto,
    /// mira (preparação visível) e dispara um projétil com pequena antecipação do movimento.
    /// </summary>
    public class ArcherBrain : EnemyBrain
    {
        float aimT = -1f;
        Vector3 retreatPoint;

        protected override void Think()
        {
            switch (State)
            {
                case EnemyState.Idle:
                    if (target != null) SetState(EnemyState.Alert);
                    break;
                case EnemyState.Chase:
                case EnemyState.Position:
                    if (target == null) { ReleaseSlot(); SetState(EnemyState.Returning); break; }
                    float d = FlatDistance(target.transform.position);
                    bool los = HasLineOfSight(target.Center);
                    if (Def.retreatRange > 0f && d < Def.retreatRange && attackCooldown > -0.1f)
                    {
                        Vector3 away = transform.position - target.transform.position;
                        away.y = 0f;
                        Vector3 p = transform.position + away.normalized * 3.5f;
                        if (NavMesh.SamplePosition(p, out var hit, 2f, NavMesh.AllAreas)) { retreatPoint = hit.position; MoveTo(retreatPoint); }
                        SetState(EnemyState.Position);
                        break;
                    }
                    if (los && d <= Def.preferredRange + 2.5f && attackCooldown <= 0f)
                    {
                        StartAim();
                        break;
                    }
                    Vector3 slotPos = RequestSlotPosition(true);
                    if (!los || d > Def.preferredRange + 2.5f) MoveTo(slotPos);
                    else StopAgent();
                    break;
                case EnemyState.Returning:
                    if (target != null) { SetState(EnemyState.Chase); break; }
                    MoveTo(home);
                    if (FlatDistance(home) < 1f) SetState(EnemyState.Idle);
                    break;
            }
        }

        void StartAim()
        {
            SetState(EnemyState.Windup);
            aimT = 0f;
            anim?.Play("bow_draw", 0.3f / Mathf.Max(0.1f, Def.aimTime));
            Services.Audio?.Play("bow_draw", transform.position, 0.7f);
        }

        protected override void OnEnterState(EnemyState s)
        {
            if (s != EnemyState.Windup) aimT = -1f;
        }

        protected override void Tick(float dt)
        {
            if (aimT < 0f || State != EnemyState.Windup) return;
            StopAgent();
            aimT += dt;
            if (target != null)
            {
                Vector3 look = target.transform.position - transform.position;
                look.y = 0f;
                if (look.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(look), 720f * dt);
            }
            if (aimT < Def.aimTime) return;
            aimT = -1f;
            if (target != null && !target.IsDead && HasLineOfSight(target.Center))
            {
                Vector3 origin = transform.position + Vector3.up * 1.15f + transform.forward * 0.4f;
                Vector3 aim = target.Center + Vector3.down * 0.15f;
                var tm = target.GetComponent<PlayerMotor>();
                if (tm != null && Def.projectile != null)
                {
                    float flight = Vector3.Distance(origin, aim) / Mathf.Max(1f, Def.projectile.speed);
                    aim += tm.Velocity * flight * 0.5f;
                }
                ctx.Projectiles.Spawn(Def.projectile, Actor, Team.Enemy, origin, aim - origin, Def.projectileDamage);
                anim?.Play("bow_release", 1f);
            }
            attackCooldown = Def.attackCooldown * ctx.Random.Range(0.85f, 1.25f);
            SetState(EnemyState.Chase);
        }

        protected override void OnDamaged(Actor a, DamageResult r)
        {
            base.OnDamaged(a, r);
            if (State == EnemyState.React) aimT = -1f;
        }
    }
}

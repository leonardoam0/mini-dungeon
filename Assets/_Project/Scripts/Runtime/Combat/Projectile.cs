using System.Collections.Generic;
using UnityEngine;

namespace Ruinas
{
    /// <summary>
    /// Projétil com colisão contínua (SphereCast por passo). O dano só é aplicado no impacto, que coincide
    /// com o efeito visual; aliados são atravessados.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        public ProjectileDefinition Def { get; private set; }
        public Actor Owner { get; private set; }
        public Team Team { get; private set; }
        public float Damage;
        public DamageKind Kind = DamageKind.Ranged;
        public DamageFlags Flags = DamageFlags.CanCrit;
        public StatusApplication[] Statuses;
        public Vector3 Velocity;
        public float Life;
        public int PierceLeft;
        public int EventId;
        public float DamageScaleForArea = 1f;

        readonly HashSet<int> touched = new HashSet<int>();
        TrailRenderer trail;
        Transform visual;

        public void Setup(ProjectileDefinition def, Transform visualRoot)
        {
            Def = def;
            visual = visualRoot;
            trail = GetComponentInChildren<TrailRenderer>(true);
        }

        public void Launch(Actor owner, Team team, Vector3 position, Vector3 velocity, float damage, int eventId, int pierce, StatusApplication[] statuses)
        {
            Owner = owner;
            Team = team;
            transform.position = position;
            Velocity = velocity;
            Damage = damage;
            EventId = eventId;
            PierceLeft = pierce;
            Statuses = statuses;
            Life = 0f;
            touched.Clear();
            Orient();
            if (trail != null)
            {
                trail.Clear();
                trail.emitting = true;
            }
        }

        void Orient()
        {
            if (Def != null && Def.alignToVelocity && Velocity.sqrMagnitude > 1e-4f)
                transform.rotation = Quaternion.LookRotation(Velocity.normalized, Vector3.up);
        }

        bool IsHostile(Actor a)
        {
            if (a == null || a == Owner) return false;
            if (a.team == Team.Neutral || Team == Team.Neutral) return false;
            return a.team != Team;
        }

        /// <summary>Avança o projétil. Retorna false quando ele deve ser devolvido ao pool.</summary>
        public bool Step(float dt, LevelContext ctx)
        {
            if (dt <= 0f) return true;
            Velocity += Vector3.down * Def.gravity * dt;
            Vector3 move = Velocity * dt;
            float dist = move.magnitude;
            if (dist < 1e-5f) return true;
            Vector3 dir = move / dist;
            Vector3 pos = transform.position;
            int mask = Layers.EnvironmentMask | Layers.ActorsMask;

            for (int guard = 0; guard < 5 && dist > 0f; guard++)
            {
                if (!Physics.SphereCast(pos, Def.radius, dir, out var hit, dist, mask, QueryTriggerInteraction.Collide))
                {
                    pos += dir * dist;
                    dist = 0f;
                    break;
                }

                pos += dir * hit.distance;
                dist -= hit.distance;

                if (hit.collider.gameObject.layer != Layers.Actors)
                {
                    transform.position = pos;
                    Impact(ctx, hit.point, hit.normal, null);
                    return false;
                }

                var recv = hit.collider.GetComponentInParent<DamageReceiver>();
                var actor = recv != null ? recv.Actor : null;
                if (actor != null && !touched.Contains(actor.Id))
                {
                    touched.Add(actor.Id);
                    if (IsHostile(actor) && !recv.IsDead)
                    {
                        var req = new DamageRequest
                        {
                            attacker = Owner != null && Owner.isActiveAndEnabled ? Owner : null,
                            target = recv,
                            amount = Damage,
                            kind = Kind,
                            flags = Flags,
                            hitPoint = hit.point,
                            direction = dir,
                            knockback = Def.knockback,
                            stagger = Def.stagger,
                            hitstop = 0.03f,
                            eventId = EventId,
                            statuses = Statuses,
                            sourceTag = "proj:" + Def.id,
                        };
                        // Sem atacante vivo (ex.: arqueiro já derrotado) a hostilidade já foi resolvida pelo time do projétil.
                        ctx.Combat.Apply(req);
                        ctx.Vfx.Spawn(Def.impactVfx, hit.point, Quaternion.LookRotation(-dir));
                        Services.Audio?.Play(Def.impactSfx, hit.point);
                        if (PierceLeft <= 0)
                        {
                            transform.position = pos;
                            Impact(ctx, hit.point, -dir, actor, false);
                            return false;
                        }
                        PierceLeft--;
                    }
                }
                // Atravessa o ator (aliado ou já atingido) e continua.
                pos += dir * 0.08f;
                dist -= 0.08f;
            }

            transform.position = pos;
            Orient();
            Life += dt;
            if (Life >= Def.maxLifetime) return false;
            return true;
        }

        void Impact(LevelContext ctx, Vector3 point, Vector3 normal, Actor actor, bool playFx = true)
        {
            if (playFx)
            {
                ctx.Vfx.Spawn(Def.impactVfx, point, Quaternion.LookRotation(normal.sqrMagnitude > 0 ? normal : Vector3.up));
                Services.Audio?.Play(Def.impactSfx, point);
            }
            var area = Def.areaOnImpact;
            if (area != null && area.enabled)
            {
                Vector3 ground = point;
                if (Physics.Raycast(point + Vector3.up * 0.5f, Vector3.down, out var g, 4f, Layers.EnvironmentMask, QueryTriggerInteraction.Ignore))
                    ground = g.point;
                ctx.Areas.Spawn(Owner, Team, ground, area, DamageScaleForArea);
            }
        }

        public void OnReleased()
        {
            if (trail != null)
            {
                trail.emitting = false;
                trail.Clear();
            }
            Owner = null;
            Statuses = null;
        }
    }
}

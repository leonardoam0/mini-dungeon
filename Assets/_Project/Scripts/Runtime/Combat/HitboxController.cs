using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ruinas
{
    /// <summary>
    /// Detecção de contato corpo a corpo pela trajetória da arma: durante a janela ativa, o ângulo coberto
    /// avança de sweepFrom para sweepTo e só alvos dentro do arco já varrido, no alcance, com linha de visão
    /// livre (sem atravessar paredes) e ainda não atingidos neste evento recebem dano.
    /// </summary>
    [RequireComponent(typeof(Actor))]
    public class HitboxController : MonoBehaviour
    {
        static readonly Collider[] buffer = new Collider[48];

        Actor owner;
        AttackDefinition attack;
        float damage;
        int eventId;
        float elapsed;
        bool active;
        Vector3 facing;
        StatusApplication[] statuses;
        DamageFlags flags;
        DamageKind kind;
        readonly HashSet<int> hitIds = new HashSet<int>();

        public bool Active => active;
        public int HitsThisEvent => hitIds.Count;
        public event Action<DamageResult> Hit;

        void Awake() => owner = GetComponent<Actor>();

        public void Begin(AttackDefinition atk, float damageAmount, int evt, Vector3 facingDir,
            StatusApplication[] statusApps = null, DamageFlags extraFlags = DamageFlags.CanCrit, DamageKind damageKind = DamageKind.Melee)
        {
            attack = atk;
            damage = damageAmount;
            eventId = evt;
            elapsed = 0f;
            facingDir.y = 0f;
            facing = facingDir.sqrMagnitude > 1e-4f ? facingDir.normalized : transform.forward;
            statuses = statusApps;
            flags = extraFlags;
            kind = damageKind;
            hitIds.Clear();
            active = atk != null;
            if (active) Sweep();
        }

        public void End()
        {
            active = false;
            attack = null;
        }

        void Update()
        {
            if (!active || owner.IsDead) return;
            if (owner.Hitstop > 0f) return;
            elapsed += Time.deltaTime;
            Sweep();
        }

        void Sweep()
        {
            var ctx = LevelContext.Current;
            if (ctx == null || attack == null) return;
            float dur = Mathf.Max(0.01f, attack.activeEnd - attack.activeStart);
            float u = Mathf.Clamp01(elapsed / dur);
            float current = Mathf.Lerp(attack.sweepFrom, attack.sweepTo, u);
            float minA = Mathf.Min(attack.sweepFrom, current) - 10f;
            float maxA = Mathf.Max(attack.sweepFrom, current) + 10f;

            Vector3 origin = owner.transform.position;
            float reach = attack.areaAroundSelf ? attack.areaRadius : attack.range;
            int n = Physics.OverlapSphereNonAlloc(origin + Vector3.up * attack.height, reach + 1.5f, buffer, Layers.ActorsMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < n; i++)
            {
                var recv = buffer[i].GetComponentInParent<DamageReceiver>();
                if (recv == null || recv.IsDead) continue;
                var t = recv.Actor;
                if (t == null || t == owner || !owner.IsHostileTo(t) || hitIds.Contains(t.Id)) continue;

                Vector3 to = t.transform.position - origin;
                float dy = to.y;
                to.y = 0f;
                float centerDist = to.magnitude;
                float edgeDist = centerDist - t.radius;
                if (Mathf.Abs(dy) > 1.8f) continue;

                if (attack.areaAroundSelf)
                {
                    if (edgeDist > attack.areaRadius) continue;
                }
                else
                {
                    if (edgeDist > attack.range) continue;
                    if (centerDist > 0.55f)
                    {
                        float ang = Vector3.SignedAngle(facing, to, Vector3.up);
                        if (ang < minA || ang > maxA) continue;
                    }
                }

                // Sem acerto através de paredes: linha de visão contra o cenário.
                if (Physics.Linecast(owner.Center, t.Center, Layers.EnvironmentMask, QueryTriggerInteraction.Ignore)) continue;

                hitIds.Add(t.Id);
                var req = new DamageRequest
                {
                    attacker = owner,
                    target = recv,
                    amount = damage,
                    kind = kind,
                    flags = flags,
                    hitPoint = t.Center - to.normalized * t.radius * 0.8f,
                    direction = centerDist > 0.01f ? to / centerDist : facing,
                    knockback = attack.knockback,
                    stagger = attack.stagger,
                    hitstop = attack.hitstop,
                    eventId = eventId,
                    statuses = statuses,
                    sourceTag = "melee:" + attack.id,
                };
                var res = ctx.Combat.Apply(req);
                if (res.applied) Hit?.Invoke(res);
            }
        }
    }
}

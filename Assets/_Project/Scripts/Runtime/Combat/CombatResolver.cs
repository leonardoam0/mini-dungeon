using UnityEngine;

namespace Ruinas
{
    /// <summary>
    /// Fluxo ÚNICO de dano e cura da cena: hostilidade, deduplicação por evento, multiplicadores,
    /// crítico, armadura, status, reação (empurrão, pausa de impacto, flash) e eventos para HUD/áudio/VFX.
    /// </summary>
    public class CombatResolver : MonoBehaviour
    {
        readonly HitRegistry registry = new HitRegistry();
        int nextEventId = 1;
        GameRandom rng;
        float pruneTimer;
        LevelContext ctx;

        public int RegisteredHits => registry.Count;

        public void Init(LevelContext context, GameRandom random)
        {
            ctx = context;
            rng = random;
            registry.Clear();
        }

        public int NewEventId()
        {
            nextEventId++;
            if (nextEventId == 0 || nextEventId == int.MaxValue) nextEventId = 1;
            return nextEventId;
        }

        public DamageResult Apply(DamageRequest req)
        {
            var res = new DamageResult { request = req };
            var target = req.target;
            if (target == null || !target.CanBeDamaged) return res;
            var t = target.Actor;
            if (t == null) return res;
            bool fromStatus = (req.flags & DamageFlags.FromStatus) != 0;
            if (req.attacker != null && !fromStatus && !req.attacker.IsHostileTo(t)) return res;
            if (!registry.TryRegister(req.eventId, t.Id, Time.time)) return res;

            var a = req.attacker;
            float attackerMult = a != null ? a.Stats.DamageDealtMultiplier(req.kind) : 1f;
            bool crit = (req.flags & DamageFlags.CanCrit) != 0 && a != null && rng != null && rng.Chance(a.Stats.critChance);
            float amount = DamageMath.Compute(req.amount, attackerMult, crit, a != null ? a.Stats.critMultiplier : 1f,
                t.Stats.DamageTakenMultiplier, t.Stats.ArmorReduction, (req.flags & DamageFlags.IgnoreArmor) != 0);
            int final = DamageMath.Round(amount);
            if (final <= 0) return res;

            bool killed = target.TakeDamage(final);
            res.applied = true;
            res.amount = final;
            res.crit = crit;
            res.killed = killed;
            res.healthAfter = target.Health;

            if (!killed && req.statuses != null && t.Status != null)
            {
                foreach (var s in req.statuses)
                {
                    if (s.effect == null) continue;
                    float chance = s.chance <= 0f ? 1f : s.chance;
                    if (rng == null || rng.Chance(chance)) t.Status.Apply(s.effect, a, s.duration, s.potency);
                }
            }

            if ((req.flags & DamageFlags.NoReaction) == 0)
            {
                Vector3 dir = req.direction;
                if (dir.sqrMagnitude < 1e-4f && a != null) dir = t.transform.position - a.transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 1e-4f && req.knockback > 0f && t.Knockback != null)
                    t.Knockback.ApplyKnockback(dir.normalized * req.knockback * (killed ? 1.3f : 1f));
                if (req.hitstop > 0f)
                {
                    t.AddHitstop(req.hitstop);
                    if (a != null) a.AddHitstop(req.hitstop * 0.75f);
                }
                t.Anim?.Flash(crit ? 1f : 0.85f);
            }
            else
            {
                Color tint = req.kind == DamageKind.Poison ? new Color(0.45f, 1f, 0.35f) : req.kind == DamageKind.Fire ? new Color(1f, 0.6f, 0.2f) : Color.white;
                t.Anim?.Flash(0.35f, tint);
            }

            if ((req.flags & (DamageFlags.FromStatus | DamageFlags.Area)) == 0)
            {
                string sfx = req.kind == DamageKind.Melee ? "hit_flesh" : req.kind == DamageKind.Magic ? "hit_magic" : null;
                if (sfx != null) Services.Audio?.Play(sfx, req.hitPoint, crit ? 1.15f : 1f);
            }

            t.NotifyDamaged(res);
            if (a != null) a.NotifyDealtHit(res);
            ctx?.Events.RaiseDamaged(res);
            if (killed)
            {
                t.NotifyDied(res);
                ctx?.Events.RaiseKilled(t, a, res);
            }
            return res;
        }

        public float Heal(Actor target, float amount, string source)
        {
            if (target == null || target.Receiver == null) return 0f;
            float healed = target.Receiver.Heal(amount);
            if (healed > 0f) ctx?.Events.RaiseHealed(target, healed, source);
            return healed;
        }

        public void ResetRegistry() => registry.Clear();

        void Update()
        {
            pruneTimer -= Time.unscaledDeltaTime;
            if (pruneTimer <= 0f)
            {
                pruneTimer = 1f;
                registry.Prune(Time.time);
            }
        }
    }
}

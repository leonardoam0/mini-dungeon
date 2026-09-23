using UnityEngine;

namespace Ruinas
{
    /// <summary>
    /// Região de dano periódico (nuvens verdes). O visual usa o mesmo raio da região e começa a
    /// dissipar no instante em que o dano é desativado — não sugere perigo depois do fim.
    /// </summary>
    public class AreaEffect : MonoBehaviour
    {
        static readonly Collider[] buffer = new Collider[32];

        public Team Team { get; private set; }
        public Actor Owner { get; private set; }
        public AreaEffectSpec Spec { get; private set; }
        public bool DamageActive { get; private set; }
        public float Radius => Spec != null ? Spec.radius : 0f;

        float elapsed;
        float tickTimer;
        float damageScale = 1f;
        float releaseTimer = -1f;
        PooledVfx visual;
        Telegraph ring;
        AreaEffectSystem system;

        public void Begin(AreaEffectSystem owner, Actor source, Team team, Vector3 position, AreaEffectSpec spec, float scale)
        {
            system = owner;
            Owner = source;
            Team = team;
            Spec = spec;
            damageScale = scale;
            transform.position = position;
            elapsed = 0f;
            tickTimer = 0.25f;
            releaseTimer = -1f;
            DamageActive = true;

            var ctx = LevelContext.Current;
            if (ctx != null)
            {
                visual = ctx.Vfx.Spawn(spec.visualKey, position, Quaternion.identity, spec.duration + 0.6f,
                    new VfxParams { radius = spec.radius, duration = spec.duration, hasColor = false });
                // Nuvens hostis ao jogador recebem um anel tracejado: forma, além da cor, indica perigo.
                if (team == Team.Enemy && spec.hostileTint)
                    ring = ctx.Vfx.ShowTelegraph(position + Vector3.up * 0.03f, spec.radius, spec.duration, new Color(0.55f, 1f, 0.35f, 0.9f), true, false);
            }
        }

        void Update()
        {
            if (Spec == null) return;
            float dt = Time.deltaTime;
            elapsed += dt;

            if (DamageActive)
            {
                tickTimer -= dt;
                if (tickTimer <= 0f)
                {
                    tickTimer += Mathf.Max(0.1f, Spec.tickInterval);
                    DoTick();
                }
                if (elapsed >= Spec.duration) EndDamage();
            }
            else if (releaseTimer >= 0f)
            {
                releaseTimer -= dt;
                if (releaseTimer < 0f) system.Release(this);
            }
        }

        void DoTick()
        {
            var ctx = LevelContext.Current;
            if (ctx == null) return;
            Vector3 c = transform.position;
            int n = Physics.OverlapSphereNonAlloc(c + Vector3.up * 0.8f, Spec.radius + 1f, buffer, Layers.ActorsMask, QueryTriggerInteraction.Collide);
            int evt = ctx.Combat.NewEventId();
            for (int i = 0; i < n; i++)
            {
                var recv = buffer[i].GetComponentInParent<DamageReceiver>();
                if (recv == null || recv.IsDead || recv.Actor == null) continue;
                var a = recv.Actor;
                if (a.team == Team || a.team == Team.Neutral || Team == Team.Neutral) continue;
                Vector3 d = a.transform.position - c;
                if (Mathf.Abs(d.y) > 2f) continue;
                d.y = 0f;
                if (d.magnitude > Spec.radius + a.radius * 0.5f) continue;
                ctx.Combat.Apply(new DamageRequest
                {
                    attacker = Owner != null && Owner.isActiveAndEnabled ? Owner : null,
                    target = recv,
                    amount = Spec.damagePerTick * damageScale,
                    kind = Spec.kind,
                    flags = DamageFlags.NoReaction | DamageFlags.Area,
                    hitPoint = a.Center,
                    eventId = evt,
                    statuses = Spec.status.effect != null ? new[] { Spec.status } : null,
                    sourceTag = "area:" + Spec.visualKey,
                });
            }
        }

        public void EndDamage()
        {
            if (!DamageActive) return;
            DamageActive = false;
            if (visual != null) visual.StopAndRelease(0.35f);
            visual = null;
            if (ring != null) ring.Hide();
            ring = null;
            releaseTimer = 0.4f;
        }

        public void ForceRelease()
        {
            DamageActive = false;
            if (visual != null) visual.StopAndRelease(0.05f);
            visual = null;
            if (ring != null) ring.Hide();
            ring = null;
            Spec = null;
            Owner = null;
        }
    }
}

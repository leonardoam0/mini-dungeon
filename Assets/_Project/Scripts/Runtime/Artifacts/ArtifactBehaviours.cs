using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Ruinas
{
    /// <summary>
    /// Mobilidade — "Pena Saltadora": salto curto na direção da mira; na aterrissagem, onda que empurra e
    /// atordoa inimigos próximos. O deslocamento respeita paredes (é feito pelo motor com colisão).
    /// </summary>
    public class FeatherLeapArtifact : IArtifactBehaviour
    {
        ArtifactUse use;
        bool active;
        readonly List<Actor> scratch = new List<Actor>();
        public bool IsActive => active;

        public void Activate(in ArtifactUse u)
        {
            use = u;
            active = true;
            var pc = u.player;
            Vector3 dir = u.aimDir.sqrMagnitude > 1e-4f ? u.aimDir : pc.transform.forward;
            float dist = u.def.distance;
            // Limita a distância ao ponto mirado quando ele está mais perto.
            Vector3 toAim = u.aimPoint - pc.transform.position;
            toAim.y = 0f;
            if (toAim.magnitude > 1f) dist = Mathf.Min(dist, toAim.magnitude);
            pc.Actions.LockForArtifact(0.5f, "leap");
            pc.Motor.StartLeap(dir, dist, 0.46f, 1.35f, Land);
            Services.Audio?.Play(u.def.sfxStart, pc.transform.position);
            LevelContext.Current?.Vfx.Spawn("feather_burst", pc.transform.position + Vector3.up * 0.4f, Quaternion.identity);
        }

        void Land()
        {
            active = false;
            var ctx = LevelContext.Current;
            var pc = use.player;
            if (ctx == null || pc == null || pc.IsDefeated) return;
            Vector3 c = pc.transform.position;
            ctx.Vfx.Spawn("shockwave_ring", c + Vector3.up * 0.05f, Quaternion.identity, 0f, new VfxParams { radius = use.def.radius, color = new Color(0.9f, 0.95f, 1f), hasColor = true });
            ctx.Vfx.Spawn("dust_puff", c, Quaternion.identity);
            Services.Audio?.Play(use.def.sfxEnd, c);
            ctx.CameraRig?.AddShake(0.12f);
            ctx.Actors.HostilesInRadius(pc.Actor, c, use.def.radius, scratch);
            int evt = ctx.Combat.NewEventId();
            foreach (var a in scratch)
            {
                Vector3 dir = a.transform.position - c;
                dir.y = 0f;
                ctx.Combat.Apply(new DamageRequest
                {
                    attacker = pc.Actor,
                    target = a.Receiver,
                    amount = use.def.damage * use.power,
                    kind = DamageKind.Magic,
                    flags = DamageFlags.None,
                    hitPoint = a.Center,
                    direction = dir.sqrMagnitude > 1e-4f ? dir.normalized : pc.transform.forward,
                    knockback = use.def.knockback,
                    stagger = 3f,
                    hitstop = 0.04f,
                    eventId = evt,
                    statuses = use.def.applyStatus.effect != null ? new[] { use.def.applyStatus } : null,
                    sourceTag = "artifact:" + use.def.id,
                });
            }
        }

        public void Tick(float dt) { }

        public void Cancel()
        {
            if (active && use.player != null && use.player.Motor.IsLeaping) use.player.Motor.CancelForced();
            active = false;
        }
    }

    /// <summary>
    /// Controle/dano em área — "Sino do Pulso": expansão rápida de uma esfera magenta. Cada inimigo é
    /// atingido UMA vez, no instante em que a frente da esfera o alcança (dano, empurrão e vulnerabilidade).
    /// </summary>
    public class MagentaPulseArtifact : IArtifactBehaviour
    {
        const float Windup = 0.14f;
        const float GrowTime = 0.3f;

        ArtifactUse use;
        bool active;
        float t;
        bool released;
        Vector3 center;
        int eventId;
        readonly HashSet<int> hit = new HashSet<int>();
        readonly List<Actor> scratch = new List<Actor>();

        public bool IsActive => active;

        public void Activate(in ArtifactUse u)
        {
            use = u;
            active = true;
            released = false;
            t = 0f;
            hit.Clear();
            // Trava curta: no trecho de referência o herói continua andando enquanto o pulso se expande.
            u.player.Actions.LockForArtifact(0.08f, "cast");
            Services.Audio?.Play(u.def.sfxStart, u.player.transform.position);
            LevelContext.Current?.Vfx.SpawnAttached("magic_charge", u.player.transform, Vector3.up * 1.2f, Windup + 0.1f,
                new VfxParams { color = u.def.color, hasColor = true });
        }

        public void Tick(float dt)
        {
            if (!active) return;
            var ctx = LevelContext.Current;
            var pc = use.player;
            if (ctx == null || pc == null || pc.IsDefeated) { active = false; return; }
            t += dt;
            if (!released && t >= Windup)
            {
                released = true;
                center = pc.transform.position;
                eventId = ctx.Combat.NewEventId();
                ctx.Vfx.Spawn("pulse_magenta", center, Quaternion.identity, 1.2f, new VfxParams { radius = use.def.radius });
                ctx.Vfx.Spawn("beam_magenta", center, Quaternion.identity, 1.1f, new VfxParams { height = 9f, duration = 1.1f, color = new Color(2.4f, 0.55f, 2.8f), hasColor = true });
                Services.Audio?.Play(use.def.sfxEnd, center);
                ctx.CameraRig?.AddShake(0.1f);
            }
            if (!released) return;

            float u = Mathf.Clamp01((t - Windup) / GrowTime);
            float front = Mathf.Lerp(0.3f, use.def.radius, 1f - (1f - u) * (1f - u) * (1f - u));
            ctx.Actors.HostilesInRadius(pc.Actor, center, front, scratch);
            foreach (var a in scratch)
            {
                if (!hit.Add(a.Id)) continue;
                Vector3 dir = a.transform.position - center;
                dir.y = 0f;
                ctx.Combat.Apply(new DamageRequest
                {
                    attacker = pc.Actor,
                    target = a.Receiver,
                    amount = use.def.damage * use.power,
                    kind = DamageKind.Magic,
                    flags = DamageFlags.CanCrit,
                    hitPoint = a.Center,
                    direction = dir.sqrMagnitude > 1e-4f ? dir.normalized : Vector3.forward,
                    knockback = use.def.knockback,
                    stagger = 2.5f,
                    hitstop = 0.05f,
                    eventId = eventId,
                    statuses = use.def.applyStatus.effect != null ? new[] { use.def.applyStatus } : null,
                    sourceTag = "artifact:" + use.def.id,
                });
            }
            if (u >= 1f) active = false;
        }

        public void Cancel() => active = false;
    }

    /// <summary>
    /// Suporte/invocação — "Feixe Dourado": chama uma criatura acompanhante de manta vermelha que segue
    /// o jogador e cospe em inimigos. Enquanto ela existe, o slot fica destacado no HUD.
    /// </summary>
    public class SummonCompanionArtifact : IArtifactBehaviour
    {
        CompanionBrain companion;
        public bool IsActive => companion != null && companion.IsAlive;

        public void Activate(in ArtifactUse u)
        {
            var ctx = LevelContext.Current;
            var db = Services.Database;
            var prefab = u.def.summonPrefab != null ? u.def.summonPrefab : db?.companionPrefab;
            if (ctx == null || prefab == null) return;
            var pc = u.player;
            Vector3 side = Quaternion.Euler(0f, -110f, 0f) * pc.transform.forward;
            Vector3 p = pc.transform.position + side * 1.8f;
            if (NavMesh.SamplePosition(p, out var hit, 2.5f, NavMesh.AllAreas)) p = hit.position;
            else p = pc.transform.position;
            var go = Object.Instantiate(prefab, p, Quaternion.LookRotation(pc.transform.forward));
            companion = go.GetComponent<CompanionBrain>();
            if (companion != null) companion.Init(pc, u.def.duration > 0f ? u.def.duration : 25f, u.power * pc.Actor.Stats.globalDamage);
            pc.Actions.LockForArtifact(0.3f, "cast");
            ctx.Vfx.Spawn("summon_puff", p, Quaternion.identity);
            Services.Audio?.Play(u.def.sfxStart, p);
        }

        public void Tick(float dt) { }

        public void Cancel()
        {
            if (companion != null) companion.Dismiss();
            companion = null;
        }
    }
}

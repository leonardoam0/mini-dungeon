using System.Collections.Generic;
using UnityEngine;

namespace Ruinas
{
    /// <summary>Pool e simulação de projéteis da cena.</summary>
    public class ProjectileSystem : MonoBehaviour
    {
        readonly Dictionary<ProjectileDefinition, Stack<Projectile>> pools = new Dictionary<ProjectileDefinition, Stack<Projectile>>();
        readonly List<Projectile> active = new List<Projectile>();
        LevelContext ctx;
        Transform poolRoot;

        public int ActiveCount => active.Count;

        public void Init(LevelContext context)
        {
            ctx = context;
            poolRoot = new GameObject("[Projeteis]").transform;
            poolRoot.SetParent(transform, false);
        }

        public Projectile Spawn(ProjectileDefinition def, Actor owner, Team team, Vector3 position, Vector3 direction,
            float damage, float speedMultiplier = 1f, StatusApplication[] statuses = null, int extraPierce = 0)
        {
            if (def == null) return null;
            var p = Get(def);
            direction = direction.sqrMagnitude > 1e-4f ? direction.normalized : Vector3.forward;
            int evt = ctx.Combat.NewEventId();
            p.Launch(owner, team, position, direction * def.speed * speedMultiplier, damage, evt, def.pierce + extraPierce, statuses ?? def.statuses);
            p.DamageScaleForArea = owner != null ? owner.Stats.globalDamage : 1f;
            active.Add(p);
            if (!string.IsNullOrEmpty(def.launchSfx)) Services.Audio?.Play(def.launchSfx, position);
            return p;
        }

        /// <summary>Lançamento com velocidade explícita (arcos balísticos com gravidade).</summary>
        public Projectile SpawnWithVelocity(ProjectileDefinition def, Actor owner, Team team, Vector3 position, Vector3 velocity, float damage)
        {
            if (def == null) return null;
            var p = Get(def);
            int evt = ctx.Combat.NewEventId();
            p.Launch(owner, team, position, velocity, damage, evt, def.pierce, def.statuses);
            p.DamageScaleForArea = owner != null ? owner.Stats.globalDamage : 1f;
            active.Add(p);
            if (!string.IsNullOrEmpty(def.launchSfx)) Services.Audio?.Play(def.launchSfx, position);
            return p;
        }

        Projectile Get(ProjectileDefinition def)
        {
            if (!pools.TryGetValue(def, out var stack))
            {
                stack = new Stack<Projectile>();
                pools[def] = stack;
            }
            Projectile p;
            if (stack.Count > 0)
            {
                p = stack.Pop();
                p.gameObject.SetActive(true);
                return p;
            }
            var go = new GameObject("Proj_" + def.id);
            go.layer = Layers.Projectile;
            go.transform.SetParent(poolRoot, false);
            Transform visual = null;
            if (def.visualPrefab != null)
            {
                var v = Instantiate(def.visualPrefab, go.transform);
                v.transform.localPosition = Vector3.zero;
                v.transform.localRotation = Quaternion.identity;
                visual = v.transform;
            }
            p = go.AddComponent<Projectile>();
            p.Setup(def, visual);
            return p;
        }

        void Update()
        {
            if (ctx == null) return;
            float dt = Time.deltaTime;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var p = active[i];
                if (p == null) { active.RemoveAt(i); continue; }
                if (!p.Step(dt, ctx)) Release(i);
            }
        }

        void Release(int index)
        {
            var p = active[index];
            active.RemoveAt(index);
            p.OnReleased();
            p.gameObject.SetActive(false);
            pools[p.Def].Push(p);
        }

        public void ClearAll()
        {
            for (int i = active.Count - 1; i >= 0; i--) Release(i);
        }
    }
}

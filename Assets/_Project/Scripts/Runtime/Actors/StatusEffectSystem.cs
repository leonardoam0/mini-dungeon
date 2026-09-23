using UnityEngine;

namespace Ruinas
{
    /// <summary>
    /// Componente de status de um ator: aplica ticks pelo fluxo único de dano, recalcula modificadores
    /// e mantém/limpa os efeitos visuais correspondentes.
    /// </summary>
    [RequireComponent(typeof(Actor))]
    public class StatusEffectSystem : MonoBehaviour
    {
        public readonly StatusEffectCollection Effects = new StatusEffectCollection();
        Actor actor;

        void Awake()
        {
            actor = GetComponent<Actor>();
            Effects.Added += OnAdded;
            Effects.Removed += OnRemoved;
            Effects.Changed += Recompute;
        }

        public void Apply(StatusEffectDefinition def, Actor source, float duration = 0f, float potency = 1f)
        {
            if (def == null || actor == null || actor.IsDead) return;
            Effects.Apply(def, source, duration, potency);
        }

        public bool Has(StatusKind kind) => Effects.Has(kind);

        public void ClearAll() => Effects.Clear();

        void Update()
        {
            if (actor.IsDead)
            {
                if (Effects.Entries.Count > 0) Effects.Clear();
                return;
            }
            Effects.Tick(Time.deltaTime, OnTick);
        }

        void OnTick(StatusEffectCollection.Entry e)
        {
            var ctx = LevelContext.Current;
            if (ctx == null) return;
            var d = e.def;
            float mult = e.potency * e.stacks;
            if (d.damagePerTick > 0f)
            {
                var req = new DamageRequest
                {
                    attacker = e.source != null && e.source.isActiveAndEnabled ? e.source : null,
                    target = actor.Receiver,
                    amount = d.damagePerTick * mult,
                    kind = d.damageKind,
                    flags = DamageFlags.FromStatus | DamageFlags.NoReaction | DamageFlags.IgnoreArmor,
                    hitPoint = actor.Center,
                    sourceTag = "status:" + d.id,
                };
                ctx.Combat.Apply(req);
            }
            if (d.healPerTick > 0f)
                ctx.Combat.Heal(actor, d.healPerTick * mult, "status:" + d.id);
        }

        void OnAdded(StatusEffectCollection.Entry e)
        {
            var ctx = LevelContext.Current;
            if (ctx != null && !string.IsNullOrEmpty(e.def.vfxKey))
                e.visual = ctx.Vfx.SpawnAttached(e.def.vfxKey, transform, Vector3.up * actor.centerHeight, -1f);
            if (e.def.stuns && actor.Anim != null) actor.Anim.Play("stunned", 1f);
        }

        void OnRemoved(StatusEffectCollection.Entry e)
        {
            if (e.visual is PooledVfx v && v != null) v.StopAndRelease();
            e.visual = null;
        }

        void Recompute()
        {
            Effects.Aggregate(actor.Stats.status);
        }

        void OnDisable()
        {
            // Limpeza garantida ao morrer, reiniciar ou trocar de cena.
            if (Effects.Entries.Count > 0) Effects.Clear();
        }
    }
}

using System;
using System.Collections.Generic;

namespace Ruinas
{
    /// <summary>
    /// Regras de status sem dependência de cena: aplicação conforme política de acúmulo, ticks, expiração
    /// e agregação de modificadores. Testado em EditMode.
    /// </summary>
    public class StatusEffectCollection
    {
        public class Entry
        {
            public StatusEffectDefinition def;
            public Actor source;
            public float remaining;
            public float tickTimer;
            public int stacks = 1;
            public float potency = 1f;
            public object visual;
        }

        public readonly List<Entry> Entries = new List<Entry>();
        public event Action<Entry> Added;
        public event Action<Entry> Removed;
        public event Action Changed;

        public Entry Find(StatusEffectDefinition def)
        {
            for (int i = 0; i < Entries.Count; i++) if (Entries[i].def == def) return Entries[i];
            return null;
        }

        public bool Has(StatusKind kind)
        {
            for (int i = 0; i < Entries.Count; i++) if (Entries[i].def != null && Entries[i].def.kind == kind) return true;
            return false;
        }

        public Entry Apply(StatusEffectDefinition def, Actor source, float duration, float potency = 1f)
        {
            if (def == null) return null;
            if (duration <= 0f) duration = def.defaultDuration;
            if (potency <= 0f) potency = 1f;
            var e = Find(def);
            if (e == null)
            {
                e = new Entry { def = def, source = source, remaining = duration, tickTimer = def.tickInterval, stacks = 1, potency = potency };
                Entries.Add(e);
                Added?.Invoke(e);
                Changed?.Invoke();
                return e;
            }

            switch (def.stackPolicy)
            {
                case StackPolicy.RefreshDuration:
                    e.remaining = duration;
                    e.potency = Math.Max(e.potency, potency);
                    break;
                case StackPolicy.AddStacks:
                    e.stacks = Math.Min(Math.Max(1, def.maxStacks), e.stacks + 1);
                    e.remaining = duration;
                    break;
                case StackPolicy.IgnoreIfActive:
                    break;
                case StackPolicy.KeepLongest:
                    e.remaining = Math.Max(e.remaining, duration);
                    break;
            }
            e.source = source ?? e.source;
            Changed?.Invoke();
            return e;
        }

        public void Tick(float dt, Action<Entry> onTick)
        {
            bool changed = false;
            for (int i = Entries.Count - 1; i >= 0; i--)
            {
                var e = Entries[i];
                e.remaining -= dt;
                if (e.def.tickInterval > 0f)
                {
                    e.tickTimer -= dt;
                    int guard = 0;
                    while (e.tickTimer <= 0f && guard++ < 8)
                    {
                        onTick?.Invoke(e);
                        e.tickTimer += e.def.tickInterval;
                    }
                }
                if (e.remaining <= 0f)
                {
                    Entries.RemoveAt(i);
                    Removed?.Invoke(e);
                    changed = true;
                }
            }
            if (changed) Changed?.Invoke();
        }

        public void Remove(StatusKind kind)
        {
            bool changed = false;
            for (int i = Entries.Count - 1; i >= 0; i--)
            {
                if (Entries[i].def.kind != kind) continue;
                var e = Entries[i];
                Entries.RemoveAt(i);
                Removed?.Invoke(e);
                changed = true;
            }
            if (changed) Changed?.Invoke();
        }

        public void Clear()
        {
            if (Entries.Count == 0) return;
            for (int i = Entries.Count - 1; i >= 0; i--)
            {
                var e = Entries[i];
                Entries.RemoveAt(i);
                Removed?.Invoke(e);
            }
            Changed?.Invoke();
        }

        public void Aggregate(StatModifiers mods)
        {
            mods.Reset();
            foreach (var e in Entries)
            {
                var d = e.def;
                mods.moveSpeed *= d.moveSpeedMultiplier;
                mods.damageTaken *= d.damageTakenMultiplier;
                mods.damageDealt *= d.damageDealtMultiplier;
                if (d.stuns) mods.stunned = true;
            }
        }
    }
}

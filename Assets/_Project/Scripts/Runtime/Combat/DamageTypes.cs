using System.Collections.Generic;
using UnityEngine;

namespace Ruinas
{
    public struct DamageRequest
    {
        public Actor attacker;
        public DamageReceiver target;
        public float amount;
        public DamageKind kind;
        public DamageFlags flags;
        public Vector3 hitPoint;
        public Vector3 direction;
        public float knockback;
        public float stagger;
        public float hitstop;
        /// <summary>Identificador do evento de ataque: um alvo só recebe dano uma vez por evento (0 = sem deduplicação).</summary>
        public int eventId;
        public StatusApplication[] statuses;
        public string sourceTag;
    }

    public struct DamageResult
    {
        public DamageRequest request;
        public bool applied;
        public int amount;
        public bool crit;
        public bool killed;
        public float healthAfter;

        public Actor Target => request.target != null ? request.target.Actor : null;
    }

    /// <summary>Fórmula de dano isolada (testável).</summary>
    public static class DamageMath
    {
        public static float Compute(float baseAmount, float attackerMultiplier, bool crit, float critMultiplier,
            float takenMultiplier, float armorReduction, bool ignoreArmor)
        {
            float amt = baseAmount * attackerMultiplier * (crit ? critMultiplier : 1f) * takenMultiplier;
            if (!ignoreArmor) amt *= 1f - Mathf.Clamp(armorReduction, 0f, 0.8f);
            return amt;
        }

        public static int Round(float amount) => amount <= 0f ? 0 : Mathf.Max(1, Mathf.RoundToInt(amount));
    }

    /// <summary>Registro (evento, alvo) com expiração: garante uma aplicação por evento de ataque.</summary>
    public class HitRegistry
    {
        readonly Dictionary<long, float> hits = new Dictionary<long, float>();
        readonly List<long> scratch = new List<long>();
        public float Retention = 4f;
        public int Count => hits.Count;

        public static long Key(int eventId, int targetId) => ((long)eventId << 32) | (uint)targetId;

        public bool TryRegister(int eventId, int targetId, float now)
        {
            if (eventId == 0) return true;
            long k = Key(eventId, targetId);
            if (hits.ContainsKey(k)) return false;
            hits[k] = now;
            return true;
        }

        public void Prune(float now)
        {
            scratch.Clear();
            foreach (var kv in hits) if (now - kv.Value > Retention) scratch.Add(kv.Key);
            foreach (var k in scratch) hits.Remove(k);
        }

        public void Clear() => hits.Clear();
    }
}

using System;
using UnityEngine;

namespace Ruinas
{
    /// <summary>Vida de um ator. Só o CombatResolver aplica dano; curas passam por Heal.</summary>
    public class DamageReceiver : MonoBehaviour
    {
        public float MaxHealth { get; private set; } = 100f;
        public float Health { get; private set; } = 100f;
        public bool IsDead { get; private set; }
        public bool Invulnerable { get; set; }
        public Actor Actor { get; private set; }
        public float Fraction => MaxHealth > 0f ? Health / MaxHealth : 0f;

        float invulnerableUntil = -1f;

        /// <summary>(vida atual, vida máxima, variação)</summary>
        public event Action<float, float, float> HealthChanged;

        internal void Bind(Actor actor) => Actor = actor;

        public void Init(float maxHealth)
        {
            MaxHealth = Mathf.Max(1f, maxHealth);
            Health = MaxHealth;
            IsDead = false;
            invulnerableUntil = -1f;
            HealthChanged?.Invoke(Health, MaxHealth, 0f);
        }

        public void SetMaxHealth(float max, bool keepFraction = true)
        {
            max = Mathf.Max(1f, max);
            float f = Fraction;
            MaxHealth = max;
            Health = keepFraction ? Mathf.Max(IsDead ? 0f : 1f, max * f) : Mathf.Min(Health, max);
            HealthChanged?.Invoke(Health, MaxHealth, 0f);
        }

        public void GrantInvulnerability(float seconds) => invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + seconds);

        public bool CanBeDamaged => !IsDead && !Invulnerable && Time.time >= invulnerableUntil;

        /// <summary>Retorna true se este dano matou o ator.</summary>
        public bool TakeDamage(int amount)
        {
            if (!CanBeDamaged || amount <= 0) return false;
            float before = Health;
            Health = Mathf.Max(0f, Health - amount);
            HealthChanged?.Invoke(Health, MaxHealth, Health - before);
            if (Health <= 0f)
            {
                IsDead = true;
                return true;
            }
            return false;
        }

        public float Heal(float amount)
        {
            if (IsDead || amount <= 0f) return 0f;
            float before = Health;
            Health = Mathf.Min(MaxHealth, Health + amount);
            float healed = Health - before;
            if (healed > 0f) HealthChanged?.Invoke(Health, MaxHealth, healed);
            return healed;
        }

        public void Revive(float fraction)
        {
            IsDead = false;
            Health = Mathf.Clamp(MaxHealth * fraction, 1f, MaxHealth);
            HealthChanged?.Invoke(Health, MaxHealth, 0f);
        }
    }
}

using System;
using UnityEngine;

namespace Ruinas
{
    [Serializable]
    public class StatModifiers
    {
        public float moveSpeed = 1f;
        public float meleeDamage = 1f;
        public float rangedDamage = 1f;
        public float magicDamage = 1f;
        public float damageDealt = 1f;
        public float damageTaken = 1f;
        public float cooldown = 1f;
        public float armor;
        public float healthBonus;
        public bool stunned;

        public void Reset()
        {
            moveSpeed = meleeDamage = rangedDamage = magicDamage = damageDealt = damageTaken = cooldown = 1f;
            armor = 0f;
            healthBonus = 0f;
            stunned = false;
        }
    }

    /// <summary>
    /// Atributos efetivos = base × equipamento × status. Cada camada é recalculada pelo seu sistema
    /// (EquipmentSystem, StatusEffectSystem), nunca somada incrementalmente.
    /// </summary>
    public class ActorStats
    {
        public float baseMoveSpeed = 4f;
        public float critChance = 0.05f;
        public float critMultiplier = 2f;
        /// <summary>Multiplicador global (nível do personagem ou configuração de demonstração).</summary>
        public float globalDamage = 1f;
        public readonly StatModifiers equipment = new StatModifiers();
        public readonly StatModifiers status = new StatModifiers();

        public float MoveSpeed => baseMoveSpeed * equipment.moveSpeed * status.moveSpeed;
        public bool Stunned => status.stunned;
        public float CooldownMultiplier => Mathf.Clamp(equipment.cooldown * status.cooldown, 0.3f, 3f);
        public float ArmorReduction => Mathf.Clamp(equipment.armor + status.armor, 0f, 0.8f);
        public float DamageTakenMultiplier => equipment.damageTaken * status.damageTaken;

        public float DamageDealtMultiplier(DamageKind kind)
        {
            float k = 1f;
            switch (kind)
            {
                case DamageKind.Melee: k = equipment.meleeDamage * status.meleeDamage; break;
                case DamageKind.Ranged: k = equipment.rangedDamage * status.rangedDamage; break;
                case DamageKind.Magic: k = equipment.magicDamage * status.magicDamage; break;
            }
            return globalDamage * k * equipment.damageDealt * status.damageDealt;
        }
    }
}

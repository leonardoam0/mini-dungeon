using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ruinas
{
    [CreateAssetMenu(menuName = "Ruinas/Enchantment")]
    public class EnchantmentDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public EnchantKind kind;
        public float value = 0.15f;
        [Range(0, 1)] public float chance = 1f;
        public float cooldown = 0f;
        public StatusApplication status;
        public AreaEffectSpec area;
    }
}

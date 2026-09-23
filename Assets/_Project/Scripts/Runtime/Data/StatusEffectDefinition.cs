using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ruinas
{
    [CreateAssetMenu(menuName = "Ruinas/Status Effect")]
    public class StatusEffectDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public StatusKind kind;
        public bool isBuff;
        public float defaultDuration = 3f;
        public float tickInterval = 0.5f;
        public float damagePerTick;
        public DamageKind damageKind = DamageKind.Poison;
        public float healPerTick;
        public float moveSpeedMultiplier = 1f;
        public float damageTakenMultiplier = 1f;
        public float damageDealtMultiplier = 1f;
        public bool stuns;
        public StackPolicy stackPolicy = StackPolicy.RefreshDuration;
        public int maxStacks = 1;
        [Tooltip("Chave de VFX em loop enquanto o status está ativo.")] public string vfxKey;
        public Color tint = Color.white;
    }
}

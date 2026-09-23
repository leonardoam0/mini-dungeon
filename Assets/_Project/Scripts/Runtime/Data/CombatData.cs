using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ruinas
{
    public enum Team { Player, Enemy, Neutral }

    public enum DamageKind { Melee, Ranged, Magic, Poison, Fire, Fall, Pure }

    [Flags]
    public enum DamageFlags
    {
        None = 0,
        CanCrit = 1,
        IgnoreArmor = 2,
        NoReaction = 4,
        NoNumber = 8,
        FromStatus = 16,
        Area = 32,
    }

    public enum StatusKind { Poison, Burning, Slow, Stun, Vulnerable, Haste, Strength, Regeneration }

    public enum StackPolicy { RefreshDuration, AddStacks, IgnoreIfActive, KeepLongest }

    [Serializable]
    public struct StatusApplication
    {
        public StatusEffectDefinition effect;
        public float duration;
        [Range(0, 1)] public float chance;
        public float potency;
    }

    [Serializable]
    public class AreaEffectSpec
    {
        public bool enabled;
        public float radius = 1.8f;
        public float duration = 4f;
        public float tickInterval = 0.5f;
        public float damagePerTick = 3f;
        public DamageKind kind = DamageKind.Poison;
        public StatusApplication status;
        public string visualKey = "cloud_green";
        public bool hostileTint = true;
    }
}

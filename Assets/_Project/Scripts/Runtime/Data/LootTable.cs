using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ruinas
{
    [CreateAssetMenu(menuName = "Ruinas/Loot Table")]
    public class LootTable : ScriptableObject
    {
        public string id;
        [Range(0, 1)] public float dropChance = 1f;
        public int rolls = 1;
        public LootEntry[] entries;
        [Tooltip("Sempre entregues (ex.: recompensa fixa de baú).")] public LootEntry[] guaranteed;
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ruinas
{
    public enum ItemKind { Melee, Ranged, Armor, Artifact }

    public enum Rarity { Common, Rare, Unique }

    public enum EnchantKind
    {
        Sharpness,     // +% dano corpo a corpo
        ToxicCloud,    // chance de nuvem tóxica ao acertar
        Fire,          // aplica queimadura ao acertar
        Swiftness,     // +% velocidade
        Recharge,      // −% recarga de artefatos
        Vitality,      // +vida máxima
        Piercing,      // projéteis atravessam alvos extras
        Guard,         // −% dano recebido
    }

    /// <summary>Instância de item no inventário: identidade estável + nível de poder.</summary>
    [Serializable]
    public class ItemInstance
    {
        public string uid;
        public ItemDefinition def;
        public int power = 1;

        public float PowerMultiplier => 1f + 0.1f * (power - 1);

        public ItemInstanceData ToData() => new ItemInstanceData { uid = uid, itemId = def != null ? def.id : "", power = power };

        public static ItemInstance Create(ItemDefinition def, int power = 1)
        {
            return new ItemInstance { uid = Guid.NewGuid().ToString("N"), def = def, power = Mathf.Max(1, power) };
        }
    }

    public enum ArtifactKind { FeatherLeap, MagentaPulse, SummonCompanion }

    [Serializable]
    public class LootEntry
    {
        public ItemDefinition item;
        public int emeraldsMin;
        public int emeraldsMax;
        public int arrows;
        public float healthOrb;
        public float weight = 1f;
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ruinas
{
    [CreateAssetMenu(menuName = "Ruinas/Item")]
    public class ItemDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public ItemKind kind;
        public Rarity rarity;
        public Sprite icon;
        [Tooltip("Malha extrudada usada na mão do personagem e no chão.")] public Mesh worldMesh;
        public Material worldMaterial;
        public Vector3 holdOffset;
        public Vector3 holdEuler;
        public float holdScale = 1f;

        [Header("Corpo a corpo")]
        public AttackDefinition[] combo;
        public float meleeDamage = 10f;

        [Header("Distância")]
        public ProjectileDefinition projectile;
        public float rangedDamage = 8f;
        public float drawTime = 0.3f;
        public float fireCooldown = 0.3f;
        public int projectilesPerShot = 1;
        public float spreadDegrees = 0f;

        [Header("Armadura")]
        public float healthBonus;
        [Range(0, 0.8f)] public float damageReduction;
        public float moveSpeedBonus;
        public float cooldownReduction;
        public Color armorTint = Color.white;
        public int armorSkinIndex;

        [Header("Artefato")]
        public ArtifactDefinition artifact;

        [Header("Encantamentos fixos")]
        public EnchantmentDefinition[] enchantments;

        public int emeraldValue = 5;

        public string KindLabel
        {
            get
            {
                switch (kind)
                {
                    case ItemKind.Melee: return "Arma corpo a corpo";
                    case ItemKind.Ranged: return "Arma à distância";
                    case ItemKind.Armor: return "Armadura";
                    default: return "Artefato";
                }
            }
        }

        public string RarityLabel => rarity == Rarity.Common ? "Comum" : rarity == Rarity.Rare ? "Raro" : "Único";

        public static Color RarityColor(Rarity r)
        {
            switch (r)
            {
                case Rarity.Rare: return new Color(0.35f, 0.85f, 1f);
                case Rarity.Unique: return new Color(1f, 0.62f, 0.18f);
                default: return new Color(0.86f, 0.86f, 0.82f);
            }
        }
    }
}

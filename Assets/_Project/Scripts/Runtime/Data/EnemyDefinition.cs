using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ruinas
{
    [CreateAssetMenu(menuName = "Ruinas/Enemy")]
    public class EnemyDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public EnemyArchetype archetype;
        public GameObject prefab;
        public float maxHealth = 50f;
        public float moveSpeed = 3f;
        [Tooltip("Resistência a interrupções: golpes com stagger menor não interrompem.")] public float poise = 1f;
        public float perceptionRadius = 12f;
        public float leashRadius = 28f;
        public float decisionInterval = 0.25f;

        [Header("Ataques")]
        public AttackDefinition[] attacks;
        public float attackDamage = 8f;
        public float attackCooldown = 1.2f;
        public float preferredRange = 1.4f;
        public float retreatRange = 0f;
        public ProjectileDefinition projectile;
        public float projectileDamage = 6f;
        public float aimTime = 0.8f;

        [Header("Recompensa")]
        public int xpReward = 10;
        public LootTable loot;

        [Header("Apresentação")]
        public float scale = 1f;
        public bool isElite;
        public Color debrisColorA = Color.green;
        public Color debrisColorB = Color.gray;
        public string alertSfx = "zombie_groan";
        public string hurtSfx = "hit_flesh";
        public string deathSfx = "enemy_death";
    }
}

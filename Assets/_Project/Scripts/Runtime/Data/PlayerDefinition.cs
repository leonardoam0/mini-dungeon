using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ruinas
{
    [CreateAssetMenu(menuName = "Ruinas/Player Definition")]
    public class PlayerDefinition : ScriptableObject
    {
        public GameObject prefab;
        public float maxHealth = 100f;
        public float moveSpeed = 4.6f;
        public float acceleration = 42f;
        public float deceleration = 55f;
        public float turnSpeed = 1080f;
        public float gravity = 30f;
        public float radius = 0.35f;

        [Header("Esquiva")]
        public float dodgeDistance = 3.2f;
        public float dodgeDuration = 0.34f;
        public float dodgeRecovery = 0.1f;
        public float dodgeCooldown = 0.9f;
        [Tooltip("Invulnerabilidade durante a esquiva (desativada até haver regra documentada).")]
        public bool dodgeInvulnerable = false;
        public float dodgeInvulnerableTime = 0f;

        [Header("Poção")]
        [Range(0, 1)] public float potionHealFraction = 0.6f;
        public float potionCooldown = 25f;
        public float potionUseTime = 0.45f;

        [Header("Combate")]
        public float critChance = 0.06f;
        public float critMultiplier = 2f;
        public int startingArrows = 30;
        public int maxArrows = 99;
        public int lives = 3;

        [Header("Equipamento inicial (IDs)")]
        public string[] startingItems;
        public string startMelee;
        public string startRanged;
        public string startArmor;
        public string[] startArtifacts = new string[3];
    }
}

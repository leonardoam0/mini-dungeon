using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ruinas
{
    [CreateAssetMenu(menuName = "Ruinas/Projectile")]
    public class ProjectileDefinition : ScriptableObject
    {
        public string id;
        public GameObject visualPrefab;
        public float speed = 24f;
        public float gravity = 0f;
        public float maxLifetime = 2.2f;
        public float radius = 0.12f;
        public int pierce = 0;
        public float knockback = 2f;
        public float stagger = 0.6f;
        public string impactVfx = "impact_small";
        public string impactSfx = "arrow_hit";
        public string launchSfx = "bow_release";
        public Color trailColor = new Color(1f, 1f, 1f, 0.5f);
        public bool alignToVelocity = true;
        [Tooltip("Ao atingir o chão/alvo cria uma área (ex.: esporos).")] public AreaEffectSpec areaOnImpact;
        public StatusApplication[] statuses;
    }
}

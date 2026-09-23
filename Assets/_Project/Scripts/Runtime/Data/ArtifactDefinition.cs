using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ruinas
{
    [CreateAssetMenu(menuName = "Ruinas/Artifact")]
    public class ArtifactDefinition : ScriptableObject
    {
        public string id;
        public ArtifactKind kind;
        public float cooldown = 5f;
        public float useLock = 0.25f;
        public float duration = 0f;
        public float radius = 2.5f;
        public float damage = 20f;
        public float knockback = 5f;
        public float stunDuration = 1f;
        public float distance = 5f;
        public StatusApplication applyStatus;
        public GameObject summonPrefab;
        public string sfxStart;
        public string sfxEnd;
        public Color color = Color.white;
    }
}

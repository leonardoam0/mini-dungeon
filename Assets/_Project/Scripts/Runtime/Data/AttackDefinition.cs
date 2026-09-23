using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ruinas
{
    /// <summary>Um golpe: tempos, alcance, arco, impacto e política de cancelamento.</summary>
    [CreateAssetMenu(menuName = "Ruinas/Attack")]
    public class AttackDefinition : ScriptableObject
    {
        public string id;
        [Tooltip("Clip de pose (PoseLibrary) sincronizado com os tempos abaixo.")]
        public string poseClip;
        [Min(0.05f)] public float duration = 0.5f;
        [Tooltip("Início da janela ativa (instante de contato).")] public float activeStart = 0.18f;
        public float activeEnd = 0.28f;
        [Tooltip("Janela em que um novo comando encadeia o próximo golpe.")] public float comboWindowStart = 0.12f;
        public float comboWindowEnd = 0.5f;
        [Tooltip("A partir deste tempo o golpe pode ser cancelado por esquiva.")] public float dodgeCancelAfter = 0.3f;
        [Tooltip("Ao fim deste tempo o próximo golpe pode começar (recuperação mínima).")] public float chainAt = 0.34f;

        [Header("Alcance")]
        public float range = 1.9f;
        public float arcDegrees = 110f;
        [Tooltip("Varredura do golpe em graus relativos à frente: de início a fim.")] public float sweepFrom = 55f;
        public float sweepTo = -55f;
        public float height = 1.0f;
        public bool areaAroundSelf;
        public float areaRadius = 0f;

        [Header("Impacto")]
        public float damageMultiplier = 1f;
        public float knockback = 3f;
        public float stagger = 1f;
        public float hitstop = 0.05f;
        public float lunge = 0.35f;

        [Header("Aviso (inimigos)")]
        public bool telegraph;
        public float telegraphRadius = 1.5f;

        [Header("Feedback")]
        public string swingSfx = "swing";
        public string hitSfx = "hit_flesh";
        public bool trail = true;
    }
}

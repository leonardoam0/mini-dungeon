using System;
using UnityEngine;

namespace Ruinas
{
    [Flags]
    public enum RefButtons
    {
        None = 0,
        Melee = 1,
        Ranged = 2,
        Dodge = 4,
        Artifact1 = 8,
        Artifact2 = 16,
        Artifact3 = 32,
        Potion = 64,
        Interact = 128,
        AttackInPlace = 256,
    }

    /// <summary>Quadro-chave de entrada. Movimento no espaço da tela; mira relativa à âncora da arena.</summary>
    [Serializable]
    public struct RefInputKey
    {
        public float time;
        public Vector2 move;
        public bool hasAim;
        public Vector3 aimLocal;
        public RefButtons pressed;
        public RefButtons held;
    }

    [Serializable]
    public class RefActorSpawn
    {
        public EnemyDefinition enemy;
        [Tooltip("Acompanhante aliado (criatura de manta vermelha) em vez de inimigo.")] public bool companion;
        public Vector3 localPosition;
        public float yaw;
        public StatusEffectDefinition startStatus;
        public float statusDuration = 3f;
        [Tooltip("Permanece parado (sem IA) até este instante.")] public float holdUntil;
    }

    public enum RefEventKind
    {
        ObeliskBeam, PerimeterShift, ResolveEncounter, SpawnLoot, Cloud, FireBurst, Marker,
        /// <summary>Carga violeta do obelisco (orbe) antes do feixe.</summary>
        ObeliskCharge,
        /// <summary>Labareda alta e breve.</summary>
        FireColumn,
        /// <summary>Explosão de fogo com anel de choque.</summary>
        Explosion,
        /// <summary>Clarão branco ascendente.</summary>
        Flash,
        /// <summary>Muda o estado visual do obelisco (texto = nome do EncounterState).</summary>
        ObeliskState,
        /// <summary>Coletável consumível (texto = id do consumível).</summary>
        SpawnConsumable,
    }

    /// <summary>Posição do jogador (relativa à âncora da arena) em um instante do trecho de referência.</summary>
    [Serializable]
    public struct RefPathKey
    {
        public float time;
        public Vector3 local;
    }

    [Serializable]
    public class RefEvent
    {
        public float time;
        public RefEventKind kind;
        public Vector3 localPosition;
        public float duration = 1f;
        public string text;
    }

    /// <summary>
    /// Roteiro determinístico do modo de referência (~14,6 s): posições iniciais, entradas gravadas/autorais,
    /// eventos e instantes de captura. A configuração de demonstração fica aqui, separada da campanha.
    /// </summary>
    [CreateAssetMenu(menuName = "Ruinas/Reference Script")]
    public class ReferenceScript : ScriptableObject
    {
        public float duration = 14.6f;
        public int seed = 88;
        public Vector3 playerStartLocal;
        public float playerStartYaw = 45f;
        public RefActorSpawn[] actors;
        public RefInputKey[] input;
        public RefEvent[] events;
        [Tooltip("Trajetória medida no vídeo; quando presente, o jogador a segue pelos sistemas reais de movimento.")]
        public RefPathKey[] path;
        [Tooltip("Antecipação (s) do ponto-alvo da trajetória.")] public float pathLead = 0.08f;

        [Header("Demonstração (não altera o balanceamento da campanha)")]
        public int displayLevel = 88;
        [Range(0, 1)] public float xpFraction = 0.86f;
        public int lives = 3;
        public int arrows = 30;
        public float playerMaxHealth = 420f;
        public float playerDamageMultiplier = 12f;
        public float enemyHealthMultiplier = 12f;
        public float enemyDamageMultiplier = 0.8f;
        [Tooltip("Velocidade base do jogador na demonstração (0 = definição padrão).")] public float playerMoveSpeed = 0f;
        public int itemPower = 12;
        public string meleeId;
        public string rangedId;
        public string armorId;
        public string[] artifactIds = new string[3];
    }
}

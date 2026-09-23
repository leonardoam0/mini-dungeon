using System;
using UnityEngine;

namespace Ruinas
{
    [Serializable]
    public class SpawnEntry
    {
        public EnemyDefinition enemy;
        public int count = 1;
        [Tooltip("Grupo de pontos de surgimento (vazio = qualquer ponto).")] public string spawnGroup = "";
    }

    [Serializable]
    public class EncounterWave
    {
        public SpawnEntry[] spawns;
        public float delayBefore = 1f;
        [Tooltip("A próxima onda começa quando restarem até N inimigos desta.")] public int advanceWhenRemaining = 0;
    }

    /// <summary>Dados de um encontro: tempos de transição, ondas e recompensa (concedida uma única vez).</summary>
    [CreateAssetMenu(menuName = "Ruinas/Encounter")]
    public class EncounterDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string objectiveText;
        public float activatingDuration = 2.2f;
        public float resolvingDuration = 3f;
        public EncounterWave[] waves;
        public LootTable reward;
        public int xpReward = 40;
        public bool lockGatesWhileActive = true;
        [Tooltip("Fração das ondas a partir da qual o contorno muda de ciano para verde.")]
        [Range(0, 1)] public float perimeterShiftAt = 0.6f;
    }
}

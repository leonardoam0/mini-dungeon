using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Ruinas
{
    /// <summary>
    /// Diretor de um encontro: conecta a máquina de estados às entidades da cena (ondas, portões, obelisco,
    /// contorno, recompensa). Reiniciar limpa inimigos, áreas e avisos sem duplicar recompensas; a conclusão
    /// acontece uma única vez.
    /// </summary>
    public class ArenaDirector : MonoBehaviour
    {
        public EncounterDefinition definition;
        public string encounterId;
        public GateController[] lockGates;
        public GateController[] exitGates;
        public SpawnPoint[] spawnPoints;
        public ObeliskController obelisk;
        public PerimeterGlow perimeter;
        public Chest rewardChest;
        public Transform rewardPoint;
        public TriggerZone activationZone;
        public float arenaRadius = 9f;

        public float HealthMultiplier { get; set; } = 1f;
        public float DamageMultiplier { get; set; } = 1f;
        public bool SuppressLoot { get; set; }
        /// <summary>Quando verdadeiro, ondas e resolução automática ficam suspensas (roteiro de referência).</summary>
        public bool ExternalControl { get; set; }

        EncounterStateMachine sm;
        readonly List<EnemyBrain> alive = new List<EnemyBrain>();
        int spawnCursor;
        LevelContext ctx;

        public string Id => string.IsNullOrEmpty(encounterId) ? (definition != null ? definition.id : name) : encounterId;
        public EncounterState State => sm != null ? sm.State : EncounterState.Dormant;
        public IReadOnlyList<EnemyBrain> Alive => alive;
        public int CompletionCount => sm != null ? sm.CompletionCount : 0;
        public float Progress => sm != null ? sm.Progress : 0f;
        public event Action<ArenaDirector> Cleared;

        void Awake() => EnsureMachine();

        void EnsureMachine()
        {
            if (sm != null) return;
            int waves = definition != null && definition.waves != null ? definition.waves.Length : 0;
            sm = new EncounterStateMachine(waves, definition != null ? definition.activatingDuration : 2f, definition != null ? definition.resolvingDuration : 3f);
            sm.Changed += OnChanged;
            sm.WaveStarted += SpawnWave;
            sm.Completed += OnCompleted;
        }

        void Start()
        {
            ctx = LevelContext.Current;
            ApplyVisuals(State);
            if (activationZone != null && activationZone.encounter == null) activationZone.encounter = this;
        }

        public void Activate()
        {
            EnsureMachine();
            if (sm.Activate()) Services.Audio?.Play("obelisk_activate", transform.position);
        }

        void Update()
        {
            if (sm == null) return;
            for (int i = alive.Count - 1; i >= 0; i--)
                if (alive[i] == null || !alive[i].IsAlive) alive.RemoveAt(i);
            if (ExternalControl) return;

            int next = sm.WaveIndex + 1;
            var waves = definition != null ? definition.waves : null;
            float delay = waves != null && next < waves.Length ? waves[next].delayBefore : 1f;
            int advance = waves != null && sm.WaveIndex >= 0 && sm.WaveIndex < waves.Length ? waves[sm.WaveIndex].advanceWhenRemaining : 0;
            var before = sm.State;
            sm.Tick(Time.deltaTime, alive.Count, delay, advance);
            if (sm.State == EncounterState.Active && before == EncounterState.Active && perimeter != null)
                perimeter.SetState(EncounterState.Active, sm.Progress, definition != null ? definition.perimeterShiftAt : 0.6f);
        }

        void OnChanged(EncounterState prev, EncounterState next)
        {
            ApplyVisuals(next);
            ctx = ctx != null ? ctx : LevelContext.Current;
            switch (next)
            {
                case EncounterState.Activating:
                    SetGates(lockGates, false);
                    if (definition != null && !string.IsNullOrEmpty(definition.objectiveText)) ctx?.Events.RaiseObjective(definition.objectiveText);
                    Services.Audio?.PlayMusic("music_combat", 1.2f);
                    break;
                case EncounterState.Resolving:
                    // Encerramento: dissipa ataques e partículas remanescentes da arena.
                    ctx?.Areas.ClearInRadius(transform.position, arenaRadius + 4f);
                    Services.Audio?.Play("encounter_clear", transform.position);
                    break;
                case EncounterState.Cleared:
                    SetGates(lockGates, true);
                    SetGates(exitGates, true);
                    Services.Audio?.PlayMusic("music_explore", 2f);
                    break;
                case EncounterState.Dormant:
                    SetGates(lockGates, true);
                    break;
            }
            ctx?.Events.RaiseEncounterChanged(this, next);
        }

        void ApplyVisuals(EncounterState s)
        {
            if (obelisk != null) obelisk.SetState(s);
            if (perimeter != null) perimeter.SetState(s, sm != null ? sm.Progress : 0f, definition != null ? definition.perimeterShiftAt : 0.6f);
        }

        static void SetGates(GateController[] gates, bool open)
        {
            if (gates == null) return;
            foreach (var g in gates) if (g != null) g.SetOpen(open);
        }

        void SpawnWave(int index)
        {
            if (definition == null || definition.waves == null || index >= definition.waves.Length) return;
            var wave = definition.waves[index];
            if (wave.spawns == null) return;
            foreach (var entry in wave.spawns)
            {
                if (entry == null || entry.enemy == null) continue;
                for (int i = 0; i < entry.count; i++)
                {
                    var sp = PickSpawn(entry.spawnGroup);
                    Vector3 pos = sp != null ? sp.transform.position : transform.position;
                    if (sp != null && ctx != null)
                    {
                        Vector2 off = ctx.Random.InsideUnitCircle() * sp.radius;
                        pos += new Vector3(off.x, 0f, off.y);
                    }
                    Spawn(entry.enemy, pos, sp != null ? sp.transform.eulerAngles.y : 0f, true);
                }
            }
        }

        SpawnPoint PickSpawn(string group)
        {
            if (spawnPoints == null || spawnPoints.Length == 0) return null;
            for (int tries = 0; tries < spawnPoints.Length; tries++)
            {
                var sp = spawnPoints[(spawnCursor++) % spawnPoints.Length];
                if (sp == null) continue;
                if (string.IsNullOrEmpty(group) || sp.group == group) return sp;
            }
            return spawnPoints[0];
        }

        public EnemyBrain Spawn(EnemyDefinition def, Vector3 position, float yaw, bool playSpawn)
        {
            ctx = ctx != null ? ctx : LevelContext.Current;
            if (def == null || def.prefab == null || ctx == null) return null;
            if (NavMesh.SamplePosition(position, out var hit, 3f, NavMesh.AllAreas)) position = hit.position;
            var go = Instantiate(def.prefab, position, Quaternion.Euler(0f, yaw, 0f));
            go.transform.localScale = Vector3.one * def.scale;
            var brain = go.GetComponent<EnemyBrain>();
            if (brain == null) { Destroy(go); return null; }
            brain.OwnerEncounter = this;
            brain.Init(def, ctx, HealthMultiplier, DamageMultiplier, playSpawn);
            brain.Despawned += b => alive.Remove(b);
            alive.Add(brain);
            if (playSpawn) ctx.Vfx.Spawn("spawn_burst", position, Quaternion.identity);
            return brain;
        }

        void OnCompleted()
        {
            ctx = ctx != null ? ctx : LevelContext.Current;
            if (rewardChest != null) rewardChest.Unlock();
            else if (definition != null && definition.reward != null && ctx != null)
                ctx.Loot.Roll(definition.reward, rewardPoint != null ? rewardPoint.position : transform.position, 1);
            if (definition != null && ctx != null && ctx.Player != null && ctx.Player.Progression != null)
                ctx.Player.Progression.AddXp(definition.xpReward);
            ctx?.Mission?.OnEncounterCleared(this);
            Cleared?.Invoke(this);
        }

        /// <summary>Reinício do encontro (derrota ou tecla de reinício): sem inimigos, áreas ou avisos órfãos.</summary>
        public void ResetEncounter()
        {
            EnsureMachine();
            ctx = ctx != null ? ctx : LevelContext.Current;
            for (int i = alive.Count - 1; i >= 0; i--) if (alive[i] != null) alive[i].Despawn();
            alive.Clear();
            ctx?.Areas.ClearInRadius(transform.position, arenaRadius + 6f);
            ExternalControl = false;
            if (sm.State == EncounterState.Cleared) return;
            sm.Reset();
            if (activationZone != null) activationZone.ResetZone();
            ApplyVisuals(sm.State);
            SetGates(lockGates, true);
        }

        /// <summary>Restaura um encontro concluído vindo do save (sem recompensa).</summary>
        public void RestoreCleared()
        {
            EnsureMachine();
            sm.ForceCleared();
            if (activationZone != null) activationZone.MarkFired();
            SetGates(lockGates, true);
            SetGates(exitGates, true);
            if (rewardChest != null) rewardChest.Unlock();
            ApplyVisuals(EncounterState.Cleared);
        }

        /// <summary>Arena já ativa (início do vídeo de referência), sem ondas automáticas.</summary>
        public void ForceActiveForReference()
        {
            EnsureMachine();
            ExternalControl = true;
            // Última onda: ao devolver o controle, a arena resolve quando não restarem inimigos.
            sm.ForceActive(int.MaxValue);
            if (activationZone != null) activationZone.MarkFired();
            ApplyVisuals(EncounterState.Active);
            // No início do vídeo o contorno é ciano; a troca para verde vem do roteiro (PerimeterShift).
            if (perimeter != null)
            {
                perimeter.ScriptedColor = true;
                perimeter.SnapColor(false);
            }
        }

        /// <summary>Devolve o controle às regras normais (resolve quando não houver inimigos).</summary>
        public void ReleaseExternalControl() => ExternalControl = false;
    }
}

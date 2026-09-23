using System;

namespace Ruinas
{
    public enum EncounterState { Dormant, Activating, Active, Resolving, Cleared }

    /// <summary>
    /// Regras do encontro sem dependência de cena (testáveis): transições válidas, ondas, conclusão
    /// única e reinício. A recompensa só pode ser concedida uma vez por instância.
    /// </summary>
    public class EncounterStateMachine
    {
        public EncounterState State { get; private set; } = EncounterState.Dormant;
        public float StateTime { get; private set; }
        public int WaveIndex { get; private set; } = -1;
        public int WaveCount { get; }
        public bool RewardGranted { get; private set; }
        public int CompletionCount { get; private set; }

        readonly float activatingDuration;
        readonly float resolvingDuration;
        float waveDelay = -1f;

        public event Action<EncounterState, EncounterState> Changed;
        public event Action<int> WaveStarted;
        public event Action Completed;

        public EncounterStateMachine(int waveCount, float activating, float resolving)
        {
            WaveCount = Math.Max(0, waveCount);
            activatingDuration = activating;
            resolvingDuration = resolving;
        }

        void Set(EncounterState s)
        {
            if (s == State) return;
            var prev = State;
            State = s;
            StateTime = 0f;
            Changed?.Invoke(prev, s);
        }

        public bool Activate()
        {
            if (State != EncounterState.Dormant) return false;
            Set(EncounterState.Activating);
            return true;
        }

        /// <summary>Entra direto no estado ativo (modo de referência começa com a arena já ativa).</summary>
        public void ForceActive(int wave)
        {
            if (State == EncounterState.Cleared) return;
            Set(EncounterState.Active);
            WaveIndex = Math.Max(0, Math.Min(wave, WaveCount - 1));
            waveDelay = -1f;
        }

        /// <summary>Marca como concluído sem recompensa (estado vindo do save).</summary>
        public void ForceCleared()
        {
            RewardGranted = true;
            Set(EncounterState.Cleared);
        }

        public void Reset()
        {
            if (State == EncounterState.Cleared) return;
            WaveIndex = -1;
            waveDelay = -1f;
            Set(EncounterState.Dormant);
        }

        /// <summary>Avança o tempo. aliveEnemies = inimigos vivos da onda atual.</summary>
        public void Tick(float dt, int aliveEnemies, float nextWaveDelay, int advanceWhenRemaining)
        {
            StateTime += dt;
            switch (State)
            {
                case EncounterState.Activating:
                    if (StateTime >= activatingDuration)
                    {
                        Set(EncounterState.Active);
                        StartWave(0);
                    }
                    break;
                case EncounterState.Active:
                    if (waveDelay >= 0f)
                    {
                        waveDelay -= dt;
                        if (waveDelay < 0f) StartWave(WaveIndex + 1);
                        break;
                    }
                    bool lastWave = WaveIndex >= WaveCount - 1;
                    if (!lastWave && aliveEnemies <= advanceWhenRemaining)
                        waveDelay = Math.Max(0f, nextWaveDelay);
                    else if (lastWave && aliveEnemies <= 0)
                        Set(EncounterState.Resolving);
                    break;
                case EncounterState.Resolving:
                    if (StateTime >= resolvingDuration)
                    {
                        Set(EncounterState.Cleared);
                        if (!RewardGranted)
                        {
                            RewardGranted = true;
                            CompletionCount++;
                            Completed?.Invoke();
                        }
                    }
                    break;
            }
        }

        void StartWave(int index)
        {
            waveDelay = -1f;
            if (index >= WaveCount) { Set(EncounterState.Resolving); return; }
            WaveIndex = index;
            WaveStarted?.Invoke(index);
        }

        public float Progress => WaveCount <= 0 ? 1f : (WaveIndex + 1) / (float)WaveCount;
    }
}

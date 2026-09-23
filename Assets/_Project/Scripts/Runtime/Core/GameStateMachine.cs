using System;
using UnityEngine;

namespace Ruinas
{
    public enum GameState { Boot, Menu, Loading, Gameplay, Paused, Inventory, Defeat, Completion }

    /// <summary>
    /// Estados globais do jogo e política de tempo/pausa. Lógica pura (testável); o efeito colateral
    /// sobre Time.timeScale fica isolado em <see cref="ApplyTimeScale"/>.
    /// </summary>
    public class GameStateMachine
    {
        public GameState Current { get; private set; } = GameState.Boot;
        public GameState Previous { get; private set; } = GameState.Boot;
        public event Action<GameState, GameState> Changed;

        /// <summary>Escala de tempo da jogabilidade (1 normalmente; maior no piloto automático).</summary>
        public float GameplayTimeScale = 1f;

        /// <summary>Congelamento do modo de referência (inspeção de pose/luz).</summary>
        public bool DebugFreeze;

        /// <summary>Quando falso, o estado não mexe em Time.timeScale (testes de lógica).</summary>
        public bool DriveTimeScale = true;

        public static bool CanTransition(GameState from, GameState to)
        {
            if (from == to) return false;
            if (to == GameState.Loading) return true;
            switch (from)
            {
                case GameState.Boot: return to == GameState.Menu;
                case GameState.Menu: return false;
                case GameState.Loading: return to == GameState.Menu || to == GameState.Gameplay;
                case GameState.Gameplay:
                    return to == GameState.Paused || to == GameState.Inventory || to == GameState.Defeat || to == GameState.Completion;
                case GameState.Paused: return to == GameState.Gameplay;
                case GameState.Inventory: return to == GameState.Gameplay;
                case GameState.Defeat: return to == GameState.Gameplay;
                case GameState.Completion: return to == GameState.Gameplay;
            }
            return false;
        }

        public bool TrySet(GameState next)
        {
            if (!CanTransition(Current, next))
            {
                if (Current != next) Debug.LogWarning($"[Estado] Transição inválida {Current} -> {next}");
                return false;
            }
            Previous = Current;
            Current = next;
            ApplyTimeScale();
            Changed?.Invoke(Previous, Current);
            return true;
        }

        public bool IsWorldPaused =>
            Current == GameState.Paused || Current == GameState.Inventory ||
            Current == GameState.Completion || Current == GameState.Defeat;

        public bool AcceptsGameplayInput => Current == GameState.Gameplay && !DebugFreeze;

        public void ApplyTimeScale()
        {
            if (!DriveTimeScale) return;
            bool paused = IsWorldPaused || DebugFreeze || Current == GameState.Loading;
            Time.timeScale = paused ? 0f : Mathf.Max(0.01f, GameplayTimeScale);
            AudioListener.pause = IsWorldPaused;
        }
    }
}

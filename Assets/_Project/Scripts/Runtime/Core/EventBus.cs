using System;

namespace Ruinas
{
    /// <summary>
    /// Eventos tipados da cena. Vive dentro do LevelContext: ao trocar de cena, todas as assinaturas
    /// somem junto com ele (nenhuma assinatura estática sobrevive a reinícios).
    /// </summary>
    public class EventBus
    {
        public event Action<DamageResult> Damaged;
        public event Action<Actor, Actor, DamageResult> Killed;
        public event Action<Actor, float, string> Healed;
        public event Action<ArenaDirector, EncounterState> EncounterChanged;
        public event Action<string> CheckpointReached;
        public event Action<string> ObjectiveChanged;
        public event Action<ItemInstance> ItemCollected;
        public event Action<int> EmeraldsCollected;
        public event Action<int> ArrowsCollected;
        public event Action<string, string> Notification;
        public event Action<string> ChestOpened;
        public event Action MissionCompleted;
        public event Action PlayerDefeated;
        public event Action PlayerRespawned;
        public event Action<int> LeveledUp;
        public event Action<string, string> Hint;

        public void RaiseDamaged(in DamageResult r) => Damaged?.Invoke(r);
        public void RaiseKilled(Actor victim, Actor killer, in DamageResult r) => Killed?.Invoke(victim, killer, r);
        public void RaiseHealed(Actor target, float amount, string source) => Healed?.Invoke(target, amount, source);
        public void RaiseEncounterChanged(ArenaDirector d, EncounterState s) => EncounterChanged?.Invoke(d, s);
        public void RaiseCheckpoint(string id) => CheckpointReached?.Invoke(id);
        public void RaiseObjective(string text) => ObjectiveChanged?.Invoke(text);
        public void RaiseItemCollected(ItemInstance item) => ItemCollected?.Invoke(item);
        public void RaiseEmeralds(int n) => EmeraldsCollected?.Invoke(n);
        public void RaiseArrows(int n) => ArrowsCollected?.Invoke(n);
        public void Notify(string title, string body = "") => Notification?.Invoke(title, body);
        public void RaiseChestOpened(string id) => ChestOpened?.Invoke(id);
        public void RaiseMissionCompleted() => MissionCompleted?.Invoke();
        public void RaisePlayerDefeated() => PlayerDefeated?.Invoke();
        public void RaisePlayerRespawned() => PlayerRespawned?.Invoke();
        public void RaiseLevelUp(int level) => LeveledUp?.Invoke(level);
        public void ShowHint(string id, string text) => Hint?.Invoke(id, text);
    }
}

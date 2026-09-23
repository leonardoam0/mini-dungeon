using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ruinas
{
    [Serializable]
    public class ObjectiveStep
    {
        public string id;
        [TextArea] public string text;
        [Tooltip("Concluído ao entrar nesta zona (TriggerZone.zoneId).")] public string zoneId;
        [Tooltip("Concluído quando este encontro termina.")] public string encounterId;
        public bool optional;
    }

    /// <summary>
    /// Percurso completo: novo jogo / continuar / voltar ao acampamento, objetivos, checkpoints, vidas,
    /// derrota e recuperação, conclusão com recompensa única e salvamento versionado.
    /// </summary>
    public class MissionDirector : MonoBehaviour
    {
        public ObjectiveStep[] objectives;
        public ArenaDirector[] encounters;
        public Chest[] chests;
        public Pickup[] staticPickups;
        public ExitPortal exit;
        public ArenaDirector finalEncounter;
        public string startCheckpointId = "cp_acampamento";
        public int completionEmeralds = 40;
        public int completionXp = 120;

        LevelContext ctx;
        SaveData save;
        InventorySystem inventory;
        ProgressionSystem progression;
        int lives;
        float runTime;
        string checkpointId;
        bool completed;
        bool respawning;
        int kills;
        readonly HashSet<string> clearedEncounters = new HashSet<string>();
        readonly HashSet<string> openedChests = new HashSet<string>();
        readonly HashSet<string> collectedPickups = new HashSet<string>();
        readonly HashSet<string> doneObjectives = new HashSet<string>();
        readonly HashSet<string> rewardsGranted = new HashSet<string>();

        public int Lives => lives;
        public float RunTime => runTime;
        public string CurrentObjectiveText { get; private set; } = "";
        public bool Completed => completed;
        public int Kills => kills;
        public string CheckpointId => checkpointId;
        public event Action LivesChanged;

        public void Init(LevelContext context, LoadRequest request)
        {
            ctx = context;
            var db = Services.Database;
            var mode = request != null ? request.mode : MissionStartMode.NewGame;
            if (Services.Args != null && Services.Args.ContinueMission) mode = MissionStartMode.Continue;

            SaveData loaded = null;
            if (mode != MissionStartMode.NewGame) loaded = Services.Save.Load(db);

            if (mode == MissionStartMode.Continue && loaded != null && loaded.mission.inProgress) save = loaded;
            else if (loaded != null && mode != MissionStartMode.NewGame)
            {
                // Volta ao acampamento: mantém equipamento e progresso, reinicia o percurso.
                save = loaded;
                save.mission = new MissionData { completions = loaded.mission.completions, completed = loaded.mission.completed, lives = db.player.lives };
            }
            else save = CreateNewSave(db);

            inventory = InventorySystem.FromData(save.inventory, db);
            progression = ProgressionSystem.FromData(save.progress);
            lives = save.mission.lives;
            runTime = save.mission.runTime;
            checkpointId = string.IsNullOrEmpty(save.mission.checkpointId) ? startCheckpointId : save.mission.checkpointId;
            foreach (var id in save.mission.clearedEncounters) clearedEncounters.Add(id);
            foreach (var id in save.mission.openedChests) openedChests.Add(id);
            foreach (var id in save.mission.collectedPickups) collectedPickups.Add(id);
            foreach (var id in save.mission.completedObjectives) doneObjectives.Add(id);
            foreach (var id in save.mission.rewardsGranted) rewardsGranted.Add(id);

            ApplyWorldState();

            var cp = Checkpoint.Find(checkpointId) ?? Checkpoint.Find(startCheckpointId);
            Vector3 pos = cp != null ? cp.SpawnPosition : (ctx.defaultSpawn != null ? ctx.defaultSpawn.position : Vector3.zero);
            Quaternion rot = cp != null ? cp.transform.rotation : Quaternion.Euler(0f, 45f, 0f);
            ctx.SpawnPlayer(pos, rot, inventory, progression);
            if (cp != null) cp.SetActivated(true, true);

            ctx.Events.ChestOpened += OnChestOpened;
            ctx.Events.PlayerDefeated += OnPlayerDefeated;
            ctx.Events.Killed += OnKilled;
            progression.LeveledUp += OnLevelUp;

            save.mission.inProgress = true;
            if (loaded != null && Services.Save.LastStatus == SaveLoadStatus.RecoveredFromBackup)
                StartCoroutine(NotifyLater("Save recuperado", Services.Save.LastMessage));
            else if (mode == MissionStartMode.Continue && loaded == null && Services.Save.LastStatus == SaveLoadStatus.Corrupt)
                StartCoroutine(NotifyLater("Save inválido", "Um novo jogo foi iniciado."));
            WriteSave();
        }

        IEnumerator NotifyLater(string title, string body)
        {
            yield return new WaitForSeconds(0.8f);
            ctx.Events.Notify(title, body);
        }

        public void OnLevelReady()
        {
            RefreshObjective();
            if (Services.Args != null && Services.Args.AutoPlay && ctx.Player != null)
                ctx.Player.OverrideSource = new AutoPilot(this, ctx);
            if (Services.Args != null && Services.Args.TestSaveLoad) StartCoroutine(SaveLoadSelfTest());
        }

        SaveData CreateNewSave(GameDatabase db)
        {
            var d = new SaveData();
            var inv = new InventorySystem();
            var def = db.player;
            if (def.startingItems != null)
                foreach (var id in def.startingItems) { var item = db.GetItem(id); if (item != null) inv.Add(item); }
            ItemInstance Find(string id) { foreach (var i in inv.Items) if (i.def.id == id) return i; return null; }
            inv.Equip(Find(def.startMelee));
            inv.Equip(Find(def.startRanged));
            inv.Equip(Find(def.startArmor));
            if (def.startArtifacts != null)
                for (int i = 0; i < def.startArtifacts.Length && i < 3; i++) { var a = Find(def.startArtifacts[i]); if (a != null) inv.Equip(a, i); }
            inv.SetArrows(def.startingArrows);
            d.inventory = inv.ToData();
            d.mission.lives = def.lives;
            d.mission.checkpointId = startCheckpointId;
            return d;
        }

        void ApplyWorldState()
        {
            if (encounters != null)
                foreach (var e in encounters) if (e != null && clearedEncounters.Contains(e.Id)) e.RestoreCleared();
            if (chests != null)
                foreach (var c in chests) if (c != null && openedChests.Contains(c.chestId)) c.SetOpenedSilently();
            if (staticPickups != null)
                foreach (var p in staticPickups) if (p != null && collectedPickups.Contains(p.persistentId)) p.gameObject.SetActive(false);
            if (exit != null) exit.SetActive(finalEncounter == null || clearedEncounters.Contains(finalEncounter.Id));
            foreach (var cp in Checkpoint.All) if (cp != null && cp.order <= OrderOf(checkpointId)) cp.SetActivated(cp.checkpointId == checkpointId, true);
        }

        int OrderOf(string id)
        {
            var c = Checkpoint.Find(id);
            return c != null ? c.order : 0;
        }

        void Update()
        {
            if (ctx == null || !ctx.Initialized || completed) return;
            if (Services.State.Current == GameState.Gameplay) runTime += Time.deltaTime;
            var player = ctx.Player;
            if (player == null || player.IsDefeated) return;
            foreach (var cp in Checkpoint.All)
            {
                if (cp == null || cp.checkpointId == checkpointId) continue;
                if (cp.order < OrderOf(checkpointId)) continue;
                if (Vector3.Distance(cp.transform.position, player.transform.position) <= cp.activationRadius) ReachCheckpoint(cp);
            }
        }

        void ReachCheckpoint(Checkpoint cp)
        {
            foreach (var other in Checkpoint.All) if (other != null && other != cp) other.SetActivated(false, true);
            checkpointId = cp.checkpointId;
            cp.SetActivated(true, false);
            ctx.Events.RaiseCheckpoint(cp.checkpointId);
            ctx.Events.Notify("Ponto de retorno", "Progresso salvo.");
            WriteSave();
        }

        // ------------------------------------------------------------ Objetivos

        void RefreshObjective()
        {
            string text = "";
            if (objectives != null)
            {
                foreach (var o in objectives)
                {
                    if (o == null || doneObjectives.Contains(o.id)) continue;
                    if (!string.IsNullOrEmpty(o.encounterId) && clearedEncounters.Contains(o.encounterId)) { doneObjectives.Add(o.id); continue; }
                    if (o.optional) continue;
                    text = o.text;
                    break;
                }
            }
            if (completed) text = "Percurso concluído";
            if (text != CurrentObjectiveText)
            {
                CurrentObjectiveText = text;
                ctx.Events.RaiseObjective(text);
            }
        }

        public void OnObjectiveZone(TriggerZone zone)
        {
            if (objectives == null || zone == null) return;
            foreach (var o in objectives)
                if (o != null && o.zoneId == zone.zoneId) doneObjectives.Add(o.id);
            RefreshObjective();
        }

        public void OnEncounterCleared(ArenaDirector enc)
        {
            if (enc == null) return;
            clearedEncounters.Add(enc.Id);
            if (objectives != null)
                foreach (var o in objectives) if (o != null && o.encounterId == enc.Id) doneObjectives.Add(o.id);
            if (enc == finalEncounter && exit != null)
            {
                exit.SetActive(true);
                ctx.Events.Notify("A saída foi liberada", "Siga até o portal para encerrar a missão.");
            }
            RefreshObjective();
            WriteSave();
        }

        void OnChestOpened(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            openedChests.Add(id);
            save.stats.chestsOpened++;
            WriteSave();
        }

        public void MarkPickupCollected(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            collectedPickups.Add(id);
        }

        void OnKilled(Actor victim, Actor killer, DamageResult r)
        {
            if (victim != null && victim.team == Team.Enemy) { kills++; save.stats.kills++; }
        }

        void OnLevelUp(int level)
        {
            ctx.Events.RaiseLevelUp(level);
            ctx.Vfx.SpawnAttached("level_up", ctx.Player.transform, Vector3.zero, 2f);
            Services.Audio?.Play2D("level_up");
        }

        // ------------------------------------------------------------ Derrota e recuperação

        void OnPlayerDefeated()
        {
            if (respawning || completed) return;
            save.stats.deaths++;
            StartCoroutine(DefeatRoutine());
        }

        IEnumerator DefeatRoutine()
        {
            respawning = true;
            yield return new WaitForSeconds(1.6f);
            if (lives > 0)
            {
                lives--;
                LivesChanged?.Invoke();
                bool done = false;
                Services.Flow.FadeThrough(() => { RespawnAtCheckpoint(); done = true; }, 0.35f, 0.2f, 0.4f);
                while (!done) yield return null;
                ctx.Events.Notify("Você voltou ao ponto de retorno", lives == 1 ? "Resta 1 vida." : $"Restam {lives} vidas.");
                WriteSave();
            }
            else
            {
                Services.State.TrySet(GameState.Defeat);
                ctx.UI?.ShowDefeat();
            }
            respawning = false;
        }

        void RespawnAtCheckpoint()
        {
            // Encontros em andamento voltam ao estado inicial; os concluídos permanecem concluídos.
            if (encounters != null)
                foreach (var e in encounters) if (e != null && e.State != EncounterState.Cleared) e.ResetEncounter();
            ctx.ClearTransient();
            var cp = Checkpoint.Find(checkpointId) ?? Checkpoint.Find(startCheckpointId);
            Vector3 pos = cp != null ? cp.SpawnPosition : ctx.defaultSpawn.position;
            ctx.Player.Revive(pos, cp != null ? cp.transform.rotation : Quaternion.identity, 1f);
        }

        /// <summary>"Tentar novamente" na tela de derrota: vidas restauradas, volta ao último ponto de retorno.</summary>
        public void RetryFromCheckpoint()
        {
            lives = Services.Database.player.lives;
            LivesChanged?.Invoke();
            Services.State.TrySet(GameState.Gameplay);
            Services.Flow.FadeThrough(() =>
            {
                RespawnAtCheckpoint();
                WriteSave();
            });
        }

        // ------------------------------------------------------------ Conclusão

        public void CompleteMission()
        {
            if (completed) return;
            completed = true;
            if (rewardsGranted.Add("mission_complete"))
            {
                progression.AddEmeralds(completionEmeralds);
                progression.AddXp(completionXp);
            }
            save.mission.completed = true;
            save.mission.completions++;
            save.mission.inProgress = false;
            WriteSave();
            RefreshObjective();
            Services.State.TrySet(GameState.Completion);
            ctx.UI?.ShowCompletion(BuildSummary());
            ctx.Events.RaiseMissionCompleted();
            if (Services.Args != null && Services.Args.AutoPlay)
            {
                int falhas = ctx.Player != null && ctx.Player.OverrideSource is AutoPilot ap ? ap.FailedSteps : 0;
                Debug.Log($"[AutoPilot] MISSAO_CONCLUIDA tempo={runTime:0.0}s abates={kills} mortes={save.stats.deaths} nivel={progression.Level} etapas_com_falha={falhas}");
                if (Services.Args.QuitWhenDone) StartCoroutine(QuitSoon());
            }
        }

        IEnumerator QuitSoon()
        {
            yield return new WaitForSecondsRealtime(2f);
            GameBootstrap.Quit();
        }

        public MissionSummary BuildSummary()
        {
            return new MissionSummary
            {
                time = runTime,
                kills = kills,
                deaths = save.stats.deaths,
                chests = openedChests.Count,
                level = progression.Level,
                emeralds = progression.Emeralds,
                items = inventory.Items.Count,
                rewardEmeralds = completionEmeralds,
                rewardXp = completionXp,
            };
        }

        public void ReturnToCamp() => Services.Flow.Load(new LoadRequest { scene = SceneFlow.MissionScene, mode = MissionStartMode.ReturnToCamp });

        // ------------------------------------------------------------ Salvamento

        public SaveData CaptureSave()
        {
            save.version = SaveData.CurrentVersion;
            save.progress = progression.ToData();
            save.inventory = inventory.ToData();
            save.mission.lives = lives;
            save.mission.runTime = runTime;
            save.mission.checkpointId = checkpointId;
            save.mission.clearedEncounters = new List<string>(clearedEncounters);
            save.mission.openedChests = new List<string>(openedChests);
            save.mission.collectedPickups = new List<string>(collectedPickups);
            save.mission.completedObjectives = new List<string>(doneObjectives);
            save.mission.rewardsGranted = new List<string>(rewardsGranted);
            save.stats.playtime += 0f;
            return save;
        }

        public bool WriteSave() => Services.Save.Write(CaptureSave());

        /// <summary>Autoteste (-testSaveLoad): grava, relê e compara os dados essenciais.</summary>
        IEnumerator SaveLoadSelfTest()
        {
            yield return new WaitForSeconds(1f);
            var before = JsonUtility.ToJson(CaptureSave());
            bool wrote = WriteSave();
            var reloaded = Services.Save.Load(Services.Database);
            var after = reloaded != null ? JsonUtility.ToJson(reloaded) : "";
            // savedAtUtc muda a cada gravação; compara o restante.
            string Strip(string s) => System.Text.RegularExpressions.Regex.Replace(s, "\"savedAtUtc\":\"[^\"]*\"", "");
            bool ok = wrote && reloaded != null && Strip(before) == Strip(after);
            Debug.Log($"[SaveTest] resultado={(ok ? "OK" : "FALHA")} status={Services.Save.LastStatus}");
            if (Services.Args.QuitWhenDone) GameBootstrap.Quit();
        }

        void OnApplicationQuit()
        {
            if (ctx != null && ctx.Initialized && !completed) WriteSave();
        }
    }

    public struct MissionSummary
    {
        public float time;
        public int kills, deaths, chests, level, emeralds, items, rewardEmeralds, rewardXp;
    }
}

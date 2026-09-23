using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Ruinas
{
    [Serializable]
    public class SaveData
    {
        /// <summary>v1: sem vidas nem estatísticas; v2: vidas, estatísticas e objetivos.</summary>
        public const int CurrentVersion = 2;
        public int version = CurrentVersion;
        public string savedAtUtc = "";
        public ProgressData progress = new ProgressData();
        public InventoryData inventory = new InventoryData();
        public MissionData mission = new MissionData();
        public StatsData stats = new StatsData();
    }

    [Serializable]
    public class ProgressData
    {
        public int level = 1;
        public int xp;
        public int emeralds;
    }

    [Serializable]
    public class ItemInstanceData
    {
        public string uid;
        public string itemId;
        public int power = 1;
    }

    [Serializable]
    public class InventoryData
    {
        public List<ItemInstanceData> items = new List<ItemInstanceData>();
        public string melee = "";
        public string ranged = "";
        public string armor = "";
        public string[] artifacts = new string[3];
        public int arrows = 30;
    }

    [Serializable]
    public class MissionData
    {
        public bool inProgress;
        public bool completed;
        public int completions;
        public string checkpointId = "";
        public int lives = 3;
        public float runTime;
        public List<string> clearedEncounters = new List<string>();
        public List<string> openedChests = new List<string>();
        public List<string> collectedPickups = new List<string>();
        public List<string> completedObjectives = new List<string>();
        public List<string> rewardsGranted = new List<string>();
    }

    [Serializable]
    public class StatsData
    {
        public float playtime;
        public int kills;
        public int deaths;
        public int chestsOpened;
    }

    public enum SaveLoadStatus { Ok, NoSave, RecoveredFromBackup, Corrupt, Migrated, Repaired }

    /// <summary>
    /// Salvamento versionado em JSON com escrita atômica, cópia de recuperação, migração e validação.
    /// Referências de cena nunca são serializadas: só IDs estáveis de itens, baús, encontros e checkpoints.
    /// </summary>
    public class SaveService
    {
        public string Directory { get; }
        public string MainPath { get; }
        public string BackupPath { get; }
        public SaveLoadStatus LastStatus { get; private set; } = SaveLoadStatus.NoSave;
        public string LastMessage { get; private set; } = "";

        public SaveService(string directory)
        {
            Directory = directory;
            MainPath = Path.Combine(directory, "slot0.json");
            BackupPath = Path.Combine(directory, "slot0.bak.json");
        }

        public bool HasSave => File.Exists(MainPath) || File.Exists(BackupPath);

        public bool Write(SaveData data)
        {
            if (data == null) return false;
            data.version = SaveData.CurrentVersion;
            data.savedAtUtc = DateTime.UtcNow.ToString("o");
            string json = JsonUtility.ToJson(data, true);
            return SafeFile.WriteAtomic(MainPath, json, BackupPath);
        }

        public void Delete()
        {
            TryDelete(MainPath);
            TryDelete(BackupPath);
            TryDelete(MainPath + ".tmp");
        }

        static void TryDelete(string p)
        {
            try { if (File.Exists(p)) File.Delete(p); } catch (Exception e) { Debug.LogWarning(e.Message); }
        }

        /// <summary>Carrega o save principal; se inválido, tenta a cópia. Nunca lança exceção.</summary>
        public SaveData Load(GameDatabase db)
        {
            LastMessage = "";
            if (!HasSave)
            {
                LastStatus = SaveLoadStatus.NoSave;
                return null;
            }

            var data = TryParse(SafeFile.TryRead(MainPath), db, out var status, out var msg);
            if (data != null)
            {
                LastStatus = status;
                LastMessage = msg;
                return data;
            }

            var backup = TryParse(SafeFile.TryRead(BackupPath), db, out status, out msg);
            if (backup != null)
            {
                LastStatus = SaveLoadStatus.RecoveredFromBackup;
                LastMessage = "O save principal estava danificado; o progresso foi recuperado da cópia de segurança.";
                return backup;
            }

            LastStatus = SaveLoadStatus.Corrupt;
            LastMessage = "Não foi possível ler o save (arquivo inválido ou de versão incompatível).";
            return null;
        }

        public static SaveData TryParse(string json, GameDatabase db, out SaveLoadStatus status, out string message)
        {
            status = SaveLoadStatus.Corrupt;
            message = "";
            if (string.IsNullOrWhiteSpace(json)) return null;
            SaveData data;
            try
            {
                data = JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                message = e.Message;
                return null;
            }
            if (data == null || data.version <= 0 || data.version > SaveData.CurrentVersion) return null;

            bool migrated = false;
            if (data.version < SaveData.CurrentVersion)
            {
                Migrate(data);
                migrated = true;
            }

            bool repaired = Validate(data, db, out message);
            status = migrated ? SaveLoadStatus.Migrated : repaired ? SaveLoadStatus.Repaired : SaveLoadStatus.Ok;
            return data;
        }

        public static void Migrate(SaveData data)
        {
            if (data.version < 2)
            {
                // v1 não guardava vidas, estatísticas nem objetivos concluídos.
                if (data.mission == null) data.mission = new MissionData();
                if (data.mission.lives <= 0) data.mission.lives = 3;
                if (data.stats == null) data.stats = new StatsData();
                if (data.mission.completedObjectives == null) data.mission.completedObjectives = new List<string>();
                if (data.mission.rewardsGranted == null) data.mission.rewardsGranted = new List<string>();
            }
            data.version = SaveData.CurrentVersion;
        }

        /// <summary>
        /// Corrige dados fora do domínio (IDs desconhecidos, slots apontando para itens ausentes,
        /// valores negativos). Retorna true se algo foi reparado.
        /// </summary>
        public static bool Validate(SaveData data, GameDatabase db, out string message)
        {
            bool repaired = false;
            var notes = new List<string>();
            if (data.progress == null) { data.progress = new ProgressData(); repaired = true; }
            if (data.inventory == null) { data.inventory = new InventoryData(); repaired = true; }
            if (data.mission == null) { data.mission = new MissionData(); repaired = true; }
            if (data.stats == null) { data.stats = new StatsData(); repaired = true; }

            var p = data.progress;
            if (p.level < 1) { p.level = 1; repaired = true; }
            if (p.xp < 0) { p.xp = 0; repaired = true; }
            if (p.emeralds < 0) { p.emeralds = 0; repaired = true; }

            var inv = data.inventory;
            if (inv.items == null) { inv.items = new List<ItemInstanceData>(); repaired = true; }
            if (inv.artifacts == null || inv.artifacts.Length != 3)
            {
                var fixedSlots = new string[3];
                if (inv.artifacts != null) for (int i = 0; i < Mathf.Min(3, inv.artifacts.Length); i++) fixedSlots[i] = inv.artifacts[i];
                inv.artifacts = fixedSlots;
                repaired = true;
            }
            if (inv.arrows < 0) { inv.arrows = 0; repaired = true; }

            var seen = new HashSet<string>();
            for (int i = inv.items.Count - 1; i >= 0; i--)
            {
                var it = inv.items[i];
                bool unknown = it == null || string.IsNullOrEmpty(it.uid) || (db != null && db.GetItem(it.itemId) == null);
                if (unknown || !seen.Add(it.uid))
                {
                    notes.Add(it == null ? "item nulo" : $"item '{it.itemId}' removido");
                    inv.items.RemoveAt(i);
                    repaired = true;
                    continue;
                }
                if (it.power < 1) { it.power = 1; repaired = true; }
            }

            string CheckSlot(string uid)
            {
                if (string.IsNullOrEmpty(uid)) return "";
                if (seen.Contains(uid)) return uid;
                repaired = true;
                notes.Add("slot equipado inválido");
                return "";
            }
            inv.melee = CheckSlot(inv.melee);
            inv.ranged = CheckSlot(inv.ranged);
            inv.armor = CheckSlot(inv.armor);
            for (int i = 0; i < 3; i++) inv.artifacts[i] = CheckSlot(inv.artifacts[i]);

            var m = data.mission;
            if (m.lives < 0 || m.lives > 9) { m.lives = 3; repaired = true; }
            if (m.clearedEncounters == null) { m.clearedEncounters = new List<string>(); repaired = true; }
            if (m.openedChests == null) { m.openedChests = new List<string>(); repaired = true; }
            if (m.collectedPickups == null) { m.collectedPickups = new List<string>(); repaired = true; }
            if (m.completedObjectives == null) { m.completedObjectives = new List<string>(); repaired = true; }
            if (m.rewardsGranted == null) { m.rewardsGranted = new List<string>(); repaired = true; }
            if (m.checkpointId == null) { m.checkpointId = ""; repaired = true; }

            message = notes.Count > 0 ? string.Join("; ", notes) : "";
            return repaired;
        }
    }
}

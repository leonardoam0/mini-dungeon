using System;
using System.IO;
using UnityEngine;

namespace Ruinas
{
    public enum ControlProfile { Direct = 0, ClickToMove = 1 }

    [Serializable]
    public class SettingsData
    {
        public const int CurrentVersion = 1;
        public int version = CurrentVersion;
        public float masterVolume = 0.9f;
        public float musicVolume = 0.5f;
        public float sfxVolume = 0.85f;
        public float ambienceVolume = 0.7f;
        public float uiVolume = 0.8f;
        public bool reduceFlashes;
        public bool cameraShake = true;
        public bool showDamageNumbers = true;
        public bool showControlHints = true;
        public int controlProfile = (int)ControlProfile.Direct;
        public float uiScale = 1f;
        public int qualityLevel = 2;
        public bool fullscreen = true;
        public bool vsync = true;
        public float gamepadDeadzone = 0.2f;
        public string bindingOverridesJson = "";

        public void Clamp()
        {
            masterVolume = Mathf.Clamp01(masterVolume);
            musicVolume = Mathf.Clamp01(musicVolume);
            sfxVolume = Mathf.Clamp01(sfxVolume);
            ambienceVolume = Mathf.Clamp01(ambienceVolume);
            uiVolume = Mathf.Clamp01(uiVolume);
            uiScale = Mathf.Clamp(uiScale, 0.75f, 1.5f);
            qualityLevel = Mathf.Clamp(qualityLevel, 0, 2);
            gamepadDeadzone = Mathf.Clamp(gamepadDeadzone, 0.05f, 0.6f);
            if (controlProfile < 0 || controlProfile > 1) controlProfile = 0;
            if (bindingOverridesJson == null) bindingOverridesJson = "";
        }
    }

    /// <summary>Configurações persistidas separadamente do progresso.</summary>
    public class SettingsService
    {
        public SettingsData Data { get; private set; } = new SettingsData();
        public event Action Changed;
        public string FilePath { get; }

        public SettingsService(string directory)
        {
            FilePath = Path.Combine(directory, "settings.json");
            Load();
        }

        public void Load()
        {
            string json = SafeFile.TryRead(FilePath);
            if (string.IsNullOrEmpty(json))
            {
                Data = new SettingsData();
                return;
            }
            try
            {
                var d = JsonUtility.FromJson<SettingsData>(json);
                if (d == null || d.version > SettingsData.CurrentVersion) throw new Exception("versão incompatível");
                d.version = SettingsData.CurrentVersion;
                d.Clamp();
                Data = d;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Settings] Arquivo inválido ({e.Message}); usando padrões.");
                Data = new SettingsData();
            }
        }

        public void Save()
        {
            Data.Clamp();
            SafeFile.WriteAtomic(FilePath, JsonUtility.ToJson(Data, true), FilePath + ".bak");
        }

        /// <summary>Aplica as configurações de vídeo e notifica os ouvintes (áudio, HUD, entrada).</summary>
        public void Apply(bool applyDisplay = true)
        {
            Data.Clamp();
            if (applyDisplay)
            {
                if (QualitySettings.names.Length > 0)
                    QualitySettings.SetQualityLevel(Mathf.Min(Data.qualityLevel, QualitySettings.names.Length - 1), true);
                QualitySettings.vSyncCount = Data.vsync ? 1 : 0;
                Application.targetFrameRate = Data.vsync ? -1 : 144;
                if (!Application.isEditor && !(Services.Args != null && Services.Args.Windowed))
                    Screen.fullScreenMode = Data.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            }
            Changed?.Invoke();
        }
    }
}

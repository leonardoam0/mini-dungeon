using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Ruinas
{
    /// <summary>
    /// Cria os serviços persistentes antes da primeira cena, independentemente da cena aberta
    /// (Boot, Menu, arena de referência ou missão). Não contém lógica de jogabilidade.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        static GameBootstrap instance;
        public static bool Initialized => instance != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void AutoCreate()
        {
            if (instance != null || !Application.isPlaying) return;
            var go = new GameObject("[Ruinas Services]");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<GameBootstrap>();
            instance.Initialize();
        }

        void Initialize()
        {
            Services.Args = LaunchArgs.Parse(Environment.GetCommandLineArgs());

            var db = Resources.Load<GameDatabase>("GameDatabase");
            if (db == null)
            {
                Debug.LogError("[Boot] GameDatabase não encontrado em Resources. Execute Ruinas > Regenerar tudo.");
                db = ScriptableObject.CreateInstance<GameDatabase>();
            }
            db.BuildLookups();
            Services.Database = db;

            Layers.ConfigureCollisionMatrix();

            string dataDir = string.IsNullOrEmpty(Services.Args.SaveDir) ? Application.persistentDataPath : Services.Args.SaveDir;
            Directory.CreateDirectory(dataDir);
            Services.Settings = new SettingsService(dataDir);
            Services.Save = new SaveService(Path.Combine(dataDir, "save"));
            Services.State = new GameStateMachine();
            if (Services.Args.AutoPlay) Services.State.GameplayTimeScale = Services.Args.AutoPlayTimeScale;

            Services.Input = gameObject.AddComponent<InputRouter>();
            Services.Input.Init(db.inputActions, Services.Settings);
            var args = Services.Args;
            if (args.AutoPlay || args.Capture || args.TestSaveLoad || !string.IsNullOrEmpty(args.RecordDir))
                Services.Input.LockForAutomation();

            Services.Audio = gameObject.AddComponent<AudioService>();
            Services.Audio.Init(db.sfx, Services.Settings);

            Services.Flow = gameObject.AddComponent<SceneFlow>();
            Services.Flow.Init(db);

            Services.Settings.Apply();

            if (Services.Args.Windowed)
                Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);

            if (Services.Args.Perf) gameObject.AddComponent<PerfRecorder>();
            if (!string.IsNullOrEmpty(Services.Args.RecordDir))
                gameObject.AddComponent<GameplayRecorder>().Init(Services.Args.RecordDir, Services.Args.RecordFps > 0 ? Services.Args.RecordFps : 30, Services.Args.RecordSeconds);
            if (Services.Args.QuitAfterSeconds > 0f) StartCoroutine(QuitAfter(Services.Args.QuitAfterSeconds));

            Debug.Log($"[Boot] Ruínas do Obelisco {Application.version} | Unity {Application.unityVersion} | dados em {dataDir}");
        }

        IEnumerator QuitAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            Debug.Log("[Boot] -quitAfter atingido; encerrando.");
            Quit();
        }

        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void OnApplicationQuit()
        {
            Services.Settings?.Save();
        }
    }
}

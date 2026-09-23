using System;
using System.Diagnostics;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Ruinas.EditorTools
{
    /// <summary>
    /// Pipeline reprodutível: gera texturas, fonte, áudio, materiais, dados, efeitos, personagens, adereços
    /// e cenas; compila a build do Windows. Disponível no menu "Ruinas" e por -executeMethod em modo batch.
    /// </summary>
    public static class Forge
    {
        static bool failed;

        static void Step(string name, Action action)
        {
            if (failed) return;
            var sw = Stopwatch.StartNew();
            try
            {
                action();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                Debug.Log($"[Forge] {name}: ok ({sw.Elapsed.TotalSeconds:0.0}s)");
            }
            catch (Exception e)
            {
                failed = true;
                Debug.LogError($"[Forge] {name}: FALHOU — {e}");
            }
        }

        [MenuItem("Ruinas/Regenerar tudo")]
        public static void All()
        {
            failed = false;
            Step("Configuração do projeto", ProjectSetup.Apply);
            Step("Texturas", TextureForge.GenerateAll);
            Step("Fonte pixelada", FontForge.Generate);
            Step("Áudio", AudioForge.Generate);
            Step("Materiais", MaterialForge.GenerateAll);
            Step("Dados", () => DataForge.CreateAll());
            Step("Efeitos visuais", VfxForge.BuildAll);
            Step("Personagens", CharacterForge.BuildAll);
            Step("Adereços", PropForge.BuildAll);
            Step("Cenas", SceneForge.BuildAll);
            Debug.Log(failed ? "[Forge] Geração INCOMPLETA (ver erros acima)." : "[Forge] Geração concluída.");
            ExitIfBatch(failed ? 1 : 0);
        }

        [MenuItem("Ruinas/Build Windows x64")]
        public static void BuildWindows()
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[]
                {
                    "Assets/_Project/Scenes/Boot.unity",
                    "Assets/_Project/Scenes/Menu.unity",
                    "Assets/_Project/Scenes/ReferenceArena.unity",
                    "Assets/_Project/Scenes/Mission.unity",
                },
                locationPathName = "Build/Windows/RuinasDoObelisco.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            });
            var s = report.summary;
            Debug.Log($"[Forge] Build {s.result}: {s.totalSize / (1024f * 1024f):0.0} MB, {s.totalTime.TotalSeconds:0}s, erros={s.totalErrors}, avisos={s.totalWarnings}");
            ExitIfBatch(s.result == BuildResult.Succeeded ? 0 : 1);
        }

        public static void AllAndBuild()
        {
            failed = false;
            Step("Configuração do projeto", ProjectSetup.Apply);
            Step("Texturas", TextureForge.GenerateAll);
            Step("Fonte pixelada", FontForge.Generate);
            Step("Áudio", AudioForge.Generate);
            Step("Materiais", MaterialForge.GenerateAll);
            Step("Dados", () => DataForge.CreateAll());
            Step("Efeitos visuais", VfxForge.BuildAll);
            Step("Personagens", CharacterForge.BuildAll);
            Step("Adereços", PropForge.BuildAll);
            Step("Cenas", SceneForge.BuildAll);
            if (failed) { ExitIfBatch(1); return; }
            BuildWindows();
        }

        static void ExitIfBatch(int code)
        {
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }
    }
}

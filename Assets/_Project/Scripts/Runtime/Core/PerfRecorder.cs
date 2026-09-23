using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ruinas
{
    /// <summary>
    /// Registro de desempenho (-perf): tempos de quadro, CPU e GPU (FrameTimingManager), percentis e picos,
    /// com a máquina, resolução e perfil gráfico usados na medição.
    /// </summary>
    public class PerfRecorder : MonoBehaviour
    {
        readonly List<float> frame = new List<float>(20000);
        readonly List<float> cpu = new List<float>(20000);
        readonly List<float> gpu = new List<float>(20000);
        readonly Dictionary<string, List<float>> perScene = new Dictionary<string, List<float>>();
        readonly FrameTiming[] timings = new FrameTiming[1];
        float warmup = 2f;
        bool written;

        void Update()
        {
            FrameTimingManager.CaptureFrameTimings();
            if (warmup > 0f) { warmup -= Time.unscaledDeltaTime; return; }
            if (Services.Flow != null && Services.Flow.IsLoading) return;
            float ms = Time.unscaledDeltaTime * 1000f;
            frame.Add(ms);
            string scene = SceneManager.GetActiveScene().name;
            if (!perScene.TryGetValue(scene, out var list)) perScene[scene] = list = new List<float>();
            list.Add(ms);
            if (FrameTimingManager.GetLatestTimings(1, timings) > 0)
            {
                if (timings[0].cpuFrameTime > 0) cpu.Add((float)timings[0].cpuFrameTime);
                if (timings[0].gpuFrameTime > 0) gpu.Add((float)timings[0].gpuFrameTime);
            }
        }

        static float Percentile(List<float> src, float p)
        {
            if (src.Count == 0) return 0f;
            var s = new List<float>(src);
            s.Sort();
            int idx = Mathf.Clamp(Mathf.CeilToInt(p * s.Count) - 1, 0, s.Count - 1);
            return s[idx];
        }

        static string Stats(string name, List<float> v)
        {
            if (v.Count == 0) return $"  \"{name}\": null";
            float sum = 0f, max = 0f;
            int spikes = 0;
            foreach (var x in v) { sum += x; if (x > max) max = x; if (x > 33.4f) spikes++; }
            var ci = CultureInfo.InvariantCulture;
            return string.Format(ci, "  \"{0}\": {{ \"amostras\": {1}, \"media_ms\": {2:0.00}, \"p50_ms\": {3:0.00}, \"p95_ms\": {4:0.00}, \"p99_ms\": {5:0.00}, \"max_ms\": {6:0.00}, \"quadros_acima_33ms\": {7} }}",
                name, v.Count, sum / v.Count, Percentile(v, 0.5f), Percentile(v, 0.95f), Percentile(v, 0.99f), max, spikes);
        }

        void Write()
        {
            if (written) return;
            written = true;
            string path = Services.Args != null && !string.IsNullOrEmpty(Services.Args.PerfFile)
                ? Services.Args.PerfFile
                : Path.Combine(Application.persistentDataPath, "perf.json");
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"cpu\": \"{SystemInfo.processorType.Replace("\"", "'")} ({SystemInfo.processorCount} threads)\",");
            sb.AppendLine($"  \"gpu\": \"{SystemInfo.graphicsDeviceName.Replace("\"", "'")} ({SystemInfo.graphicsMemorySize} MB, {SystemInfo.graphicsDeviceType})\",");
            sb.AppendLine($"  \"memoria_mb\": {SystemInfo.systemMemorySize},");
            sb.AppendLine($"  \"resolucao\": \"{Screen.width}x{Screen.height}\",");
            sb.AppendLine($"  \"qualidade\": \"{QualitySettings.names[QualitySettings.GetQualityLevel()]}\",");
            sb.AppendLine($"  \"vsync\": {QualitySettings.vSyncCount},");
            sb.AppendLine($"  \"versao\": \"{Application.version} / Unity {Application.unityVersion}\",");
            sb.AppendLine(Stats("quadro", frame) + ",");
            sb.AppendLine(Stats("cpu", cpu) + ",");
            sb.AppendLine(Stats("gpu", gpu) + ",");
            sb.AppendLine("  \"por_cena\": {");
            int i = 0;
            foreach (var kv in perScene)
            {
                sb.Append("  " + Stats(kv.Key, kv.Value));
                sb.AppendLine(++i < perScene.Count ? "," : "");
            }
            sb.AppendLine("  }");
            sb.AppendLine("}");
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, sb.ToString());
                Debug.Log($"[Perf] relatório: {path}");
            }
            catch (System.Exception e) { Debug.LogWarning($"[Perf] {e.Message}"); }
        }

        void OnApplicationQuit() => Write();
        void OnDestroy() => Write();
    }
}

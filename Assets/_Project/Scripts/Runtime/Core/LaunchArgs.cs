using System;
using System.Globalization;

namespace Ruinas
{
    /// <summary>
    /// Parâmetros de linha de comando usados em automação (captura de referência, piloto automático, testes).
    /// </summary>
    public class LaunchArgs
    {
        public bool StartReference;      // -reference
        public bool StartMission;        // -mission (novo jogo)
        public bool ContinueMission;     // -continue
        public bool Capture;             // -capture: captura os quadros definidos no roteiro de referência
        public string CaptureDir;        // -captureDir <pasta>
        public bool QuitWhenDone;        // -quitWhenDone
        public float QuitAfterSeconds;   // -quitAfter <s>
        public bool AutoPlay;            // -autoplay: piloto automático completa a missão
        public float AutoPlayTimeScale = 1f; // -timescale <x>
        public int RecordFps;            // -recordFps <n>: grava todos os quadros (sequência PNG)
        public string RecordDir;         // -recordDir <pasta>
        public float RecordSeconds;      // -recordSeconds <s>
        public bool Perf;                // -perf: registra tempos de quadro
        public string PerfFile;          // -perfFile <arquivo>
        public int Seed = 1337;          // -seed <n>
        public string ReferenceFramesDir;// -refFrames <pasta>
        public string SaveDir;           // -saveDir <pasta>
        public bool NoHud;               // -noHud
        public bool NoMap;               // -noMap
        public bool CaptureProfile;      // -captureProfile: inicia com o recorte 9:16 visível
        public bool TestSaveLoad;        // -testSaveLoad
        public string LogFile;           // -reportFile <arquivo>
        public bool Windowed;            // -windowedTest

        public static LaunchArgs Parse(string[] args)
        {
            var a = new LaunchArgs();
            if (args == null) return a;
            for (int i = 0; i < args.Length; i++)
            {
                string k = args[i].ToLowerInvariant();
                string Next() => i + 1 < args.Length ? args[++i] : null;
                switch (k)
                {
                    case "-reference": a.StartReference = true; break;
                    case "-mission": a.StartMission = true; break;
                    case "-continue": a.ContinueMission = true; break;
                    case "-capture": a.Capture = true; break;
                    case "-capturedir": a.CaptureDir = Next(); break;
                    case "-quitwhendone": a.QuitWhenDone = true; break;
                    case "-quitafter": a.QuitAfterSeconds = ParseFloat(Next(), 0f); break;
                    case "-autoplay": a.AutoPlay = true; break;
                    case "-timescale": a.AutoPlayTimeScale = Math.Max(0.1f, ParseFloat(Next(), 1f)); break;
                    case "-recordfps": a.RecordFps = (int)ParseFloat(Next(), 30f); break;
                    case "-recorddir": a.RecordDir = Next(); break;
                    case "-recordseconds": a.RecordSeconds = ParseFloat(Next(), 0f); break;
                    case "-perf": a.Perf = true; break;
                    case "-perffile": a.PerfFile = Next(); break;
                    case "-seed": a.Seed = (int)ParseFloat(Next(), 1337f); break;
                    case "-refframes": a.ReferenceFramesDir = Next(); break;
                    case "-savedir": a.SaveDir = Next(); break;
                    case "-nohud": a.NoHud = true; break;
                    case "-nomap": a.NoMap = true; break;
                    case "-captureprofile": a.CaptureProfile = true; break;
                    case "-testsaveload": a.TestSaveLoad = true; break;
                    case "-reportfile": a.LogFile = Next(); break;
                    case "-windowedtest": a.Windowed = true; break;
                }
            }
            return a;
        }

        static float ParseFloat(string s, float fallback)
        {
            if (string.IsNullOrEmpty(s)) return fallback;
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : fallback;
        }

        public bool IsAutomated => Capture || AutoPlay || TestSaveLoad || RecordFps > 0 || QuitAfterSeconds > 0f;
    }
}

using System.Collections;
using System.IO;
using UnityEngine;

namespace Ruinas
{
    /// <summary>
    /// Gravação de jogabilidade em sequência de quadros (-recordDir &lt;pasta&gt; -recordFps 30 -recordSeconds N).
    /// O tempo do jogo avança em passos fixos (Time.captureFramerate), então a gravação não depende da
    /// velocidade do disco. Os quadros (JPG) são convertidos em vídeo por Tools/record-video.ps1 (ffmpeg).
    /// </summary>
    public class GameplayRecorder : MonoBehaviour
    {
        string dir;
        int fps;
        float seconds;
        int frame;
        int width = 1280, height = 720;
        RenderTexture rt;
        Texture2D readback;
        bool running;

        public void Init(string outputDir, int framesPerSecond, float durationSeconds)
        {
            dir = outputDir;
            fps = Mathf.Clamp(framesPerSecond, 10, 60);
            seconds = durationSeconds;
            Directory.CreateDirectory(dir);
            // A textura da tela chega com valores já em sRGB e marcada como linear: a cópia não deve converter
            // (uma RenderTexture sRGB aplicaria a gama duas vezes e a imagem ficaria lavada).
            rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            readback = new Texture2D(width, height, TextureFormat.RGB24, false);
            Time.captureFramerate = fps;
            running = true;
            StartCoroutine(Loop());
            Debug.Log($"[Gravação] {fps} qps em {dir} ({(seconds > 0f ? seconds + " s" : "até sair")})");
        }

        IEnumerator Loop()
        {
            var wait = new WaitForEndOfFrame();
            while (running)
            {
                yield return wait;
                // Não grava carregamento nem esmaecimento; na arena de referência, começa junto com o roteiro.
                if (Services.Flow != null && (Services.Flow.IsLoading || Services.Flow.FadeAlpha > 0.01f)) continue;
                var level = LevelContext.Current;
                if (level == null || !level.Initialized) continue;
                if (frame == 0 && level.Reference != null && !level.Reference.ReplayRunning) continue;
                var shot = ScreenCapture.CaptureScreenshotAsTexture();
                if (shot != null)
                {
                    Graphics.Blit(shot, rt);
                    var prev = RenderTexture.active;
                    RenderTexture.active = rt;
                    readback.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                    readback.Apply(false);
                    RenderTexture.active = prev;
                    Destroy(shot);
                    File.WriteAllBytes(Path.Combine(dir, $"q_{frame:00000}.jpg"), readback.EncodeToJPG(88));
                    frame++;
                }
                if (seconds > 0f && frame >= Mathf.CeilToInt(seconds * fps))
                {
                    running = false;
                    Time.captureFramerate = 0;
                    Debug.Log($"[Gravação] concluída: {frame} quadros.");
                    if (Services.Args != null && Services.Args.QuitWhenDone) GameBootstrap.Quit();
                }
            }
        }

        void OnDestroy()
        {
            if (rt != null) rt.Release();
            if (readback != null) Destroy(readback);
        }
    }
}

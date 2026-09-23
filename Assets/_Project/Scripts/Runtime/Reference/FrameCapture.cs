using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Ruinas
{
    /// <summary>
    /// Captura quadros reais da renderização: imagem completa (16:9) e o recorte 9:16 do perfil de
    /// comparação, redimensionado para 540×960.
    /// </summary>
    public class FrameCapture : MonoBehaviour
    {
        public ReferenceCaptureProfile profile;
        public string OutputDir { get; private set; }
        public int Captured { get; private set; }
        public int Pending { get; private set; }

        public void Init(ReferenceCaptureProfile p, string dir)
        {
            profile = p;
            OutputDir = string.IsNullOrEmpty(dir) ? Path.Combine(Application.persistentDataPath, "Capturas") : dir;
            Directory.CreateDirectory(OutputDir);
        }

        public void Capture(string name, bool alsoCrop = true)
        {
            Pending++;
            StartCoroutine(CaptureRoutine(name, alsoCrop));
        }

        IEnumerator CaptureRoutine(string name, bool alsoCrop)
        {
            yield return new WaitForEndOfFrame();
            Texture2D full = null;
            try
            {
                full = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(OutputDir, name + "_16x9.png"), full.EncodeToPNG());
                if (alsoCrop && profile != null)
                {
                    var rect = profile.CropRect(full.width, full.height);
                    var crop = new Texture2D(rect.width, rect.height, TextureFormat.RGB24, false);
                    crop.SetPixels(full.GetPixels(rect.x, rect.y, rect.width, rect.height));
                    crop.Apply();
                    var scaled = Scale(crop, profile.outputWidth, profile.outputHeight);
                    File.WriteAllBytes(Path.Combine(OutputDir, name + "_9x16.png"), scaled.EncodeToPNG());
                    Destroy(crop);
                    Destroy(scaled);
                }
                Captured++;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Captura] Falha em '{name}': {e.Message}");
            }
            finally
            {
                if (full != null) Destroy(full);
                Pending--;
            }
        }

        static Texture2D Scale(Texture2D src, int w, int h)
        {
            var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            src.filterMode = FilterMode.Bilinear;
            var prev = RenderTexture.active;
            Graphics.Blit(src, rt);
            RenderTexture.active = rt;
            var dst = new Texture2D(w, h, TextureFormat.RGB24, false);
            dst.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            dst.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return dst;
        }
    }
}

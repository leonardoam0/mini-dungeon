using System;
using UnityEngine;

namespace Ruinas.EditorTools
{
    /// <summary>
    /// Tela de pintura em pixels (origem embaixo à esquerda, como Texture2D). Todas as texturas do projeto
    /// são pintadas por código com intenção (juntas, bisel, lascas, musgo localizado), com semente fixa.
    /// </summary>
    public class PixelCanvas
    {
        public readonly int W, H;
        public readonly Color[] Px;
        readonly System.Random rng;

        public PixelCanvas(int w, int h, int seed = 1)
        {
            W = w;
            H = h;
            Px = new Color[w * h];
            rng = new System.Random(seed);
        }

        public float R() => (float)rng.NextDouble();
        public float R(float a, float b) => a + (b - a) * R();
        public int RI(int a, int b) => rng.Next(a, b);

        public bool In(int x, int y) => x >= 0 && y >= 0 && x < W && y < H;

        public Color Get(int x, int y) => In(x, y) ? Px[y * W + x] : Color.clear;

        public void Set(int x, int y, Color c)
        {
            if (In(x, y)) Px[y * W + x] = c;
        }

        /// <summary>Define usando coordenadas com y para baixo (como em editores de imagem).</summary>
        public void SetTop(int x, int yDown, Color c) => Set(x, H - 1 - yDown, c);

        public void Blend(int x, int y, Color c, float a)
        {
            if (!In(x, y)) return;
            var o = Px[y * W + x];
            Px[y * W + x] = new Color(Mathf.Lerp(o.r, c.r, a), Mathf.Lerp(o.g, c.g, a), Mathf.Lerp(o.b, c.b, a), Mathf.Max(o.a, a * c.a));
        }

        public void Fill(Color c)
        {
            for (int i = 0; i < Px.Length; i++) Px[i] = c;
        }

        public void Rect(int x, int y, int w, int h, Color c)
        {
            for (int j = y; j < y + h; j++)
                for (int i = x; i < x + w; i++)
                    Set(i, j, c);
        }

        public void Outline(int x, int y, int w, int h, Color c)
        {
            for (int i = x; i < x + w; i++) { Set(i, y, c); Set(i, y + h - 1, c); }
            for (int j = y; j < y + h; j++) { Set(x, j, c); Set(x + w - 1, j, c); }
        }

        public void Multiply(int x, int y, float f)
        {
            if (!In(x, y)) return;
            var c = Px[y * W + x];
            Px[y * W + x] = new Color(c.r * f, c.g * f, c.b * f, c.a);
        }

        public void Line(int x0, int y0, int x1, int y1, Color c)
        {
            int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;
            while (true)
            {
                Set(x0, y0, c);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        /// <summary>Variação de brilho por pixel (±amount), sem mudar o matiz.</summary>
        public void Jitter(int x, int y, int w, int h, float amount)
        {
            for (int j = y; j < y + h; j++)
                for (int i = x; i < x + w; i++)
                    Multiply(i, j, 1f + R(-amount, amount));
        }

        public void Speckle(int x, int y, int w, int h, Color c, float density)
        {
            for (int j = y; j < y + h; j++)
                for (int i = x; i < x + w; i++)
                    if (R() < density) Set(i, j, c);
        }

        /// <summary>Mancha orgânica (musgo, sujeira) por passeio aleatório.</summary>
        public void Blob(int cx, int cy, int size, Color c, float jitter = 0.08f)
        {
            int x = cx, y = cy;
            for (int k = 0; k < size; k++)
            {
                var col = c * (1f + R(-jitter, jitter));
                col.a = c.a;
                Set(x, y, col);
                int d = RI(0, 4);
                if (d == 0) x++; else if (d == 1) x--; else if (d == 2) y++; else y--;
                x = Mathf.Clamp(x, cx - size / 3, cx + size / 3);
                y = Mathf.Clamp(y, cy - size / 3, cy + size / 3);
            }
        }

        public void Blit(PixelCanvas src, int dx, int dy)
        {
            for (int j = 0; j < src.H; j++)
                for (int i = 0; i < src.W; i++)
                    Set(dx + i, dy + j, src.Px[j * src.W + i]);
        }

        public void BlitAlpha(PixelCanvas src, int dx, int dy)
        {
            for (int j = 0; j < src.H; j++)
                for (int i = 0; i < src.W; i++)
                {
                    var c = src.Px[j * src.W + i];
                    if (c.a > 0.01f) Blend(dx + i, dy + j, c, c.a);
                }
        }

        /// <summary>Copia um bloco e estende as bordas (padding contra vazamento de mip/filtro).</summary>
        public void BlitPadded(PixelCanvas src, int dx, int dy, int pad)
        {
            for (int j = -pad; j < src.H + pad; j++)
                for (int i = -pad; i < src.W + pad; i++)
                {
                    int sx = Mathf.Clamp(i, 0, src.W - 1);
                    int sy = Mathf.Clamp(j, 0, src.H - 1);
                    Set(dx + i, dy + j, src.Px[sy * src.W + sx]);
                }
        }

        public PixelCanvas Crop(int x, int y, int w, int h)
        {
            var c = new PixelCanvas(w, h);
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                    c.Px[j * w + i] = Get(x + i, y + j);
            return c;
        }

        public Texture2D ToTexture(bool linear = false)
        {
            var t = new Texture2D(W, H, TextureFormat.RGBA32, false, linear);
            t.SetPixels(Px);
            t.Apply();
            return t;
        }

        public byte[] ToPNG()
        {
            var t = ToTexture();
            var bytes = t.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(t);
            return bytes;
        }
    }

    public static class Pal
    {
        public static Color Hex(string hex, float a = 1f)
        {
            ColorUtility.TryParseHtmlString(hex.StartsWith("#") ? hex : "#" + hex, out var c);
            c.a = a;
            return c;
        }

        public static Color Shade(Color c, float f) => new Color(Mathf.Clamp01(c.r * f), Mathf.Clamp01(c.g * f), Mathf.Clamp01(c.b * f), c.a);
        public static Color Mix(Color a, Color b, float t) => Color.Lerp(a, b, t);

        /// <summary>Paleta de ASCII-art: caractere → cor ('.' e ' ' são transparentes).</summary>
        public static void Draw(PixelCanvas c, string[] rowsTopDown, System.Collections.Generic.Dictionary<char, Color> palette, int ox = 0, int oy = 0)
        {
            int h = rowsTopDown.Length;
            for (int r = 0; r < h; r++)
            {
                string row = rowsTopDown[r];
                for (int i = 0; i < row.Length; i++)
                {
                    char ch = row[i];
                    if (ch == '.' || ch == ' ') continue;
                    if (palette.TryGetValue(ch, out var col)) c.Set(ox + i, oy + (h - 1 - r), col);
                }
            }
        }
    }
}

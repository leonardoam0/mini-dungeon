using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ruinas
{
    [Serializable]
    public struct PixelGlyph
    {
        public int code;
        public int x, y, w, h;   // retângulo no atlas (pixels, origem embaixo à esquerda)
        public int advance;
    }

    /// <summary>
    /// Fonte pixelada autoral: glifos de altura fixa (célula) e largura variável. Gerada pelo FontForge.
    /// </summary>
    [CreateAssetMenu(menuName = "Ruinas/Pixel Font")]
    public class PixelFont : ScriptableObject
    {
        public Texture2D atlas;
        [Tooltip("Altura da célula em pixels da fonte (inclui acentos e descendentes).")] public int cellHeight = 11;
        [Tooltip("Linhas acima da linha de base dentro da célula.")] public int baselineFromBottom = 2;
        public int capHeight = 7;
        public int letterSpacing = 1;
        public int spaceAdvance = 3;
        public int lineGap = 2;
        public PixelGlyph[] glyphs;

        [NonSerialized] Dictionary<int, PixelGlyph> map;

        public bool TryGet(int code, out PixelGlyph g)
        {
            if (map == null)
            {
                map = new Dictionary<int, PixelGlyph>();
                if (glyphs != null) foreach (var gl in glyphs) map[gl.code] = gl;
            }
            return map.TryGetValue(code, out g);
        }

        public int Advance(char c)
        {
            if (c == ' ') return spaceAdvance + letterSpacing;
            if (TryGet(c, out var g)) return g.advance + letterSpacing;
            if (TryGet('?', out g)) return g.advance + letterSpacing;
            return spaceAdvance + letterSpacing;
        }

        public int MeasureLine(string s, int start, int end)
        {
            int w = 0;
            for (int i = start; i < end; i++) w += Advance(s[i]);
            if (end > start) w -= letterSpacing;
            return Mathf.Max(0, w);
        }

        public int LineHeight => cellHeight + lineGap;
    }
}

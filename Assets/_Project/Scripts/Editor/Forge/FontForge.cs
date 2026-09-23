using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Ruinas.EditorTools
{
    /// <summary>
    /// Fonte pixelada autoral (5×7 com descendentes e acentos do português). Célula de 11 linhas:
    /// 2 para acentos de maiúsculas, 7 de altura de maiúscula, 2 de descendentes.
    /// </summary>
    public static class FontForge
    {
        public const string TexturePath = "Assets/_Project/Art/Fonts/pixel_font.png";
        public const string AssetPath = "Assets/_Project/Art/Fonts/PixelFont.asset";
        const int Cell = 11;

        static readonly Dictionary<char, string> Glyphs = new Dictionary<char, string>
        {
            ['A'] = ".###.|#...#|#...#|#####|#...#|#...#|#...#",
            ['B'] = "####.|#...#|#...#|####.|#...#|#...#|####.",
            ['C'] = ".####|#....|#....|#....|#....|#....|.####",
            ['D'] = "####.|#...#|#...#|#...#|#...#|#...#|####.",
            ['E'] = "#####|#....|#....|####.|#....|#....|#####",
            ['F'] = "#####|#....|#....|####.|#....|#....|#....",
            ['G'] = ".####|#....|#....|#.###|#...#|#...#|.###.",
            ['H'] = "#...#|#...#|#...#|#####|#...#|#...#|#...#",
            ['I'] = "###|.#.|.#.|.#.|.#.|.#.|###",
            ['J'] = "..###|....#|....#|....#|#...#|#...#|.###.",
            ['K'] = "#...#|#..#.|#.#..|##...|#.#..|#..#.|#...#",
            ['L'] = "#....|#....|#....|#....|#....|#....|#####",
            ['M'] = "#...#|##.##|#.#.#|#.#.#|#...#|#...#|#...#",
            ['N'] = "#...#|##..#|#.#.#|#..##|#...#|#...#|#...#",
            ['O'] = ".###.|#...#|#...#|#...#|#...#|#...#|.###.",
            ['P'] = "####.|#...#|#...#|####.|#....|#....|#....",
            ['Q'] = ".###.|#...#|#...#|#...#|#.#.#|#..#.|.##.#",
            ['R'] = "####.|#...#|#...#|####.|#.#..|#..#.|#...#",
            ['S'] = ".####|#....|#....|.###.|....#|....#|####.",
            ['T'] = "#####|..#..|..#..|..#..|..#..|..#..|..#..",
            ['U'] = "#...#|#...#|#...#|#...#|#...#|#...#|.###.",
            ['V'] = "#...#|#...#|#...#|#...#|.#.#.|.#.#.|..#..",
            ['W'] = "#...#|#...#|#...#|#.#.#|#.#.#|##.##|#...#",
            ['X'] = "#...#|#...#|.#.#.|..#..|.#.#.|#...#|#...#",
            ['Y'] = "#...#|#...#|.#.#.|..#..|..#..|..#..|..#..",
            ['Z'] = "#####|....#|...#.|..#..|.#...|#....|#####",

            ['a'] = ".....|.....|.###.|....#|.####|#...#|.####",
            ['b'] = "#....|#....|####.|#...#|#...#|#...#|####.",
            ['c'] = ".....|.....|.####|#....|#....|#....|.####",
            ['d'] = "....#|....#|.####|#...#|#...#|#...#|.####",
            ['e'] = ".....|.....|.###.|#...#|#####|#....|.####",
            ['f'] = "..##|.#..|####|.#..|.#..|.#..|.#..",
            ['g'] = ".....|.....|.####|#...#|#...#|#...#|.####|....#|.###.",
            ['h'] = "#....|#....|####.|#...#|#...#|#...#|#...#",
            ['i'] = "#|.|#|#|#|#|#",
            ['j'] = "..#|...|..#|..#|..#|..#|..#|..#|##.",
            ['k'] = "#...|#...|#..#|#.#.|##..|#.#.|#..#",
            ['l'] = "#.|#.|#.|#.|#.|#.|.#",
            ['m'] = ".....|.....|##.#.|#.#.#|#.#.#|#.#.#|#.#.#",
            ['n'] = ".....|.....|####.|#...#|#...#|#...#|#...#",
            ['o'] = ".....|.....|.###.|#...#|#...#|#...#|.###.",
            ['p'] = ".....|.....|####.|#...#|#...#|#...#|####.|#....|#....",
            ['q'] = ".....|.....|.####|#...#|#...#|#...#|.####|....#|....#",
            ['r'] = "....|....|#.##|##..|#...|#...|#...",
            ['s'] = ".....|.....|.####|#....|.###.|....#|####.",
            ['t'] = ".#..|.#..|####|.#..|.#..|.#..|..##",
            ['u'] = ".....|.....|#...#|#...#|#...#|#...#|.####",
            ['v'] = ".....|.....|#...#|#...#|#...#|.#.#.|..#..",
            ['w'] = ".....|.....|#...#|#...#|#.#.#|#.#.#|.#.#.",
            ['x'] = ".....|.....|#...#|.#.#.|..#..|.#.#.|#...#",
            ['y'] = ".....|.....|#...#|#...#|#...#|#...#|.####|....#|.###.",
            ['z'] = ".....|.....|#####|...#.|..#..|.#...|#####",

            ['0'] = ".###.|#...#|#..##|#.#.#|##..#|#...#|.###.",
            ['1'] = "..#..|.##..|..#..|..#..|..#..|..#..|.###.",
            ['2'] = ".###.|#...#|....#|...#.|..#..|.#...|#####",
            ['3'] = "####.|....#|....#|.###.|....#|....#|####.",
            ['4'] = "...#.|..##.|.#.#.|#..#.|#####|...#.|...#.",
            ['5'] = "#####|#....|####.|....#|....#|#...#|.###.",
            ['6'] = ".###.|#....|#....|####.|#...#|#...#|.###.",
            ['7'] = "#####|....#|...#.|..#..|.#...|.#...|.#...",
            ['8'] = ".###.|#...#|#...#|.###.|#...#|#...#|.###.",
            ['9'] = ".###.|#...#|#...#|.####|....#|....#|.###.",

            ['.'] = ".|.|.|.|.|.|#",
            [','] = "..|..|..|..|..|..|.#|#.",
            [':'] = ".|.|#|.|.|.|#",
            [';'] = "..|..|.#|..|..|..|.#|#.",
            ['!'] = "#|#|#|#|#|.|#",
            ['?'] = ".###.|#...#|....#|...#.|..#..|.....|..#..",
            ['\''] = "#|#|.|.|.|.|.",
            ['"'] = "#.#|#.#|...|...|...|...|...",
            ['-'] = "....|....|....|####|....|....|....",
            ['+'] = ".....|..#..|..#..|#####|..#..|..#..|.....",
            ['='] = "....|....|####|....|####|....|....",
            ['/'] = "....#|...#.|...#.|..#..|.#...|.#...|#....",
            ['\\'] = "#....|.#...|.#...|..#..|...#.|...#.|....#",
            ['('] = ".#|#.|#.|#.|#.|#.|.#",
            [')'] = "#.|.#|.#|.#|.#|.#|#.",
            ['['] = "##|#.|#.|#.|#.|#.|##",
            [']'] = "##|.#|.#|.#|.#|.#|##",
            ['%'] = "##..#|##.#.|...#.|..#..|.#...|.#.##|#..##",
            ['<'] = "...#|..#.|.#..|#...|.#..|..#.|...#",
            ['>'] = "#...|.#..|..#.|...#|..#.|.#..|#...",
            ['_'] = ".....|.....|.....|.....|.....|.....|#####",
            ['*'] = ".....|#.#.#|.###.|#####|.###.|#.#.#|.....",
            ['#'] = ".#.#.|#####|.#.#.|.#.#.|.#.#.|#####|.#.#.",
            ['&'] = ".##..|#..#.|#.#..|.#...|#.#.#|#..#.|.##.#",
            ['@'] = ".###.|#...#|#.###|#.#.#|#.###|#....|.####",
            ['|'] = "#|#|#|#|#|#|#",
            ['°'] = ".#.|#.#|.#.|...|...|...|...",
            ['·'] = ".|.|.|#|.|.|.",
            ['●'] = ".....|.###.|#####|#####|#####|.###.|.....",
            ['→'] = "......|...#..|....#.|######|....#.|...#..|......",
            ['−'] = "....|....|....|####|....|....|....",
            ['—'] = ".......|.......|.......|#######|.......|.......|.......",
            ['…'] = ".....|.....|.....|.....|.....|.....|#.#.#",
            ['×'] = ".....|#...#|.#.#.|..#..|.#.#.|#...#|.....",
            ['ª'] = "###|..#|###|#.#|###|...|...",
            ['º'] = "###|#.#|#.#|###|...|...|...",
        };

        static readonly Dictionary<string, string[]> Accents = new Dictionary<string, string[]>
        {
            ["acute"] = new[] { "...#.", "..#.." },
            ["grave"] = new[] { ".#...", "..#.." },
            ["circ"] = new[] { "..#..", ".#.#." },
            ["tilde"] = new[] { ".##.#", "#..#." },
            ["uml"] = new[] { ".....", ".#.#." },
        };

        static readonly Dictionary<char, (char, string)> Composed = new Dictionary<char, (char, string)>
        {
            ['á'] = ('a', "acute"), ['à'] = ('a', "grave"), ['â'] = ('a', "circ"), ['ã'] = ('a', "tilde"),
            ['é'] = ('e', "acute"), ['ê'] = ('e', "circ"), ['ó'] = ('o', "acute"), ['ô'] = ('o', "circ"),
            ['õ'] = ('o', "tilde"), ['ú'] = ('u', "acute"), ['ü'] = ('u', "uml"),
            ['Á'] = ('A', "acute"), ['À'] = ('A', "grave"), ['Â'] = ('A', "circ"), ['Ã'] = ('A', "tilde"),
            ['É'] = ('E', "acute"), ['Ê'] = ('E', "circ"), ['Ó'] = ('O', "acute"), ['Ô'] = ('O', "circ"),
            ['Õ'] = ('O', "tilde"), ['Ú'] = ('U', "acute"), ['Ü'] = ('U', "uml"),
        };

        /// <summary>Bitmap da célula (linha 0 = topo).</summary>
        static bool[,] Build(char ch, out int width)
        {
            width = 0;
            string src = null;
            string accent = null;
            bool upper = false;
            if (Composed.TryGetValue(ch, out var comp))
            {
                src = Glyphs[comp.Item1];
                accent = comp.Item2;
                upper = char.IsUpper(comp.Item1);
            }
            else if (ch == 'í') src = ".#|#.|..|#.|#.|#.|#.";
            else if (ch == 'Í') { src = "###|.#.|.#.|.#.|.#.|.#.|###"; accent = "acute3"; upper = true; }
            else if (ch == 'ç') src = ".....|.....|.####|#....|#....|#....|.####|..#..|.##..";
            else if (ch == 'Ç') src = ".####|#....|#....|#....|#....|#....|.####|..#..|.##..";
            else if (!Glyphs.TryGetValue(ch, out src)) return null;

            var rows = src.Split('|');
            foreach (var r in rows) width = Mathf.Max(width, r.Length);
            var bits = new bool[Cell, width];
            for (int r = 0; r < rows.Length && r < 9; r++)
                for (int x = 0; x < rows[r].Length; x++)
                    if (rows[r][x] == '#') bits[r + 2, x] = true;

            if (accent != null)
            {
                string[] a = accent == "acute3" ? new[] { "..#", ".#." } : Accents[accent];
                int top = upper ? 0 : 2;
                int ox = Mathf.Max(0, (width - a[0].Length) / 2);
                for (int r = 0; r < 2; r++)
                    for (int x = 0; x < a[r].Length; x++)
                        if (a[r][x] == '#' && ox + x < width) bits[top + r, ox + x] = true;
            }
            return bits;
        }

        public static void Generate()
        {
            var chars = new List<char>();
            foreach (var k in Glyphs.Keys) chars.Add(k);
            foreach (var k in Composed.Keys) chars.Add(k);
            chars.AddRange(new[] { 'í', 'Í', 'ç', 'Ç' });

            int texW = 256, texH = 128;
            var canvas = new PixelCanvas(texW, texH);
            var glyphs = new List<PixelGlyph>();
            int penX = 1, penY = texH - 1 - Cell; // de cima para baixo
            foreach (var ch in chars)
            {
                var bits = Build(ch, out int w);
                if (bits == null) continue;
                if (penX + w + 1 >= texW)
                {
                    penX = 1;
                    penY -= Cell + 2;
                }
                for (int r = 0; r < Cell; r++)
                    for (int x = 0; x < w; x++)
                        if (bits[r, x]) canvas.Set(penX + x, penY + (Cell - 1 - r), Color.white);
                glyphs.Add(new PixelGlyph { code = ch, x = penX, y = penY, w = w, h = Cell, advance = w });
                penX += w + 2;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(TexturePath));
            File.WriteAllBytes(TexturePath, canvas.ToPNG());
            AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceSynchronousImport);
            var ti = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
            ti.textureType = TextureImporterType.Default;
            ti.filterMode = FilterMode.Point;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.SaveAndReimport();

            var font = AssetDatabase.LoadAssetAtPath<PixelFont>(AssetPath);
            if (font == null)
            {
                font = ScriptableObject.CreateInstance<PixelFont>();
                AssetDatabase.CreateAsset(font, AssetPath);
            }
            font.atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            font.cellHeight = Cell;
            font.baselineFromBottom = 2;
            font.capHeight = 7;
            font.letterSpacing = 1;
            font.spaceAdvance = 3;
            font.lineGap = 2;
            font.glyphs = glyphs.ToArray();
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
        }
    }
}

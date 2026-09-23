using UnityEngine;

namespace Ruinas.EditorTools
{
    /// <summary>
    /// Pinta os tiles do atlas do mundo (32 px por bloco). O piso usa macro-tiles de 2×2 blocos com
    /// ladrilhos biselados cor de terracota (MEDIDO no vídeo: período das juntas de ~13 px no recorte 540×960,
    /// ≈0,4 u por ladrilho → 5 por 2 blocos). Laterais de pedra azul-acinzentada, musgo embaixo, juntas e lascas.
    /// </summary>
    public static class WorldTexturePainter
    {
        public const int T = 32;
        public static int FloorTilesPerMacro = 5;

        // ---------------------------------------------------------------- Paleta (albedo, ajustável por comparação)
        // Terracota: medianas do piso no vídeo ≈ (175–195, 113–122, 82–93) sob a luz da cena.
        static readonly Color FloorBase = Pal.Hex("#a86b55");
        static readonly Color FloorGrout = Pal.Hex("#4a2d25");
        static readonly Color FloorDarkBase = Pal.Hex("#5a6b68");
        static readonly Color FloorDarkGrout = Pal.Hex("#26302e");
        static readonly Color Moss = Pal.Hex("#577a3c");
        static readonly Color MossLight = Pal.Hex("#6f9447");
        static readonly Color Brick = Pal.Hex("#45706c");
        static readonly Color Mortar = Pal.Hex("#1b2d2c");
        static readonly Color BrickDark = Pal.Hex("#2e4d4a");
        static readonly Color Stone = Pal.Hex("#4e5f5d");

        public static PixelCanvas FloorMacro(int variant, bool dark, int seed)
        {
            var c = new PixelCanvas(T * 2, T * 2, seed);
            var baseCol = dark ? FloorDarkBase : FloorBase;
            var grout = dark ? FloorDarkGrout : FloorGrout;
            c.Fill(grout);
            int n = Mathf.Max(2, FloorTilesPerMacro);
            int size = T * 2;
            for (int ty = 0; ty < n; ty++)
            {
                for (int tx = 0; tx < n; tx++)
                {
                    int x0 = tx * size / n, x1 = (tx + 1) * size / n;
                    int y0 = ty * size / n, y1 = (ty + 1) * size / n;
                    float tone = c.R(0.9f, 1.07f);
                    var col = Pal.Shade(baseCol, tone);
                    // Leve deriva de matiz para não ficar plano.
                    col = Color.Lerp(col, dark ? Pal.Hex("#4f6664") : Pal.Hex("#b87a5c"), c.R(0f, 0.3f));
                    PaintBevelTile(c, x0 + 1, y0 + 1, x1 - x0 - 2, y1 - y0 - 2, col, grout);
                    if (variant == 2 && c.R() < 0.45f) Crack(c, x0 + 2, y0 + 2, x1 - x0 - 4, y1 - y0 - 4, Pal.Shade(grout, 1.1f));
                    if (c.R() < 0.15f) Chip(c, x0, y0, x1 - x0, y1 - y0, grout);
                }
            }
            if (variant == 2)
            {
                // Musgo nas juntas e em algumas pedras.
                for (int k = 0; k < 7; k++)
                {
                    int gx = c.RI(0, n) * size / n + c.RI(-1, 2);
                    int gy = c.RI(2, size - 2);
                    c.Blob(gx, gy, c.RI(6, 14), c.R() < 0.5f ? Moss : MossLight);
                }
            }
            else if (variant == 1)
            {
                for (int k = 0; k < 4; k++) c.Speckle(c.RI(0, size - 6), c.RI(0, size - 6), 6, 6, Pal.Shade(baseCol, 0.78f), 0.12f);
            }
            return c;
        }

        static void PaintBevelTile(PixelCanvas c, int x, int y, int w, int h, Color col, Color grout)
        {
            var hi = Pal.Shade(col, 1.16f);
            var hi2 = Pal.Shade(col, 1.07f);
            var sh = Pal.Shade(col, 0.8f);
            var sh2 = Pal.Shade(col, 0.9f);
            for (int j = 0; j < h; j++)
            {
                for (int i = 0; i < w; i++)
                {
                    bool top = j >= h - 2, bottom = j <= 1, left = i <= 1, right = i >= w - 2;
                    Color p = col;
                    if (top) p = j == h - 1 ? hi : hi2;
                    if (left) p = i == 0 ? hi : (top ? hi : hi2);
                    if (bottom) p = j == 0 ? sh : sh2;
                    if (right) p = i == w - 1 ? sh : (bottom ? sh : sh2);
                    c.Set(x + i, y + j, p);
                }
            }
            // Cantos arredondados.
            c.Set(x, y, grout); c.Set(x + w - 1, y, grout); c.Set(x, y + h - 1, grout); c.Set(x + w - 1, y + h - 1, grout);
            c.Jitter(x + 2, y + 2, w - 4, h - 4, 0.035f);
            c.Speckle(x + 2, y + 2, w - 4, h - 4, Pal.Shade(col, 0.88f), 0.05f);
            c.Speckle(x + 2, y + 2, w - 4, h - 4, Pal.Shade(col, 1.08f), 0.04f);
        }

        static void Crack(PixelCanvas c, int x, int y, int w, int h, Color col)
        {
            int px = x + c.RI(0, w), py = y + c.RI(0, h);
            int len = c.RI(4, 9);
            for (int k = 0; k < len; k++)
            {
                c.Set(px, py, col);
                px += c.RI(-1, 2);
                py += c.R() < 0.5f ? 1 : -1;
                px = Mathf.Clamp(px, x, x + w - 1);
                py = Mathf.Clamp(py, y, y + h - 1);
            }
        }

        static void Chip(PixelCanvas c, int x, int y, int w, int h, Color col)
        {
            int cx = c.R() < 0.5f ? x + 1 : x + w - 3;
            int cy = c.R() < 0.5f ? y + 1 : y + h - 3;
            c.Set(cx, cy, col); c.Set(cx + 1, cy, col); c.Set(cx, cy + 1, col);
        }

        // ---------------------------------------------------------------- Tiles individuais

        public static PixelCanvas Paint(Tile t, int seed)
        {
            var c = new PixelCanvas(T, T, seed);
            switch (t)
            {
                case Tile.WallBrick: Bricks(c, Brick, Mortar, false, 0); break;
                case Tile.WallBrick2: Bricks(c, Pal.Shade(Brick, 0.95f), Mortar, false, 1); break;
                case Tile.WallBrickMossy: Bricks(c, Brick, Mortar, false, 2); MossBottom(c, 0.55f); break;
                case Tile.WallBrickMossy2: Bricks(c, Pal.Shade(Brick, 0.93f), Mortar, false, 3); MossBottom(c, 0.85f); break;
                case Tile.WallBrickDark: Bricks(c, BrickDark, Pal.Shade(Mortar, 0.8f), false, 4); break;
                case Tile.WallCap: Cap(c, Pal.Shade(Brick, 1.1f)); break;
                case Tile.PillarSide: PillarSide(c); break;
                case Tile.PillarTop: Cap(c, Pal.Shade(Brick, 1.18f)); c.Outline(3, 3, 26, 26, Pal.Shade(Brick, 0.75f)); break;
                case Tile.StoneRough: Cobble(c, Stone, Pal.Shade(Stone, 0.55f), 6); break;
                case Tile.StoneRoughDark: Cobble(c, Pal.Shade(Stone, 0.72f), Pal.Shade(Stone, 0.4f), 6); break;
                case Tile.DirtTop: Dirt(c, false); break;
                case Tile.DirtTop2: Dirt(c, true); break;
                case Tile.DirtSide: DirtSide(c); break;
                case Tile.GrassTop: Grass(c, false); break;
                case Tile.GrassTop2: Grass(c, true); break;
                case Tile.GrassSide: GrassSide(c); break;
                case Tile.LeafLitter: Litter(c); break;
                case Tile.RedBrickSide: Bricks(c, Pal.Hex("#8f4f3e"), Pal.Hex("#3f231c"), true, 5); break;
                case Tile.RedBrickTop: Cap(c, Pal.Hex("#9a5a46")); break;
                case Tile.GoldSide: Gold(c, false); break;
                case Tile.GoldTop: Gold(c, true); break;
                case Tile.WoodSide: Wood(c, false); break;
                case Tile.WoodTop: Wood(c, true); break;
                case Tile.CarvedTop: Carved(c); break;
                case Tile.PathCobble: Cobble(c, Pal.Hex("#6b6a60"), Pal.Hex("#2f2e2a"), 5); break;
                case Tile.PathCobble2: Cobble(c, Pal.Hex("#666a60"), Pal.Hex("#2d302b"), 4); MossSpots(c, 3); break;
                case Tile.Water: Water(c); break;
                case Tile.Leaves: Leaves(c, false); break;
                case Tile.LeavesDark: Leaves(c, true); break;
                case Tile.ObsidianSide: Obsidian(c, false); break;
                case Tile.ObsidianTop: Obsidian(c, true); break;
                case Tile.MossTop: Cap(c, Pal.Shade(Brick, 1.05f)); MossSpots(c, 9); break;
                case Tile.StairFront: StairFront(c); break;
                case Tile.BambooSide: Bamboo(c, false); break;
                case Tile.BambooTop: Bamboo(c, true); break;
                case Tile.SandTop: c.Fill(Pal.Hex("#b09a6a")); c.Jitter(0, 0, T, T, 0.06f); break;
                default: c.Fill(Color.magenta); break;
            }
            return c;
        }

        /// <summary>Blocos grandes de pedra em fiadas desencontradas (2 fiadas por bloco).</summary>
        static void Bricks(PixelCanvas c, Color brick, Color mortar, bool small, int seedOffset)
        {
            c.Fill(mortar);
            int rows = small ? 4 : 2;
            int rowH = T / rows;
            for (int r = 0; r < rows; r++)
            {
                int y0 = r * rowH;
                int offset = (r % 2 == 0) ? 0 : (small ? 5 : 9);
                int x = -offset;
                while (x < T)
                {
                    int w = small ? c.RI(8, 12) : c.RI(13, 19);
                    var col = Pal.Shade(brick, c.R(0.88f, 1.1f));
                    col = Color.Lerp(col, Pal.Hex("#4a7d6a"), c.R(0f, 0.15f));
                    for (int j = 1; j < rowH; j++)
                    {
                        for (int i = x + 1; i < x + w; i++)
                        {
                            int ii = ((i % T) + T) % T;
                            Color p = col;
                            if (j == rowH - 1) p = Pal.Shade(col, 1.18f);
                            else if (j == rowH - 2) p = Pal.Shade(col, 1.06f);
                            else if (j == 1) p = Pal.Shade(col, 0.8f);
                            if (i == x + 1) p = Pal.Shade(p, 1.08f);
                            if (i == x + w - 1) p = Pal.Shade(p, 0.85f);
                            c.Set(ii, y0 + j, p);
                        }
                    }
                    x += w;
                }
            }
            c.Jitter(0, 0, T, T, 0.04f);
            // Lascas e poros.
            for (int k = 0; k < 5; k++) c.Set(c.RI(0, T), c.RI(0, T), Pal.Shade(mortar, 1.3f));
        }

        static void MossBottom(PixelCanvas c, float amount)
        {
            for (int x = 0; x < T; x++)
            {
                int h = Mathf.RoundToInt(c.R(2f, 9f) * amount * 1.6f);
                for (int y = 0; y < h; y++)
                    if (c.R() < 0.85f - y * 0.06f) c.Set(x, y, c.R() < 0.3f ? MossLight : Moss);
            }
            for (int k = 0; k < (int)(6 * amount); k++) c.Blob(c.RI(0, T), c.RI(8, 24), c.RI(3, 7), c.R() < 0.5f ? Moss : MossLight);
        }

        static void MossSpots(PixelCanvas c, int n)
        {
            for (int k = 0; k < n; k++) c.Blob(c.RI(2, T - 2), c.RI(2, T - 2), c.RI(5, 12), c.R() < 0.5f ? Moss : MossLight);
        }

        static void Cap(PixelCanvas c, Color col)
        {
            c.Fill(col);
            c.Jitter(0, 0, T, T, 0.05f);
            for (int i = 0; i < T; i++)
            {
                c.Set(i, T - 1, Pal.Shade(col, 1.2f));
                c.Set(0, i, Pal.Shade(col, 1.12f));
                c.Set(i, 0, Pal.Shade(col, 0.72f));
                c.Set(T - 1, i, Pal.Shade(col, 0.8f));
            }
            c.Speckle(2, 2, T - 4, T - 4, Pal.Shade(col, 0.85f), 0.06f);
        }

        static void PillarSide(PixelCanvas c)
        {
            var col = Pal.Shade(Brick, 1.05f);
            c.Fill(Mortar);
            // Uma pedra alta por bloco com friso entalhado.
            for (int y = 1; y < T - 1; y++)
                for (int x = 1; x < T - 1; x++)
                    c.Set(x, y, col);
            c.Jitter(1, 1, T - 2, T - 2, 0.05f);
            for (int x = 1; x < T - 1; x++) { c.Set(x, T - 2, Pal.Shade(col, 1.2f)); c.Set(x, 1, Pal.Shade(col, 0.75f)); }
            for (int y = 1; y < T - 1; y++) { c.Set(1, y, Pal.Shade(col, 1.1f)); c.Set(T - 2, y, Pal.Shade(col, 0.8f)); }
            for (int x = 6; x < T - 6; x++) { c.Set(x, 20, Pal.Shade(col, 0.6f)); c.Set(x, 21, Pal.Shade(col, 1.15f)); c.Set(x, 10, Pal.Shade(col, 0.6f)); c.Set(x, 11, Pal.Shade(col, 1.15f)); }
            c.Speckle(2, 2, T - 4, T - 4, Pal.Shade(col, 0.82f), 0.05f);
        }

        static void Cobble(PixelCanvas c, Color stone, Color gap, int n)
        {
            c.Fill(gap);
            for (int k = 0; k < n * n; k++)
            {
                int cx = c.RI(0, T), cy = c.RI(0, T);
                int rx = c.RI(3, 6), ry = c.RI(2, 5);
                var col = Pal.Shade(stone, c.R(0.82f, 1.12f));
                for (int y = -ry; y <= ry; y++)
                    for (int x = -rx; x <= rx; x++)
                    {
                        float d = (x * x) / (float)(rx * rx) + (y * y) / (float)(ry * ry);
                        if (d > 1f) continue;
                        int px = ((cx + x) % T + T) % T, py = ((cy + y) % T + T) % T;
                        var p = col;
                        if (y > ry * 0.4f) p = Pal.Shade(col, 1.12f);
                        if (y < -ry * 0.5f) p = Pal.Shade(col, 0.82f);
                        c.Set(px, py, p);
                    }
            }
            c.Jitter(0, 0, T, T, 0.04f);
        }

        static void Dirt(PixelCanvas c, bool alt)
        {
            c.Fill(Pal.Hex(alt ? "#3a3026" : "#40352a"));
            c.Jitter(0, 0, T, T, 0.1f);
            c.Speckle(0, 0, T, T, Pal.Hex("#4b3828"), 0.12f);
            c.Speckle(0, 0, T, T, Pal.Hex("#2a1f17"), 0.1f);
            int leaves = alt ? 7 : 4;
            for (int k = 0; k < leaves; k++) Leaf(c, c.RI(1, T - 3), c.RI(1, T - 3));
        }

        static void Leaf(PixelCanvas c, int x, int y)
        {
            Color[] cols = { Pal.Hex("#5e5227"), Pal.Hex("#3e5a28"), Pal.Hex("#7a4326"), Pal.Hex("#6b6a2c") };
            var col = cols[c.RI(0, cols.Length)];
            c.Set(x, y, col); c.Set(x + 1, y, col); c.Set(x + 1, y + 1, Pal.Shade(col, 1.15f)); c.Set(x, y + 1, Pal.Shade(col, 0.85f));
            if (c.R() < 0.5f) c.Set(x + 2, y + 1, col);
        }

        static void DirtSide(PixelCanvas c)
        {
            c.Fill(Pal.Hex("#36281e"));
            c.Jitter(0, 0, T, T, 0.1f);
            c.Speckle(0, 0, T, T, Pal.Hex("#4a3726"), 0.1f);
            for (int k = 0; k < 4; k++)
            {
                int x = c.RI(0, T), y = c.RI(0, T);
                for (int i = 0; i < c.RI(3, 8); i++) c.Set(x + i, y + (i % 3 == 0 ? 1 : 0), Pal.Hex("#5a4632"));
            }
            for (int k = 0; k < 3; k++) c.Rect(c.RI(0, T - 3), c.RI(0, T - 2), 3, 2, Pal.Hex("#4e5552"));
        }

        static void Grass(PixelCanvas c, bool alt)
        {
            c.Fill(Pal.Hex(alt ? "#2c482a" : "#2f4d2c"));
            c.Jitter(0, 0, T, T, 0.1f);
            for (int k = 0; k < 60; k++)
            {
                int x = c.RI(0, T), y = c.RI(0, T);
                var col = c.R() < 0.5f ? Pal.Hex("#3e6a35") : Pal.Hex("#4b7a3d");
                c.Set(x, y, col);
                c.Set(x, y + 1, Pal.Shade(col, 1.15f));
            }
            if (alt) for (int k = 0; k < 3; k++) Leaf(c, c.RI(1, T - 3), c.RI(1, T - 3));
        }

        static void GrassSide(PixelCanvas c)
        {
            DirtSide(c);
            for (int x = 0; x < T; x++)
            {
                int h = c.RI(3, 8);
                for (int y = 0; y < h; y++) c.Set(x, T - 1 - y, y == h - 1 ? Pal.Hex("#2a4426") : (c.R() < 0.5f ? Pal.Hex("#355a30") : Pal.Hex("#2f4d2c")));
            }
        }

        static void Litter(PixelCanvas c)
        {
            // Folhiço do entorno: oliva-petróleo mais claro (no vídeo o fundo não é quase preto).
            c.Fill(Pal.Hex("#35402f"));
            c.Jitter(0, 0, T, T, 0.12f);
            for (int k = 0; k < 16; k++) Leaf(c, c.RI(0, T - 2), c.RI(0, T - 2));
            c.Speckle(0, 0, T, T, Pal.Hex("#4a5e44"), 0.08f);
        }

        static void Gold(PixelCanvas c, bool top)
        {
            var baseCol = Pal.Hex("#c49a3a");
            c.Fill(baseCol);
            c.Jitter(0, 0, T, T, 0.05f);
            var outline = Pal.Hex("#4a3410");
            for (int i = 0; i < T; i++) { c.Set(i, 0, outline); c.Set(i, T - 1, outline); c.Set(0, i, outline); c.Set(T - 1, i, outline); }
            for (int i = 1; i < T - 1; i++) { c.Set(i, T - 2, Pal.Hex("#f0d06a")); c.Set(1, i, Pal.Hex("#e2bd55")); c.Set(i, 1, Pal.Hex("#8f6a22")); c.Set(T - 2, i, Pal.Hex("#9a742a")); }
            if (top) c.Outline(8, 8, 16, 16, Pal.Hex("#8a6420"));
            else for (int x = 4; x < T - 4; x += 6) c.Rect(x, 14, 2, 4, Pal.Hex("#7a5a1c"));
        }

        static void Wood(PixelCanvas c, bool top)
        {
            var col = Pal.Hex("#6a482c");
            c.Fill(col);
            if (top)
            {
                for (int r = 0; r < 4; r++) { int y = r * 8; for (int x = 0; x < T; x++) c.Set(x, y, Pal.Hex("#3e2a18")); }
                for (int k = 0; k < 40; k++) c.Set(c.RI(0, T), c.RI(0, T), Pal.Hex("#5a3c22"));
            }
            else
            {
                for (int p = 0; p < 4; p++)
                {
                    int x0 = p * 8;
                    for (int y = 0; y < T; y++) c.Set(x0, y, Pal.Hex("#3e2a18"));
                    for (int k = 0; k < 6; k++)
                    {
                        int gx = x0 + c.RI(2, 7), gy = c.RI(0, T - 6);
                        for (int y = 0; y < c.RI(3, 7); y++) c.Set(gx, gy + y, Pal.Hex("#553920"));
                    }
                    c.Set(x0 + 4, c.RI(4, T - 4), Pal.Hex("#2e1e10"));
                }
            }
            c.Jitter(0, 0, T, T, 0.05f);
        }

        static void Carved(PixelCanvas c)
        {
            var col = Pal.Hex("#7d8079");
            Cap(c, col);
            var deep = Pal.Hex("#3e423d");
            var lip = Pal.Hex("#9ea296");
            // Glifo quadrado em espiral, como os ladrilhos entalhados das bordas da arena.
            c.Outline(4, 4, 24, 24, deep);
            c.Outline(8, 8, 16, 16, deep);
            c.Line(8, 8, 8, 20, deep);
            c.Line(12, 12, 20, 12, deep);
            c.Line(20, 12, 20, 20, deep);
            c.Line(12, 16, 16, 16, deep);
            for (int i = 5; i < 27; i++) { c.Set(i, 27, lip); c.Set(27, i, lip); }
        }

        static void Water(PixelCanvas c)
        {
            c.Fill(Pal.Hex("#1a4b57"));
            c.Jitter(0, 0, T, T, 0.05f);
            for (int k = 0; k < 9; k++)
            {
                int x = c.RI(0, T), y = c.RI(0, T), len = c.RI(3, 8);
                for (int i = 0; i < len; i++) c.Set((x + i) % T, y, k % 3 == 0 ? Pal.Hex("#3e909b") : Pal.Hex("#2b6f7c"));
            }
        }

        static void Leaves(PixelCanvas c, bool dark)
        {
            // Folhagem puxada para o azul-petróleo do entorno no vídeo (verde menos saturado).
            var baseCol = dark ? Pal.Hex("#27493d") : Pal.Hex("#34604e");
            c.Fill(Pal.Shade(baseCol, 0.6f));
            for (int k = 0; k < 26; k++)
            {
                int x = c.RI(0, T), y = c.RI(0, T);
                var col = Pal.Shade(baseCol, c.R(0.9f, 1.35f));
                for (int j = 0; j < 3; j++)
                    for (int i = 0; i < 4 - j; i++)
                        c.Set((x + i + j) % T, (y + j) % T, j == 2 ? Pal.Shade(col, 1.15f) : col);
            }
            c.Jitter(0, 0, T, T, 0.06f);
        }

        static void Obsidian(PixelCanvas c, bool top)
        {
            c.Fill(Pal.Hex("#231a2c"));
            c.Jitter(0, 0, T, T, 0.08f);
            c.Speckle(0, 0, T, T, Pal.Hex("#3a2b4a"), 0.1f);
            c.Speckle(0, 0, T, T, Pal.Hex("#5a4478"), 0.02f);
            if (!top) for (int x = 0; x < T; x++) { c.Set(x, 15, Pal.Hex("#15101b")); c.Set(x, 16, Pal.Hex("#3a2d48")); }
        }

        static void StairFront(PixelCanvas c)
        {
            var col = Pal.Hex("#6c675c");
            c.Fill(col);
            c.Jitter(0, 0, T, T, 0.05f);
            // Degrau: lábio claro no topo de cada meio bloco e sombra embaixo.
            for (int x = 0; x < T; x++)
            {
                c.Set(x, T - 1, Pal.Hex("#a39c8c")); c.Set(x, T - 2, Pal.Hex("#8c8677"));
                c.Set(x, 15, Pal.Hex("#a39c8c")); c.Set(x, 14, Pal.Hex("#8c8677"));
                c.Set(x, 0, Pal.Hex("#3b3832")); c.Set(x, 16, Pal.Hex("#3b3832"));
            }
            for (int x = 0; x < T; x += c.RI(9, 14)) for (int y = 1; y < 14; y++) c.Set(x, y, Pal.Hex("#4a463e"));
            MossBottom(c, 0.3f);
        }

        static void Bamboo(PixelCanvas c, bool top)
        {
            if (top)
            {
                c.Fill(Pal.Hex("#5d6a28"));
                c.Outline(4, 4, 24, 24, Pal.Hex("#8a9a3a"));
                return;
            }
            c.Fill(Pal.Hex("#1f2a12"));
            for (int s = 0; s < 3; s++)
            {
                int x0 = 2 + s * 10;
                for (int y = 0; y < T; y++)
                    for (int x = x0; x < x0 + 8; x++)
                    {
                        var col = x == x0 ? Pal.Hex("#a3b04a") : x == x0 + 7 ? Pal.Hex("#5a6a26") : Pal.Hex("#7d8c38");
                        c.Set(x, y, col);
                    }
                int node = c.RI(8, 24);
                for (int x = x0; x < x0 + 8; x++) { c.Set(x, node, Pal.Hex("#4a5520")); c.Set(x, node + 1, Pal.Hex("#b8c45a")); }
            }
        }
    }
}

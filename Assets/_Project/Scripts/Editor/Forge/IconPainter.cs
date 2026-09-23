using System.Collections.Generic;
using UnityEngine;

namespace Ruinas.EditorTools
{
    /// <summary>Ícones em pixel art autoral (grades ASCII) e sprites da interface gerados por forma.</summary>
    public static class IconPainter
    {
        static Dictionary<char, Color> P(params (char, string)[] entries)
        {
            var d = new Dictionary<char, Color>();
            foreach (var (k, hex) in entries) d[k] = Pal.Hex(hex);
            return d;
        }

        static PixelCanvas Draw(string[] rows, Dictionary<char, Color> pal)
        {
            int h = rows.Length, w = 0;
            foreach (var r in rows) w = Mathf.Max(w, r.Length);
            var c = new PixelCanvas(w, h);
            Pal.Draw(c, rows, pal);
            return c;
        }

        // ---------------------------------------------------------------- Artefatos (20×20)

        public static PixelCanvas GoldenSheaf() => Draw(new[]
        {
            "............Y.y.....",
            "..........Y.yYyy....",
            ".........yYyyYYyY...",
            "........YyyYyyyYy...",
            "......y.yYyYyYyy.y..",
            ".......yYyyYyyYyY...",
            "......yyYyYyyYy.....",
            ".....yYyYyyYyy......",
            "......yyyYyy........",
            ".....ogrrrgo........",
            "....ogrRrrgo........",
            "....orrRrro.........",
            "...ogggggo..........",
            "..ogbgbgo...........",
            "..obgbgo............",
            ".ogbgbo.............",
            ".obgbo..............",
            "ogbgo...............",
            "obbo................",
            "oo..................",
        }, P(('Y', "#fff08a"), ('y', "#dcc23a"), ('g', "#a4a334"), ('b', "#7a6a22"), ('r', "#c02830"), ('R', "#e0484a"), ('o', "#2a2410")));

        public static PixelCanvas Feather() => Draw(new[]
        {
            "...............ooo..",
            ".............ooLLLo.",
            "............oLlLLLo.",
            "...........oLlLlLLo.",
            "..........olLlLlLo..",
            ".........owlLlLlo...",
            "........owwlLlLo....",
            ".......owwwlLlo.....",
            "......owwwwlLo......",
            ".....owwwwwlo.......",
            "....owwwwwwo........",
            "...owwwwwwo.........",
            "...owwwwwo..........",
            "..owwwwwo...........",
            "..owwwwo............",
            ".oqwwwo.............",
            ".oqqoo..............",
            "oqqo................",
            "oqo.................",
            "oo..................",
        }, P(('w', "#f3f6fa"), ('l', "#9fdcff"), ('L', "#3a98f0"), ('q', "#a8aeb6"), ('o', "#1c2a3c")));

        public static PixelCanvas PulseBell() => Draw(new[]
        {
            "........oooo........",
            ".......oMMMMo.......",
            "......oMmmmMMo......",
            ".....oMmWWmmMMo.....",
            "....oMmWWmmmmMMo....",
            "....oMmWmmmmmmMo....",
            "...oMmmmmmmmmmMMo...",
            "...oMmmmmmmmmmmMo...",
            "...oMmmmmmmmmmmMo...",
            "..oMmmmmmmmmmmmMMo..",
            "..oMmmmmmmmmmmmmMo..",
            "..ooooooooooooooooo.",
            "..oggggggggggggggo..",
            "...oooooooooooooo...",
            "........oYYo........",
            "........oYYo........",
            ".........oo.........",
            ".....p........p.....",
            "...p...p....p...p...",
            ".....p........p.....",
        }, P(('M', "#9a2fa8"), ('m', "#d45ad8"), ('W', "#ffd6ff"), ('g', "#d6a93c"), ('Y', "#f0d06a"), ('p', "#f07af0"), ('o', "#2a0f2e")));

        // ---------------------------------------------------------------- HUD

        static readonly string[] PotionRows =
        {
            "......oooooooo......",
            "......oCCCCCCo......",
            "......oCccccCo......",
            ".......oooooo.......",
            ".......oGggGo.......",
            ".....ooGggggGoo.....",
            "....oGGggggggGGo....",
            "...oGgWggggggggGo...",
            "...oGWRRRRRRRRRGo...",
            "...oGRRrRRRRRRRGo...",
            "...oGRrrRRRRRRRGo...",
            "...oGRRRRRRRRRRGo...",
            "...oGRRRRRRRRRdGo...",
            "...oGRRRRRRRRddGo...",
            "...oGRRRRRRRdddGo...",
            "...oGGRRRRRRRRGGo...",
            "....oGGGGGGGGGGo....",
            ".....oooooooooo.....",
            "....................",
            "....................",
        };

        public static PixelCanvas Potion() => Draw(PotionRows, P(('C', "#e0883a"), ('c', "#b4622a"), ('G', "#9fd8ee"), ('g', "#d8f2fa"), ('W', "#ffffff"), ('R', "#e02a2a"), ('r', "#ff7a6a"), ('d', "#a01818"), ('o', "#1e1a22")));

        /// <summary>Poção de rapidez: mesmo frasco com líquido azul-claro.</summary>
        public static PixelCanvas SwiftPotion() => Draw(PotionRows, P(('C', "#e0883a"), ('c', "#b4622a"), ('G', "#9fd8ee"), ('g', "#e8f8ff"), ('W', "#ffffff"), ('R', "#4aa6f5"), ('r', "#b8e6ff"), ('d', "#2468c0"), ('o', "#1a1e2a")));

        public static PixelCanvas MapScroll() => Draw(new[]
        {
            "....................",
            "..oooooooooooooooo..",
            ".obbbbbbbbbbbbbbbbo.",
            ".oPPPPPPPPPPPPPPPPo.",
            ".oPppPPPPPPPPPPPPPo.",
            ".oPpPPPPPPPPPkPkPPo.",
            ".oPPPPPpPPPPPPkPPPo.",
            ".oPPPPPPPPPPPkPkPPo.",
            ".oPPpPPPPPPPPPPPPPo.",
            ".oPPPPPPpPPPpPPPPPo.",
            ".oPPPPPPPPPPPPPpPPo.",
            ".oPPPpPPPPpPPPPPPPo.",
            ".oPPPPPPPPPPPPPPPPo.",
            ".oPPPPpPPPPPPpPPPPo.",
            ".oPPPPPPPPPPPPPPPPo.",
            ".obbbbbbbbbbbbbbbbo.",
            "..oooooooooooooooo..",
            "....................",
            "....................",
            "....................",
        }, P(('b', "#8a6a44"), ('P', "#e8d7ae"), ('p', "#b99a6a"), ('k', "#3a2a1a"), ('o', "#2a2016")));

        public static PixelCanvas Lives() => Draw(new[]
        {
            "..oooo..",
            ".oTTTTo.",
            ".oTttTo.",
            "..oTTo..",
            ".oTTTTo.",
            "oTRRRRTo",
            "oTRrRRTo",
            ".oRRRRo.",
            "..oRRo..",
            ".oTTTTo.",
            "oTTooTTo",
        }, P(('T', "#e0cfa0"), ('t', "#8a7a5a"), ('R', "#d42a2a"), ('r', "#ff8a7a"), ('o', "#2a2016")));

        public static PixelCanvas ArrowIcon() => Draw(new[]
        {
            "............oooo",
            "...........oWWWo",
            "..........oWwWWo",
            ".........oWwWo..",
            "........obWo....",
            ".......obbo.....",
            "......obbo......",
            ".....obbo.......",
            "....obbo........",
            "...obbo.........",
            "..offo..........",
            ".oFfFo..........",
            "oFfFo...........",
            "oFFo............",
            ".oo.............",
            "................",
        }, P(('W', "#e0e4ea"), ('w', "#9aa2ac"), ('b', "#8a6436"), ('f', "#f4f4f4"), ('F', "#c83a3a"), ('o', "#1e1814")));

        public static PixelCanvas Emerald() => Draw(new[]
        {
            "....oooooo....",
            "...oLLGGGGo...",
            "..oLLGGGGggo..",
            ".oLLGGGGGGggo.",
            "oLLGGGGGGGGggo",
            "oLGGGGGGGGGggo",
            "oGGGGGGGGGGggo",
            ".oGGGGGGGGggo.",
            "..oGGGGGGggo..",
            "...oGGGGggo...",
            "....oGGggo....",
            ".....oggo.....",
            "......oo......",
        }, P(('L', "#b8ffb0"), ('G', "#3ec24a"), ('g', "#1e8a34"), ('o', "#0c2a12")));

        public static PixelCanvas HealthOrb() => Draw(new[]
        {
            "...oooo...",
            "..oRRRRo..",
            ".oRrWRRRo.",
            "oRrWRRRRRo",
            "oRRRRRRRRo",
            "oRRRRRRRdo",
            "oRRRRRRddo",
            ".oRRRRddo.",
            "..oRRddo..",
            "...oooo...",
        }, P(('R', "#e0303a"), ('r', "#ff8a8a"), ('W', "#ffffff"), ('d', "#9a1420"), ('o', "#2a0a0e")));

        // ---------------------------------------------------------------- Armas e armaduras (20×20)

        public static PixelCanvas Sword() => Draw(new[]
        {
            ".................oo.",
            "................oWWo",
            "...............oWwWo",
            "..............oWwWo.",
            ".............oWwWo..",
            "............oWwWo...",
            "...........oWwWo....",
            "..........oWwWo.....",
            ".........oWwWo......",
            "........oWwWo.......",
            ".......oWwWo........",
            "..oo..oWwWo.........",
            "..oGooWwWo..........",
            "...oGGwWo...........",
            "....oGGo............",
            "...obbGGo...........",
            "..obbo.oGo..........",
            ".obbo...oo..........",
            "oGbo................",
            "ooo.................",
        }, P(('W', "#e8eef2"), ('w', "#9aa6b0"), ('G', "#d6a93c"), ('b', "#6a4428"), ('o', "#1a1614")));

        public static PixelCanvas Axe() => Draw(new[]
        {
            "............ooooo...",
            "..........ooSSSSSo..",
            ".........oSSsssSSSo.",
            "........oSSsoossSSo.",
            ".......oSSso..osSSo.",
            "......obbso....oSo..",
            ".....obbbo......o...",
            "....obbbo...........",
            "...obbbo............",
            "..obbbo.............",
            "..obbo..............",
            ".obbo...............",
            ".obo................",
            "obbo................",
            "obo.................",
            "oo..................",
            "....................",
            "....................",
            "....................",
            "....................",
        }, P(('S', "#c8d0d6"), ('s', "#7d8890"), ('b', "#7a5230"), ('o', "#1a1614")));

        public static PixelCanvas Spear() => Draw(new[]
        {
            "................ooo.",
            "...............oWWo.",
            "..............oWwWo.",
            ".............oWwWWo.",
            "............oWwWoo..",
            "...........oGGWo....",
            "..........oGbGo.....",
            ".........obbo.......",
            "........obbo........",
            ".......obbo.........",
            "......obbo..........",
            ".....obbo...........",
            "....obbo............",
            "...obbo.............",
            "..obbo..............",
            ".obbo...............",
            "obbo................",
            "obo.................",
            "oo..................",
            "....................",
        }, P(('W', "#e8eef2"), ('w', "#9aa6b0"), ('G', "#d6a93c"), ('b', "#8a5a30"), ('o', "#1a1614")));

        public static PixelCanvas Bow() => Draw(new[]
        {
            ".........oooo.......",
            "........obbbbo......",
            ".......obboobbo.....",
            "......obbo..obo.....",
            ".....obbo....oso....",
            "....obbo......so....",
            "....obo.......s.....",
            "...obbo......s......",
            "...obo......s.......",
            "...obo.....s........",
            "...obo....s.........",
            "...obo...s..........",
            "...obbo.s...........",
            "....obos............",
            "....obbso...........",
            ".....obbo...........",
            "......obbo..........",
            ".......obboo........",
            "........obbbo.......",
            ".........oooo.......",
        }, P(('b', "#9a6a34"), ('s', "#e8e2d0"), ('o', "#1e1610")));

        public static PixelCanvas Crossbow() => Draw(new[]
        {
            "....................",
            "..oooooooooooooo....",
            ".obbbbbbbbbbbbbbo...",
            ".oooooooboooooooo...",
            ".......obo..........",
            "..ssssobbbossss.....",
            "......obWbo.........",
            "......obWbo.........",
            "......obWbo.........",
            "......obWbo.........",
            "......obbbo.........",
            "......obbbo.........",
            "......obbbo.........",
            ".....obbbbbo........",
            ".....obdddbo........",
            ".....obdddbo........",
            "......ooooo.........",
            "....................",
            "....................",
            "....................",
        }, P(('b', "#7a5230"), ('d', "#4a3018"), ('s', "#d8d2c0"), ('W', "#c8d0d6"), ('o', "#1a1614")));

        public static PixelCanvas ArmorPlate() => Draw(new[]
        {
            "....................",
            "...oooo......oooo...",
            "..oWWWWoooooooWWWo..",
            ".oWWwwWWGGGGWWwwWWo.",
            ".oWwwwwWGWWGWwwwwWo.",
            ".oWWwwoWWWWWWowwWWo.",
            "..oooooWWMMWWooooo..",
            "......oWWMMWWo......",
            "......oWWWWWWo......",
            "......owwWWwwo......",
            "......oWWWWWWo......",
            "......owwWWwwo......",
            "......oGGGGGGo......",
            "......obbGGbbo......",
            "......oooooooo......",
            "....................",
            "....................",
            "....................",
            "....................",
            "....................",
        }, P(('W', "#dde2e6"), ('w', "#a9b2ba"), ('G', "#d6a93c"), ('M', "#d14ad1"), ('b', "#5a3b24"), ('o', "#1e2226")));

        public static PixelCanvas ArmorMoss() => Draw(new[]
        {
            "....................",
            ".......oooooo.......",
            "......oCCCCCCo......",
            ".....oCCccccCCo.....",
            "....oCCcoooocCCo....",
            "...oCCco....ocCCo...",
            "...oCcoWWWWWWocCo...",
            "..oCCcoWWWWWWocCCo..",
            "..oCccowwWWwwoccCo..",
            "..oCccoWWWWWWoccCo..",
            ".oCCccoGGGGGGoccCCo.",
            ".oCccccoooooocccCCo.",
            ".oCcccccccccccccCCo.",
            ".oCCcCccCcCccCcCCo..",
            "..ooCooCooCooCooo...",
            "....o..o..o..o......",
            "....................",
            "....................",
            "....................",
            "....................",
        }, P(('C', "#4f7a3a"), ('c', "#3a5c2c"), ('W', "#dde2e6"), ('w', "#a9b2ba"), ('G', "#d6a93c"), ('o', "#152412")));

        // ---------------------------------------------------------------- Ícones do mapa (12×12)

        public static PixelCanvas MapChest() => Draw(new[]
        {
            "............",
            ".oooooooooo.",
            ".oYYYYYYYYo.",
            ".oYyyyyyyYo.",
            ".oooooooooo.",
            ".oYYYWWYYYo.",
            ".oYyyWWyyYo.",
            ".oYyyyyyyYo.",
            ".oYYYYYYYYo.",
            ".oooooooooo.",
            "............",
            "............",
        }, P(('Y', "#e8a23a"), ('y', "#b87424"), ('W', "#f4f0e0"), ('o', "#2a1a0a")));

        public static PixelCanvas MapGate() => Draw(new[]
        {
            "............",
            "...oooooo...",
            "..oWWWWWWo..",
            ".oWWooooWWo.",
            ".oWo.YY.oWo.",
            ".oWo.YY.oWo.",
            ".oWo.YY.oWo.",
            ".oWo.YY.oWo.",
            ".oWo.oo.oWo.",
            ".oWo....oWo.",
            ".ooo....ooo.",
            "............",
        }, P(('W', "#e4e4e8"), ('Y', "#f0d04a"), ('o', "#1a1a20")));

        public static PixelCanvas MapPlayer() => Draw(new[]
        {
            ".....oo.....",
            "....oWWo....",
            "...oWWWWo...",
            "..oWWggWWo..",
            ".oWWggggWWo.",
            "oWWWggggWWWo",
            "oWWWWggWWWWo",
            ".oWWWWWWWWo.",
            "..oWWWWWWo..",
            "...oWWWWo...",
            "....oWWo....",
            ".....oo.....",
        }, P(('W', "#f0f0f0"), ('g', "#7a7e86"), ('o', "#101014")));

        public static PixelCanvas MapExit() => Draw(new[]
        {
            "............",
            "...oooooo...",
            "..oMMMMMMo..",
            ".oMmmmmmmMo.",
            ".oMmWWWWmMo.",
            ".oMmWmmWmMo.",
            ".oMmWmmWmMo.",
            ".oMmWWWWmMo.",
            ".oMmmmmmmMo.",
            "..oMMMMMMo..",
            "...oooooo...",
            "............",
        }, P(('M', "#b44ad8"), ('m', "#6a2a9a"), ('W', "#ffd6ff"), ('o', "#1a0a24")));

        public static PixelCanvas MapCheckpoint() => Draw(new[]
        {
            "............",
            "..oo........",
            "..oCCCCCo...",
            "..oCccccCo..",
            "..oCCCCCo...",
            "..oCCCCo....",
            "..oo........",
            "..oo........",
            "..oo........",
            "..oo........",
            ".oooo.......",
            "............",
        }, P(('C', "#5ad0e0"), ('c', "#b8f4ff"), ('o', "#101820")));

        public static PixelCanvas MapMerchant() => Draw(new[]
        {
            "............",
            "....oooo....",
            "...oGGGGo...",
            "..oGgYYgGo..",
            "..oGYggYGo..",
            "..oGgYYgGo..",
            "..oGYggYGo..",
            "..oGgYYgGo..",
            "...oGGGGo...",
            "....oooo....",
            "............",
            "............",
        }, P(('G', "#3ec24a"), ('g', "#1e8a34"), ('Y', "#b8ffb0"), ('o', "#0c2a12")));

        public static PixelCanvas MapObjective() => Draw(new[]
        {
            "............",
            ".....oo.....",
            "....oYYo....",
            "....oYYo....",
            "....oYYo....",
            "....oYYo....",
            "....oYYo....",
            ".....oo.....",
            ".....oo.....",
            "....oYYo....",
            ".....oo.....",
            "............",
        }, P(('Y', "#ffd54a"), ('o', "#2a2008")));

        public static PixelCanvas DPad() => Draw(new[]
        {
            "...ooo...",
            "...oWo...",
            "...oWo...",
            "oooogoooo",
            "oWWgggWWo",
            "oooogoooo",
            "...oWo...",
            "...oYo...",
            "...ooo...",
        }, P(('W', "#6a6c74"), ('g', "#3a3b42"), ('Y', "#f0f0f0"), ('o', "#101014")));

        public static PixelCanvas Lock() => Draw(new[]
        {
            "..oooo..",
            ".oWooWo.",
            ".oW..Wo.",
            "oooooooo",
            "oYYYYYYo",
            "oYYooYYo",
            "oYYooYYo",
            "oYYYYYYo",
            "oooooooo",
        }, P(('W', "#c8c8d0"), ('Y', "#d6a93c"), ('o', "#1a1a20")));

        // ---------------------------------------------------------------- Sprites da interface

        public static PixelCanvas SlotFrame(bool active)
        {
            int s = 33;
            var c = new PixelCanvas(s, s);
            var outer = Pal.Hex("#0b0b0e");
            var light = Pal.Hex("#5d5f69");
            var mid = Pal.Hex("#3a3c44");
            var dark = Pal.Hex("#23242a");
            var inner = Pal.Hex("#0f1013");
            c.Fill(inner);
            for (int i = 0; i < s; i++)
            {
                for (int k = 0; k < 4; k++)
                {
                    Color col = k == 0 ? outer : (k == 3 ? dark : mid);
                    c.Set(i, k, col); c.Set(i, s - 1 - k, col); c.Set(k, i, col); c.Set(s - 1 - k, i, col);
                }
                c.Set(i, s - 2, light); c.Set(1, i, light);
            }
            // Cantos chanfrados.
            foreach (var p in new[] { new Vector2Int(0, 0), new Vector2Int(s - 1, 0), new Vector2Int(0, s - 1), new Vector2Int(s - 1, s - 1) }) c.Set(p.x, p.y, Color.clear);
            if (active)
            {
                // Moldura ativa: só a borda amarela interna (sobreposta ao slot normal).
                var a = new PixelCanvas(s, s);
                var y = Pal.Hex("#f2d23a");
                for (int i = 5; i < s - 5; i++) { a.Set(i, 5, y); a.Set(i, s - 6, y); a.Set(5, i, y); a.Set(s - 6, i, y); }
                return a;
            }
            return c;
        }

        public static PixelCanvas Panel(Color fill, Color border, Color light, Color dark)
        {
            int s = 24;
            var c = new PixelCanvas(s, s);
            c.Fill(fill);
            for (int i = 0; i < s; i++)
            {
                c.Set(i, 0, Pal.Hex("#08080a")); c.Set(i, s - 1, Pal.Hex("#08080a")); c.Set(0, i, Pal.Hex("#08080a")); c.Set(s - 1, i, Pal.Hex("#08080a"));
                c.Set(i, s - 2, light); c.Set(1, i, light);
                c.Set(i, 1, dark); c.Set(s - 2, i, dark);
                c.Set(i, s - 3, border); c.Set(2, i, border); c.Set(i, 2, border); c.Set(s - 3, i, border);
            }
            c.Set(0, 0, Color.clear); c.Set(s - 1, 0, Color.clear); c.Set(0, s - 1, Color.clear); c.Set(s - 1, s - 1, Color.clear);
            return c;
        }

        public static PixelCanvas SelectBorder()
        {
            int s = 16;
            var c = new PixelCanvas(s, s);
            var a = Pal.Hex("#f6ecc4");
            var b = Pal.Hex("#d6a93c");
            for (int i = 0; i < s; i++)
            {
                c.Set(i, 0, a); c.Set(i, s - 1, a); c.Set(0, i, a); c.Set(s - 1, i, a);
                c.Set(i, 1, b); c.Set(i, s - 2, b); c.Set(1, i, b); c.Set(s - 2, i, b);
            }
            c.Set(0, 0, Color.clear); c.Set(s - 1, 0, Color.clear); c.Set(0, s - 1, Color.clear); c.Set(s - 1, s - 1, Color.clear);
            return c;
        }

        public static PixelCanvas Solid(int w, int h, Color col)
        {
            var c = new PixelCanvas(w, h);
            c.Fill(col);
            return c;
        }

        public static PixelCanvas GradientUp(int w, int h)
        {
            var c = new PixelCanvas(w, h);
            for (int y = 0; y < h; y++)
            {
                float a = Mathf.Pow(1f - y / (float)(h - 1), 1.6f);
                for (int x = 0; x < w; x++) c.Set(x, y, new Color(1f, 1f, 1f, a));
            }
            return c;
        }

        public static PixelCanvas DamageBox()
        {
            int w = 12, h = 10;
            var c = new PixelCanvas(w, h);
            c.Fill(Pal.Hex("#c4212b"));
            for (int i = 0; i < w; i++) { c.Set(i, 0, Pal.Hex("#5a0a10")); c.Set(i, h - 1, Pal.Hex("#5a0a10")); c.Set(i, h - 2, Pal.Hex("#e8505a")); }
            for (int j = 0; j < h; j++) { c.Set(0, j, Pal.Hex("#5a0a10")); c.Set(w - 1, j, Pal.Hex("#5a0a10")); }
            return c;
        }

        // ---------------------------------------------------------------- Coração

        // Coração anguloso (lóbulos de topo plano, entalhe em V, laterais retas e ponta inferior), como no HUD do vídeo.
        static readonly Vector2[] HeartPolygon =
        {
            new Vector2(0f, 0.52f), new Vector2(0.3f, 0.86f), new Vector2(0.74f, 0.86f), new Vector2(1f, 0.58f),
            new Vector2(1f, 0.14f), new Vector2(0f, -1f), new Vector2(-1f, 0.14f), new Vector2(-1f, 0.58f),
            new Vector2(-0.74f, 0.86f), new Vector2(-0.3f, 0.86f),
        };

        static bool HeartMask(int x, int y, int w, int h)
        {
            float nx = (x + 0.5f - w * 0.5f) / (w * 0.5f);
            float ny = (y + 0.5f - h * 0.5f) / (h * 0.5f) * 1.08f;
            bool inside = false;
            for (int i = 0, j = HeartPolygon.Length - 1; i < HeartPolygon.Length; j = i++)
            {
                var a = HeartPolygon[i];
                var b = HeartPolygon[j];
                if ((a.y > ny) != (b.y > ny) && nx < (b.x - a.x) * (ny - a.y) / (b.y - a.y) + a.x) inside = !inside;
            }
            return inside;
        }

        public static void Heart(out PixelCanvas frame, out PixelCanvas fill, out PixelCanvas back, out PixelCanvas shine)
        {
            int W = 50, H = 42;
            int hw = 38, hh = 32;
            int ox = (W - hw) / 2, oy = 5;
            bool[,] heart = new bool[W, H];
            for (int y = 0; y < hh; y++)
                for (int x = 0; x < hw; x++)
                    if (HeartMask(x, y, hw, hh)) heart[ox + x, oy + y] = true;

            bool Dilated(int x, int y, int r)
            {
                for (int j = -r; j <= r; j++)
                    for (int i = -r; i <= r; i++)
                    {
                        if (Mathf.Abs(i) + Mathf.Abs(j) > r + 1) continue;
                        int xx = x + i, yy = y + j;
                        if (xx >= 0 && yy >= 0 && xx < W && yy < H && heart[xx, yy]) return true;
                    }
                return false;
            }

            frame = new PixelCanvas(W, H);
            fill = new PixelCanvas(W, H);
            back = new PixelCanvas(W, H);
            shine = new PixelCanvas(W, H);
            var rim = Pal.Hex("#5f616b");
            var body = Pal.Hex("#2c2d33");
            var edge = Pal.Hex("#0c0c0f");
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    bool h = heart[x, y];
                    if (h)
                    {
                        back.Set(x, y, Pal.Hex("#2a0a0e"));
                        float t = y / (float)H;
                        var red = Color.Lerp(Pal.Hex("#b0141f"), Pal.Hex("#d7263a"), t);
                        fill.Set(x, y, red);
                        continue;
                    }
                    bool d1 = Dilated(x, y, 1), d4 = Dilated(x, y, 4), d5 = Dilated(x, y, 5);
                    if (d1) frame.Set(x, y, edge);
                    else if (d4) frame.Set(x, y, body);
                    else if (d5) frame.Set(x, y, rim);
                }
            // Brilho: dois arcos claros no topo dos lóbulos.
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    if (!heart[x, y]) continue;
                    bool top = y + 2 < H && !heart[x, y + 2];
                    if (top && y > H * 0.6f) shine.Set(x, y, new Color(1f, 0.55f, 0.6f, 0.9f));
                }
            // Contorno claro externo no topo da moldura.
            for (int x = 0; x < W; x++)
                for (int y = H - 1; y >= 0; y--)
                {
                    if (frame.Get(x, y).a > 0f) { frame.Set(x, y, Pal.Hex("#7a7c86")); break; }
                }
        }

        // ---------------------------------------------------------------- Efeitos

        public static PixelCanvas SoftCircle(int s)
        {
            var c = new PixelCanvas(s, s);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(s * 0.5f, s * 0.5f)) / (s * 0.5f);
                    float a = Mathf.Clamp01(1f - d);
                    c.Set(x, y, new Color(1f, 1f, 1f, a * a));
                }
            return c;
        }

        public static PixelCanvas Noise(int s, int seed)
        {
            var c = new PixelCanvas(s, s, seed);
            float ox = c.R(0f, 100f), oy = c.R(0f, 100f);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float n = Mathf.PerlinNoise(ox + x * 0.12f, oy + y * 0.12f) * 0.7f + Mathf.PerlinNoise(ox + x * 0.35f, oy + y * 0.35f) * 0.3f;
                    c.Set(x, y, new Color(n, n, n, 1f));
                }
            return c;
        }

        public static PixelCanvas BeamGradient()
        {
            int w = 16, h = 64;
            var c = new PixelCanvas(w, h);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float fx = 1f - Mathf.Abs((x + 0.5f) / w * 2f - 1f);
                    float core = Mathf.Pow(fx, 1.6f);
                    float streak = 0.75f + 0.25f * Mathf.Sin(y * 0.9f + x * 0.3f);
                    c.Set(x, y, new Color(1f, 1f, 1f, core * streak));
                }
            return c;
        }

        public static PixelCanvas Ring(int s)
        {
            var c = new PixelCanvas(s, s);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(s * 0.5f, s * 0.5f)) / (s * 0.5f);
                    float a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.86f) / 0.12f);
                    c.Set(x, y, new Color(1f, 1f, 1f, a));
                }
            return c;
        }

        public static PixelCanvas SquareParticle()
        {
            var c = new PixelCanvas(8, 8);
            c.Fill(Color.white);
            return c;
        }
    }
}

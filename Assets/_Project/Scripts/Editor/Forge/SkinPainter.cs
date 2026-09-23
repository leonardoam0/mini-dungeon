using System;
using UnityEngine;

namespace Ruinas.EditorTools
{
    public enum BoxFace { Top, Bottom, Right, Front, Left, Back }

    /// <summary>Caixa de um personagem: tamanho em texels e canto superior esquerdo do desdobramento no atlas.</summary>
    public struct SkinBox
    {
        public Vector3Int size;
        public Vector2Int uv;
        public SkinBox(int w, int h, int d, int u, int v) { size = new Vector3Int(w, h, d); uv = new Vector2Int(u, v); }

        /// <summary>Região da face no atlas (coordenadas com y para baixo).</summary>
        public RectInt Region(BoxFace f)
        {
            int w = size.x, h = size.y, d = size.z, u = uv.x, v = uv.y;
            switch (f)
            {
                case BoxFace.Top: return new RectInt(u + d, v, w, d);
                case BoxFace.Bottom: return new RectInt(u + d + w, v, w, d);
                case BoxFace.Right: return new RectInt(u, v + d, d, h);
                case BoxFace.Front: return new RectInt(u + d, v + d, w, h);
                case BoxFace.Left: return new RectInt(u + d + w, v + d, d, h);
                default: return new RectInt(u + d + w + d, v + d, w, h);
            }
        }
    }

    /// <summary>Layouts dos personagens (humanoide 64×64; lhama 128×64; vinha 64×64).</summary>
    public static class SkinLayout
    {
        public static readonly SkinBox Head = new SkinBox(8, 8, 8, 0, 0);
        public static readonly SkinBox Body = new SkinBox(8, 12, 4, 16, 16);
        public static readonly SkinBox ArmR = new SkinBox(4, 12, 4, 40, 16);
        public static readonly SkinBox ArmL = new SkinBox(4, 12, 4, 32, 48);
        public static readonly SkinBox LegR = new SkinBox(4, 12, 4, 0, 16);
        public static readonly SkinBox LegL = new SkinBox(4, 12, 4, 16, 48);
        // Membros finos (esqueleto) usam as mesmas origens com 2×12×2.
        public static readonly SkinBox ThinArmR = new SkinBox(2, 12, 2, 40, 16);
        public static readonly SkinBox ThinArmL = new SkinBox(2, 12, 2, 32, 48);
        public static readonly SkinBox ThinLegR = new SkinBox(2, 12, 2, 0, 16);
        public static readonly SkinBox ThinLegL = new SkinBox(2, 12, 2, 16, 48);
        // Elmo/plumagem (sobreposição da cabeça).
        public static readonly SkinBox Crest = new SkinBox(2, 3, 6, 32, 0);

        // Lhama (128×64)
        public static readonly SkinBox LlamaBody = new SkinBox(12, 10, 18, 0, 0);
        public static readonly SkinBox LlamaNeck = new SkinBox(6, 12, 6, 64, 0);
        public static readonly SkinBox LlamaHead = new SkinBox(7, 7, 9, 64, 20);
        public static readonly SkinBox LlamaEar = new SkinBox(2, 3, 2, 100, 0);
        public static readonly SkinBox LlamaLeg = new SkinBox(4, 12, 4, 0, 32);

        // Vinha (64×64)
        public static readonly SkinBox VineStalk = new SkinBox(6, 8, 6, 0, 0);
        public static readonly SkinBox VineBulb = new SkinBox(10, 8, 10, 0, 20);
        public static readonly SkinBox VinePetal = new SkinBox(8, 1, 6, 26, 0);
        public static readonly SkinBox VineLeaf = new SkinBox(6, 1, 10, 32, 40);
    }

    /// <summary>Pinta as texturas dos personagens face a face.</summary>
    public static class SkinPainter
    {
        /// <summary>Pinta uma face; (u, v) locais com v = 0 na base da face.</summary>
        public static void Face(PixelCanvas c, SkinBox b, BoxFace f, Func<int, int, int, int, Color> paint)
        {
            var r = b.Region(f);
            for (int v = 0; v < r.height; v++)
                for (int u = 0; u < r.width; u++)
                {
                    var col = paint(u, v, r.width, r.height);
                    if (col.a <= 0f) continue;
                    c.Set(r.x + u, c.H - r.y - r.height + v, col);
                }
        }

        public static void AllFaces(PixelCanvas c, SkinBox b, Func<BoxFace, int, int, int, int, Color> paint)
        {
            foreach (BoxFace f in Enum.GetValues(typeof(BoxFace))) Face(c, b, f, (u, v, w, h) => paint(f, u, v, w, h));
        }

        static Color J(PixelCanvas c, Color col, float a = 0.05f) => Pal.Shade(col, 1f + c.R(-a, a));

        // ------------------------------------------------------------------ Heroína (armadura clara/metálica)

        public static PixelCanvas Hero(bool mossCloak)
        {
            var c = new PixelCanvas(64, 64, mossCloak ? 71 : 70);
            var plate = Pal.Hex("#d7dce0");
            var plateMid = Pal.Hex("#b4bcc3");
            var plateDark = Pal.Hex("#7f8a93");
            var outline = Pal.Hex("#4a5058");
            var visor = Pal.Hex("#262a31");
            var gold = Pal.Hex("#d6a93c");
            var magenta = Pal.Hex("#d14ad1");
            var chain = Pal.Hex("#6f777f");
            var boot = Pal.Hex("#3b3230");
            var cloak = Pal.Hex("#3f5e33");
            var cloakDark = Pal.Hex("#2e4527");

            // Elmo
            SkinPainter.AllFaces(c, SkinLayout.Head, (f, u, v, w, h) =>
            {
                bool edge = u == 0 || u == w - 1 || v == 0 || v == h - 1;
                Color col = v >= h - 2 ? plate : plateMid;
                if (edge) col = Pal.Shade(col, 0.82f);
                if (f == BoxFace.Front)
                {
                    if (v == 4 || v == 3) col = visor;
                    if (v == 4 && (u == 2 || u == 5)) col = Pal.Hex("#8fe8ff");
                    if (v == 1 && u > 1 && u < 6) col = plateDark;
                    if (u == 3 || u == 4) { if (v >= 5) col = gold; }
                }
                if (f == BoxFace.Top)
                {
                    col = plate;
                    if (u == 3 || u == 4) col = magenta;
                    if (edge) col = plateMid;
                }
                if (f == BoxFace.Left || f == BoxFace.Right) { if (v == 4) col = visor; if (u == w / 2 && v < 3) col = plateDark; }
                if (mossCloak && f != BoxFace.Front && v <= 5) col = (u + v) % 5 == 0 ? cloakDark : cloak;
                if (mossCloak && f == BoxFace.Front && (u == 0 || u == w - 1)) col = cloak;
                return J(c, col, 0.03f);
            });
            // Plumagem magenta (topo)
            SkinPainter.AllFaces(c, SkinLayout.Crest, (f, u, v, w, h) => J(c, v == h - 1 ? Pal.Hex("#f07af0") : magenta, 0.06f));

            // Tronco: peitoral branco com friso dourado e gema magenta
            SkinPainter.AllFaces(c, SkinLayout.Body, (f, u, v, w, h) =>
            {
                Color col = plate;
                if (f == BoxFace.Front)
                {
                    bool border = u == 0 || u == w - 1 || v == h - 1;
                    col = border ? gold : (v > 6 ? plate : plateMid);
                    if ((u == 3 || u == 4) && (v == 8 || v == 9)) col = magenta;
                    if (v <= 1) col = v == 1 ? Pal.Hex("#5a3b24") : Pal.Hex("#3f2a1a");
                    if (v <= 1 && (u == 3 || u == 4)) col = gold;
                }
                else if (f == BoxFace.Back)
                {
                    col = v > 6 ? plateMid : plateDark;
                    if (v <= 1) col = Pal.Hex("#3f2a1a");
                }
                else if (f == BoxFace.Left || f == BoxFace.Right) col = v <= 1 ? Pal.Hex("#3f2a1a") : chain;
                if (mossCloak && f == BoxFace.Back) col = (u + v) % 4 == 0 ? cloakDark : cloak;
                if (mossCloak && (f == BoxFace.Left || f == BoxFace.Right) && v > 2) col = cloak;
                if (mossCloak && f == BoxFace.Front && (u == 0 || u == w - 1) && v > 2) col = cloak;
                if (f == BoxFace.Top) col = mossCloak ? cloak : plateMid;
                return J(c, col, 0.04f);
            });

            void Arm(SkinBox b)
            {
                SkinPainter.AllFaces(c, b, (f, u, v, w, h) =>
                {
                    Color col;
                    if (v >= h - 4) col = v == h - 4 ? gold : plate;               // ombreira
                    else if (v <= 2) col = v == 2 ? plateDark : plate;              // manopla
                    else col = (u + v) % 2 == 0 ? chain : Pal.Shade(chain, 0.85f);  // cota de malha
                    if (f == BoxFace.Top) col = plate;
                    if (mossCloak && v >= h - 5) col = cloak;
                    return J(c, col, 0.04f);
                });
            }
            Arm(SkinLayout.ArmR);
            Arm(SkinLayout.ArmL);

            void Leg(SkinBox b)
            {
                SkinPainter.AllFaces(c, b, (f, u, v, w, h) =>
                {
                    Color col;
                    if (v <= 2) col = v == 2 ? Pal.Shade(boot, 1.3f) : boot;
                    else if (v == 6 || v == 7) col = plate;
                    else col = v > 7 ? plateMid : plateDark;
                    if (f == BoxFace.Front && v == 7 && (u == 1 || u == 2)) col = gold;
                    return J(c, col, 0.04f);
                });
            }
            Leg(SkinLayout.LegR);
            Leg(SkinLayout.LegL);
            return c;
        }

        // ------------------------------------------------------------------ Carniçal musgoso (perseguidor)

        public static PixelCanvas Zombie()
        {
            var c = new PixelCanvas(64, 64, 80);
            var skin = Pal.Hex("#5b8a48");
            var skinDark = Pal.Hex("#426b35");
            var shirt = Pal.Hex("#2c6a64");
            var pants = Pal.Hex("#333868");
            var moss = Pal.Hex("#3d6a2c");
            SkinPainter.AllFaces(c, SkinLayout.Head, (f, u, v, w, h) =>
            {
                Color col = (u + v * 3) % 7 == 0 ? skinDark : skin;
                if (f == BoxFace.Front)
                {
                    if (v == 4 && (u == 1 || u == 2 || u == 5 || u == 6)) col = Pal.Hex("#15210f");
                    if (v == 3 && (u == 2 || u == 5)) col = Pal.Hex("#1c2a14");
                    if (v == 1 && u >= 2 && u <= 5) col = Pal.Hex("#2a3a20");
                }
                if (f == BoxFace.Top || v == h - 1) col = (u % 3 == 0) ? moss : Pal.Hex("#4b7a36");
                return J(c, col, 0.06f);
            });
            SkinPainter.AllFaces(c, SkinLayout.Body, (f, u, v, w, h) =>
            {
                Color col = shirt;
                if (v <= 2 && (u + v) % 3 != 0) col = skin;            // camisa rasgada
                if (v >= h - 2 && f != BoxFace.Front) col = moss;
                if (f == BoxFace.Front && v >= h - 3 && u >= 3 && u <= 4) col = skin;
                if (f == BoxFace.Top) col = moss;
                return J(c, col, 0.07f);
            });
            void Arm(SkinBox b) => SkinPainter.AllFaces(c, b, (f, u, v, w, h) => J(c, v >= h - 4 ? (v == h - 1 || (u + v) % 3 == 0 ? moss : shirt) : ((u + v) % 5 == 0 ? skinDark : skin), 0.06f));
            Arm(SkinLayout.ArmR); Arm(SkinLayout.ArmL);
            void Leg(SkinBox b) => SkinPainter.AllFaces(c, b, (f, u, v, w, h) => J(c, v <= 1 ? skinDark : pants, 0.06f));
            Leg(SkinLayout.LegR); Leg(SkinLayout.LegL);
            return c;
        }

        // ------------------------------------------------------------------ Arqueiro ossudo

        public static PixelCanvas Skeleton()
        {
            var c = new PixelCanvas(64, 64, 90);
            var bone = Pal.Hex("#d5cfbf");
            var boneMid = Pal.Hex("#b3ad9d");
            var boneDark = Pal.Hex("#7d786c");
            var hole = Pal.Hex("#1f1c19");
            var moss = Pal.Hex("#4a6f35");
            SkinPainter.AllFaces(c, SkinLayout.Head, (f, u, v, w, h) =>
            {
                Color col = v >= h - 2 ? bone : boneMid;
                if (f == BoxFace.Front)
                {
                    if ((v == 4 || v == 3) && (u == 1 || u == 2 || u == 5 || u == 6)) col = hole;
                    if (v == 2 && (u == 3 || u == 4)) col = boneDark;
                    if (v == 1 && u >= 1 && u <= 6 && u % 2 == 1) col = boneDark;
                }
                if (f == BoxFace.Top && (u + v) % 6 == 0) col = moss;
                return J(c, col, 0.04f);
            });
            SkinPainter.AllFaces(c, SkinLayout.Body, (f, u, v, w, h) =>
            {
                if (f == BoxFace.Front || f == BoxFace.Back)
                {
                    bool spine = u == 3 || u == 4;
                    bool rib = v >= 5 && v % 2 == 1;
                    if (spine || rib) return J(c, v % 2 == 1 ? bone : boneMid, 0.04f);
                    return J(c, Pal.Hex("#2a2622"), 0.05f);
                }
                return J(c, v % 2 == 1 ? boneMid : boneDark, 0.05f);
            });
            void Limb(SkinBox b) => SkinPainter.AllFaces(c, b, (f, u, v, w, h) => J(c, v == h - 1 || v == 0 ? boneMid : bone, 0.05f));
            Limb(SkinLayout.ThinArmR); Limb(SkinLayout.ThinArmL); Limb(SkinLayout.ThinLegR); Limb(SkinLayout.ThinLegL);
            return c;
        }

        // ------------------------------------------------------------------ Guardião musgoso (elite)

        public static PixelCanvas Brute()
        {
            var c = new PixelCanvas(64, 64, 100);
            var stone = Pal.Hex("#4a6c66");
            var stoneDark = Pal.Hex("#2e4744");
            var skin = Pal.Hex("#4d7a3e");
            var moss = Pal.Hex("#5f8f3f");
            var eye = Pal.Hex("#f2d24a");
            SkinPainter.AllFaces(c, SkinLayout.Head, (f, u, v, w, h) =>
            {
                Color col = (v == 5 || u == 0 || u == w - 1) ? stoneDark : stone;
                if (f == BoxFace.Front)
                {
                    if (v == 3 && (u == 2 || u == 5)) col = eye;
                    if (v == 3 && (u == 1 || u == 3 || u == 4 || u == 6)) col = Pal.Hex("#101614");
                    if (v <= 1) col = skin;
                }
                if (f == BoxFace.Top) col = (u + v) % 3 == 0 ? moss : stone;
                return J(c, col, 0.05f);
            });
            SkinPainter.AllFaces(c, SkinLayout.Body, (f, u, v, w, h) =>
            {
                Color col = v >= 7 ? stone : skin;
                if (v == 7 || (v > 7 && (u == 0 || u == w - 1))) col = stoneDark;
                if (v >= h - 2) col = moss;
                if (f == BoxFace.Front && v < 7 && (u + v) % 4 == 0) col = Pal.Hex("#3a6230");
                return J(c, col, 0.06f);
            });
            void Arm(SkinBox b) => SkinPainter.AllFaces(c, b, (f, u, v, w, h) => J(c, v >= h - 5 ? (v == h - 5 ? stoneDark : stone) : (v <= 2 ? stoneDark : skin), 0.06f));
            Arm(SkinLayout.ArmR); Arm(SkinLayout.ArmL);
            void Leg(SkinBox b) => SkinPainter.AllFaces(c, b, (f, u, v, w, h) => J(c, v <= 2 ? stoneDark : (v > 8 ? moss : Pal.Hex("#2f3b2c")), 0.06f));
            Leg(SkinLayout.LegR); Leg(SkinLayout.LegL);
            return c;
        }

        // ------------------------------------------------------------------ Vinha esporífera

        public static PixelCanvas Vine()
        {
            var c = new PixelCanvas(64, 64, 110);
            var stalk = Pal.Hex("#3f6f2e");
            var stripe = Pal.Hex("#2c5222");
            var bulb = Pal.Hex("#8a3f7a");
            var bulbLight = Pal.Hex("#b45aa0");
            var spot = Pal.Hex("#e0d04a");
            SkinPainter.AllFaces(c, SkinLayout.VineStalk, (f, u, v, w, h) => J(c, (v % 3 == 0) ? stripe : stalk, 0.06f));
            SkinPainter.AllFaces(c, SkinLayout.VineBulb, (f, u, v, w, h) =>
            {
                Color col = v >= h - 2 ? bulbLight : bulb;
                if ((u * 7 + v * 3) % 11 == 0) col = spot;
                if (f == BoxFace.Front && v >= 2 && v <= 4 && u >= 2 && u <= 7) col = Pal.Hex("#2a1024");
                if (f == BoxFace.Top) col = (u + v) % 4 == 0 ? spot : Pal.Hex("#9e4a8c");
                return J(c, col, 0.05f);
            });
            SkinPainter.AllFaces(c, SkinLayout.VinePetal, (f, u, v, w, h) => J(c, u == 0 || u == w - 1 ? Pal.Hex("#5a8a3a") : Pal.Hex("#6fa045"), 0.06f));
            SkinPainter.AllFaces(c, SkinLayout.VineLeaf, (f, u, v, w, h) => J(c, u == w / 2 ? Pal.Hex("#2e5a24") : Pal.Hex("#447a33"), 0.07f));
            return c;
        }

        // ------------------------------------------------------------------ Lhama de manta vermelha

        public static PixelCanvas Llama()
        {
            var c = new PixelCanvas(128, 64, 120);
            var wool = Pal.Hex("#e6d8b8");
            var woolShade = Pal.Hex("#c9b892");
            var red = Pal.Hex("#b7202b");
            var redDark = Pal.Hex("#7e1119");
            var trim = Pal.Hex("#f0c948");
            SkinPainter.AllFaces(c, SkinLayout.LlamaBody, (f, u, v, w, h) =>
            {
                Color col = (u * 3 + v * 5) % 7 == 0 ? woolShade : wool;
                // Manta: cobre o topo e a metade superior das laterais, com friso amarelo.
                bool carpet = false;
                if (f == BoxFace.Top) carpet = u > 2 && u < w - 3;
                if (f == BoxFace.Left || f == BoxFace.Right) carpet = v >= 4 && u > 3 && u < w - 3;
                if (carpet)
                {
                    col = (u + v) % 4 == 0 ? redDark : red;
                    bool border = (f == BoxFace.Top && (u == 3 || u == w - 4)) || (f != BoxFace.Top && (v == 4 || u == 4 || u == w - 4));
                    if (border) col = trim;
                    if (f != BoxFace.Top && v == 6 && u % 3 == 0) col = trim;
                }
                return J(c, col, 0.04f);
            });
            SkinPainter.AllFaces(c, SkinLayout.LlamaNeck, (f, u, v, w, h) => J(c, (u + v) % 6 == 0 ? woolShade : wool, 0.04f));
            SkinPainter.AllFaces(c, SkinLayout.LlamaHead, (f, u, v, w, h) =>
            {
                Color col = wool;
                if (f == BoxFace.Front && v <= 2) col = woolShade;
                if ((f == BoxFace.Left || f == BoxFace.Right) && v == 4 && (u == 2 || u == 3)) col = Pal.Hex("#1d1a18");
                if (f == BoxFace.Front && v == 1 && (u == 2 || u == 4)) col = Pal.Hex("#6a5a48");
                return J(c, col, 0.04f);
            });
            SkinPainter.AllFaces(c, SkinLayout.LlamaEar, (f, u, v, w, h) => J(c, wool, 0.04f));
            SkinPainter.AllFaces(c, SkinLayout.LlamaLeg, (f, u, v, w, h) => J(c, v <= 1 ? Pal.Hex("#8a7a60") : wool, 0.05f));
            return c;
        }
    }
}

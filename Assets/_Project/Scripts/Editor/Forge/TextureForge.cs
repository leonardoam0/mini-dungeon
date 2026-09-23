using System.IO;
using UnityEditor;
using UnityEngine;

namespace Ruinas.EditorTools
{
    /// <summary>Grava as texturas geradas e aplica as configurações de importação (filtro de ponto, mips, sprites 9-slice).</summary>
    public static class TextureForge
    {
        public const string Root = "Assets/_Project/Art/Textures";
        public const string WorldAtlasPath = Root + "/World/world_atlas.png";
        public const string WorldEmissionPath = Root + "/World/world_emission.png";

        static void Save(string path, PixelCanvas c)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, c.ToPNG());
        }

        public static void GenerateAll()
        {
            GenerateWorldAtlas();
            GenerateSkins();
            GenerateIcons();
            GenerateUi();
            GenerateFx();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureImports();
        }

        public static void GenerateWorldAtlas()
        {
            var atlas = new PixelCanvas(VoxelMesher.AtlasSize, VoxelMesher.AtlasSize);
            atlas.Fill(new Color(0.2f, 0.2f, 0.2f, 1f));
            void Put(int tileIndex, PixelCanvas tile)
            {
                int col = tileIndex % VoxelMesher.Columns, row = tileIndex / VoxelMesher.Columns;
                atlas.BlitPadded(tile, col * VoxelMesher.Cell + VoxelMesher.Pad, row * VoxelMesher.Cell + VoxelMesher.Pad, VoxelMesher.Pad);
            }
            void PutMacro(Tile first, PixelCanvas macro)
            {
                for (int q = 0; q < 4; q++)
                {
                    int qx = q & 1, qz = q >> 1;
                    Put((int)first + q, macro.Crop(qx * 32, qz * 32, 32, 32));
                }
            }
            PutMacro(Tile.FloorA0, WorldTexturePainter.FloorMacro(0, false, 11));
            PutMacro(Tile.FloorB0, WorldTexturePainter.FloorMacro(1, false, 12));
            PutMacro(Tile.FloorC0, WorldTexturePainter.FloorMacro(2, false, 13));
            PutMacro(Tile.FloorDark0, WorldTexturePainter.FloorMacro(0, true, 14));
            for (int t = (int)Tile.WallBrick; t < (int)Tile.Count; t++)
                Put(t, WorldTexturePainter.Paint((Tile)t, 100 + t));
            Save(WorldAtlasPath, atlas);

            // Emissão separada da cor-base: por ora só marcas discretas nos ladrilhos entalhados.
            var emission = new PixelCanvas(VoxelMesher.AtlasSize, VoxelMesher.AtlasSize);
            emission.Fill(Color.black);
            var glyph = new PixelCanvas(32, 32);
            glyph.Fill(Color.black);
            var cyan = new Color(0.18f, 0.42f, 0.4f);
            glyph.Line(12, 12, 20, 12, cyan);
            glyph.Line(20, 12, 20, 20, cyan);
            int ci = (int)Tile.CarvedTop;
            emission.BlitPadded(glyph, (ci % 16) * 64 + 16, (ci / 16) * 64 + 16, 16);
            Save(WorldEmissionPath, emission);
        }

        public static void GenerateSkins()
        {
            string dir = Root + "/Characters/";
            Save(dir + "hero.png", SkinPainter.Hero(false));
            Save(dir + "hero_moss.png", SkinPainter.Hero(true));
            Save(dir + "zombie.png", SkinPainter.Zombie());
            Save(dir + "skeleton.png", SkinPainter.Skeleton());
            Save(dir + "brute.png", SkinPainter.Brute());
            Save(dir + "vine.png", SkinPainter.Vine());
            Save(dir + "llama.png", SkinPainter.Llama());
        }

        public static void GenerateIcons()
        {
            string dir = Root + "/Icons/";
            Save(dir + "icon_sheaf.png", IconPainter.GoldenSheaf());
            Save(dir + "icon_feather.png", IconPainter.Feather());
            Save(dir + "icon_bell.png", IconPainter.PulseBell());
            Save(dir + "icon_potion.png", IconPainter.Potion());
            Save(dir + "icon_potion_swift.png", IconPainter.SwiftPotion());
            Save(dir + "icon_map.png", IconPainter.MapScroll());
            Save(dir + "icon_lives.png", IconPainter.Lives());
            Save(dir + "icon_arrow.png", IconPainter.ArrowIcon());
            Save(dir + "icon_emerald.png", IconPainter.Emerald());
            Save(dir + "icon_health_orb.png", IconPainter.HealthOrb());
            Save(dir + "icon_sword.png", IconPainter.Sword());
            Save(dir + "icon_axe.png", IconPainter.Axe());
            Save(dir + "icon_spear.png", IconPainter.Spear());
            Save(dir + "icon_bow.png", IconPainter.Bow());
            Save(dir + "icon_crossbow.png", IconPainter.Crossbow());
            Save(dir + "icon_armor_plate.png", IconPainter.ArmorPlate());
            Save(dir + "icon_armor_moss.png", IconPainter.ArmorMoss());
            Save(dir + "map_chest.png", IconPainter.MapChest());
            Save(dir + "map_gate.png", IconPainter.MapGate());
            Save(dir + "map_player.png", IconPainter.MapPlayer());
            Save(dir + "map_exit.png", IconPainter.MapExit());
            Save(dir + "map_checkpoint.png", IconPainter.MapCheckpoint());
            Save(dir + "map_merchant.png", IconPainter.MapMerchant());
            Save(dir + "map_objective.png", IconPainter.MapObjective());
            Save(dir + "icon_dpad.png", IconPainter.DPad());
            Save(dir + "icon_lock.png", IconPainter.Lock());
        }

        public static void GenerateUi()
        {
            string dir = Root + "/UI/";
            Save(dir + "slot_frame.png", IconPainter.SlotFrame(false));
            Save(dir + "slot_frame_active.png", IconPainter.SlotFrame(true));
            Save(dir + "panel.png", IconPainter.Panel(Pal.Hex("#16171b"), Pal.Hex("#34353d"), Pal.Hex("#4a4b55"), Pal.Hex("#0f1013")));
            Save(dir + "panel_light.png", IconPainter.Panel(Pal.Hex("#22232a"), Pal.Hex("#3d3e47"), Pal.Hex("#555764"), Pal.Hex("#15161a")));
            Save(dir + "button.png", IconPainter.Panel(Pal.Hex("#1e1f25"), Pal.Hex("#3a3b44"), Pal.Hex("#50525d"), Pal.Hex("#121317")));
            Save(dir + "tooltip.png", IconPainter.Panel(new Color(0.07f, 0.075f, 0.09f, 0.94f), Pal.Hex("#34353d"), Pal.Hex("#4a4b55"), Pal.Hex("#0f1013")));
            Save(dir + "badge.png", IconPainter.Panel(Pal.Hex("#15161a"), Pal.Hex("#2c2d34"), Pal.Hex("#5a5c66"), Pal.Hex("#0a0a0c")));
            Save(dir + "select.png", IconPainter.SelectBorder());
            Save(dir + "bar.png", IconPainter.Panel(Pal.Hex("#101014"), Pal.Hex("#2a2b31"), Pal.Hex("#3c3d45"), Pal.Hex("#08080a")));
            Save(dir + "white.png", IconPainter.Solid(4, 4, Color.white));
            Save(dir + "gradient_bottom.png", IconPainter.GradientUp(4, 64));
            Save(dir + "damage_box.png", IconPainter.DamageBox());
            IconPainter.Heart(out var frame, out var fill, out var back, out var shine);
            Save(dir + "heart_frame.png", frame);
            Save(dir + "heart_fill.png", fill);
            Save(dir + "heart_back.png", back);
            Save(dir + "heart_shine.png", shine);
        }

        public static void GenerateFx()
        {
            string dir = Root + "/FX/";
            Save(dir + "soft_circle.png", IconPainter.SoftCircle(64));
            Save(dir + "noise.png", IconPainter.Noise(64, 5));
            Save(dir + "beam.png", IconPainter.BeamGradient());
            Save(dir + "ring.png", IconPainter.Ring(128));
            Save(dir + "square.png", IconPainter.SquareParticle());
        }

        // ------------------------------------------------------------------ Importação

        public static void ConfigureImports()
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                SetTexture(WorldAtlasPath, FilterMode.Point, true, TextureWrapMode.Clamp, true);
                SetTexture(WorldEmissionPath, FilterMode.Point, true, TextureWrapMode.Clamp, true);
                foreach (var f in Directory.GetFiles(Root + "/Characters", "*.png"))
                    SetTexture(f.Replace('\\', '/'), FilterMode.Point, false, TextureWrapMode.Clamp, true);
                foreach (var f in Directory.GetFiles(Root + "/Icons", "*.png"))
                    SetSprite(f.Replace('\\', '/'), Vector4.zero, true);
                foreach (var f in Directory.GetFiles(Root + "/UI", "*.png"))
                {
                    string p = f.Replace('\\', '/');
                    string n = Path.GetFileNameWithoutExtension(p);
                    Vector4 border = Vector4.zero;
                    if (n.StartsWith("slot_frame")) border = new Vector4(7, 7, 7, 7);
                    else if (n == "panel" || n == "panel_light" || n == "button" || n == "tooltip" || n == "bar") border = new Vector4(4, 4, 4, 4);
                    else if (n == "badge") border = new Vector4(4, 4, 4, 4);
                    else if (n == "select") border = new Vector4(3, 3, 3, 3);
                    else if (n == "damage_box") border = new Vector4(2, 2, 2, 3);
                    SetSprite(p, border, n != "gradient_bottom");
                }
                foreach (var f in Directory.GetFiles(Root + "/FX", "*.png"))
                    SetTexture(f.Replace('\\', '/'), f.Contains("square") ? FilterMode.Point : FilterMode.Bilinear, false, f.Contains("noise") || f.Contains("beam") ? TextureWrapMode.Repeat : TextureWrapMode.Clamp, true);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        static void SetTexture(string path, FilterMode filter, bool mips, TextureWrapMode wrap, bool srgb)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) return;
            ti.textureType = TextureImporterType.Default;
            ti.filterMode = filter;
            ti.mipmapEnabled = mips;
            ti.mipmapFilter = TextureImporterMipFilter.BoxFilter;
            ti.wrapMode = wrap;
            ti.sRGBTexture = srgb;
            ti.alphaIsTransparency = true;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.maxTextureSize = 2048;
            ti.anisoLevel = 0;
            ti.SaveAndReimport();
        }

        static void SetSprite(string path, Vector4 border, bool point)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) return;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 100;
            ti.filterMode = point ? FilterMode.Point : FilterMode.Bilinear;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.npotScale = TextureImporterNPOTScale.None;
            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteBorder = border;
            settings.spriteExtrude = 0;
            ti.SetTextureSettings(settings);
            ti.SaveAndReimport();
        }

        public static Sprite LoadSprite(string relPath) => AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/" + relPath);
        public static Texture2D LoadTexture(string relPath) => AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/" + relPath);
    }
}

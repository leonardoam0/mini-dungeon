using UnityEngine;

namespace Ruinas.EditorTools
{
    /// <summary>
    /// Layouts autorais (composição manual). A arena reproduz a leitura do vídeo; o percurso completo é
    /// PROPOSTO: acampamento → corredor → sala das colunas (+ desvio opcional) → arena → respiro → salão
    /// do guardião → cofre e saída.
    /// </summary>
    public static class LevelLayouts
    {
        public class ArenaSpec
        {
            public Vector3 anchor;
            public Vector3 obelisk;
            public Vector3 gatePos;
            public Bounds inner;
            public int ox, oz, H;
        }

        static readonly Color BrazierColor = new Color(1f, 0.68f, 0.32f);

        /// <summary>
        /// Módulo da arena. Plataforma de 14×14 (ox..ox+13), interior 12×12 com contorno luminoso.
        /// <paramref name="westStairsZ"/> desloca a escada oeste; <paramref name="cornerPillar"/> põe o pilar alto de
        /// tijolos com folhagem vermelha e o braseiro no canto noroeste (leitura do vídeo, arena de referência).
        /// </summary>
        public static ArenaSpec Arena(LevelBuilder b, int ox, int oz, int H, int groundH, string key, bool westStairs = true, bool southStairs = true,
            int westStairsZ = 4, bool cornerPillar = false)
        {
            var spec = new ArenaSpec { ox = ox, oz = oz, H = H };
            int x1 = ox + 13, z1 = oz + 13;
            // Plataforma: interior de ladrilhos, borda escura, cantos entalhados.
            b.Platform(ox, oz, x1, z1, H, BlockRegistry.Floor, BlockRegistry.Floor, 0);
            for (int x = ox; x <= x1; x++)
                for (int z = oz; z <= z1; z++)
                {
                    bool border = x == ox || x == x1 || z == oz || z == z1;
                    if (border) b.Grid.Set(x, H - 1, z, BlockRegistry.FloorDark);
                }
            foreach (var c in new[] { new Vector2Int(ox, oz), new Vector2Int(x1, oz), new Vector2Int(ox, z1), new Vector2Int(x1, z1), new Vector2Int(ox, oz + 7), new Vector2Int(ox + 7, oz) })
                b.Grid.Set(c.x, H - 1, c.y, BlockRegistry.Carved);
            // Laterais próximas com musgo concentrado embaixo.
            for (int z = oz; z <= z1; z++) for (int y = groundH; y < groundH + 2; y++) if (b.R() < 0.5f) b.Grid.Set(ox, y, z, BlockRegistry.WallMossy);
            for (int x = ox; x <= x1; x++) for (int y = groundH; y < groundH + 2; y++) if (b.R() < 0.5f) b.Grid.Set(x, y, oz, BlockRegistry.WallMossy);

            spec.anchor = new Vector3(ox + 7f, H, oz + 7f);
            spec.obelisk = spec.anchor + new Vector3(1.5f, 0f, 1.5f);
            spec.inner = new Bounds(spec.anchor + Vector3.up * 2f, new Vector3(12f, 4f, 12f));

            // Contorno luminoso (interior 12×12).
            float y0 = H + 0.02f;
            b.Strip(new Vector3(ox + 1, y0, oz + 1), new Vector3(x1, y0, oz + 1));
            b.Strip(new Vector3(ox + 1, y0, z1), new Vector3(x1, y0, z1));
            b.Strip(new Vector3(ox + 1, y0, oz + 1), new Vector3(ox + 1, y0, z1));
            b.Strip(new Vector3(x1, y0, oz + 1), new Vector3(x1, y0, z1));

            // Muro do fundo (+Z) e muro lateral (+X) com portão.
            // Na arena de referência o muro norte é baixo (~1 bloco) com pilares de 2–3 blocos, como no vídeo.
            b.Wall(ox - 1, z1 + 1, x1 + 1, z1 + 1, groundH, cornerPillar ? H : H + 2, BlockRegistry.Wall);
            b.Wall(x1 + 1, oz - 1, x1 + 1, z1 + 1, groundH, cornerPillar ? H + 1 : H + 2, BlockRegistry.Wall);
            for (int x = ox + 1; x <= x1; x += 4) b.Pillar(x, z1 + 1, groundH, cornerPillar ? H + 2 : H + 4, 1);
            b.Pillar(x1 + 1, z1 + 1, groundH, H + 5, 1);
            b.Pillar(ox - 1, z1 + 1, groundH, H + 4, 1);
            // Abertura do portão (3 blocos) e pilares com lanternas verdes.
            int gz0 = oz + 6, gz1 = oz + 8;
            b.Clear(x1 + 1, H, gz0, x1 + 1, b.SY - 1, gz1);
            b.Fill(x1 + 1, H - 1, gz0, x1 + 1, H - 1, gz1, BlockRegistry.FloorDark);
            b.MarkPlayable(x1 + 1, gz0, x1 + 1, gz1, H);
            b.Pillar(x1 + 1, gz0 - 1, groundH, H + 3, 1);
            b.Pillar(x1 + 1, gz1 + 1, groundH, H + 3, 1);
            b.Prop(PropForge.Lantern, new Vector3(x1 + 1.5f, H + 4, gz0 - 0.5f), 0f, 1f);
            b.Prop(PropForge.Lantern, new Vector3(x1 + 1.5f, H + 4, gz1 + 1.5f), 0f, 1f);
            if (!cornerPillar) b.Prop(PropForge.Lantern, new Vector3(ox + 5.5f, H + 5, z1 + 1.5f), 0f, 0.9f);
            spec.gatePos = new Vector3(x1 + 1.5f, H, oz + 7.5f);
            b.Gates.Add((spec.gatePos, 90f, key + "_portao"));

            // Escadas nas bordas próximas.
            if (westStairs)
            {
                int s0 = oz + westStairsZ, s1 = s0 + 5;
                b.Stairs(ox - 1, new Vector2Int(-1, 0), s0, s1, 8, H);
                if (cornerPillar)
                {
                    // Canto noroeste: pilar alto de tijolos com folhagem vermelha e braseiro à frente dele.
                    b.Pillar(ox - 1, z1 + 1, groundH, H + 4, 2, BlockRegistry.RedBrick);
                    b.Prop(PropForge.RedFlower, new Vector3(ox, H + 5, z1 + 2f), 20f, 1.45f);
                    b.Pillar(ox, z1, H, H, 1, BlockRegistry.Pillar);
                    b.UnmarkPlayable(ox, z1, ox, z1);
                    b.Prop(PropForge.Brazier, new Vector3(ox + 0.5f, H + 1, z1 + 0.5f), 0f, 0.9f, g => ConfigureBrazier(g, true));
                    b.Prop(PropForge.Reeds, new Vector3(ox - 2.5f, groundH, z1 + 0.5f), 0f, 1.2f);
                    b.Prop(PropForge.Reeds, new Vector3(ox - 4.2f, groundH, z1 + 1.6f), 40f, 0.9f);
                    b.Prop(PropForge.Fern, new Vector3(ox - 5.2f, groundH, z1 + 0.8f), 10f, 1f);
                }
                else
                {
                    // Pilar de tijolos com a flor vermelha e o braseiro no topo da escada.
                    b.Pillar(ox - 3, s1 + 1, groundH, H + 2, 2, BlockRegistry.RedBrick);
                    b.Prop(PropForge.RedFlower, new Vector3(ox - 2f, H + 3, s1 + 2f), 20f, 1.25f);
                    b.Pillar(ox - 1, s1 + 1, groundH, H, 1, BlockRegistry.Pillar);
                    b.Prop(PropForge.Brazier, new Vector3(ox - 0.5f, H + 1, s1 + 1.5f), 0f, 0.9f, g => ConfigureBrazier(g, true));
                    b.Prop(PropForge.Reeds, new Vector3(ox - 0.5f, groundH, s1 + 2.5f), 0f, 1.2f);
                    b.Prop(PropForge.Reeds, new Vector3(ox - 3.7f, groundH, s1 + 3.6f), 40f, 0.9f);
                    b.Prop(PropForge.Fern, new Vector3(ox - 4.2f, groundH, s1 + 3f), 10f, 1f);
                }
                b.Pillar(ox - 1, s0 - 1, groundH, H - 1, 1, BlockRegistry.Pillar);
                b.Prop(PropForge.Brazier, new Vector3(ox - 0.5f, H, s0 - 0.5f), 0f, 0.8f, g => ConfigureBrazier(g, false));
            }
            if (southStairs)
            {
                b.Stairs(oz - 1, new Vector2Int(0, -1), ox + 7, ox + 11, 8, H);
                b.Pillar(ox + 12, oz - 1, groundH, H, 1, BlockRegistry.Pillar);
                b.Prop(PropForge.Brazier, new Vector3(ox + 12.5f, H + 1, oz - 0.5f), 0f, 0.9f, g => ConfigureBrazier(g, true));
                b.Pillar(ox + 6, oz - 1, groundH, H - 1, 1, BlockRegistry.Pillar);
                b.Prop(PropForge.Brazier, new Vector3(ox + 6.5f, H, oz - 0.5f), 0f, 0.8f, g => ConfigureBrazier(g, false));
            }

            // Obelisco e luz ciano do contorno.
            b.Prop(PropForge.Obelisk, spec.obelisk, 0f, 1f, null, "Obelisco");
            foreach (var c in new[] { new Vector3(ox + 1.2f, H + 0.4f, oz + 1.2f), new Vector3(x1 - 0.2f, H + 0.4f, oz + 1.2f), new Vector3(ox + 1.2f, H + 0.4f, z1 - 0.2f), new Vector3(x1 - 0.2f, H + 0.4f, z1 - 0.2f) })
                b.Light(c, new Color(0.62f, 1f, 0.94f), 5f, 0.9f);

            // Pontos de surgimento.
            b.Spawn(key + ":fundo", new Vector3(ox + 12f, H, oz + 12f));
            b.Spawn(key + ":fundo", new Vector3(ox + 12.5f, H, oz + 3f));
            b.Spawn(key + ":vinha", new Vector3(ox + 9.5f, H, oz + 11.5f));
            b.Spawn(key + ":", new Vector3(ox + 3f, H, oz + 11f));
            b.Spawn(key + ":", new Vector3(ox + 11f, H, oz + 7f));
            b.Spawn(key + ":", new Vector3(ox + 4f, H, oz + 3f));
            b.Anchors[key] = spec.anchor;
            b.Zone(key + "_ativacao", new Vector3(ox + 1.5f, H - 1, oz + 1.5f), new Vector3(x1 - 0.5f, H + 4, z1 - 0.5f), ZoneAction.ActivateEncounter, null, key);
            return spec;
        }

        public static void ConfigureBrazier(GameObject g, bool shadows)
        {
            var l = g.GetComponentInChildren<Light>();
            if (l == null) return;
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            l.shadowStrength = 0.75f;
        }

        static void JungleDecor(LevelBuilder b, int x0, int z0, int x1, int z1, int groundH, int trees, int bushes)
        {
            for (int i = 0; i < trees; i++)
            {
                int x = b.RI(x0, x1 + 1), z = b.RI(z0, z1 + 1);
                if (!Free(b, x, z, 2)) continue;
                int g = b.SurfaceAt(x, z);
                if (g < 0) continue;
                b.Tree(x, z, g, b.RI(3, 6), b.RI(2, 4), b.R() < 0.35f);
            }
            for (int i = 0; i < bushes; i++)
            {
                int x = b.RI(x0, x1 + 1), z = b.RI(z0, z1 + 1);
                if (!Free(b, x, z, 1)) continue;
                int g = b.SurfaceAt(x, z);
                if (g < 0) continue;
                if (b.R() < 0.5f) b.Bush(x, z, g, b.RI(1, 3));
                else b.Prop(b.R() < 0.5f ? PropForge.Fern : PropForge.GrassTuft, new Vector3(x + 0.5f, g, z + 0.5f), b.R() * 360f, 0.8f + b.R() * 0.6f);
            }
        }

        /// <summary>Blocos e pilares de pedra em ruínas (topo irregular), fora da área caminhável.</summary>
        static void Ruins(LevelBuilder b, int x0, int z0, int x1, int z1, int groundH, int count)
        {
            for (int i = 0; i < count; i++)
            {
                int w = b.R() < 0.4f ? 2 : 1;
                int x = b.RI(x0, x1 + 1 - w), z = b.RI(z0, z1 + 1 - w);
                if (!Free(b, x, z, 1)) continue;
                int g = b.SurfaceAt(x, z);
                if (g < 0) continue;
                int h = b.R() < 0.5f ? b.RI(1, 3) : b.RI(3, 6);
                byte block = b.R() < 0.35f ? BlockRegistry.WallMossy : (b.R() < 0.5f ? BlockRegistry.Wall : BlockRegistry.Stone);
                b.Pillar(x, z, g, g + h - 1, w, block);
            }
        }

        static bool Free(LevelBuilder b, int x, int z, int margin)
        {
            for (int dx = -margin; dx <= margin; dx++)
                for (int dz = -margin; dz <= margin; dz++)
                {
                    int X = x + dx, Z = z + dz;
                    if (X < 0 || Z < 0 || X >= b.SX || Z >= b.SZ) return false;
                    if (b.Playable[X, Z]) return false;
                }
            return true;
        }

        static void Brazier(LevelBuilder b, int x, int z, int H, int groundH, bool shadows = false, int height = 1)
        {
            b.Pillar(x, z, groundH, H + height - 1, 1, BlockRegistry.Pillar);
            b.Prop(PropForge.Brazier, new Vector3(x + 0.5f, H + height, z + 0.5f), 0f, 0.85f, g => ConfigureBrazier(g, shadows));
        }

        // ------------------------------------------------------------------ Arena de referência

        public static LevelBuilder Reference()
        {
            var b = new LevelBuilder(50, 24, 50, 88);
            int groundH = 7;
            b.Ground(3);
            // Terreno baixo com fossos escuros, caminho inferior e selva atrás dos muros.
            b.Platform(0, 0, 49, 49, groundH - 2, BlockRegistry.Litter, BlockRegistry.Stone, 0, false);
            b.Platform(0, 32, 49, 49, 9, BlockRegistry.Litter, BlockRegistry.Dirt, 0, false);
            b.Platform(34, 0, 49, 49, 9, BlockRegistry.Litter, BlockRegistry.Dirt, 0, false);
            // Caminho inferior oeste (onde a escada termina) e terraço sul — antes da arena,
            // para que os pilares e escadas dela não sejam apagados.
            b.Platform(2, 13, 9, 31, groundH, BlockRegistry.Path, BlockRegistry.Stone, 0);
            b.Platform(10, 13, 17, 22, groundH, BlockRegistry.Path, BlockRegistry.Stone, 0);
            b.Platform(10, 29, 17, 31, groundH, BlockRegistry.Grass, BlockRegistry.Dirt, 0);
            b.Platform(19, 2, 31, 7, groundH, BlockRegistry.Path, BlockRegistry.Stone, 0);
            b.Platform(21, 8, 24, 15, groundH, BlockRegistry.Path, BlockRegistry.Stone, 0);
            b.Platform(30, 8, 31, 15, groundH, BlockRegistry.Path, BlockRegistry.Stone, 0);
            // Escada oeste mais ao norte e pilar vermelho no canto noroeste (trajeto medido no vídeo).
            var spec = Arena(b, 18, 16, 11, groundH, "arena", true, true, 7, true);
            // Água escura ao sul-oeste (profundidade) e ruínas.
            b.Water(2, 2, 16, 10, groundH - 3);
            b.Pillar(6, 20, groundH, groundH + 3, 2, BlockRegistry.WallMossy);
            b.Pillar(12, 29, groundH, groundH + 2, 1, BlockRegistry.Pillar);
            b.Prop(PropForge.Pot, new Vector3(3.5f, groundH, 27.5f), 0f);
            b.Prop(PropForge.Crate, new Vector3(4.5f, groundH, 29.5f), 20f);
            b.Prop(PropForge.Fern, new Vector3(8.5f, groundH, 30.5f), 0f, 1.3f);
            // Atrás da arena, ruínas de pedra azul-petróleo (blocos e pilares em alturas variadas) com pouca
            // vegetação — como o fundo do vídeo — em vez de selva densa.
            Ruins(b, 0, 33, 49, 49, 9, 34);
            Ruins(b, 35, 0, 49, 32, 9, 16);
            JungleDecor(b, 0, 33, 49, 49, 9, 5, 22);
            JungleDecor(b, 35, 0, 49, 32, 9, 3, 10);
            JungleDecor(b, 0, 0, 17, 12, groundH, 4, 12);
            // Passarela além do portão (bloqueada) e paredes de fundo.
            b.Platform(33, 21, 37, 26, 11, BlockRegistry.FloorDark, BlockRegistry.Floor, 0, true);
            b.Wall(38, 18, 38, 29, groundH, 15, BlockRegistry.WallMossy);
            b.Room("arena", new Vector3(0, 0, 0), new Vector3(50, 24, 50));
            return b;
        }

        // ------------------------------------------------------------------ Missão completa

        public static LevelBuilder Mission()
        {
            var b = new LevelBuilder(104, 26, 72, 7);
            b.Ground(4);

            // 1. Acampamento (H=5)
            b.Platform(4, 10, 17, 24, 5, BlockRegistry.Grass, BlockRegistry.Dirt, 0);
            for (int x = 7; x <= 15; x++) for (int z = 16; z <= 18; z++) b.Grid.Set(x, 4, z, BlockRegistry.Path);
            b.Prop(PropForge.Tent, new Vector3(7f, 5, 21.5f), 200f);
            b.Prop(PropForge.Campfire, new Vector3(11f, 5, 13.5f), 0f);
            b.Prop(PropForge.Crate, new Vector3(15.5f, 5, 22.5f), 10f);
            b.Prop(PropForge.Crate, new Vector3(14.6f, 5, 22.6f), 35f, 0.8f);
            b.Prop(PropForge.Pot, new Vector3(5.5f, 5, 12.5f), 0f);
            b.Prop(PropForge.Banner, new Vector3(16.8f, 5, 15.5f), 90f);
            b.Checkpoints.Add((new Vector3(9.5f, 5, 17.5f), 90f, "cp_acampamento", 0));
            b.Zone("dica_mover", new Vector3(4, 4, 10), new Vector3(18, 9, 25), ZoneAction.Hint, "Mova-se com WASD (ou analógico esquerdo). Mire com o mouse. ESC pausa.");
            b.Zone("dica_ataque", new Vector3(16, 4, 14), new Vector3(20, 9, 23), ZoneAction.Hint, "Botão esquerdo ataca · botão direito dispara · ESPAÇO esquiva · 1/2/3 artefatos · Q poção.");
            b.Room("acampamento", new Vector3(0, 0, 4), new Vector3(24, 20, 30));
            JungleDecor(b, 0, 0, 22, 9, 4, 8, 14);
            JungleDecor(b, 0, 25, 22, 40, 4, 10, 16);
            JungleDecor(b, 0, 9, 3, 25, 4, 3, 6);

            // 2. Corredor com desnível (H=5 → 7)
            b.Platform(18, 16, 23, 21, 5, BlockRegistry.Path, BlockRegistry.Stone, 0);
            b.Platform(28, 16, 33, 21, 7, BlockRegistry.Floor, BlockRegistry.Floor, 0);
            b.Stairs(27, new Vector2Int(-1, 0), 16, 21, 4, 7);
            b.Wall(18, 22, 33, 22, 4, 8, BlockRegistry.Wall);
            for (int x = 19; x <= 33; x += 4) b.Pillar(x, 22, 4, 10, 1);
            Brazier(b, 21, 22, 8, 4, false, 1);
            Brazier(b, 29, 22, 9, 4, false, 1);
            b.Wall(18, 15, 33, 15, 2, 4, BlockRegistry.WallMossy);
            b.Zone("dica_escada", new Vector3(22, 4, 15), new Vector3(26, 9, 22), ZoneAction.Hint, "Tab (ou direcional para baixo) liga o mapa sobreposto sem pausar o jogo.");
            b.Zone("corredor_ativacao", new Vector3(29, 6, 16), new Vector3(33, 11, 22), ZoneAction.ActivateEncounter, null, "corredor");
            b.Spawn("corredor:", new Vector3(32.5f, 7, 17.5f));
            b.Spawn("corredor:", new Vector3(32.5f, 7, 20.5f));
            b.Room("corredor", new Vector3(14, 0, 8), new Vector3(36, 20, 28));

            // 3. Sala das colunas (H=7)
            b.Platform(34, 7, 51, 31, 7, BlockRegistry.Floor, BlockRegistry.Floor, 0);
            b.Wall(33, 6, 33, 32, 4, 10, BlockRegistry.Wall);
            b.Clear(33, 7, 16, 33, 25, 21);
            b.Fill(33, 6, 16, 33, 6, 21, BlockRegistry.FloorDark);
            b.MarkPlayable(33, 16, 33, 21, 7);
            b.Wall(33, 32, 52, 32, 4, 10, BlockRegistry.Wall);
            b.Wall(52, 6, 52, 32, 4, 10, BlockRegistry.Wall);
            b.Clear(52, 7, 18, 53, 25, 23);
            b.Fill(52, 4, 18, 53, 6, 23, BlockRegistry.FloorDark);
            b.MarkPlayable(52, 18, 53, 23, 7);
            b.Clear(40, 7, 32, 44, 25, 32);
            b.Fill(40, 6, 32, 44, 6, 32, BlockRegistry.FloorDark);
            b.MarkPlayable(40, 32, 44, 32, 7);
            foreach (var p in new[] { new Vector2Int(38, 11), new Vector2Int(38, 25), new Vector2Int(46, 11), new Vector2Int(46, 25) })
            {
                b.Pillar(p.x, p.y, 7, 11, 2);
                b.UnmarkPlayable(p.x, p.y, p.x + 1, p.y + 1);
            }
            for (int x = 34; x <= 51; x += 5) if (x < 40 || x > 44) b.Pillar(x, 32, 4, 12, 1);
            Brazier(b, 35, 30, 7, 4, false, 1);
            Brazier(b, 50, 30, 7, 4, false, 1);
            Brazier(b, 35, 8, 7, 4, false, 1);
            Brazier(b, 50, 8, 7, 4, false, 1);
            b.Gates.Add((new Vector3(33.5f, 7, 19f), 90f, "sala_entrada"));
            b.Gates.Add((new Vector3(52.5f, 7, 21f), 90f, "sala_leste"));
            b.Gates.Add((new Vector3(42.5f, 7, 32.5f), 0f, "sala_norte"));
            b.Chests.Add((new Vector3(49.5f, 7, 27.5f), 225f, "bau_sala", "bau_sala", true));
            b.Zone("sala_ativacao", new Vector3(35, 6, 12), new Vector3(42, 12, 26), ZoneAction.ActivateEncounter, null, "sala");
            b.Spawn("sala:fundo", new Vector3(49.5f, 7, 26.5f));
            b.Spawn("sala:fundo", new Vector3(49.5f, 7, 9.5f));
            b.Spawn("sala:", new Vector3(44.5f, 7, 19.5f));
            b.Spawn("sala:", new Vector3(40.5f, 7, 29.5f));
            b.Spawn("sala:", new Vector3(43.5f, 7, 9.5f));
            b.Checkpoints.Add((new Vector3(50f, 7, 19f), 270f, "cp_sala", 1));
            b.Room("sala", new Vector3(30, 0, 2), new Vector3(56, 22, 36));
            b.Water(34, 0, 51, 5, 2);

            // 4. Desvio opcional: santuário coberto de vinhas (H=5)
            b.Platform(40, 33, 44, 34, 7, BlockRegistry.FloorDark, BlockRegistry.Floor, 0);
            b.Stairs(35, new Vector2Int(0, 1), 40, 44, 4, 7);
            b.Platform(32, 39, 52, 52, 5, BlockRegistry.Litter, BlockRegistry.Dirt, 0);
            b.Wall(31, 38, 31, 53, 4, 9, BlockRegistry.WallMossy);
            b.Wall(53, 38, 53, 53, 4, 9, BlockRegistry.WallMossy);
            b.Wall(31, 53, 53, 53, 4, 10, BlockRegistry.WallMossy);
            b.Water(34, 44, 37, 49, 4);
            b.UnmarkPlayable(34, 44, 37, 49);
            b.Pillar(40, 46, 5, 8, 1, BlockRegistry.Bamboo);
            b.Pillar(49, 42, 5, 7, 1, BlockRegistry.Bamboo);
            b.Pillar(47, 48, 5, 9, 2, BlockRegistry.WallMossy);
            b.UnmarkPlayable(40, 46, 40, 46);
            b.UnmarkPlayable(49, 42, 49, 42);
            b.UnmarkPlayable(47, 48, 48, 49);
            b.Chests.Add((new Vector3(44.5f, 5, 50.5f), 180f, "bau_desvio", "bau_desvio", false));
            b.Prop(PropForge.Lantern, new Vector3(32.5f, 10, 52.5f), 0f, 0.8f);
            b.Prop(PropForge.Lantern, new Vector3(52.5f, 10, 52.5f), 0f, 0.8f);
            foreach (var p in new[] { new Vector3(38.5f, 5, 41.5f), new Vector3(50.5f, 5, 47.5f), new Vector3(33.5f, 5, 51f) }) b.Prop(PropForge.Fern, p, b.R() * 360f, 1.2f);
            b.Zone("desvio_ativacao", new Vector3(36, 4, 39), new Vector3(50, 9, 46), ZoneAction.ActivateEncounter, null, "desvio");
            b.Spawn("desvio:vinha", new Vector3(47.5f, 5, 44.5f));
            b.Spawn("desvio:", new Vector3(41.5f, 5, 49.5f));
            b.Spawn("desvio:", new Vector3(50.5f, 5, 50.5f));
            b.Room("desvio", new Vector3(28, 0, 33), new Vector3(56, 20, 56));
            JungleDecor(b, 22, 40, 30, 70, 4, 6, 10);
            JungleDecor(b, 54, 36, 62, 70, 4, 6, 10);

            // 5. Arena do obelisco (H=11). Terrenos vizinhos primeiro; a arena por último.
            b.Platform(56, 29, 100, 33, 8, BlockRegistry.Litter, BlockRegistry.Dirt, 0, false);
            b.Platform(54, 14, 61, 17, 7, BlockRegistry.Path, BlockRegistry.Stone, 0);
            b.Platform(54, 24, 61, 27, 7, BlockRegistry.Grass, BlockRegistry.Dirt, 0);
            b.Platform(64, 2, 76, 5, 7, BlockRegistry.Path, BlockRegistry.Stone, 0);
            b.Platform(66, 6, 68, 13, 7, BlockRegistry.Path, BlockRegistry.Stone, 0);
            b.Platform(74, 6, 76, 13, 7, BlockRegistry.Grass, BlockRegistry.Dirt, 0);
            var arena = Arena(b, 62, 14, 11, 7, "arena");
            b.Chests.Add((new Vector3(73.5f, 11, 25.5f), 225f, "bau_arena", "bau_arena", true));
            b.Prop(PropForge.Pot, new Vector3(65.5f, 7, 3.5f), 0f);
            b.Prop(PropForge.Pot, new Vector3(75.5f, 7, 2.5f), 30f);
            b.Pickups.Add((new Vector3(70.5f, 7, 3.5f), PickupKind.HealthOrb, null, 1, "orb_terraco"));
            b.Pickups.Add((new Vector3(56.5f, 7, 26f), PickupKind.Arrows, null, 10, "flechas_arena"));
            b.Checkpoints.Add((new Vector3(56.5f, 7, 15.5f), 0f, "cp_arena", 2));
            b.Room("arena", new Vector3(50, 0, 0), new Vector3(80, 24, 32));
            JungleDecor(b, 60, 29, 76, 34, 8, 6, 12);

            // 6. Respiro além do portão (H=11)
            b.Platform(77, 16, 92, 30, 11, BlockRegistry.Grass, BlockRegistry.Dirt, 0);
            for (int x = 77; x <= 92; x++) for (int z = 20; z <= 22; z++) b.Grid.Set(x, 10, z, BlockRegistry.Path);
            for (int z = 22; z <= 30; z++) for (int x = 84; x <= 86; x++) b.Grid.Set(x, 10, z, BlockRegistry.Path);
            b.Water(86, 13, 91, 17, 10);
            b.UnmarkPlayable(86, 13, 91, 17);
            b.Checkpoints.Add((new Vector3(80.5f, 11, 25.5f), 90f, "cp_respiro", 3));
            b.Prop(PropForge.Merchant, new Vector3(89.5f, 11, 26.5f), 200f);
            Brazier(b, 78, 29, 11, 7, false, 1);
            Brazier(b, 91, 29, 11, 7, false, 1);
            b.Tree(92, 18, 11, 4, 2);
            b.Prop(PropForge.Fern, new Vector3(79.5f, 11, 17.5f), 0f, 1.2f);
            b.Prop(PropForge.Crate, new Vector3(91.5f, 11, 24.5f), 0f);
            b.Zone("dica_respiro", new Vector3(77, 10, 16), new Vector3(92, 15, 31), ZoneAction.Hint, "Área segura: I abre o inventário para trocar equipamentos; E no altar para aprimorar itens.");
            b.Room("respiro", new Vector3(74, 0, 10), new Vector3(96, 24, 34));

            // 7. Salão do guardião (H=13)
            b.Stairs(34, new Vector2Int(0, -1), 83, 87, 4, 13);
            b.Platform(76, 35, 96, 54, 13, BlockRegistry.Floor, BlockRegistry.Floor, 0);
            b.Wall(75, 34, 75, 55, 7, 17, BlockRegistry.Wall);
            b.Wall(97, 34, 97, 55, 7, 17, BlockRegistry.Wall);
            b.Wall(75, 55, 97, 55, 7, 17, BlockRegistry.Wall);
            b.Clear(84, 13, 55, 88, 25, 55);
            b.Fill(84, 12, 55, 88, 12, 55, BlockRegistry.FloorDark);
            b.MarkPlayable(84, 55, 88, 55, 13);
            foreach (var p in new[] { new Vector2Int(80, 40), new Vector2Int(91, 40), new Vector2Int(80, 49), new Vector2Int(91, 49) })
            {
                b.Pillar(p.x, p.y, 13, 17, 2);
                b.UnmarkPlayable(p.x, p.y, p.x + 1, p.y + 1);
            }
            // Pilares do muro norte, exceto dentro da abertura do cofre (x 84–88), que precisa ficar livre.
            for (int x = 76; x <= 96; x += 5) if (x < 84 || x > 88) b.Pillar(x, 55, 7, 19, 1);
            b.Prop(PropForge.Lantern, new Vector3(83.5f, 18, 55.5f), 0f, 1f);
            b.Prop(PropForge.Lantern, new Vector3(89.5f, 18, 55.5f), 0f, 1f);
            Brazier(b, 77, 53, 13, 7, true, 1);
            Brazier(b, 95, 53, 13, 7, false, 1);
            Brazier(b, 77, 36, 13, 7, false, 1);
            Brazier(b, 95, 36, 13, 7, true, 1);
            b.Gates.Add((new Vector3(85.5f, 13, 35.5f), 0f, "salao_entrada"));
            b.Gates.Add((new Vector3(86.5f, 13, 55.5f), 0f, "salao_saida"));
            b.Zone("salao_ativacao", new Vector3(78, 12, 37), new Vector3(94, 17, 45), ZoneAction.ActivateEncounter, null, "salao");
            b.Spawn("salao:fundo", new Vector3(93.5f, 13, 51.5f));
            b.Spawn("salao:fundo", new Vector3(78.5f, 13, 51.5f));
            b.Spawn("salao:vinha", new Vector3(86.5f, 13, 47.5f));
            b.Spawn("salao:vinha", new Vector3(80.5f, 13, 45.5f));
            b.Spawn("salao:elite", new Vector3(86.5f, 13, 51f));
            b.Spawn("salao:", new Vector3(92.5f, 13, 44.5f));
            b.Spawn("salao:", new Vector3(79.5f, 13, 38.5f));
            b.Checkpoints.Add((new Vector3(85.5f, 11, 29.5f), 0f, "cp_salao", 4));
            b.Room("salao", new Vector3(72, 0, 31), new Vector3(100, 26, 58));

            // 8. Cofre e saída (H=13)
            b.Platform(80, 56, 92, 64, 13, BlockRegistry.Carved, BlockRegistry.Floor, 0);
            b.Wall(79, 56, 79, 65, 7, 18, BlockRegistry.Wall);
            b.Wall(93, 56, 93, 65, 7, 18, BlockRegistry.Wall);
            b.Wall(79, 65, 93, 65, 7, 18, BlockRegistry.Wall);
            b.Chests.Add((new Vector3(83.5f, 13, 60.5f), 90f, "bau_final", "bau_final", true));
            b.Prop(PropForge.Exit, new Vector3(86.5f, 13, 63.3f), 0f, 1f, null, "Saida");
            Brazier(b, 80, 57, 13, 7, false, 1);
            Brazier(b, 92, 57, 13, 7, false, 1);
            b.Room("cofre", new Vector3(76, 0, 54), new Vector3(96, 26, 68));

            // Selva e ruínas ao redor (continuidade além da área caminhável).
            JungleDecor(b, 22, 0, 33, 14, 4, 6, 10);
            JungleDecor(b, 78, 0, 103, 12, 4, 10, 16);
            JungleDecor(b, 94, 12, 103, 71, 4, 12, 18);
            JungleDecor(b, 60, 56, 78, 71, 4, 8, 12);

            // Rota do piloto automático (validação do percurso de ponta a ponta).
            b.Route.Add(("acampamento", new Vector3(16f, 5, 18.5f), RouteAction.GoTo, null));
            b.Route.Add(("corredor", new Vector3(31f, 7, 18.5f), RouteAction.ClearEncounter, "corredor"));
            b.Route.Add(("sala", new Vector3(42f, 7, 19f), RouteAction.ClearEncounter, "sala"));
            b.Route.Add(("bau da sala", new Vector3(49.5f, 7, 26f), RouteAction.OpenChest, "bau_sala"));
            b.Route.Add(("desvio", new Vector3(42.5f, 5, 42f), RouteAction.ClearEncounter, "desvio"));
            b.Route.Add(("bau do desvio", new Vector3(44.5f, 5, 49f), RouteAction.OpenChest, "bau_desvio"));
            b.Route.Add(("volta do desvio", new Vector3(42.5f, 7, 30f), RouteAction.GoTo, null));
            b.Route.Add(("saida leste", new Vector3(53f, 7, 20.5f), RouteAction.GoTo, null));
            b.Route.Add(("arena", new Vector3(69f, 11, 21f), RouteAction.ClearEncounter, "arena"));
            b.Route.Add(("respiro", new Vector3(84f, 11, 22f), RouteAction.GoTo, null));
            b.Route.Add(("salao", new Vector3(86f, 13, 42f), RouteAction.ClearEncounter, "salao"));
            b.Route.Add(("bau final", new Vector3(84.5f, 13, 59f), RouteAction.OpenChest, "bau_final"));
            b.Route.Add(("saida", new Vector3(86.5f, 13, 61.5f), RouteAction.Exit, null));
            return b;
        }

        // ------------------------------------------------------------------ Diorama do menu

        public static LevelBuilder Menu()
        {
            var b = new LevelBuilder(40, 20, 40, 5);
            b.Ground(5);
            JungleDecor(b, 0, 0, 39, 39, 5, 18, 30);
            Arena(b, 13, 13, 9, 5, "menu", true, true);
            return b;
        }
    }
}

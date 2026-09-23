using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ruinas.EditorTools
{
    /// <summary>
    /// Descrição de um nível em blocos + posicionamentos. Coordenadas em blocos (1 unidade);
    /// "altura de piso" H significa superfície em y = H (blocos 0..H-1 preenchidos).
    /// </summary>
    public class LevelBuilder
    {
        public readonly VoxelGrid Grid;
        public readonly BlockRegistry Reg = new BlockRegistry();
        public readonly int SX, SY, SZ;
        public readonly bool[,] Playable;
        public readonly float[,] Walk;     // altura da superfície caminhável (NaN = nenhuma)
        public readonly bool[,] StairCell;
        public readonly System.Random Rng;

        public struct Ramp { public Vector3 center, size; public Quaternion rotation; }
        public class Placement
        {
            public GameObject prefab;
            public Vector3 position;
            public float yaw;
            public float scale = 1f;
            public string name;
            public Action<GameObject> configure;
        }
        public class LightSpec { public Vector3 position; public Color color; public float range, intensity; public bool flicker, shadows; }
        public class ZoneSpec { public string id; public Bounds bounds; public ZoneAction action; public string text; public string encounterKey; }
        public class RoomSpec { public string name; public Bounds bounds; public int priority; }
        public class StripSpec { public Vector3 from, to; public float width; }

        public readonly List<Ramp> Ramps = new List<Ramp>();
        public readonly List<Placement> Props = new List<Placement>();
        public readonly List<LightSpec> Lights = new List<LightSpec>();
        public readonly List<ZoneSpec> Zones = new List<ZoneSpec>();
        public readonly List<RoomSpec> Rooms = new List<RoomSpec>();
        public readonly List<StripSpec> Strips = new List<StripSpec>();
        public readonly Dictionary<string, List<Vector3>> SpawnPoints = new Dictionary<string, List<Vector3>>();
        public readonly List<(Vector3 pos, float yaw, string id, int order)> Checkpoints = new List<(Vector3, float, string, int)>();
        public readonly List<(Vector3 pos, float yaw, string id, string loot, bool locked)> Chests = new List<(Vector3, float, string, string, bool)>();
        public readonly List<(Vector3 pos, float yaw, string id)> Gates = new List<(Vector3, float, string)>();
        public readonly Dictionary<string, Vector3> Anchors = new Dictionary<string, Vector3>();
        public readonly List<(string label, Vector3 pos, RouteAction action, string refId)> Route = new List<(string, Vector3, RouteAction, string)>();
        public readonly List<(Vector3 pos, PickupKind kind, string item, int amount, string id)> Pickups = new List<(Vector3, PickupKind, string, int, string)>();

        public LevelBuilder(int sx, int sy, int sz, int seed)
        {
            SX = sx; SY = sy; SZ = sz;
            Grid = new VoxelGrid(sx, sy, sz);
            Playable = new bool[sx, sz];
            Walk = new float[sx, sz];
            StairCell = new bool[sx, sz];
            for (int x = 0; x < sx; x++) for (int z = 0; z < sz; z++) Walk[x, z] = float.NaN;
            Rng = new System.Random(seed);
        }

        public float R() => (float)Rng.NextDouble();
        public int RI(int a, int b) => Rng.Next(a, b);
        bool InXZ(int x, int z) => x >= 0 && z >= 0 && x < SX && z < SZ;

        // ------------------------------------------------------------------ Blocos

        public void Fill(int x0, int y0, int z0, int x1, int y1, int z1, byte b) => Grid.Fill(x0, y0, z0, x1, y1, z1, b);

        public void Clear(int x0, int y0, int z0, int x1, int y1, int z1) => Grid.Fill(x0, y0, z0, x1, y1, z1, BlockRegistry.Air);

        /// <summary>Terreno base: preenche até groundH com camadas de pedra/terra e superfície variada.</summary>
        public void Ground(int groundH)
        {
            for (int x = 0; x < SX; x++)
                for (int z = 0; z < SZ; z++)
                {
                    for (int y = 0; y < groundH - 1; y++) Grid.Set(x, y, z, y < groundH - 2 ? BlockRegistry.Stone : BlockRegistry.Dirt);
                    float n = Mathf.PerlinNoise(x * 0.13f + 3.1f, z * 0.13f + 7.7f);
                    byte top = n > 0.62f ? BlockRegistry.Grass : n > 0.4f ? BlockRegistry.Litter : BlockRegistry.Dirt;
                    Grid.Set(x, groundH - 1, z, top);
                }
        }

        /// <summary>Plataforma retangular com superfície em H (inclusivo em x/z). Marca como jogável.</summary>
        public void Platform(int x0, int z0, int x1, int z1, int H, byte top, byte body, int fromY = 0, bool playable = true)
        {
            Fill(x0, fromY, z0, x1, H - 2, z1, body);
            Fill(x0, H - 1, z0, x1, H - 1, z1, top);
            Clear(x0, H, z0, x1, SY - 1, z1);
            if (playable) MarkPlayable(x0, z0, x1, z1, H);
        }

        public void MarkPlayable(int x0, int z0, int x1, int z1, float h)
        {
            for (int x = Mathf.Min(x0, x1); x <= Mathf.Max(x0, x1); x++)
                for (int z = Mathf.Min(z0, z1); z <= Mathf.Max(z0, z1); z++)
                {
                    if (!InXZ(x, z)) continue;
                    Playable[x, z] = true;
                    Walk[x, z] = h;
                }
        }

        public void UnmarkPlayable(int x0, int z0, int x1, int z1)
        {
            for (int x = Mathf.Min(x0, x1); x <= Mathf.Max(x0, x1); x++)
                for (int z = Mathf.Min(z0, z1); z <= Mathf.Max(z0, z1); z++)
                    if (InXZ(x, z)) { Playable[x, z] = false; Walk[x, z] = float.NaN; }
        }

        /// <summary>
        /// Escada que desce a partir de uma borda: passos de meio bloco (1 bloco de profundidade).
        /// edge = coordenada da primeira célula do degrau mais alto; dir = direção de descida (±X ou ±Z);
        /// [w0, w1] = faixa perpendicular. Visual em blocos só-visuais; colisão por rampa suave.
        /// </summary>
        public void Stairs(int edge, Vector2Int dir, int w0, int w1, int steps, int topH, int fromY = 0)
        {
            for (int i = 0; i < steps; i++)
            {
                float h = topH - 0.5f * (i + 1);
                int along = edge + (dir.x != 0 ? dir.x : dir.y) * i;
                for (int w = w0; w <= w1; w++)
                {
                    int x = dir.x != 0 ? along : w;
                    int z = dir.x != 0 ? w : along;
                    int full = Mathf.FloorToInt(h);
                    Fill(x, fromY, z, x, full - 1, z, BlockRegistry.StairFull);
                    Clear(x, full, z, x, SY - 1, z);
                    if (h - full > 0.01f) Grid.Set(x, full, z, BlockRegistry.StairSlab);
                    if (InXZ(x, z))
                    {
                        Playable[x, z] = true;
                        Walk[x, z] = h;
                        StairCell[x, z] = true;
                    }
                }
            }
            // Rampa: da borda (topH − 0,25) até o fim (topH − 0,5·steps − 0,25), sobre toda a largura.
            float length = steps;
            float drop = 0.5f * steps;
            float angle = Mathf.Atan2(drop, length) * Mathf.Rad2Deg;
            float width = w1 - w0 + 1;
            float centerAlong = edge + (dir.x != 0 ? dir.x : dir.y) * (steps * 0.5f) + ((dir.x != 0 ? dir.x : dir.y) < 0 ? 1f : 0f);
            float midH = topH - 0.25f - drop * 0.5f;
            float thickness = 0.4f;
            Vector3 center;
            Quaternion rot;
            float slopeLen = Mathf.Sqrt(length * length + drop * drop) + 0.3f;
            if (dir.x != 0)
            {
                center = new Vector3(centerAlong, midH - thickness * 0.5f, w0 + width * 0.5f);
                rot = Quaternion.Euler(0f, 0f, dir.x > 0 ? -angle : angle);
                Ramps.Add(new Ramp { center = center, size = new Vector3(slopeLen, thickness, width), rotation = rot });
            }
            else
            {
                center = new Vector3(w0 + width * 0.5f, midH - thickness * 0.5f, centerAlong);
                rot = Quaternion.Euler(dir.y > 0 ? angle : -angle, 0f, 0f);
                Ramps.Add(new Ramp { center = center, size = new Vector3(width, thickness, slopeLen), rotation = rot });
            }
        }

        public void Wall(int x0, int z0, int x1, int z1, int fromY, int toY, byte block)
        {
            Fill(x0, fromY, z0, x1, toY, z1, block);
            BlockCells(x0, z0, x1, z1, toY + 1);
        }

        public void Pillar(int x, int z, int fromY, int toY, int size = 1, byte block = BlockRegistry.Pillar)
        {
            Fill(x, fromY, z, x + size - 1, toY, z + size - 1, block);
            BlockCells(x, z, x + size - 1, z + size - 1, toY + 1);
        }

        /// <summary>Células cobertas por um volume sólido acima do piso deixam de ser caminháveis.</summary>
        void BlockCells(int x0, int z0, int x1, int z1, int top)
        {
            for (int x = Mathf.Min(x0, x1); x <= Mathf.Max(x0, x1); x++)
                for (int z = Mathf.Min(z0, z1); z <= Mathf.Max(z0, z1); z++)
                {
                    if (!InXZ(x, z) || !Playable[x, z]) continue;
                    if (top > Walk[x, z] + 0.5f) { Playable[x, z] = false; Walk[x, z] = float.NaN; }
                }
        }

        public void Tree(int x, int z, int groundY, int trunk, int radius, bool dark = false)
        {
            for (int y = groundY; y < groundY + trunk; y++) Grid.Set(x, y, z, BlockRegistry.Wood);
            int cy = groundY + trunk;
            for (int dy = -radius; dy <= radius; dy++)
                for (int dz = -radius - 1; dz <= radius + 1; dz++)
                    for (int dx = -radius - 1; dx <= radius + 1; dx++)
                    {
                        float d = (dx * dx) / (float)((radius + 1) * (radius + 1)) + (dy * dy) / (float)(radius * radius) * 1.4f + (dz * dz) / (float)((radius + 1) * (radius + 1));
                        if (d > 1f || R() < 0.08f) continue;
                        int X = x + dx, Y = cy + dy, Z = z + dz;
                        if (!Grid.InBounds(X, Y, Z) || Grid.Get(X, Y, Z) != 0) continue;
                        Grid.Set(X, Y, Z, (dark || R() < 0.3f) ? BlockRegistry.LeavesDark : BlockRegistry.Leaves);
                    }
        }

        public void Bush(int x, int z, int groundY, int size = 1)
        {
            for (int dx = -size; dx <= size; dx++)
                for (int dz = -size; dz <= size; dz++)
                    for (int dy = 0; dy <= size; dy++)
                    {
                        if (Mathf.Abs(dx) + Mathf.Abs(dz) + dy > size + 1 || R() < 0.15f) continue;
                        int X = x + dx, Y = groundY + dy, Z = z + dz;
                        if (Grid.InBounds(X, Y, Z) && Grid.Get(X, Y, Z) == 0) Grid.Set(X, Y, Z, R() < 0.5f ? BlockRegistry.Leaves : BlockRegistry.LeavesDark);
                    }
        }

        public void Water(int x0, int z0, int x1, int z1, int surfaceBlockY)
        {
            Clear(x0, surfaceBlockY, z0, x1, SY - 1, z1);
            Fill(x0, surfaceBlockY - 2, z0, x1, surfaceBlockY - 1, z1, BlockRegistry.Stone);
            Fill(x0, surfaceBlockY, z0, x1, surfaceBlockY, z1, BlockRegistry.Water);
        }

        // ------------------------------------------------------------------ Posicionamentos

        public void Prop(GameObject prefab, Vector3 pos, float yaw = 0f, float scale = 1f, Action<GameObject> configure = null, string name = null)
        {
            if (prefab == null) return;
            Props.Add(new Placement { prefab = prefab, position = pos, yaw = yaw, scale = scale, configure = configure, name = name });
        }

        public void Light(Vector3 pos, Color c, float range, float intensity, bool flicker = false, bool shadows = false)
            => Lights.Add(new LightSpec { position = pos, color = c, range = range, intensity = intensity, flicker = flicker, shadows = shadows });

        public void Zone(string id, Vector3 min, Vector3 max, ZoneAction action, string text = null, string encounterKey = null)
        {
            var b = new Bounds();
            b.SetMinMax(min, max);
            Zones.Add(new ZoneSpec { id = id, bounds = b, action = action, text = text, encounterKey = encounterKey });
        }

        public void Room(string name, Vector3 min, Vector3 max, int priority = 0)
        {
            var b = new Bounds();
            b.SetMinMax(min, max);
            Rooms.Add(new RoomSpec { name = name, bounds = b, priority = priority });
        }

        public void Spawn(string key, Vector3 pos)
        {
            if (!SpawnPoints.TryGetValue(key, out var list)) SpawnPoints[key] = list = new List<Vector3>();
            list.Add(pos);
        }

        /// <summary>Faixa luminosa rente ao piso (contorno da arena).</summary>
        public void Strip(Vector3 from, Vector3 to, float width = 0.1f) => Strips.Add(new StripSpec { from = from, to = to, width = width });

        // ------------------------------------------------------------------ Consultas

        public bool IsSolid(int x, int y, int z)
        {
            var d = Reg[Grid.Get(x, y, z)];
            return d.shape != BlockShape.Empty && d.solid;
        }

        /// <summary>Superfície de chão mais alta abaixo de y (para apoiar adereços), ou -1.</summary>
        public int SurfaceAt(int x, int z, int belowY = -1)
        {
            int top = Grid.TopSolid(x, z, Reg, belowY < 0 ? SY - 1 : belowY);
            return top < 0 ? -1 : top + 1;
        }
    }
}

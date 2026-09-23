using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ruinas
{
    /// <summary>
    /// Gera malhas de blocos por região: faces ocultas descartadas, UV no atlas (topo alinhado ao mundo,
    /// laterais com v para cima), variação de tom por bloco e oclusão ambiente por vértice (0fps).
    /// Também gera a malha de colisão, sem os blocos só visuais (degraus usam rampas suaves).
    /// </summary>
    public static class VoxelMesher
    {
        public const int AtlasSize = 1024, Cell = 64, TileSize = 32, Pad = 16, Columns = 16;
        static readonly float[] AoCurve = { 0.42f, 0.62f, 0.81f, 1f };

        // Vértices de cada face na ordem BL, TL, TR, BR (vista de fora) — horário para o Unity.
        static readonly Vector3[][] FaceVerts =
        {
            new[] { new Vector3(0, 1, 0), new Vector3(0, 1, 1), new Vector3(1, 1, 1), new Vector3(1, 1, 0) }, // +Y
            new[] { new Vector3(0, 0, 1), new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 1) }, // -Y
            new[] { new Vector3(1, 0, 1), new Vector3(1, 1, 1), new Vector3(0, 1, 1), new Vector3(0, 0, 1) }, // +Z
            new[] { new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 1, 0), new Vector3(1, 0, 0) }, // -Z
            new[] { new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(1, 1, 1), new Vector3(1, 0, 1) }, // +X
            new[] { new Vector3(0, 0, 1), new Vector3(0, 1, 1), new Vector3(0, 1, 0), new Vector3(0, 0, 0) }, // -X
        };
        static readonly Vector3Int[] Dirs =
        {
            new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0), new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, -1), new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0),
        };
        static readonly Vector2[] FaceUV = { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };

        public static Rect TileRect(int tile)
        {
            int col = tile % Columns;
            int row = tile / Columns;
            float s = 1f / AtlasSize;
            return new Rect((col * Cell + Pad) * s, (row * Cell + Pad) * s, TileSize * s, TileSize * s);
        }

        public class MeshData
        {
            public readonly List<Vector3> vertices = new List<Vector3>();
            public readonly List<Vector3> normals = new List<Vector3>();
            public readonly List<Vector2> uvs = new List<Vector2>();
            public readonly List<Color32> colors = new List<Color32>();
            public readonly List<int> triangles = new List<int>();

            public Mesh ToMesh(string name, bool withAttributes = true)
            {
                var m = new Mesh { name = name };
                if (vertices.Count > 65000) m.indexFormat = IndexFormat.UInt32;
                m.SetVertices(vertices);
                if (withAttributes)
                {
                    m.SetNormals(normals);
                    m.SetUVs(0, uvs);
                    m.SetColors(colors);
                }
                m.SetTriangles(triangles, 0);
                if (!withAttributes) m.RecalculateNormals();
                m.RecalculateBounds();
                return m;
            }
        }

        public static MeshData Build(VoxelGrid g, BlockRegistry reg, int x0, int z0, int x1, int z1, bool collision)
        {
            var md = new MeshData();
            for (int y = 0; y < g.SizeY; y++)
                for (int z = z0; z < z1; z++)
                    for (int x = x0; x < x1; x++)
                    {
                        byte id = g.Get(x, y, z);
                        if (id == 0) continue;
                        var d = reg[id];
                        if (d.shape == BlockShape.Empty) continue;
                        if (collision && (!d.solid || d.visualOnly)) continue;
                        EmitBlock(g, reg, md, x, y, z, id, d, x0, z0, collision);
                    }
            return md;
        }

        static bool Hides(VoxelGrid g, BlockRegistry reg, byte self, BlockDef sd, int nx, int ny, int nz, int face, bool collision)
        {
            byte n = g.Get(nx, ny, nz);
            if (n == 0) return false;
            var nd = reg[n];
            if (collision)
            {
                if (!nd.solid || nd.visualOnly) return false;
                if (nd.shape == BlockShape.Full) return true;
                return face == 0 && sd.shape == BlockShape.Full && nd.shape == BlockShape.SlabBottom;
            }
            if (self == BlockRegistry.Water) return n == BlockRegistry.Water || (nd.shape == BlockShape.Full && nd.occluder && face != 0);
            if (nd.shape == BlockShape.Full && nd.occluder) return true;
            // Topo de um bloco cheio sob um meio-bloco fica coberto.
            if (face == 0 && sd.shape == BlockShape.Full && nd.shape == BlockShape.SlabBottom && n != BlockRegistry.Water) return true;
            // Laterais de meio-blocos vizinhos de meio-blocos na mesma altura.
            if (face >= 2 && sd.shape == BlockShape.SlabBottom && nd.shape == BlockShape.SlabBottom && n != BlockRegistry.Water) return true;
            return false;
        }

        static bool Occludes(VoxelGrid g, BlockRegistry reg, int x, int y, int z)
        {
            byte n = g.Get(x, y, z);
            if (n == 0) return false;
            var d = reg[n];
            return d.shape == BlockShape.Full && d.occluder;
        }

        static void EmitBlock(VoxelGrid g, BlockRegistry reg, MeshData md, int x, int y, int z, byte id, BlockDef d, int ox, int oz, bool collision)
        {
            float height = d.shape == BlockShape.SlabBottom ? (id == BlockRegistry.Water ? 0.82f : 0.5f) : 1f;
            uint h = GameRandom.Hash(x, y, z, 7);
            float bright = 1f + ((h & 0xFF) / 255f - 0.5f) * 2f * d.jitter;
            var tint = d.tint;
            byte cr = (byte)Mathf.Clamp(tint.r * bright, 0, 255), cg = (byte)Mathf.Clamp(tint.g * bright, 0, 255), cb = (byte)Mathf.Clamp(tint.b * bright, 0, 255);

            for (int f = 0; f < 6; f++)
            {
                var dir = Dirs[f];
                // Topo de meio-bloco nunca é escondido pelo vizinho de cima (há meio bloco de ar).
                bool slabTop = f == 0 && d.shape == BlockShape.SlabBottom;
                if (!slabTop && Hides(g, reg, id, d, x + dir.x, y + dir.y, z + dir.z, f, collision)) continue;

                int start = md.vertices.Count;
                var verts = FaceVerts[f];
                float[] ao = { 1f, 1f, 1f, 1f };
                int[] aoLevel = { 3, 3, 3, 3 };
                if (!collision && d.shape == BlockShape.Full && d.occluder)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        aoLevel[i] = VertexAO(g, reg, x, y, z, f, verts[i]);
                        ao[i] = AoCurve[aoLevel[i]];
                    }
                }

                Tile tile = collision ? Tile.StoneRough : PickTile(g, reg, d, x, y, z, f, h);
                Rect r = TileRect((int)tile);
                for (int i = 0; i < 4; i++)
                {
                    Vector3 v = verts[i];
                    v.y *= height;
                    md.vertices.Add(new Vector3(x - ox + v.x, y + v.y, z - oz + v.z));
                    if (collision) continue;
                    md.normals.Add(dir);
                    Vector2 uv = FaceUV[i];
                    if (f >= 2 && height < 1f) uv.y *= height;
                    md.uvs.Add(new Vector2(r.x + uv.x * r.width, r.y + uv.y * r.height));
                    md.colors.Add(new Color32(cr, cg, cb, (byte)(ao[i] * 255f)));
                }

                if (aoLevel[0] + aoLevel[2] >= aoLevel[1] + aoLevel[3])
                {
                    md.triangles.Add(start); md.triangles.Add(start + 1); md.triangles.Add(start + 2);
                    md.triangles.Add(start); md.triangles.Add(start + 2); md.triangles.Add(start + 3);
                }
                else
                {
                    md.triangles.Add(start + 1); md.triangles.Add(start + 2); md.triangles.Add(start + 3);
                    md.triangles.Add(start + 1); md.triangles.Add(start + 3); md.triangles.Add(start);
                }
            }
        }

        static Tile PickTile(VoxelGrid g, BlockRegistry reg, BlockDef d, int x, int y, int z, int face, uint h)
        {
            if (face == 0)
            {
                if (d.macroTop && d.macroVariants != null && d.macroVariants.Length > 0)
                {
                    uint mh = GameRandom.Hash(x >> 1, 0, z >> 1, 3);
                    // Variante C (rachada/musgo) é mais rara.
                    int vi = d.macroVariants.Length == 1 ? 0 : (mh % 100) < 18 && d.macroVariants.Length > 2 ? 2 : (int)((mh >> 8) % 2);
                    int q = (x & 1) + 2 * (z & 1);
                    return (Tile)((int)d.macroVariants[vi] + q);
                }
                if (d.top == Tile.DirtTop) return (h >> 9) % 3 == 0 ? Tile.DirtTop2 : Tile.DirtTop;
                if (d.top == Tile.GrassTop) return (h >> 9) % 3 == 0 ? Tile.GrassTop2 : Tile.GrassTop;
                if (d.top == Tile.PathCobble) return (h >> 9) % 2 == 0 ? Tile.PathCobble2 : Tile.PathCobble;
                return d.top;
            }
            if (face == 1) return d.bottom;
            // Musgo concentrado perto do chão e em trechos úmidos.
            byte below = g.Get(x, y - 1, z);
            var bd = reg[below];
            bool ground = y == 0 || below == BlockRegistry.Dirt || below == BlockRegistry.Grass || below == BlockRegistry.Litter || below == BlockRegistry.Water || (bd.shape == BlockShape.Empty && y < 3);
            float mossChance = ground ? 0.7f : 0.1f;
            if (((h >> 12) & 0xFF) / 255f < mossChance && d.mossySide != 0) return d.mossySide;
            if (d.sideVariants != null && d.sideVariants.Length > 0) return d.sideVariants[(h >> 4) % (uint)d.sideVariants.Length];
            return d.side;
        }

        static int VertexAO(VoxelGrid g, BlockRegistry reg, int x, int y, int z, int face, Vector3 corner)
        {
            var n = Dirs[face];
            int fx = x + n.x, fy = y + n.y, fz = z + n.z;
            // Eixos tangentes da face.
            Vector3Int t1, t2;
            if (n.y != 0) { t1 = new Vector3Int(1, 0, 0); t2 = new Vector3Int(0, 0, 1); }
            else if (n.z != 0) { t1 = new Vector3Int(1, 0, 0); t2 = new Vector3Int(0, 1, 0); }
            else { t1 = new Vector3Int(0, 0, 1); t2 = new Vector3Int(0, 1, 0); }
            int s1 = Component(corner, t1) > 0.5f ? 1 : -1;
            int s2 = Component(corner, t2) > 0.5f ? 1 : -1;
            bool side1 = Occludes(g, reg, fx + t1.x * s1, fy + t1.y * s1, fz + t1.z * s1);
            bool side2 = Occludes(g, reg, fx + t2.x * s2, fy + t2.y * s2, fz + t2.z * s2);
            bool cornerB = Occludes(g, reg, fx + t1.x * s1 + t2.x * s2, fy + t1.y * s1 + t2.y * s2, fz + t1.z * s1 + t2.z * s2);
            if (side1 && side2) return 0;
            return 3 - ((side1 ? 1 : 0) + (side2 ? 1 : 0) + (cornerB ? 1 : 0));
        }

        static float Component(Vector3 v, Vector3Int axis) => axis.x != 0 ? v.x : axis.y != 0 ? v.y : v.z;
    }
}

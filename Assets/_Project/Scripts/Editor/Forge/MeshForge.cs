using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ruinas.EditorTools
{
    /// <summary>
    /// Malhas geradas: caixas com UV de skin (peças rígidas), itens extrudados de sprites, adereços
    /// por caixas (cor por vértice ou tiles do atlas) e primitivas de efeitos. Salvas como assets.
    /// </summary>
    public static class MeshForge
    {
        public const string Dir = "Assets/_Project/Art/Meshes";
        public const float Texel = 0.06f;     // 1 texel de personagem em unidades de mundo
        public const float ItemPixel = 0.045f; // 1 pixel de ícone extrudado

        // Ordem de faces igual ao VoxelMesher: +Y, -Y, +Z, -Z, +X, -X (vértices BL, TL, TR, BR vistos de fora).
        static readonly Vector3[][] FaceVerts =
        {
            new[] { new Vector3(0, 1, 0), new Vector3(0, 1, 1), new Vector3(1, 1, 1), new Vector3(1, 1, 0) },
            new[] { new Vector3(0, 0, 1), new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 1) },
            new[] { new Vector3(1, 0, 1), new Vector3(1, 1, 1), new Vector3(0, 1, 1), new Vector3(0, 0, 1) },
            new[] { new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 1, 0), new Vector3(1, 0, 0) },
            new[] { new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(1, 1, 1), new Vector3(1, 0, 1) },
            new[] { new Vector3(0, 0, 1), new Vector3(0, 1, 1), new Vector3(0, 1, 0), new Vector3(0, 0, 0) },
        };
        static readonly Vector3[] Normals = { Vector3.up, Vector3.down, Vector3.forward, Vector3.back, Vector3.right, Vector3.left };
        static readonly BoxFace[] FaceToSkin = { BoxFace.Top, BoxFace.Bottom, BoxFace.Front, BoxFace.Back, BoxFace.Right, BoxFace.Left };

        public class Builder
        {
            public readonly List<Vector3> v = new List<Vector3>();
            public readonly List<Vector3> n = new List<Vector3>();
            public readonly List<Vector2> uv = new List<Vector2>();
            public readonly List<Color32> c = new List<Color32>();
            public readonly List<int> t = new List<int>();

            public void Quad(Vector3[] corners, Vector3 normal, Vector2[] uvs, Color32 col)
            {
                int s = v.Count;
                for (int i = 0; i < 4; i++)
                {
                    v.Add(corners[i]);
                    n.Add(normal);
                    uv.Add(uvs[i]);
                    c.Add(col);
                }
                t.Add(s); t.Add(s + 1); t.Add(s + 2);
                t.Add(s); t.Add(s + 2); t.Add(s + 3);
            }

            /// <summary>Caixa com cor única e UV de um retângulo do atlas (ou branco).</summary>
            public void Box(Vector3 min, Vector3 size, Color32 col, Rect uvRect, bool[] skipFaces = null)
            {
                for (int f = 0; f < 6; f++)
                {
                    if (skipFaces != null && skipFaces[f]) continue;
                    var corners = new Vector3[4];
                    for (int i = 0; i < 4; i++) corners[i] = min + Vector3.Scale(FaceVerts[f][i], size);
                    var uvs = new[]
                    {
                        new Vector2(uvRect.xMin, uvRect.yMin), new Vector2(uvRect.xMin, uvRect.yMax),
                        new Vector2(uvRect.xMax, uvRect.yMax), new Vector2(uvRect.xMax, uvRect.yMin),
                    };
                    Quad(corners, Normals[f], uvs, col);
                }
            }

            /// <summary>Caixa com um tile do atlas do mundo por face, repetido por unidade (UV do mundo).</summary>
            public void TiledBox(Vector3 min, Vector3 size, Tile top, Tile side, Color32 col)
            {
                for (int f = 0; f < 6; f++)
                {
                    var r = VoxelMesher.TileRect((int)(f <= 1 ? top : side));
                    var corners = new Vector3[4];
                    for (int i = 0; i < 4; i++) corners[i] = min + Vector3.Scale(FaceVerts[f][i], size);
                    float fw = f <= 1 ? size.x : (f >= 4 ? size.z : size.x);
                    float fh = f <= 1 ? size.z : size.y;
                    // Recorte do tile proporcional ao tamanho (sem esticar a textura).
                    float uw = Mathf.Min(1f, fw), vh = Mathf.Min(1f, fh);
                    var uvs = new[]
                    {
                        new Vector2(r.xMin, r.yMin), new Vector2(r.xMin, r.yMin + r.height * vh),
                        new Vector2(r.xMin + r.width * uw, r.yMin + r.height * vh), new Vector2(r.xMin + r.width * uw, r.yMin),
                    };
                    Quad(corners, Normals[f], uvs, col);
                }
            }

            public Mesh ToMesh(string name)
            {
                var m = new Mesh { name = name };
                if (v.Count > 65000) m.indexFormat = IndexFormat.UInt32;
                m.SetVertices(v);
                m.SetNormals(n);
                m.SetUVs(0, uv);
                m.SetColors(c);
                m.SetTriangles(t, 0);
                m.RecalculateBounds();
                return m;
            }
        }

        public static Mesh Save(Mesh m, string relPath)
        {
            string path = $"{Dir}/{relPath}.asset";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                existing.Clear();
                EditorUtility.CopySerialized(m, existing);
                Object.DestroyImmediate(m);
                EditorUtility.SetDirty(existing);
                return existing;
            }
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        // ------------------------------------------------------------------ Peças de personagem

        /// <summary>Caixa de skin: tamanho em texels, "min" em texels relativo ao pivô.</summary>
        public static Mesh SkinBoxMesh(string name, SkinBox box, Vector3 minTexels, int texW, int texH, float inflate = 0f)
        {
            var b = new Builder();
            Vector3 size = (Vector3)box.size * Texel + Vector3.one * inflate * 2f;
            Vector3 min = minTexels * Texel - Vector3.one * inflate;
            for (int f = 0; f < 6; f++)
            {
                var reg = box.Region(FaceToSkin[f]);
                float u0 = reg.x / (float)texW, u1 = (reg.x + reg.width) / (float)texW;
                float vTop = 1f - reg.y / (float)texH, vBot = 1f - (reg.y + reg.height) / (float)texH;
                var corners = new Vector3[4];
                for (int i = 0; i < 4; i++) corners[i] = min + Vector3.Scale(FaceVerts[f][i], size);
                var uvs = new[] { new Vector2(u0, vBot), new Vector2(u0, vTop), new Vector2(u1, vTop), new Vector2(u1, vBot) };
                b.Quad(corners, Normals[f], uvs, new Color32(255, 255, 255, 255));
            }
            return b.ToMesh(name);
        }

        // ------------------------------------------------------------------ Itens extrudados

        /// <summary>Extrusão de um sprite (1 pixel = cubo): faces internas descartadas, cor por vértice.</summary>
        public static Mesh ExtrudeSprite(string name, Texture2D tex, Vector2 gripPixel, float pixel = ItemPixel, float thicknessPixels = 1f)
        {
            var px = tex.GetPixels32();
            int w = tex.width, h = tex.height;
            bool Solid(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && px[y * w + x].a > 128;
            var b = new Builder();
            float th = thicknessPixels * pixel;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (!Solid(x, y)) continue;
                    var col = px[y * w + x];
                    col.a = 255;
                    Vector3 min = new Vector3((x - gripPixel.x) * pixel, (y - gripPixel.y) * pixel, -th * 0.5f);
                    var size = new Vector3(pixel, pixel, th);
                    var skip = new bool[6];
                    skip[0] = Solid(x, y + 1);
                    skip[1] = Solid(x, y - 1);
                    skip[4] = Solid(x + 1, y);
                    skip[5] = Solid(x - 1, y);
                    // Faces laterais levemente mais escuras dão volume ao pixel.
                    var side = new Color32((byte)(col.r * 0.78f), (byte)(col.g * 0.78f), (byte)(col.b * 0.78f), 255);
                    for (int f = 0; f < 6; f++)
                    {
                        if (skip[f]) continue;
                        var corners = new Vector3[4];
                        for (int i = 0; i < 4; i++) corners[i] = min + Vector3.Scale(FaceVerts[f][i], size);
                        var uvs = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
                        b.Quad(corners, Normals[f], uvs, f == 2 || f == 3 ? col : side);
                    }
                }
            return b.ToMesh(name);
        }

        // ------------------------------------------------------------------ Primitivas de efeito

        public static Mesh Cube(string name)
        {
            var b = new Builder();
            b.Box(new Vector3(-0.5f, -0.5f, -0.5f), Vector3.one, new Color32(255, 255, 255, 255), new Rect(0, 0, 1, 1));
            return b.ToMesh(name);
        }

        public static Mesh QuadMesh(string name)
        {
            var b = new Builder();
            b.Quad(new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(-0.5f, 0.5f, 0), new Vector3(0.5f, 0.5f, 0), new Vector3(0.5f, -0.5f, 0) },
                Vector3.back, new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right }, new Color32(255, 255, 255, 255));
            return b.ToMesh(name);
        }

        /// <summary>Feixe: dois quads cruzados de 1×1 com pivô na base (escala Y = altura).</summary>
        public static Mesh BeamCross(string name)
        {
            var b = new Builder();
            var white = new Color32(255, 255, 255, 255);
            var uvs = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
            b.Quad(new[] { new Vector3(-0.5f, 0, 0), new Vector3(-0.5f, 1, 0), new Vector3(0.5f, 1, 0), new Vector3(0.5f, 0, 0) }, Vector3.back, uvs, white);
            b.Quad(new[] { new Vector3(0, 0, -0.5f), new Vector3(0, 1, -0.5f), new Vector3(0, 1, 0.5f), new Vector3(0, 0, 0.5f) }, Vector3.left, uvs, white);
            return b.ToMesh(name);
        }

        public static Mesh Sphere(string name, int lon = 28, int lat = 18)
        {
            var b = new Builder();
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            for (int j = 0; j <= lat; j++)
            {
                float v = j / (float)lat;
                float th = v * Mathf.PI;
                for (int i = 0; i <= lon; i++)
                {
                    float u = i / (float)lon;
                    float ph = u * Mathf.PI * 2f;
                    var p = new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph)) * 0.5f;
                    verts.Add(p);
                    uvs.Add(new Vector2(u, 1f - v));
                }
            }
            b.v.AddRange(verts);
            foreach (var p in verts) b.n.Add(p.normalized);
            b.uv.AddRange(uvs);
            for (int i = 0; i < verts.Count; i++) b.c.Add(new Color32(255, 255, 255, 255));
            for (int j = 0; j < lat; j++)
                for (int i = 0; i < lon; i++)
                {
                    int a = j * (lon + 1) + i, c = a + lon + 1;
                    b.t.Add(a); b.t.Add(a + 1); b.t.Add(c);
                    b.t.Add(a + 1); b.t.Add(c + 1); b.t.Add(c);
                }
            return b.ToMesh(name);
        }

        /// <summary>Quad horizontal (anel de aviso no chão), normal para cima.</summary>
        public static Mesh GroundQuad(string name)
        {
            var b = new Builder();
            b.Quad(new[] { new Vector3(-0.5f, 0, -0.5f), new Vector3(-0.5f, 0, 0.5f), new Vector3(0.5f, 0, 0.5f), new Vector3(0.5f, 0, -0.5f) },
                Vector3.up, new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right }, new Color32(255, 255, 255, 255));
            return b.ToMesh(name);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ruinas
{
    /// <summary>Desenha segmentos (linhas finas) e quads de preenchimento já projetados em coordenadas da UI.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class MapOverlayGraphic : MaskableGraphic
    {
        public struct Seg
        {
            public Vector2 a, b;
            public Color32 color;
            public float width;
        }

        public struct Quad
        {
            public Vector2 p0, p1, p2, p3;
            public Color32 color;
        }

        public readonly List<Seg> Segments = new List<Seg>(2048);
        public readonly List<Quad> Quads = new List<Quad>(1024);

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            foreach (var q in Quads)
            {
                int i = vh.currentVertCount;
                vh.AddVert(q.p0, q.color, Vector2.zero);
                vh.AddVert(q.p1, q.color, Vector2.zero);
                vh.AddVert(q.p2, q.color, Vector2.zero);
                vh.AddVert(q.p3, q.color, Vector2.zero);
                vh.AddTriangle(i, i + 1, i + 2);
                vh.AddTriangle(i + 2, i + 3, i);
            }
            foreach (var s in Segments)
            {
                Vector2 d = s.b - s.a;
                float len = d.magnitude;
                if (len < 0.01f) continue;
                Vector2 n = new Vector2(-d.y, d.x) / len * (s.width * 0.5f);
                Vector2 ext = d / len * (s.width * 0.5f);
                int i = vh.currentVertCount;
                vh.AddVert(s.a - n - ext, s.color, Vector2.zero);
                vh.AddVert(s.a + n - ext, s.color, Vector2.zero);
                vh.AddVert(s.b + n + ext, s.color, Vector2.zero);
                vh.AddVert(s.b - n + ext, s.color, Vector2.zero);
                vh.AddTriangle(i, i + 1, i + 2);
                vh.AddTriangle(i + 2, i + 3, i);
            }
        }

        public void Commit() => SetVerticesDirty();
    }
}

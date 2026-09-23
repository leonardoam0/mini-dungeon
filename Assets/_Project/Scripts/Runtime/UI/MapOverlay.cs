using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ruinas
{
    /// <summary>
    /// Mapa semitransparente sobreposto ao mundo: contornos claros/amarelados das áreas exploradas,
    /// preenchimento discreto, alturas (níveis diferentes do atual ficam mais apagados), escadas marcadas,
    /// ícones de interesse e o marcador do jogador. Liga/desliga sem pausar.
    /// </summary>
    public class MapOverlay : MonoBehaviour
    {
        struct Edge
        {
            public int x0, z0, x1, z1;
            public float h;
            public bool stairs;
        }

        public float scale = 0.38f;
        public float revealRadius = 11f;
        // Traço fino e quase sem preenchimento, como no vídeo (linha ~1,5 px no recorte 540×960).
        public float lineWidth = 2f;
        public Color lineColor = new Color(0.98f, 0.93f, 0.76f, 0.9f);
        public Color fillColor = new Color(0.95f, 0.88f, 0.7f, 0.035f);

        LevelContext ctx;
        MapData data;
        MapOverlayGraphic graphic;
        RectTransform rect;
        bool[] explored;
        readonly List<Edge> edges = new List<Edge>(4096);
        readonly List<Edge> runs = new List<Edge>(2048);
        bool dirty = true;
        float revealTimer;
        int revealGeneration;
        Image playerMarker;
        readonly List<Image> iconPool = new List<Image>();
        public bool Visible { get; private set; } = true;
        public int ExploredCount { get; private set; }
        public int SegmentCount => graphic != null ? graphic.Segments.Count : 0;

        public void Build(LevelContext context, RectTransform parent)
        {
            ctx = context;
            data = ctx.Map;
            rect = UIFactory.Rect("Mapa", parent);
            rect.Stretch();
            graphic = rect.gameObject.AddComponent<MapOverlayGraphic>();
            graphic.raycastTarget = false;
            if (data != null && data.width > 0) explored = new bool[data.width * data.depth];
            playerMarker = UIFactory.Image("Jogador", rect, UIFactory.Skin.mapPlayer, Color.white, false);
            playerMarker.rectTransform.sizeDelta = new Vector2(27f, 27f);
        }

        public void SetVisible(bool v)
        {
            Visible = v;
            rect.gameObject.SetActive(v);
        }

        /// <summary>Revela a área ao redor de um ponto (usado também pelo save/checkpoints).</summary>
        public void Reveal(Vector3 world, float radius)
        {
            if (data == null || explored == null) return;
            var c = data.WorldToCell(world);
            int r = Mathf.CeilToInt(radius);
            float r2 = radius * radius;
            float py = world.y;
            for (int z = c.y - r; z <= c.y + r; z++)
            {
                for (int x = c.x - r; x <= c.x + r; x++)
                {
                    if (!data.InBounds(x, z)) continue;
                    int dx = x - c.x, dz = z - c.y;
                    if (dx * dx + dz * dz > r2) continue;
                    int i = data.Index(x, z);
                    if (explored[i] || !data.IsWalkable(x, z)) continue;
                    if (Mathf.Abs(data.Height(x, z) - py) > 7f) continue;
                    explored[i] = true;
                    ExploredCount++;
                    dirty = true;
                }
            }
        }

        public bool IsExplored(Vector3 world)
        {
            if (data == null || explored == null) return false;
            var c = data.WorldToCell(world);
            return data.InBounds(c.x, c.y) && explored[data.Index(c.x, c.y)];
        }

        void RebuildEdges()
        {
            dirty = false;
            edges.Clear();
            int w = data.width, d = data.depth;
            bool Open(int x, int z) => data.InBounds(x, z) && explored[data.Index(x, z)];
            for (int z = 0; z < d; z++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (!Open(x, z)) continue;
                    float h = data.Height(x, z);
                    bool st = (data.Flags(x, z) & MapCellFlags.Stairs) != 0;
                    // Aresta onde o vizinho não é explorado/caminhável ou há desnível (penhasco).
                    if (!Open(x, z - 1) || Cliff(h, x, z - 1)) edges.Add(new Edge { x0 = x, z0 = z, x1 = x + 1, z1 = z, h = h, stairs = st });
                    if (!Open(x, z + 1) || Cliff(h, x, z + 1)) edges.Add(new Edge { x0 = x, z0 = z + 1, x1 = x + 1, z1 = z + 1, h = h, stairs = st });
                    if (!Open(x - 1, z) || Cliff(h, x - 1, z)) edges.Add(new Edge { x0 = x, z0 = z, x1 = x, z1 = z + 1, h = h, stairs = st });
                    if (!Open(x + 1, z) || Cliff(h, x + 1, z)) edges.Add(new Edge { x0 = x + 1, z0 = z, x1 = x + 1, z1 = z + 1, h = h, stairs = st });
                }
            }
            MergeRuns();
        }

        bool Cliff(float h, int x, int z)
        {
            if (!data.InBounds(x, z)) return true;
            bool stairs = (data.Flags(x, z) & MapCellFlags.Stairs) != 0;
            return !stairs && Mathf.Abs(data.Height(x, z) - h) > 1.1f;
        }

        /// <summary>Junta arestas colineares consecutivas na mesma altura (menos segmentos para desenhar).</summary>
        void MergeRuns()
        {
            runs.Clear();
            edges.Sort((a, b) =>
            {
                bool ha = a.z0 == a.z1, hb = b.z0 == b.z1;
                if (ha != hb) return ha ? -1 : 1;
                if (ha)
                {
                    int c = a.z0.CompareTo(b.z0);
                    if (c != 0) return c;
                    c = a.h.CompareTo(b.h);
                    return c != 0 ? c : a.x0.CompareTo(b.x0);
                }
                int c2 = a.x0.CompareTo(b.x0);
                if (c2 != 0) return c2;
                c2 = a.h.CompareTo(b.h);
                return c2 != 0 ? c2 : a.z0.CompareTo(b.z0);
            });
            for (int i = 0; i < edges.Count; i++)
            {
                var e = edges[i];
                if (runs.Count > 0)
                {
                    var last = runs[runs.Count - 1];
                    bool horiz = e.z0 == e.z1 && last.z0 == last.z1 && e.z0 == last.z0 && e.x0 == last.x1;
                    bool vert = e.x0 == e.x1 && last.x0 == last.x1 && e.x0 == last.x0 && e.z0 == last.z1;
                    if ((horiz || vert) && Mathf.Approximately(e.h, last.h) && e.stairs == last.stairs)
                    {
                        last.x1 = e.x1;
                        last.z1 = e.z1;
                        runs[runs.Count - 1] = last;
                        continue;
                    }
                }
                runs.Add(e);
            }
        }

        void LateUpdate()
        {
            if (ctx == null || data == null || explored == null || ctx.Player == null) return;
            var player = ctx.Player.transform;
            if (revealGeneration != DeterministicVfx.Generation) { revealGeneration = DeterministicVfx.Generation; revealTimer = 0f; }
            revealTimer -= DeterministicVfx.UnscaledDeltaTime;
            if (revealTimer <= 0f)
            {
                revealTimer = 0.2f;
                Reveal(player.position, revealRadius);
            }
            if (!Visible) return;
            if (dirty) RebuildEdges();
            var cam = ctx.CameraRig != null ? ctx.CameraRig.cam : Camera.main;
            if (cam == null) return;

            Vector3 pivot = player.position;
            float py = pivot.y;
            graphic.Segments.Clear();
            graphic.Quads.Clear();

            foreach (var e in runs)
            {
                float rel = e.h - py;
                float alpha = Mathf.Abs(rel) < 1.1f ? 1f : Mathf.Lerp(0.55f, 0.3f, Mathf.Clamp01((Mathf.Abs(rel) - 1f) / 6f));
                var c = lineColor;
                c.a *= alpha;
                Vector2 a = Project(cam, MapProjection.CellCorner(data, e.x0, e.z0, e.h), pivot);
                Vector2 b = Project(cam, MapProjection.CellCorner(data, e.x1, e.z1, e.h), pivot);
                graphic.Segments.Add(new MapOverlayGraphic.Seg { a = a, b = b, color = c, width = lineWidth });
            }

            // Preenchimento discreto das áreas exploradas, por faixas contínuas.
            for (int z = 0; z < data.depth; z++)
            {
                int x = 0;
                while (x < data.width)
                {
                    int i = data.Index(x, z);
                    if (!explored[i]) { x++; continue; }
                    float h = data.Height(x, z);
                    int start = x;
                    while (x < data.width && explored[data.Index(x, z)] && Mathf.Approximately(data.Height(x, z), h)) x++;
                    float rel = Mathf.Abs(h - py);
                    var fc = fillColor;
                    if (rel > 1.1f) fc.a *= 0.5f;
                    graphic.Quads.Add(new MapOverlayGraphic.Quad
                    {
                        p0 = Project(cam, MapProjection.CellCorner(data, start, z, h), pivot),
                        p1 = Project(cam, MapProjection.CellCorner(data, start, z + 1, h), pivot),
                        p2 = Project(cam, MapProjection.CellCorner(data, x, z + 1, h), pivot),
                        p3 = Project(cam, MapProjection.CellCorner(data, x, z, h), pivot),
                        color = fc,
                    });
                }
            }
            graphic.Commit();

            // Marcador do jogador (sempre no centro do mapa, que é o próprio jogador).
            Vector2 pp = Project(cam, pivot, pivot);
            playerMarker.rectTransform.anchoredPosition = pp;
            Vector3 fwd = player.forward;
            Vector2 f2 = Project(cam, pivot + fwd * 2f, pivot) - pp;
            float ang = Mathf.Atan2(f2.y, f2.x) * Mathf.Rad2Deg - 90f;
            playerMarker.rectTransform.localRotation = Quaternion.Euler(0f, 0f, ang);

            UpdateIcons(cam, pivot);
        }

        Vector2 Project(Camera cam, Vector3 world, Vector3 pivot)
        {
            Vector3 m = MapProjection.WorldToMap(world, pivot, scale);
            Vector3 sp = cam.WorldToScreenPoint(m);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, sp, null, out var local);
            return local;
        }

        void UpdateIcons(Camera cam, Vector3 pivot)
        {
            int used = 0;
            var skin = UIFactory.Skin;
            foreach (var poi in MapPoi.All)
            {
                if (poi == null || !poi.IsRelevant) continue;
                if (!poi.alwaysVisible && !IsExplored(poi.transform.position)) continue;
                Sprite s = null;
                switch (poi.kind)
                {
                    case MapPoiKind.Chest: s = skin.mapChest; break;
                    case MapPoiKind.Gate: s = skin.mapGate; break;
                    case MapPoiKind.Exit: s = skin.mapExit; break;
                    case MapPoiKind.Checkpoint: s = skin.mapCheckpoint; break;
                    case MapPoiKind.Merchant: s = skin.mapMerchant; break;
                    case MapPoiKind.Objective: s = skin.mapObjective; break;
                }
                if (s == null) continue;
                Image img;
                if (used < iconPool.Count) img = iconPool[used];
                else
                {
                    img = UIFactory.Image("Icone", rect, s, Color.white, false);
                    img.rectTransform.sizeDelta = new Vector2(30f, 30f);
                    iconPool.Add(img);
                }
                used++;
                img.sprite = s;
                img.enabled = true;
                img.rectTransform.anchoredPosition = Project(cam, poi.transform.position + Vector3.up * 0.5f, pivot);
            }
            for (int i = used; i < iconPool.Count; i++) iconPool[i].enabled = false;
            playerMarker.transform.SetAsLastSibling();
        }
    }
}

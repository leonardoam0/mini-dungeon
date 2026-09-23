using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ruinas
{
    /// <summary>
    /// Texto em fonte pixelada renderizado por quads (sem TextMeshPro). Suporta alinhamento, quebra de
    /// linha, sombra/contorno e troca de cor inline com "{#RRGGBB}texto{/}".
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class PixelText : MaskableGraphic, ILayoutElement
    {
        [SerializeField] PixelFont font;
        [SerializeField, TextArea] string text = "";
        [SerializeField] float pixelSize = 3f;
        [SerializeField] TextAnchor alignment = TextAnchor.UpperLeft;
        [SerializeField] bool wrap;
        [SerializeField] bool shadow = true;
        [SerializeField] Color shadowColor = new Color(0f, 0f, 0f, 0.8f);
        [SerializeField] Vector2 shadowOffset = new Vector2(1f, -1f);
        [SerializeField] bool outline;
        [SerializeField] Color outlineColor = new Color(0.06f, 0.05f, 0.08f, 1f);

        struct Run { public int start, end; public int width; }
        readonly List<Run> lines = new List<Run>();
        static readonly List<UIVertex> quadVerts = new List<UIVertex>(4) { new UIVertex(), new UIVertex(), new UIVertex(), new UIVertex() };

        public PixelFont Font { get => font; set { font = value; SetAllDirty(); } }
        public float PixelSize { get => pixelSize; set { if (!Mathf.Approximately(pixelSize, value)) { pixelSize = value; SetVerticesDirty(); SetLayoutDirty(); } } }
        public TextAnchor Alignment { get => alignment; set { alignment = value; SetVerticesDirty(); } }
        public bool Wrap { get => wrap; set { wrap = value; SetVerticesDirty(); SetLayoutDirty(); } }
        public bool Shadow { get => shadow; set { shadow = value; SetVerticesDirty(); } }
        public Color ShadowColor { get => shadowColor; set { shadowColor = value; SetVerticesDirty(); } }
        public bool Outline { get => outline; set { outline = value; SetVerticesDirty(); } }
        public Color OutlineColor { get => outlineColor; set { outlineColor = value; SetVerticesDirty(); } }

        public string Text
        {
            get => text;
            set
            {
                if (value == null) value = "";
                if (text == value) return;
                text = value;
                SetVerticesDirty();
                SetLayoutDirty();
            }
        }

        public override Texture mainTexture => font != null && font.atlas != null ? font.atlas : s_WhiteTexture;

        // ------------------------------------------------------------ layout

        string StripTags(string s)
        {
            if (s.IndexOf('{') < 0) return s;
            var sb = new System.Text.StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '{')
                {
                    int close = s.IndexOf('}', i);
                    if (close > i && (i + 1 < s.Length) && (s[i + 1] == '#' || s[i + 1] == '/'))
                    {
                        i = close;
                        continue;
                    }
                }
                sb.Append(s[i]);
            }
            return sb.ToString();
        }

        void Layout(string plain, float maxWidthPx)
        {
            lines.Clear();
            if (font == null) return;
            int lineStart = 0;
            int i = 0;
            int lastSpace = -1;
            int width = 0;
            while (i <= plain.Length)
            {
                bool end = i == plain.Length;
                char c = end ? '\n' : plain[i];
                if (c == '\n')
                {
                    lines.Add(new Run { start = lineStart, end = i, width = font.MeasureLine(plain, lineStart, i) });
                    lineStart = i + 1;
                    lastSpace = -1;
                    width = 0;
                    i++;
                    continue;
                }
                if (c == ' ') lastSpace = i;
                width += font.Advance(c);
                if (wrap && maxWidthPx > 0 && width - font.letterSpacing > maxWidthPx && i > lineStart)
                {
                    int breakAt = lastSpace > lineStart ? lastSpace : i;
                    lines.Add(new Run { start = lineStart, end = breakAt, width = font.MeasureLine(plain, lineStart, breakAt) });
                    lineStart = breakAt == lastSpace ? breakAt + 1 : breakAt;
                    i = lineStart;
                    lastSpace = -1;
                    width = 0;
                    continue;
                }
                i++;
            }
        }

        public Vector2 MeasurePixels(float maxWidthUnits = 0f)
        {
            if (font == null || string.IsNullOrEmpty(text)) return Vector2.zero;
            string plain = StripTags(text);
            Layout(plain, maxWidthUnits > 0 ? maxWidthUnits / pixelSize : 0f);
            int w = 0;
            foreach (var l in lines) w = Mathf.Max(w, l.width);
            int h = lines.Count * font.LineHeight - font.lineGap;
            return new Vector2(w * pixelSize, h * pixelSize);
        }

        // ------------------------------------------------------------ geometria

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (font == null || font.atlas == null || string.IsNullOrEmpty(text)) return;

            // Mapeia cor por caractere da string sem tags.
            var colors = new List<Color32>(text.Length);
            var plainSb = new System.Text.StringBuilder(text.Length);
            Color32 baseCol = color;
            Color32 cur = baseCol;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '{' && i + 1 < text.Length)
                {
                    int close = text.IndexOf('}', i);
                    if (close > i)
                    {
                        if (text[i + 1] == '/') { cur = baseCol; i = close; continue; }
                        if (text[i + 1] == '#' && ColorUtility.TryParseHtmlString(text.Substring(i + 1, close - i - 1), out var parsed))
                        {
                            parsed.a *= color.a;
                            cur = parsed;
                            i = close;
                            continue;
                        }
                    }
                }
                plainSb.Append(c);
                colors.Add(cur);
            }
            string plain = plainSb.ToString();

            Rect r = GetPixelAdjustedRect();
            Layout(plain, wrap ? r.width / pixelSize : 0f);
            if (lines.Count == 0) return;

            int lineH = font.LineHeight;
            float blockH = (lines.Count * lineH - font.lineGap) * pixelSize;

            float top;
            switch (alignment)
            {
                case TextAnchor.LowerLeft: case TextAnchor.LowerCenter: case TextAnchor.LowerRight:
                    top = r.yMin + blockH; break;
                case TextAnchor.MiddleLeft: case TextAnchor.MiddleCenter: case TextAnchor.MiddleRight:
                    top = r.center.y + blockH * 0.5f; break;
                default:
                    top = r.yMax; break;
            }
            top = Mathf.Round(top);

            float aw = font.atlas.width, ah = font.atlas.height;

            // Passes: contorno, sombra, texto.
            for (int pass = 0; pass < 3; pass++)
            {
                if (pass == 0 && !outline) continue;
                if (pass == 1 && !shadow) continue;
                for (int li = 0; li < lines.Count; li++)
                {
                    var line = lines[li];
                    float lineW = line.width * pixelSize;
                    float left;
                    switch (alignment)
                    {
                        case TextAnchor.UpperCenter: case TextAnchor.MiddleCenter: case TextAnchor.LowerCenter:
                            left = r.center.x - lineW * 0.5f; break;
                        case TextAnchor.UpperRight: case TextAnchor.MiddleRight: case TextAnchor.LowerRight:
                            left = r.xMax - lineW; break;
                        default:
                            left = r.xMin; break;
                    }
                    left = Mathf.Round(left);
                    float cellTop = top - li * lineH * pixelSize;
                    float pen = left;
                    for (int ci = line.start; ci < line.end; ci++)
                    {
                        char c = plain[ci];
                        if (c == ' ') { pen += font.Advance(' ') * pixelSize; continue; }
                        if (!font.TryGet(c, out var g) && !font.TryGet('?', out g)) { pen += font.Advance(c) * pixelSize; continue; }

                        float x0 = pen;
                        float y0 = cellTop - g.h * pixelSize;
                        float x1 = x0 + g.w * pixelSize;
                        float y1 = cellTop;
                        var uv0 = new Vector2(g.x / aw, g.y / ah);
                        var uv1 = new Vector2((g.x + g.w) / aw, (g.y + g.h) / ah);

                        if (pass == 0)
                        {
                            Color32 oc = outlineColor; oc.a = (byte)(oc.a * (colors[ci].a / 255f));
                            AddQuad(vh, x0 - pixelSize, y0, x1 - pixelSize, y1, uv0, uv1, oc);
                            AddQuad(vh, x0 + pixelSize, y0, x1 + pixelSize, y1, uv0, uv1, oc);
                            AddQuad(vh, x0, y0 - pixelSize, x1, y1 - pixelSize, uv0, uv1, oc);
                            AddQuad(vh, x0, y0 + pixelSize, x1, y1 + pixelSize, uv0, uv1, oc);
                        }
                        else if (pass == 1)
                        {
                            Color32 sc = shadowColor; sc.a = (byte)(sc.a * (colors[ci].a / 255f));
                            float ox = shadowOffset.x * pixelSize, oy = shadowOffset.y * pixelSize;
                            AddQuad(vh, x0 + ox, y0 + oy, x1 + ox, y1 + oy, uv0, uv1, sc);
                        }
                        else
                        {
                            AddQuad(vh, x0, y0, x1, y1, uv0, uv1, colors[ci]);
                        }
                        pen += font.Advance(c) * pixelSize;
                    }
                }
            }
        }

        static void AddQuad(VertexHelper vh, float x0, float y0, float x1, float y1, Vector2 uv0, Vector2 uv1, Color32 c)
        {
            int i = vh.currentVertCount;
            vh.AddVert(new Vector3(x0, y0), c, new Vector4(uv0.x, uv0.y));
            vh.AddVert(new Vector3(x0, y1), c, new Vector4(uv0.x, uv1.y));
            vh.AddVert(new Vector3(x1, y1), c, new Vector4(uv1.x, uv1.y));
            vh.AddVert(new Vector3(x1, y0), c, new Vector4(uv1.x, uv0.y));
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i + 2, i + 3, i);
        }

        // ------------------------------------------------------------ ILayoutElement

        public void CalculateLayoutInputHorizontal() { }
        public void CalculateLayoutInputVertical() { }
        public float minWidth => 0;
        public float preferredWidth => wrap ? rectTransform.rect.width : MeasurePixels().x;
        public float flexibleWidth => -1;
        public float minHeight => 0;
        public float preferredHeight => MeasurePixels(wrap ? rectTransform.rect.width : 0f).y;
        public float flexibleHeight => -1;
        public float maxWidth => -1;
        public float maxHeight => -1;
        public int layoutPriority => 0;

        /// <summary>Ajusta o retângulo ao tamanho do texto (útil em rótulos soltos).</summary>
        public void FitToText(float padX = 0f, float padY = 0f)
        {
            var s = MeasurePixels(wrap ? rectTransform.rect.width : 0f);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, wrap ? rectTransform.rect.width : s.x + padX);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, s.y + padY);
        }
    }
}

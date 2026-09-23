using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Ruinas
{
    /// <summary>Construtores de UI em código (HUD, menus e sobreposições são montados em runtime).</summary>
    public static class UIFactory
    {
        public static readonly Color TextNormal = new Color(0.88f, 0.86f, 0.80f);
        public static readonly Color TextDim = new Color(0.62f, 0.62f, 0.60f);
        public static readonly Color TextGold = new Color(1f, 0.84f, 0.33f);
        public static readonly Color PanelTint = new Color(1f, 1f, 1f, 0.96f);
        public const int UILayer = 5;

        public static PixelFont Font => Services.Database != null ? Services.Database.font : null;
        public static UISkin Skin => Services.Database != null ? Services.Database.uiSkin : null;

        public static Canvas CreateCanvas(string name, int sortOrder, Transform parent = null, bool raycaster = true)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = UILayer;
            if (parent != null) go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;
            canvas.pixelPerfect = false;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            if (raycaster) go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static void ApplyUiScale(Canvas canvas, float scale)
        {
            var scaler = canvas != null ? canvas.GetComponent<CanvasScaler>() : null;
            if (scaler != null) scaler.referenceResolution = new Vector2(1920, 1080) / Mathf.Max(0.5f, scale);
        }

        public static EventSystem EnsureEventSystem()
        {
            var es = EventSystem.current;
            if (es == null) es = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            if (es != null) return es;
            var go = new GameObject("EventSystem");
            es = go.AddComponent<EventSystem>();
            var module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
            module.moveRepeatDelay = 0.35f;
            module.moveRepeatRate = 0.12f;
            // Execuções automatizadas: a interface não recebe navegação de dispositivos reais.
            if (Services.Input != null && Services.Input.AutomationLock) module.enabled = false;
            return es;
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = UILayer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Place(this RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(this RectTransform rt, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        public static Image Image(string name, Transform parent, Sprite sprite, Color color, bool sliced = true, bool raycast = false)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            if (sprite != null && sliced && sprite.border.sqrMagnitude > 0f)
            {
                img.type = UnityEngine.UI.Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 1f / 3f; // bordas de 1px da arte = 3 unidades de UI
            }
            return img;
        }

        public static PixelText Text(string name, Transform parent, string text, float pixelSize, Color color, TextAnchor align = TextAnchor.UpperLeft)
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<PixelText>();
            t.Font = Font;
            t.PixelSize = pixelSize;
            t.color = color;
            t.Alignment = align;
            t.Text = text;
            t.raycastTarget = false;
            return t;
        }

        public static Button Button(string name, Transform parent, string label, UnityAction onClick, Vector2 size, float textSize = 3f)
        {
            var skin = Skin;
            var bg = Image(name, parent, skin != null ? skin.button : null, Color.white, true, true);
            bg.rectTransform.sizeDelta = size;
            var btn = bg.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            var cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = Color.white;
            cb.selectedColor = Color.white;
            cb.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            cb.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
            btn.colors = cb;
            btn.targetGraphic = bg;
            if (onClick != null) btn.onClick.AddListener(onClick);

            var hl = Image("Selecao", bg.transform, skin != null ? skin.buttonSelected : null, Color.white, true, false);
            hl.rectTransform.Stretch(-3, -3, -3, -3);
            var txt = Text("Rotulo", bg.transform, label, textSize, TextNormal, TextAnchor.MiddleCenter);
            txt.rectTransform.Stretch(12, 0, 12, 0);

            var sh = bg.gameObject.AddComponent<SelectHighlight>();
            sh.highlight = hl;
            sh.label = txt;
            return btn;
        }

        public static Slider Slider(string name, Transform parent, float value, Action<float> onChange, Vector2 size)
        {
            var skin = Skin;
            var root = Image(name, parent, skin != null ? skin.bar : null, Color.white, true, true);
            root.rectTransform.sizeDelta = size;
            var slider = root.gameObject.AddComponent<Slider>();
            var fillArea = Rect("Area", root.transform);
            fillArea.Stretch(6, 6, 6, 6);
            var fill = Image("Preenchimento", fillArea, skin != null ? skin.barFill : null, new Color(0.62f, 0.38f, 0.86f), true, false);
            fill.rectTransform.Stretch();
            slider.fillRect = fill.rectTransform;
            slider.targetGraphic = root;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.value = value;
            slider.transition = Selectable.Transition.None;
            if (onChange != null) slider.onValueChanged.AddListener(v => onChange(v));
            var hl = Image("Selecao", root.transform, skin != null ? skin.buttonSelected : null, Color.white, true, false);
            hl.rectTransform.Stretch(-3, -3, -3, -3);
            var sh = root.gameObject.AddComponent<SelectHighlight>();
            sh.highlight = hl;
            return slider;
        }

        public static void SetupVerticalNavigation(Selectable[] items, bool wrap = true)
        {
            for (int i = 0; i < items.Length; i++)
            {
                var nav = new Navigation { mode = Navigation.Mode.Explicit };
                int up = i - 1, down = i + 1;
                if (wrap)
                {
                    if (up < 0) up = items.Length - 1;
                    if (down >= items.Length) down = 0;
                }
                nav.selectOnUp = up >= 0 && up < items.Length ? items[up] : null;
                nav.selectOnDown = down >= 0 && down < items.Length ? items[down] : null;
                items[i].navigation = nav;
            }
        }

        public static void Select(Selectable s)
        {
            var es = EnsureEventSystem();
            if (s == null || es == null) return;
            es.SetSelectedGameObject(null);
            es.SetSelectedGameObject(s.gameObject);
        }
    }
}

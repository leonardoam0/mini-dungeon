using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Ruinas
{
    /// <summary>
    /// Composição da interface de uma cena: HUD, mapa, números de dano, objetivo, avisos, prompt de
    /// interação, barra de elite, telas empilháveis e ferramentas do modo de referência. Menus capturam a
    /// entrada; ao fechar, o clique que fechou o menu não vira ação no mundo.
    /// </summary>
    public class UIRoot : MonoBehaviour
    {
        class Toast
        {
            public RectTransform root;
            public PixelText title, body;
            public Image bg;
            public float age, life;
        }

        LevelContext ctx;
        Canvas hudCanvas, menuCanvas, toolsCanvas;
        RectTransform hudRoot, menuRoot, toolsRoot;
        public HUDPresenter Hud { get; private set; }
        public MapOverlay Map { get; private set; }
        public DamageNumbers Numbers { get; private set; }
        public PickupPrompt PickupHint { get; private set; }
        public PauseScreen Pause { get; private set; }
        public SettingsScreen Settings { get; private set; }
        public ControlsScreen Controls { get; private set; }
        public InventoryScreen Inventory { get; private set; }
        public MerchantScreen Merchant { get; private set; }
        public DefeatScreen Defeat { get; private set; }
        public CompletionScreen Completion { get; private set; }
        readonly List<UIScreen> stack = new List<UIScreen>();
        public bool ScreenOpen => stack.Count > 0;
        public System.Action StackEmptied;

        PixelText objectiveText;
        Image objectiveBg;
        readonly List<Toast> toasts = new List<Toast>();
        readonly HashSet<string> shownHints = new HashSet<string>();
        RectTransform toastArea;
        Image promptBg;
        PixelText promptText;
        RectTransform bossRoot;
        PixelText bossName;
        Image bossFill;
        PixelText refTime;
        RectTransform refHelp;
        RawImage onion;
        readonly Image[] mask = new Image[2];
        string refFramesDir;
        Texture2D onionTex;

        public bool HudVisible { get; private set; } = true;
        public bool MapVisible => Map != null && Map.Visible;
        public bool CaptureMaskVisible { get; private set; }
        bool menuMode;

        public static UIRoot Create(LevelContext context)
        {
            var go = new GameObject("[UI]");
            var root = go.AddComponent<UIRoot>();
            root.ctx = context;
            root.BuildGameplay();
            return root;
        }

        public static UIRoot CreateForMenu()
        {
            var go = new GameObject("[UI Menu]");
            var root = go.AddComponent<UIRoot>();
            root.menuMode = true;
            root.BuildMenus();
            return root;
        }

        void BuildGameplay()
        {
            UIFactory.EnsureEventSystem();
            hudCanvas = UIFactory.CreateCanvas("HUD", 10, transform, false);
            hudRoot = (RectTransform)hudCanvas.transform;

            Map = gameObject.AddComponent<MapOverlay>();
            Map.Build(ctx, hudRoot);
            Numbers = gameObject.AddComponent<DamageNumbers>();
            Numbers.Build(ctx, hudRoot);
            PickupHint = gameObject.AddComponent<PickupPrompt>();
            PickupHint.Build(ctx, hudRoot);
            Hud = gameObject.AddComponent<HUDPresenter>();
            Hud.Build(ctx, hudRoot);
            Hud.Bind(ctx.Player);

            BuildObjective();
            BuildToasts();
            BuildPrompt();
            BuildBossBar();
            BuildMenus();
            if (ctx.Reference != null) BuildReferenceTools();

            ctx.Events.ObjectiveChanged += OnObjective;
            ctx.Events.Notification += (t, b) => ShowToast(t, b, 3.5f, UIFactory.TextGold);
            ctx.Events.Hint += OnHint;
            ctx.Events.ItemCollected += it =>
            {
                if (it?.def != null) ShowToast("Item obtido", $"{{#{ColorUtility.ToHtmlStringRGB(ItemDefinition.RarityColor(it.def.rarity))}}}{it.def.displayName}{{/}}  ·  I abre o inventário", 3.5f, UIFactory.TextGold);
            };
            ctx.Events.LeveledUp += lvl => ShowToast($"Nível {lvl}!", "+3% de dano e +4% de vida", 3f, new Color(0.75f, 0.55f, 1f));
            if (ctx.Mission != null && !string.IsNullOrEmpty(ctx.Mission.CurrentObjectiveText)) OnObjective(ctx.Mission.CurrentObjectiveText);
            ApplyUiScale();
            if (Services.Settings != null) Services.Settings.Changed += ApplyUiScale;
        }

        void OnDestroy()
        {
            if (Services.Settings != null) Services.Settings.Changed -= ApplyUiScale;
            if (onionTex != null) Destroy(onionTex);
        }

        void BuildMenus()
        {
            UIFactory.EnsureEventSystem();
            menuCanvas = UIFactory.CreateCanvas("Menus", 100, transform);
            menuRoot = (RectTransform)menuCanvas.transform;
            Settings = new SettingsScreen(); Settings.Create(this, menuRoot);
            Controls = new ControlsScreen(); Controls.Create(this, menuRoot);
            if (menuMode) return;
            Pause = new PauseScreen(); Pause.Create(this, menuRoot);
            Inventory = new InventoryScreen(); Inventory.Create(this, menuRoot);
            Merchant = new MerchantScreen(); Merchant.Create(this, menuRoot);
            Defeat = new DefeatScreen(); Defeat.Create(this, menuRoot);
            Completion = new CompletionScreen(); Completion.Create(this, menuRoot);
        }

        public void ApplyUiScale()
        {
            float s = Services.Settings != null ? Services.Settings.Data.uiScale : 1f;
            if (hudCanvas != null) UIFactory.ApplyUiScale(hudCanvas, s);
            if (menuCanvas != null) UIFactory.ApplyUiScale(menuCanvas, s);
        }

        // ------------------------------------------------------------------ Pilha de telas

        public void Push(UIScreen s)
        {
            if (s == null || stack.Contains(s)) return;
            if (stack.Count == 0 && !menuMode) Services.State.TrySet(s.OpenState);
            if (stack.Count > 0) stack[stack.Count - 1].Panel.gameObject.SetActive(false);
            stack.Add(s);
            s.Panel.gameObject.SetActive(true);
            s.OnOpen();
            s.Focus();
            Services.Audio?.PlayUi("ui_open");
        }

        public void Back()
        {
            if (stack.Count == 0) return;
            var top = stack[stack.Count - 1];
            if (!top.CanCancel) return;
            PopTop(true);
        }

        void PopTop(bool resume)
        {
            var top = stack[stack.Count - 1];
            top.OnClose();
            top.Panel.gameObject.SetActive(false);
            stack.RemoveAt(stack.Count - 1);
            Services.Audio?.PlayUi("ui_back");
            if (stack.Count > 0)
            {
                var s = stack[stack.Count - 1];
                s.Panel.gameObject.SetActive(true);
                s.Focus();
            }
            else
            {
                if (resume && !menuMode) Services.State.TrySet(GameState.Gameplay);
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
                if (Services.Input != null) Services.Input.SuppressPointerUntilRelease = true;
                StackEmptied?.Invoke();
            }
        }

        public void CloseAll(bool resume = true)
        {
            while (stack.Count > 0)
            {
                var top = stack[stack.Count - 1];
                top.OnClose();
                top.Panel.gameObject.SetActive(false);
                stack.RemoveAt(stack.Count - 1);
            }
            if (resume && !menuMode) Services.State.TrySet(GameState.Gameplay);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            if (Services.Input != null) Services.Input.SuppressPointerUntilRelease = true;
        }

        public void ShowDefeat() => Push(Defeat);

        public void ShowCompletion(MissionSummary s)
        {
            Completion.SetSummary(s);
            Push(Completion);
        }

        public void OpenMerchant(MerchantStation station)
        {
            if (ScreenOpen) return;
            Merchant.SetStation(station);
            Push(Merchant);
        }

        // ------------------------------------------------------------------ Widgets

        void BuildObjective()
        {
            objectiveBg = UIFactory.Image("Objetivo", hudRoot, UIFactory.Skin.tooltip, new Color(1f, 1f, 1f, 0.85f));
            objectiveBg.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -36f), new Vector2(520f, 86f));
            var label = UIFactory.Text("Rotulo", objectiveBg.rectTransform, "OBJETIVO", 2f, UIFactory.TextGold, TextAnchor.UpperLeft);
            label.rectTransform.Stretch(18, 12, 18, 0);
            objectiveText = UIFactory.Text("Texto", objectiveBg.rectTransform, "", 2.6f, UIFactory.TextNormal, TextAnchor.UpperLeft);
            objectiveText.rectTransform.Stretch(18, 40, 18, 8);
            objectiveText.Wrap = true;
            objectiveBg.gameObject.SetActive(false);
        }

        void OnObjective(string text)
        {
            if (objectiveBg == null) return;
            objectiveBg.gameObject.SetActive(!string.IsNullOrEmpty(text) && HudVisible);
            objectiveText.Text = text ?? "";
            var size = objectiveText.MeasurePixels(484f);
            objectiveBg.rectTransform.sizeDelta = new Vector2(520f, Mathf.Max(86f, 56f + size.y));
        }

        void BuildToasts()
        {
            toastArea = UIFactory.Rect("Avisos", hudRoot);
            toastArea.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(760f, 400f));
        }

        void OnHint(string id, string text)
        {
            if (string.IsNullOrEmpty(text) || !shownHints.Add(id ?? text)) return;
            ShowToast("Dica", text, 7f, new Color(0.55f, 0.95f, 1f));
        }

        public void ShowToast(string title, string body, float life, Color titleColor)
        {
            if (toastArea == null) return;
            if (toasts.Count >= 3)
            {
                var old = toasts[0];
                toasts.RemoveAt(0);
                Destroy(old.root.gameObject);
            }
            var t = new Toast { age = 0f, life = life };
            t.bg = UIFactory.Image("Aviso", toastArea, UIFactory.Skin.tooltip, new Color(1f, 1f, 1f, 0.92f));
            t.root = t.bg.rectTransform;
            t.title = UIFactory.Text("T", t.root, title, 2.8f, titleColor, TextAnchor.UpperCenter);
            t.title.rectTransform.Stretch(16, 12, 16, 0);
            t.body = UIFactory.Text("B", t.root, body ?? "", 2.3f, UIFactory.TextNormal, TextAnchor.UpperCenter);
            t.body.rectTransform.Stretch(20, 46, 20, 10);
            t.body.Wrap = true;
            float h = 60f + (string.IsNullOrEmpty(body) ? 0f : t.body.MeasurePixels(720f).y + 8f);
            t.root.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(760f, h));
            toasts.Add(t);
            LayoutToasts();
            Services.Audio?.PlayUi("notify");
        }

        void LayoutToasts()
        {
            float y = 0f;
            for (int i = toasts.Count - 1; i >= 0; i--)
            {
                toasts[i].root.anchoredPosition = new Vector2(0f, -y);
                y += toasts[i].root.sizeDelta.y + 8f;
            }
        }

        void BuildPrompt()
        {
            promptBg = UIFactory.Image("Interacao", hudRoot, UIFactory.Skin.tooltip, new Color(1f, 1f, 1f, 0.9f));
            promptBg.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 250f), new Vector2(360f, 54f));
            promptText = UIFactory.Text("Texto", promptBg.rectTransform, "", 2.6f, UIFactory.TextNormal, TextAnchor.MiddleCenter);
            promptText.rectTransform.Stretch();
            promptBg.gameObject.SetActive(false);
        }

        void BuildBossBar()
        {
            bossRoot = UIFactory.Rect("Elite", hudRoot);
            bossRoot.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(620f, 60f));
            bossName = UIFactory.Text("Nome", bossRoot, "", 2.6f, new Color(1f, 0.75f, 0.6f), TextAnchor.UpperCenter);
            bossName.rectTransform.Stretch(0, 0, 0, 30);
            var bg = UIFactory.Image("Fundo", bossRoot, UIFactory.Skin.bar, Color.white);
            bg.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(600f, 26f));
            bossFill = UIFactory.Image("Vida", bg.rectTransform, UIFactory.Skin.white, new Color(0.85f, 0.2f, 0.18f), false);
            bossFill.rectTransform.anchorMin = new Vector2(0f, 0f);
            bossFill.rectTransform.anchorMax = new Vector2(1f, 1f);
            bossFill.rectTransform.pivot = new Vector2(0f, 0.5f);
            bossFill.rectTransform.offsetMin = new Vector2(6f, 6f);
            bossFill.rectTransform.offsetMax = new Vector2(-6f, -6f);
            bossRoot.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ Ferramentas de referência

        void BuildReferenceTools()
        {
            toolsCanvas = UIFactory.CreateCanvas("FerramentasReferencia", 150, transform, false);
            toolsRoot = (RectTransform)toolsCanvas.transform;

            onion = UIFactory.Rect("CamadaReferencia", toolsRoot).gameObject.AddComponent<RawImage>();
            onion.raycastTarget = false;
            onion.color = new Color(1f, 1f, 1f, 0.5f);
            onion.gameObject.SetActive(false);

            for (int i = 0; i < 2; i++)
            {
                mask[i] = UIFactory.Image(i == 0 ? "MascaraEsq" : "MascaraDir", toolsRoot, UIFactory.Skin.white, new Color(0f, 0f, 0f, 0.92f), false);
                mask[i].gameObject.SetActive(false);
            }

            refTime = UIFactory.Text("Tempo", toolsRoot, "", 2.2f, new Color(0.6f, 1f, 0.95f, 0.9f), TextAnchor.UpperRight);
            refTime.rectTransform.Place(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -20f), new Vector2(700f, 60f));

            var helpBg = UIFactory.Image("Ajuda", toolsRoot, UIFactory.Skin.panel, new Color(1f, 1f, 1f, 0.95f));
            helpBg.rectTransform.Place(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 60f), new Vector2(620f, 560f));
            var help = UIFactory.Text("Texto", helpBg.rectTransform,
                "{#FFD54A}MODO DE REFERÊNCIA{/}\n\n" +
                "R        reinicia a arena (Ctrl+R: com reprodução)\n" +
                "F10      reproduz o roteiro/gravação\n" +
                "F9       grava/encerra entradas\n" +
                "F5       congela a simulação\n" +
                "F6       câmera livre (J L I K U O)\n" +
                "F2 / F3  HUD / mapa\n" +
                "F7       sobrepõe o quadro do vídeo\n" +
                "F11      máscara 9:16 do recorte\n" +
                "F8       captura o quadro atual\n" +
                "TAB      mapa   ·   ESC pausa\n" +
                "F1       fecha esta ajuda", 2.2f, UIFactory.TextNormal, TextAnchor.UpperLeft);
            help.rectTransform.Stretch(24, 24, 24, 24);
            refHelp = helpBg.rectTransform;
            refHelp.gameObject.SetActive(false);
            refFramesDir = FindReferenceFramesDir();
            UpdateMaskLayout();
        }

        static string FindReferenceFramesDir()
        {
            var args = Services.Args;
            if (args != null && !string.IsNullOrEmpty(args.ReferenceFramesDir) && Directory.Exists(args.ReferenceFramesDir)) return args.ReferenceFramesDir;
            string[] candidates =
            {
                Path.Combine(Application.dataPath, "..", "Docs", "Referencia", "quadros"),
                Path.Combine(Application.dataPath, "..", "..", "..", "Docs", "Referencia", "quadros"),
            };
            foreach (var c in candidates) if (Directory.Exists(c)) return Path.GetFullPath(c);
            return null;
        }

        public void ToggleReferenceHelp()
        {
            if (refHelp != null) refHelp.gameObject.SetActive(!refHelp.gameObject.activeSelf);
        }

        public void ToggleOnionSkin(float time)
        {
            if (onion == null) return;
            if (onion.gameObject.activeSelf) { onion.gameObject.SetActive(false); return; }
            if (string.IsNullOrEmpty(refFramesDir))
            {
                ShowToast("Quadros de referência não encontrados", "Use -refFrames <pasta> (quadros f_001.png... a cada 0,5 s).", 4f, UIFactory.TextGold);
                return;
            }
            int index = Mathf.Clamp(Mathf.RoundToInt(time / 0.5f) + 1, 1, 999);
            string file = Path.Combine(refFramesDir, $"f_{index:000}.png");
            if (!File.Exists(file)) { ShowToast("Quadro ausente", file, 3f, UIFactory.TextGold); return; }
            if (onionTex == null) onionTex = new Texture2D(2, 2);
            onionTex.LoadImage(File.ReadAllBytes(file));
            onion.texture = onionTex;
            onion.gameObject.SetActive(true);
            UpdateMaskLayout();
        }

        public void SetCaptureMask(bool on)
        {
            CaptureMaskVisible = on;
            foreach (var m in mask) if (m != null) m.gameObject.SetActive(on);
            UpdateMaskLayout();
        }

        void UpdateMaskLayout()
        {
            var prof = Services.Database != null ? Services.Database.captureProfile : null;
            if (prof == null || toolsRoot == null) return;
            // Em unidades de canvas (altura 1080 de referência).
            var size = toolsRoot.rect.size;
            if (size.x <= 0f) size = new Vector2(1920f, 1080f);
            var r = prof.CropRect(Mathf.RoundToInt(size.x), Mathf.RoundToInt(size.y));
            if (mask[0] != null)
            {
                mask[0].rectTransform.Place(new Vector2(0f, 0f), new Vector2(0f, 0f), Vector2.zero, new Vector2(r.x, size.y));
                mask[1].rectTransform.Place(new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(r.x + r.width, 0f), new Vector2(size.x - r.x - r.width, size.y));
            }
            if (onion != null) onion.rectTransform.Place(new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(r.x, r.y), new Vector2(r.width, r.height));
        }

        public void SetHudVisible(bool on)
        {
            HudVisible = on;
            if (Hud != null) hudRoot.Find("HUD")?.gameObject.SetActive(on);
            foreach (Transform child in hudRoot)
            {
                string n = child.name;
                if (n == "Degrade" || n == "Base" || n == "HUD" || n == "Objetivo" || n == "VidaBaixa") child.gameObject.SetActive(on && (n != "Objetivo" || !string.IsNullOrEmpty(objectiveText.Text)));
            }
        }

        public void SetMapVisible(bool on) => Map?.SetVisible(on);

        // ------------------------------------------------------------------ Atualização

        void Update()
        {
            var input = Services.Input;
            if (input == null) return;
            var state = Services.State.Current;

            bool cancelPad = Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame &&
                             !(Services.Input != null && Services.Input.AutomationLock);
            if (input.Pause.WasPressedThisFrame() || (cancelPad && ScreenOpen))
            {
                if (ScreenOpen) Back();
                else if (!menuMode && state == GameState.Gameplay) Push(Pause);
            }
            else if (!menuMode && input.Inventory.WasPressedThisFrame())
            {
                if (ScreenOpen && stack[stack.Count - 1] == Inventory) Back();
                else if (!ScreenOpen && state == GameState.Gameplay) Push(Inventory);
            }
            else if (!menuMode && input.Map.WasPressedThisFrame() && state == GameState.Gameplay)
            {
                SetMapVisible(!MapVisible);
            }

            if (ScreenOpen) stack[stack.Count - 1].Tick();
            if (menuMode) return;

            UpdateToasts();
            UpdatePrompt();
            UpdateBossBar();
            if (refTime != null && ctx.Reference != null)
            {
                var r = ctx.Reference;
                string rec = r.Recording ? "   {#FF5A5A}● GRAVANDO{/}" : "";
                string frz = Services.State.DebugFreeze ? "   {#9FE7FF}CONGELADO{/}" : "";
                refTime.Text = (r.ReplayRunning ? $"REF {r.ReplayTime:0.00}s / {Services.Database.referenceScript.duration:0.0}s" : "REF livre   F1 ajuda") + rec + frz;
            }
        }

        void UpdateToasts()
        {
            bool changed = false;
            for (int i = toasts.Count - 1; i >= 0; i--)
            {
                var t = toasts[i];
                t.age += DeterministicVfx.UnscaledDeltaTime;
                float a = Mathf.Clamp01((t.life - t.age) / 0.4f) * Mathf.Clamp01(t.age / 0.15f);
                t.bg.color = new Color(1f, 1f, 1f, 0.92f * a);
                t.title.color = new Color(t.title.color.r, t.title.color.g, t.title.color.b, a);
                t.body.color = new Color(t.body.color.r, t.body.color.g, t.body.color.b, a);
                if (t.age >= t.life)
                {
                    Destroy(t.root.gameObject);
                    toasts.RemoveAt(i);
                    changed = true;
                }
            }
            if (changed) LayoutToasts();
        }

        void UpdatePrompt()
        {
            var p = ctx.Player;
            var it = p != null && Services.State.Current == GameState.Gameplay ? p.NearestInteractable : null;
            bool show = it != null && HudVisible;
            if (promptBg.gameObject.activeSelf != show) promptBg.gameObject.SetActive(show);
            if (!show) return;
            string key = Services.Input.InteractLabel(Services.Input.DisplayDevice);
            promptText.Text = $"{{#FFD54A}}[{key}]{{/}}  {it.Prompt}";
            float w = promptText.MeasurePixels().x + 60f;
            promptBg.rectTransform.sizeDelta = new Vector2(Mathf.Max(260f, w), 54f);
        }

        void UpdateBossBar()
        {
            Actor elite = null;
            var p = ctx.Player;
            if (p != null)
            {
                foreach (var a in ctx.Actors.All)
                {
                    if (a == null || a.IsDead || a.EnemyDef == null || !a.EnemyDef.isElite) continue;
                    if (Vector3.Distance(a.transform.position, p.transform.position) > 22f) continue;
                    elite = a;
                    break;
                }
            }
            bool show = elite != null && HudVisible;
            if (bossRoot.gameObject.activeSelf != show) bossRoot.gameObject.SetActive(show);
            if (!show) return;
            bossName.Text = elite.displayName;
            bossFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(elite.Receiver.Fraction), 1f);
        }
    }
}

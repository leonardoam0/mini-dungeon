using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ruinas
{
    /// <summary>Tela modal empilhável (pausa, configurações, inventário...). Navegável por teclado, mouse e controle.</summary>
    public abstract class UIScreen
    {
        protected UIRoot Root;
        public RectTransform Panel { get; protected set; }
        protected Selectable firstSelected;
        public virtual GameState OpenState => GameState.Paused;
        public virtual bool CanCancel => true;

        public void Create(UIRoot root, RectTransform parent)
        {
            Root = root;
            Build(parent);
            if (Panel != null) Panel.gameObject.SetActive(false);
        }

        protected abstract void Build(RectTransform parent);
        public virtual void OnOpen() { }
        public virtual void OnClose() { }
        public virtual void Tick() { }
        public virtual void Focus() => UIFactory.Select(firstSelected);

        protected RectTransform CreatePanel(RectTransform parent, string name, Vector2 size, string title)
        {
            var dim = UIFactory.Image(name, parent, UIFactory.Skin.white, new Color(0f, 0.01f, 0.01f, 0.62f), false, true);
            dim.rectTransform.Stretch();
            var box = UIFactory.Image("Painel", dim.transform, UIFactory.Skin.panel, UIFactory.PanelTint);
            box.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            if (!string.IsNullOrEmpty(title))
            {
                var t = UIFactory.Text("Titulo", box.transform, title, 4f, UIFactory.TextGold, TextAnchor.UpperCenter);
                t.rectTransform.Stretch(20, 26, 20, 0);
                t.Outline = true;
            }
            Panel = dim.rectTransform;
            return box.rectTransform;
        }

        protected static Button AddButton(RectTransform parent, string label, float y, Action onClick, float width = 460f)
        {
            var b = UIFactory.Button(label, parent, label, () => onClick?.Invoke(), new Vector2(width, 66f));
            b.GetComponent<RectTransform>().Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(width, 66f));
            return b;
        }
    }

    // ------------------------------------------------------------------------------------------------ Pausa

    public class PauseScreen : UIScreen
    {
        protected override void Build(RectTransform parent)
        {
            var box = CreatePanel(parent, "Pausa", new Vector2(560f, 560f), "PAUSA");
            bool reference = LevelContext.Current != null && LevelContext.Current.Reference != null;
            var items = new List<Selectable>
            {
                AddButton(box, "Continuar", -110f, () => Root.CloseAll()),
                AddButton(box, "Configurações", -186f, () => Root.Push(Root.Settings)),
                AddButton(box, "Controles", -262f, () => Root.Push(Root.Controls)),
                AddButton(box, reference ? "Reiniciar arena" : "Voltar ao ponto de retorno", -338f, () =>
                {
                    Root.CloseAll();
                    var ctx = LevelContext.Current;
                    if (ctx?.Reference != null) ctx.Reference.RestartArena(true);
                    else ctx?.Mission?.RetryFromCheckpoint();
                }),
                AddButton(box, "Menu principal", -414f, () =>
                {
                    LevelContext.Current?.Mission?.WriteSave();
                    Root.CloseAll();
                    Services.Flow.Load(new LoadRequest { scene = SceneFlow.MenuScene });
                }),
            };
            UIFactory.SetupVerticalNavigation(items.ToArray());
            firstSelected = items[0];
        }
    }

    // ------------------------------------------------------------------------------------------------ Configurações

    public class SettingsScreen : UIScreen
    {
        public override GameState OpenState => Services.State.Current == GameState.Menu ? GameState.Menu : GameState.Paused;

        protected override void Build(RectTransform parent)
        {
            var box = CreatePanel(parent, "Configuracoes", new Vector2(900f, 940f), "CONFIGURAÇÕES");
            var s = Services.Settings.Data;
            var items = new List<Selectable>();
            float y = -100f;

            void AddSlider(string label, float value, Action<float> set)
            {
                var lbl = UIFactory.Text(label, box, label, 2.8f, UIFactory.TextNormal, TextAnchor.MiddleLeft);
                lbl.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, 1f), new Vector2(-380f, y), new Vector2(360f, 50f));
                var sl = UIFactory.Slider(label + "Slider", box, value, v => { set(v); Services.Settings.Apply(false); }, new Vector2(380f, 40f));
                sl.GetComponent<RectTransform>().Place(new Vector2(0.5f, 1f), new Vector2(0f, 1f), new Vector2(0f, y - 5f), new Vector2(380f, 40f));
                items.Add(sl);
                y -= 62f;
            }

            void AddCycle(string label, string[] options, int current, Action<int> set)
            {
                var b = UIFactory.Button(label, box, "", null, new Vector2(760f, 54f));
                b.GetComponent<RectTransform>().Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(760f, 54f));
                var cyc = b.gameObject.AddComponent<OptionCycler>();
                cyc.Setup(label, options, current, b.GetComponentInChildren<PixelText>(), i => { set(i); Services.Settings.Apply(); });
                items.Add(b);
                y -= 62f;
            }

            string[] onOff = { "Desligado", "Ligado" };
            AddSlider("Volume geral", s.masterVolume, v => s.masterVolume = v);
            AddSlider("Música", s.musicVolume, v => s.musicVolume = v);
            AddSlider("Efeitos", s.sfxVolume, v => s.sfxVolume = v);
            AddSlider("Ambiente", s.ambienceVolume, v => s.ambienceVolume = v);
            AddSlider("Interface", s.uiVolume, v => s.uiVolume = v);
            AddCycle("Perfil de controle", new[] { "Movimento direto (WASD)", "Clique para mover" }, s.controlProfile, i => s.controlProfile = i);
            AddCycle("Reduzir flashes", onOff, s.reduceFlashes ? 1 : 0, i => s.reduceFlashes = i == 1);
            AddCycle("Tremor de câmera", onOff, s.cameraShake ? 1 : 0, i => s.cameraShake = i == 1);
            AddCycle("Números de dano", onOff, s.showDamageNumbers ? 1 : 0, i => s.showDamageNumbers = i == 1);
            AddCycle("Escala da interface", new[] { "90%", "100%", "115%", "130%" }, ScaleIndex(s.uiScale), i => { s.uiScale = new[] { 0.9f, 1f, 1.15f, 1.3f }[i]; Root.ApplyUiScale(); });
            AddCycle("Qualidade gráfica", new[] { "Baixa", "Média", "Alta" }, s.qualityLevel, i => s.qualityLevel = i);
            AddCycle("Tela cheia", onOff, s.fullscreen ? 1 : 0, i => s.fullscreen = i == 1);
            AddCycle("Sincronização vertical", onOff, s.vsync ? 1 : 0, i => s.vsync = i == 1);
            AddCycle("Zona morta do analógico", new[] { "10%", "20%", "30%" }, Mathf.Clamp(Mathf.RoundToInt(s.gamepadDeadzone * 10f) - 1, 0, 2), i => s.gamepadDeadzone = 0.1f * (i + 1));

            var back = AddButton(box, "Voltar", y - 10f, () => Root.Back(), 360f);
            items.Add(back);
            UIFactory.SetupVerticalNavigation(items.ToArray());
            firstSelected = items[0];
        }

        static int ScaleIndex(float s) => s < 0.95f ? 0 : s < 1.1f ? 1 : s < 1.25f ? 2 : 3;

        public override void OnClose() => Services.Settings.Save();
    }

    // ------------------------------------------------------------------------------------------------ Controles

    public class ControlsScreen : UIScreen
    {
        class Row
        {
            public UnityEngine.InputSystem.InputAction action;
            public PixelText kbm, pad;
        }

        readonly List<Row> rows = new List<Row>();
        PixelText status;

        public override GameState OpenState => Services.State.Current == GameState.Menu ? GameState.Menu : GameState.Paused;

        static string Name(string action)
        {
            switch (action)
            {
                case "Melee": return "Ataque corpo a corpo";
                case "Ranged": return "Ataque à distância";
                case "Dodge": return "Esquiva";
                case "Artifact1": return "Artefato 1";
                case "Artifact2": return "Artefato 2";
                case "Artifact3": return "Artefato 3";
                case "Potion": return "Poção";
                case "Interact": return "Interagir";
                case "Inventory": return "Inventário";
                case "Map": return "Mapa sobreposto";
                case "AttackInPlace": return "Atacar parado (segurar)";
                default: return action;
            }
        }

        protected override void Build(RectTransform parent)
        {
            var box = CreatePanel(parent, "Controles", new Vector2(1180f, 960f), "CONTROLES");
            var input = Services.Input;
            var items = new List<Selectable>();
            var head = UIFactory.Text("Cabecalho", box, "AÇÃO                                  TECLADO/MOUSE           CONTROLE", 2.4f, UIFactory.TextDim, TextAnchor.UpperLeft);
            head.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, 1f), new Vector2(-540f, -92f), new Vector2(1080f, 30f));
            float y = -126f;
            foreach (var a in input.RemappableActions)
            {
                var row = new Row { action = a };
                var lbl = UIFactory.Text(a.name, box, Name(a.name), 2.8f, UIFactory.TextNormal, TextAnchor.MiddleLeft);
                lbl.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, 1f), new Vector2(-540f, y), new Vector2(460f, 52f));
                var k = UIFactory.Button("Kbm_" + a.name, box, "", () => Rebind(row, ActiveDevice.KeyboardMouse), new Vector2(250f, 50f), 2.6f);
                k.GetComponent<RectTransform>().Place(new Vector2(0.5f, 1f), new Vector2(0f, 1f), new Vector2(-60f, y), new Vector2(250f, 50f));
                var g = UIFactory.Button("Pad_" + a.name, box, "", () => Rebind(row, ActiveDevice.Gamepad), new Vector2(250f, 50f), 2.6f);
                g.GetComponent<RectTransform>().Place(new Vector2(0.5f, 1f), new Vector2(0f, 1f), new Vector2(220f, y), new Vector2(250f, 50f));
                row.kbm = k.GetComponentInChildren<PixelText>();
                row.pad = g.GetComponentInChildren<PixelText>();
                rows.Add(row);
                var nk = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = g };
                k.navigation = nk;
                var ng = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = k };
                g.navigation = ng;
                items.Add(k);
                items.Add(g);
                y -= 56f;
            }
            var fixedInfo = UIFactory.Text("Fixos", box,
                "Movimento: WASD ou setas / analógico esquerdo   ·   Mira: mouse / analógico direito\nPausa: ESC / START   ·   Referência: R reinicia, F1 ajuda", 2.2f, UIFactory.TextDim, TextAnchor.UpperCenter);
            fixedInfo.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y - 6f), new Vector2(1100f, 60f));
            status = UIFactory.Text("Status", box, "", 2.6f, UIFactory.TextGold, TextAnchor.UpperCenter);
            status.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y - 64f), new Vector2(1100f, 30f));
            var reset = AddButton(box, "Restaurar padrões", y - 100f, () => { Services.Input.ResetBindings(); Refresh(); }, 380f);
            reset.GetComponent<RectTransform>().anchoredPosition = new Vector2(-200f, y - 100f);
            var back = AddButton(box, "Voltar", y - 100f, () => Root.Back(), 380f);
            back.GetComponent<RectTransform>().anchoredPosition = new Vector2(200f, y - 100f);

            // Navegação vertical coluna a coluna.
            for (int i = 0; i < items.Count; i++)
            {
                var nav = items[i].navigation;
                nav.selectOnUp = i >= 2 ? items[i - 2] : back;
                nav.selectOnDown = i + 2 < items.Count ? items[i + 2] : (i % 2 == 0 ? (Selectable)reset : back);
                items[i].navigation = nav;
            }
            reset.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = back, selectOnUp = items[items.Count - 2], selectOnDown = items[0] };
            back.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = reset, selectOnUp = items[items.Count - 1], selectOnDown = items[1] };
            firstSelected = items[0];
            Refresh();
        }

        void Rebind(Row row, ActiveDevice device)
        {
            if (Services.Input.IsRebinding) return;
            status.Text = device == ActiveDevice.Gamepad ? "Pressione um botão do controle (ESC cancela)" : "Pressione uma tecla ou botão do mouse (ESC cancela)";
            Services.Input.StartRebind(row.action, device, ok =>
            {
                status.Text = ok ? "Atalho atualizado (conflitos são trocados automaticamente)." : "Remapeamento cancelado.";
                Refresh();
            });
        }

        void Refresh()
        {
            var input = Services.Input;
            foreach (var r in rows)
            {
                r.kbm.Text = input.BindingLabel(r.action, ActiveDevice.KeyboardMouse);
                r.pad.Text = input.BindingLabel(r.action, ActiveDevice.Gamepad);
            }
        }

        public override void OnOpen() => Refresh();
        public override bool CanCancel => !Services.Input.IsRebinding;
    }

    // ------------------------------------------------------------------------------------------------ Inventário

    public class InventoryScreen : UIScreen
    {
        readonly List<Button> itemButtons = new List<Button>();
        readonly List<ItemInstance> shown = new List<ItemInstance>();
        RectTransform grid;
        RectTransform details;
        PixelText detailText;
        PixelText header;
        Button equipBtn, salvageBtn;
        readonly Button[] artifactSlotBtns = new Button[3];
        PixelText equippedText;
        ItemInstance selected;
        public override GameState OpenState => GameState.Inventory;

        protected override void Build(RectTransform parent)
        {
            var box = CreatePanel(parent, "Inventario", new Vector2(1500f, 900f), "INVENTÁRIO");
            equippedText = UIFactory.Text("Equipado", box, "", 2.5f, UIFactory.TextNormal, TextAnchor.UpperLeft);
            equippedText.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -100f), new Vector2(380f, 520f));
            equippedText.Wrap = true;

            header = UIFactory.Text("Cabecalho", box, "", 2.5f, UIFactory.TextDim, TextAnchor.UpperLeft);
            header.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(440f, -100f), new Vector2(600f, 30f));
            grid = UIFactory.Rect("Grade", box);
            grid.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(440f, -140f), new Vector2(560f, 640f));

            details = UIFactory.Rect("Detalhes", box);
            details.Place(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -100f), new Vector2(430f, 700f));
            var dbg = UIFactory.Image("Fundo", details, UIFactory.Skin.panelLight, new Color(1f, 1f, 1f, 0.9f));
            dbg.rectTransform.Stretch();
            detailText = UIFactory.Text("Texto", details, "", 2.4f, UIFactory.TextNormal, TextAnchor.UpperLeft);
            detailText.rectTransform.Stretch(22, 22, 22, 250);
            detailText.Wrap = true;
            equipBtn = UIFactory.Button("Equipar", details, "Equipar", () => EquipSelected(-1), new Vector2(380f, 58f), 2.8f);
            equipBtn.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 170f), new Vector2(380f, 58f));
            for (int i = 0; i < 3; i++)
            {
                int slot = i;
                artifactSlotBtns[i] = UIFactory.Button("Slot" + i, details, (i + 1).ToString(), () => EquipSelected(slot), new Vector2(110f, 58f), 2.8f);
                artifactSlotBtns[i].GetComponent<RectTransform>().Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-125f + i * 125f, 170f), new Vector2(110f, 58f));
            }
            salvageBtn = UIFactory.Button("Desmontar", details, "Desmontar", SalvageSelected, new Vector2(380f, 58f), 2.8f);
            salvageBtn.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 100f), new Vector2(380f, 58f));
            var close = UIFactory.Button("Fechar", details, "Fechar", () => Root.Back(), new Vector2(380f, 58f), 2.8f);
            close.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(380f, 58f));
        }

        public override void OnOpen()
        {
            Rebuild();
        }

        void Rebuild()
        {
            var ctx = LevelContext.Current;
            var inv = ctx?.Player?.Inventory;
            foreach (var b in itemButtons) if (b != null) UnityEngine.Object.Destroy(b.gameObject);
            itemButtons.Clear();
            shown.Clear();
            if (inv == null) return;

            var prog = ctx.Player.Progression;
            header.Text = $"{inv.Items.Count} itens   ·   esmeraldas: {prog?.Emeralds ?? 0}   ·   flechas: {inv.Arrows}";
            equippedText.Text = BuildEquippedText(inv);

            int cols = 5;
            float cell = 104f;
            for (int i = 0; i < inv.Items.Count; i++)
            {
                var it = inv.Items[i];
                shown.Add(it);
                var b = UIFactory.Button("Item" + i, grid, "", null, new Vector2(96f, 96f), 2f);
                var rt = b.GetComponent<RectTransform>();
                rt.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2((i % cols) * cell, -(i / cols) * cell), new Vector2(96f, 96f));
                var icon = UIFactory.Image("Icone", rt, it.def.icon, Color.white, false);
                icon.rectTransform.Stretch(14, 14, 14, 14);
                icon.preserveAspect = true;
                var bg = b.GetComponent<Image>();
                bg.color = Color.Lerp(Color.white, ItemDefinition.RarityColor(it.def.rarity), 0.45f);
                if (inv.IsEquipped(it))
                {
                    var eq = UIFactory.Text("E", rt, "E", 2f, UIFactory.TextGold, TextAnchor.UpperRight);
                    eq.rectTransform.Stretch(6, 4, 8, 0);
                }
                var item = it;
                b.onClick.AddListener(() => Select(item));
                var trig = b.gameObject.AddComponent<SelectForward>();
                trig.onSelect = () => Select(item);
                itemButtons.Add(b);
            }

            // Navegação em grade + acesso aos botões de detalhe pela direita.
            for (int i = 0; i < itemButtons.Count; i++)
            {
                var nav = new Navigation { mode = Navigation.Mode.Explicit };
                nav.selectOnLeft = i % cols > 0 ? itemButtons[i - 1] : null;
                nav.selectOnRight = i % cols < cols - 1 && i + 1 < itemButtons.Count ? itemButtons[i + 1] : (Selectable)equipBtn;
                nav.selectOnUp = i - cols >= 0 ? itemButtons[i - cols] : null;
                nav.selectOnDown = i + cols < itemButtons.Count ? itemButtons[i + cols] : null;
                itemButtons[i].navigation = nav;
            }
            Selectable back = itemButtons.Count > 0 ? itemButtons[0] : null;
            equipBtn.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = back, selectOnDown = salvageBtn, selectOnUp = artifactSlotBtns[0] };
            salvageBtn.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = back, selectOnUp = equipBtn };
            for (int i = 0; i < 3; i++)
                artifactSlotBtns[i].navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = i > 0 ? artifactSlotBtns[i - 1] : back, selectOnRight = i < 2 ? artifactSlotBtns[i + 1] : null, selectOnDown = salvageBtn };

            firstSelected = itemButtons.Count > 0 ? itemButtons[0] : (Selectable)equipBtn;
            Select(selected != null && inv.Items.Contains(selected) ? selected : (shown.Count > 0 ? shown[0] : null));
        }

        static string BuildEquippedText(InventorySystem inv)
        {
            string Line(string slot, ItemInstance it) => $"{{#B8B2A0}}{slot}{{/}}\n{(it != null ? it.def.displayName + (it.power > 1 ? $" +{it.power - 1}" : "") : "{#6E6A60}(vazio){/}")}\n";
            return "EQUIPADO\n\n" +
                   Line("Corpo a corpo", inv.Melee) +
                   Line("Distância", inv.Ranged) +
                   Line("Armadura", inv.Armor) +
                   Line("Artefato 1", inv.Artifacts[0]) +
                   Line("Artefato 2", inv.Artifacts[1]) +
                   Line("Artefato 3", inv.Artifacts[2]);
        }

        void Select(ItemInstance it)
        {
            selected = it;
            var inv = LevelContext.Current?.Player?.Inventory;
            if (it == null || inv == null)
            {
                detailText.Text = "Nenhum item.";
                equipBtn.gameObject.SetActive(false);
                salvageBtn.gameObject.SetActive(false);
                foreach (var b in artifactSlotBtns) b.gameObject.SetActive(false);
                return;
            }
            detailText.Text = Describe(it, inv);
            bool artifact = it.def.kind == ItemKind.Artifact;
            bool equipped = inv.IsEquipped(it);
            equipBtn.gameObject.SetActive(!artifact);
            equipBtn.GetComponentInChildren<PixelText>().Text = equipped ? "Remover" : "Equipar";
            foreach (var b in artifactSlotBtns) b.gameObject.SetActive(artifact);
            salvageBtn.gameObject.SetActive(!equipped);
            salvageBtn.GetComponentInChildren<PixelText>().Text = $"Desmontar (+{SalvageValue(it)} esm.)";
        }

        static int SalvageValue(ItemInstance it) => Mathf.Max(1, it.def.emeraldValue + (it.power - 1) * 3);

        static string Describe(ItemInstance it, InventorySystem inv)
        {
            var d = it.def;
            string rc = "#" + ColorUtility.ToHtmlStringRGB(ItemDefinition.RarityColor(d.rarity));
            var sb = new System.Text.StringBuilder();
            sb.Append($"{{{rc}}}{d.displayName}{(it.power > 1 ? $" +{it.power - 1}" : "")}{{/}}\n");
            sb.Append($"{{#A8A294}}{d.KindLabel} · {d.RarityLabel} · poder {it.power}{{/}}\n\n");
            sb.Append(d.description).Append("\n\n");
            ItemInstance cur = null;
            switch (d.kind)
            {
                case ItemKind.Melee:
                    cur = inv.Melee;
                    sb.Append(Compare("Dano", d.meleeDamage * it.PowerMultiplier, cur != null ? cur.def.meleeDamage * cur.PowerMultiplier : 0f, cur != null && cur != it));
                    if (d.combo != null && d.combo.Length > 0 && d.combo[0] != null)
                        sb.Append($"Alcance {d.combo[0].range:0.0} · {d.combo.Length} golpes\n");
                    break;
                case ItemKind.Ranged:
                    cur = inv.Ranged;
                    sb.Append(Compare("Dano", d.rangedDamage * it.PowerMultiplier, cur != null ? cur.def.rangedDamage * cur.PowerMultiplier : 0f, cur != null && cur != it));
                    sb.Append($"Preparação {d.drawTime:0.00}s\n");
                    break;
                case ItemKind.Armor:
                    cur = inv.Armor;
                    sb.Append(Compare("Vida", d.healthBonus * it.PowerMultiplier, cur != null ? cur.def.healthBonus * cur.PowerMultiplier : 0f, cur != null && cur != it));
                    sb.Append($"Redução de dano {d.damageReduction * 100f:0}%\n");
                    if (d.moveSpeedBonus > 0f) sb.Append($"Velocidade +{d.moveSpeedBonus * 100f:0}%\n");
                    if (d.cooldownReduction > 0f) sb.Append($"Recarga de artefatos −{d.cooldownReduction * 100f:0}%\n");
                    break;
                case ItemKind.Artifact:
                    if (d.artifact != null) sb.Append($"Recarga {d.artifact.cooldown:0}s\n");
                    int slot = inv.ArtifactSlotOf(it);
                    sb.Append(slot >= 0 ? $"{{#FFD54A}}No slot {slot + 1}{{/}}\n" : "Escolha um slot abaixo.\n");
                    break;
            }
            if (d.enchantments != null && d.enchantments.Length > 0)
            {
                sb.Append("\n{#C68CFF}Encantamentos{/}\n");
                foreach (var e in d.enchantments) if (e != null) sb.Append($"· {e.displayName}: {e.description}\n");
            }
            return sb.ToString();
        }

        static string Compare(string label, float value, float current, bool show)
        {
            if (!show) return $"{label} {value:0}\n";
            float diff = value - current;
            string col = diff > 0.5f ? "#7CFF6A" : diff < -0.5f ? "#FF6A5A" : "#C8C8C8";
            return $"{label} {value:0}  {{{col}}}({(diff >= 0 ? "+" : "")}{diff:0} vs. equipado){{/}}\n";
        }

        void EquipSelected(int artifactSlot)
        {
            var inv = LevelContext.Current?.Player?.Inventory;
            if (inv == null || selected == null) return;
            if (selected.def.kind != ItemKind.Artifact && inv.IsEquipped(selected)) inv.Unequip(selected.def.kind);
            else inv.Equip(selected, artifactSlot);
            Services.Audio?.PlayUi("equip");
            LevelContext.Current?.Mission?.WriteSave();
            Rebuild();
            Refocus();
        }

        void SalvageSelected()
        {
            var ctx = LevelContext.Current;
            var inv = ctx?.Player?.Inventory;
            if (inv == null || selected == null || inv.IsEquipped(selected)) return;
            int value = SalvageValue(selected);
            inv.Remove(selected);
            ctx.Player.Progression?.AddEmeralds(value);
            Services.Audio?.PlayUi("salvage");
            selected = null;
            ctx.Mission?.WriteSave();
            Rebuild();
            Refocus();
        }

        void Refocus()
        {
            int idx = selected != null ? shown.IndexOf(selected) : 0;
            if (idx >= 0 && idx < itemButtons.Count) UIFactory.Select(itemButtons[idx]);
            else UIFactory.Select(firstSelected);
        }
    }

    // ------------------------------------------------------------------------------------------------ Mercador

    public class MerchantScreen : UIScreen
    {
        MerchantStation station;
        PixelText info;
        Button arrows, upMelee, upRanged, upArmor;
        public override GameState OpenState => GameState.Inventory;

        public void SetStation(MerchantStation s) => station = s;

        protected override void Build(RectTransform parent)
        {
            var box = CreatePanel(parent, "Mercador", new Vector2(760f, 700f), "ALTAR DE REFINO");
            info = UIFactory.Text("Info", box, "", 2.6f, UIFactory.TextNormal, TextAnchor.UpperCenter);
            info.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(680f, 80f));
            info.Wrap = true;
            arrows = AddButton(box, "", -190f, BuyArrows, 640f);
            upMelee = AddButton(box, "", -266f, () => Upgrade(ItemKind.Melee), 640f);
            upRanged = AddButton(box, "", -342f, () => Upgrade(ItemKind.Ranged), 640f);
            upArmor = AddButton(box, "", -418f, () => Upgrade(ItemKind.Armor), 640f);
            var close = AddButton(box, "Fechar", -520f, () => Root.Back(), 360f);
            UIFactory.SetupVerticalNavigation(new Selectable[] { arrows, upMelee, upRanged, upArmor, close });
            firstSelected = arrows;
        }

        public override void OnOpen() => Refresh();

        void Refresh()
        {
            var p = LevelContext.Current?.Player;
            if (p == null || station == null) return;
            info.Text = $"Esmeraldas: {{#7CFF6A}}{p.Progression.Emeralds}{{/}}   ·   Flechas: {p.Inventory.Arrows}\nAprimorar aumenta o poder do item (+10% por nível).";
            arrows.GetComponentInChildren<PixelText>().Text = $"Comprar {station.arrowBundle} flechas — {station.arrowPrice} esm.";
            SetUpgrade(upMelee, "Aprimorar arma corpo a corpo", p.Inventory.Melee);
            SetUpgrade(upRanged, "Aprimorar arma à distância", p.Inventory.Ranged);
            SetUpgrade(upArmor, "Aprimorar armadura", p.Inventory.Armor);
        }

        void SetUpgrade(Button b, string label, ItemInstance it)
        {
            var t = b.GetComponentInChildren<PixelText>();
            if (it == null) { t.Text = label + " (vazio)"; b.interactable = false; return; }
            b.interactable = true;
            t.Text = $"{label}: {it.power} → {it.power + 1} — {station.UpgradePrice(it)} esm.";
        }

        void BuyArrows()
        {
            var p = LevelContext.Current?.Player;
            if (p == null) return;
            if (p.Inventory.Arrows >= p.Inventory.MaxArrows || !p.Progression.SpendEmeralds(station.arrowPrice)) { Services.Audio?.PlayUi("denied"); return; }
            p.Inventory.AddArrows(station.arrowBundle);
            Services.Audio?.PlayUi("buy");
            Refresh();
        }

        void Upgrade(ItemKind kind)
        {
            var p = LevelContext.Current?.Player;
            if (p == null) return;
            var it = kind == ItemKind.Melee ? p.Inventory.Melee : kind == ItemKind.Ranged ? p.Inventory.Ranged : p.Inventory.Armor;
            if (it == null || !p.Progression.SpendEmeralds(station.UpgradePrice(it))) { Services.Audio?.PlayUi("denied"); return; }
            it.power++;
            p.Equipment.Refresh();
            Services.Audio?.PlayUi("upgrade");
            LevelContext.Current.Mission?.WriteSave();
            Refresh();
        }
    }

    // ------------------------------------------------------------------------------------------------ Derrota e conclusão

    public class DefeatScreen : UIScreen
    {
        public override GameState OpenState => GameState.Defeat;
        public override bool CanCancel => false;

        protected override void Build(RectTransform parent)
        {
            var box = CreatePanel(parent, "Derrota", new Vector2(700f, 440f), "DERROTA");
            var t = UIFactory.Text("Texto", box, "Suas vidas acabaram.\nO progresso até o último ponto de retorno está salvo.", 2.8f, UIFactory.TextNormal, TextAnchor.UpperCenter);
            t.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(640f, 90f));
            var retry = AddButton(box, "Tentar novamente", -220f, () =>
            {
                Root.CloseAll(false);
                LevelContext.Current?.Mission?.RetryFromCheckpoint();
            });
            var menu = AddButton(box, "Menu principal", -300f, () => Services.Flow.Load(new LoadRequest { scene = SceneFlow.MenuScene }));
            UIFactory.SetupVerticalNavigation(new Selectable[] { retry, menu });
            firstSelected = retry;
        }
    }

    public class CompletionScreen : UIScreen
    {
        PixelText summary;
        public override GameState OpenState => GameState.Completion;
        public override bool CanCancel => false;

        protected override void Build(RectTransform parent)
        {
            var box = CreatePanel(parent, "Conclusao", new Vector2(760f, 700f), "RUÍNAS CONCLUÍDAS");
            summary = UIFactory.Text("Resumo", box, "", 2.8f, UIFactory.TextNormal, TextAnchor.UpperCenter);
            summary.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(680f, 380f));
            var camp = AddButton(box, "Voltar ao acampamento", -500f, () => LevelContext.Current?.Mission?.ReturnToCamp());
            var menu = AddButton(box, "Menu principal", -580f, () => Services.Flow.Load(new LoadRequest { scene = SceneFlow.MenuScene }));
            UIFactory.SetupVerticalNavigation(new Selectable[] { camp, menu });
            firstSelected = camp;
        }

        public void SetSummary(MissionSummary s)
        {
            int m = Mathf.FloorToInt(s.time / 60f), sec = Mathf.FloorToInt(s.time % 60f);
            summary.Text =
                $"Tempo: {m:00}:{sec:00}\n" +
                $"Inimigos derrotados: {s.kills}\n" +
                $"Derrotas: {s.deaths}\n" +
                $"Baús abertos: {s.chests}\n" +
                $"Nível: {s.level}\n" +
                $"Itens no inventário: {s.items}\n\n" +
                $"{{#FFD54A}}Recompensa: +{s.rewardEmeralds} esmeraldas, +{s.rewardXp} XP{{/}}\n" +
                $"Total de esmeraldas: {s.emeralds}";
        }
    }
}

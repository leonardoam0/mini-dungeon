using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ruinas
{
    /// <summary>
    /// HUD inferior reconstruído a partir da referência (medidas em 1080p, estimadas do vídeo 540×960 que
    /// corresponde a um recorte central de ~607 px): três artefatos à esquerda, coração central, poção e mapa
    /// à direita, vidas, nível e barra roxa de experiência. Todo valor vem do estado real do jogo.
    /// </summary>
    public class HUDPresenter : MonoBehaviour
    {
        class SlotView
        {
            public RectTransform root;
            public Image frame;
            public Image activeFrame;
            public Image icon;
            public Image cooldown;
            public PixelText badge;
            public Image badgeBg;
            public float shake;
            public float pulse;
        }

        LevelContext ctx;
        PlayerController player;
        readonly SlotView[] artifactSlots = new SlotView[3];
        SlotView potionSlot, mapSlot;
        Image heartFill, heartLag, heartFrame, heartBack, heartShine;
        RectTransform heartRoot;
        PixelText livesText, levelLabel, levelText, arrowsText, emeraldsText;
        Image xpFill;
        RectTransform xpBar;
        RectTransform damageLayer;
        Image lowHealthVignette;
        readonly List<PixelText> damagePool = new List<PixelText>();
        readonly List<float> damageAge = new List<float>();
        float lagFraction = 1f;
        float lagDelay;
        float displayedFraction = 1f;
        int lastLives = -1, lastLevel = -1, lastArrows = -1, lastEmeralds = -1;
        ActiveDevice lastDevice = (ActiveDevice)(-1);

        static readonly Color BadgeY = new Color(1f, 0.83f, 0.2f);
        static readonly Color BadgeB = new Color(0.95f, 0.26f, 0.26f);
        static readonly Color BadgeX = new Color(0.3f, 0.62f, 1f);
        static readonly Color BadgeWhite = new Color(0.92f, 0.92f, 0.92f);

        public void Build(LevelContext context, RectTransform parent)
        {
            ctx = context;
            var skin = UIFactory.Skin;

            // Faixa escura inferior (degradê + base), como na referência.
            var grad = UIFactory.Image("Degrade", parent, skin.gradientBottom, new Color(0f, 0f, 0f, 0.55f), false);
            grad.rectTransform.anchorMin = new Vector2(0f, 0f);
            grad.rectTransform.anchorMax = new Vector2(1f, 0f);
            grad.rectTransform.pivot = new Vector2(0.5f, 0f);
            grad.rectTransform.sizeDelta = new Vector2(0f, 170f);
            grad.rectTransform.anchoredPosition = Vector2.zero;
            var band = UIFactory.Image("Base", parent, skin.white, new Color(0.01f, 0.01f, 0.015f, 0.9f), false);
            band.rectTransform.anchorMin = new Vector2(0f, 0f);
            band.rectTransform.anchorMax = new Vector2(1f, 0f);
            band.rectTransform.pivot = new Vector2(0.5f, 0f);
            band.rectTransform.sizeDelta = new Vector2(0f, 22f);

            lowHealthVignette = UIFactory.Image("VidaBaixa", parent, skin.gradientBottom, new Color(0.6f, 0f, 0.05f, 0f), false);
            lowHealthVignette.rectTransform.Stretch();

            var group = UIFactory.Rect("HUD", parent);
            group.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(1000f, 200f));

            // ESTIMADO no vídeo (recorte 540×960 convertido para 1080p): slots de ~92 px com base a 54 px do rodapé,
            // centros a -242/-132 (artefatos Y/B) e +133/+236 (poção/mapa); coração de ~152×132 subindo acima dos slots.
            float slot = 92f, slotBottom = 54f, heartW = 152f, heartH = 132f, mapSize = 80f;
            float[] artifactCenters = { -340f, -242f, -132f };
            for (int i = 0; i < 3; i++)
                artifactSlots[i] = CreateSlot(group, "Artefato" + (i + 1), new Vector2(artifactCenters[i] - slot * 0.5f, slotBottom), slot);
            potionSlot = CreateSlot(group, "Pocao", new Vector2(133f - slot * 0.5f, slotBottom), slot);
            mapSlot = CreateSlot(group, "Mapa", new Vector2(236f - mapSize * 0.5f, slotBottom + (slot - mapSize) * 0.5f), mapSize);
            potionSlot.icon.sprite = skin.potionIcon;
            potionSlot.icon.enabled = true;
            mapSlot.icon.sprite = skin.mapIcon;
            mapSlot.icon.enabled = true;

            // Coração
            heartRoot = UIFactory.Rect("Coracao", group);
            heartRoot.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-2f, 42f), new Vector2(heartW, heartH));
            heartFrame = UIFactory.Image("Moldura", heartRoot, skin.heartFrame, Color.white, false);
            heartFrame.rectTransform.Stretch();
            heartBack = UIFactory.Image("Fundo", heartRoot, skin.heartBack, Color.white, false);
            heartBack.rectTransform.Stretch();
            heartLag = UIFactory.Image("Perda", heartRoot, skin.heartFill, new Color(1f, 0.85f, 0.85f, 0.9f), false);
            heartLag.rectTransform.Stretch();
            SetupFill(heartLag);
            heartFill = UIFactory.Image("Vida", heartRoot, skin.heartFill, Color.white, false);
            heartFill.rectTransform.Stretch();
            SetupFill(heartFill);
            heartShine = UIFactory.Image("Brilho", heartRoot, skin.heartShine, Color.white, false);
            heartShine.rectTransform.Stretch();

            // Vidas (à esquerda da base do coração) e nível + barra roxa (à direita).
            livesText = UIFactory.Text("Vidas", group, "3", 3f, Color.white, TextAnchor.LowerRight);
            livesText.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(-55f, 14f), new Vector2(60f, 28f));
            var livesIcon = UIFactory.Image("IconeVidas", group, skin.livesIcon, Color.white, false);
            livesIcon.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(-52f, 17f), new Vector2(15f, 20f));

            levelLabel = UIFactory.Text("LV", group, "LV", 2f, new Color(0.75f, 0.75f, 0.78f), TextAnchor.LowerLeft);
            levelLabel.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(22f, 16f), new Vector2(40f, 22f));
            levelText = UIFactory.Text("Nivel", group, "1", 2.9f, Color.white, TextAnchor.LowerLeft);
            levelText.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(42f, 14f), new Vector2(60f, 28f));

            xpBar = UIFactory.Rect("XP", group);
            xpBar.Place(new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(82f, 23f), new Vector2(177f, 5f));
            var xpBg = UIFactory.Image("Fundo", xpBar, skin.white, new Color(0.12f, 0.1f, 0.16f, 0.9f), false);
            xpBg.rectTransform.Stretch();
            xpFill = UIFactory.Image("Barra", xpBar, skin.white, new Color(0.56f, 0.33f, 0.86f), false);
            xpFill.rectTransform.anchorMin = Vector2.zero;
            xpFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            xpFill.rectTransform.pivot = new Vector2(0f, 0.5f);
            xpFill.rectTransform.offsetMin = Vector2.zero;
            xpFill.rectTransform.offsetMax = Vector2.zero;

            // Indicadores adicionais fora do recorte central (não observados no vídeo).
            var arrowsIcon = UIFactory.Image("IconeFlechas", group, skin.arrowIcon, Color.white, false);
            arrowsIcon.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(-440f, 52f), new Vector2(30f, 30f));
            arrowsText = UIFactory.Text("Flechas", group, "0", 3f, Color.white, TextAnchor.LowerRight);
            arrowsText.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(-414f, 50f), new Vector2(60f, 30f));
            var gemIcon = UIFactory.Image("IconeEsmeraldas", group, skin.emeraldIcon, Color.white, false);
            gemIcon.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(312f, 52f), new Vector2(27f, 30f));
            emeraldsText = UIFactory.Text("Esmeraldas", group, "0", 3f, Color.white, TextAnchor.LowerLeft);
            emeraldsText.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(345f, 50f), new Vector2(80f, 30f));

            damageLayer = UIFactory.Rect("DanoRecebido", group);
            damageLayer.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 175f), new Vector2(300f, 120f));

            Services.Input.DeviceChanged += OnDevice;
            Services.Input.BindingsChanged += RefreshBadges;
        }

        void OnDestroy()
        {
            if (Services.Input != null)
            {
                Services.Input.DeviceChanged -= OnDevice;
                Services.Input.BindingsChanged -= RefreshBadges;
            }
            Unbind();
        }

        static void SetupFill(Image img)
        {
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Vertical;
            img.fillOrigin = (int)Image.OriginVertical.Bottom;
            img.fillAmount = 1f;
        }

        SlotView CreateSlot(RectTransform parent, string name, Vector2 bottomLeft, float size)
        {
            var skin = UIFactory.Skin;
            var v = new SlotView();
            v.root = UIFactory.Rect(name, parent);
            v.root.Place(new Vector2(0.5f, 0f), new Vector2(0f, 0f), bottomLeft, new Vector2(size, size));
            v.frame = UIFactory.Image("Moldura", v.root, skin.slotFrame, Color.white);
            v.frame.rectTransform.Stretch();
            v.icon = UIFactory.Image("Icone", v.root, null, Color.white, false);
            v.icon.rectTransform.Stretch(15, 15, 15, 15);
            v.icon.preserveAspect = true;
            v.icon.enabled = false;
            v.cooldown = UIFactory.Image("Recarga", v.root, skin.white, new Color(0.02f, 0.02f, 0.04f, 0.78f), false);
            v.cooldown.rectTransform.Stretch(12, 12, 12, 12);
            v.cooldown.type = Image.Type.Filled;
            v.cooldown.fillMethod = Image.FillMethod.Vertical;
            v.cooldown.fillOrigin = (int)Image.OriginVertical.Top;
            v.cooldown.fillAmount = 0f;
            v.activeFrame = UIFactory.Image("Ativo", v.root, skin.slotFrameActive, Color.white);
            v.activeFrame.rectTransform.Stretch();
            v.activeFrame.enabled = false;
            v.badgeBg = UIFactory.Image("Selo", v.root, skin.badge, Color.white);
            v.badgeBg.rectTransform.Place(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(4f, -4f), new Vector2(27f, 24f));
            v.badge = UIFactory.Text("Tecla", v.badgeBg.rectTransform, "", 2f, Color.white, TextAnchor.MiddleCenter);
            v.badge.rectTransform.Stretch(0, 1, 0, 0);
            v.badge.Shadow = false;
            return v;
        }

        public void Bind(PlayerController p)
        {
            Unbind();
            player = p;
            if (player == null) return;
            player.Artifacts.Changed += RefreshArtifacts;
            player.Artifacts.Denied += OnArtifactDenied;
            player.Artifacts.Used += OnArtifactUsed;
            player.Actions.PotionDenied += OnPotionDenied;
            player.Actions.PotionUsed += OnPotionUsed;
            player.Actor.Receiver.HealthChanged += OnHealthChanged;
            RefreshArtifacts();
            RefreshBadges();
            displayedFraction = lagFraction = player.Actor.Receiver.Fraction;
        }

        void Unbind()
        {
            if (player == null) return;
            player.Artifacts.Changed -= RefreshArtifacts;
            player.Artifacts.Denied -= OnArtifactDenied;
            player.Artifacts.Used -= OnArtifactUsed;
            player.Actions.PotionDenied -= OnPotionDenied;
            player.Actions.PotionUsed -= OnPotionUsed;
            if (player.Actor != null && player.Actor.Receiver != null) player.Actor.Receiver.HealthChanged -= OnHealthChanged;
            player = null;
        }

        void OnDevice(ActiveDevice d) => RefreshBadges();

        void RefreshBadges()
        {
            var input = Services.Input;
            if (input == null) return;
            var dev = input.DisplayDevice;
            lastDevice = dev;
            SetBadge(artifactSlots[0], input.BindingLabel(input.Artifact1, dev), dev == ActiveDevice.Gamepad ? BadgeX : BadgeWhite);
            SetBadge(artifactSlots[1], input.BindingLabel(input.Artifact2, dev), dev == ActiveDevice.Gamepad ? BadgeY : BadgeWhite);
            SetBadge(artifactSlots[2], input.BindingLabel(input.Artifact3, dev), dev == ActiveDevice.Gamepad ? BadgeB : BadgeWhite);
            SetBadge(potionSlot, input.BindingLabel(input.Potion, dev), BadgeWhite);
            SetBadge(mapSlot, input.BindingLabel(input.Map, dev), BadgeWhite);
            bool dpad = dev == ActiveDevice.Gamepad && input.BindingLabel(input.Map, dev) == "+";
            if (dpad && UIFactory.Skin.dpadIcon != null)
            {
                mapSlot.badge.Text = "";
                mapSlot.badgeBg.sprite = UIFactory.Skin.dpadIcon;
            }
            else mapSlot.badgeBg.sprite = UIFactory.Skin.badge;
        }

        static void SetBadge(SlotView v, string label, Color c)
        {
            if (v == null) return;
            v.badge.Text = label;
            v.badge.color = c;
            float w = Mathf.Max(27f, 12f + label.Length * 12f);
            v.badgeBg.rectTransform.sizeDelta = new Vector2(w, 24f);
            v.badgeBg.enabled = !string.IsNullOrEmpty(label);
        }

        void RefreshArtifacts()
        {
            if (player == null) return;
            for (int i = 0; i < 3; i++)
            {
                var s = player.Artifacts.Slots[i];
                var v = artifactSlots[i];
                v.icon.sprite = s.item != null ? s.item.def.icon : null;
                v.icon.enabled = v.icon.sprite != null;
            }
        }

        void OnArtifactDenied(int i, string reason) => artifactSlots[i].shake = 0.25f;
        void OnArtifactUsed(int i) => artifactSlots[i].pulse = 0.2f;
        void OnPotionDenied() => potionSlot.shake = 0.25f;
        void OnPotionUsed() => potionSlot.pulse = 0.25f;

        void OnHealthChanged(float hp, float max, float delta)
        {
            if (delta < -0.5f)
            {
                lagDelay = 0.45f;
                SpawnDamageTaken(Mathf.RoundToInt(-delta));
            }
        }

        void SpawnDamageTaken(int amount)
        {
            PixelText t = null;
            for (int i = 0; i < damagePool.Count; i++)
                if (!damagePool[i].gameObject.activeSelf) { t = damagePool[i]; damageAge[i] = 0f; break; }
            if (t == null)
            {
                if (damagePool.Count >= 6) { t = damagePool[0]; damageAge[0] = 0f; }
                else
                {
                    t = UIFactory.Text("Dano", damageLayer, "", 2.6f, Color.white, TextAnchor.MiddleCenter);
                    t.Outline = true;
                    t.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(120f, 30f));
                    damagePool.Add(t);
                    damageAge.Add(0f);
                }
            }
            t.Text = "-" + amount;
            t.gameObject.SetActive(true);
            t.rectTransform.anchoredPosition = new Vector2(Random.Range(-18f, 18f), 0f);
        }

        void Update()
        {
            if (player == null || ctx == null) return;
            float dt = DeterministicVfx.UnscaledDeltaTime;
            var recv = player.Actor.Receiver;
            float f = recv.Fraction;

            displayedFraction = Mathf.MoveTowards(displayedFraction, f, dt * 2.5f);
            if (lagDelay > 0f) lagDelay -= dt;
            else lagFraction = Mathf.MoveTowards(lagFraction, displayedFraction, dt * 0.8f);
            if (lagFraction < displayedFraction) lagFraction = displayedFraction;
            // O recheio ocupa ~85% da altura da moldura (a arte tem borda).
            heartFill.fillAmount = Remap(displayedFraction);
            heartLag.fillAmount = Remap(lagFraction);

            // Vida baixa: sinal gradual e discreto (pulso + vinheta), atenuado com "reduzir flashes".
            bool reduce = Services.Settings != null && Services.Settings.Data.reduceFlashes;
            float low = Mathf.Clamp01((0.35f - f) / 0.35f);
            float beat = low > 0f ? (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * (3f + low * 3f))) : 0f;
            heartRoot.localScale = Vector3.one * (1f + beat * low * 0.05f);
            lowHealthVignette.color = new Color(0.55f, 0f, 0.05f, low * (reduce ? 0.12f : 0.25f) * (0.6f + 0.4f * beat));

            for (int i = 0; i < 3; i++)
            {
                var v = artifactSlots[i];
                v.cooldown.fillAmount = player.Artifacts.CooldownFraction(i);
                v.activeFrame.enabled = player.Artifacts.IsActive(i);
                AnimateSlot(v, dt);
            }
            potionSlot.cooldown.fillAmount = player.Actions.PotionCooldown > 0f ? player.Actions.PotionCooldown / Mathf.Max(0.01f, player.Actions.PotionCooldownTotal) : 0f;
            AnimateSlot(potionSlot, dt);
            // No vídeo o slot do mapa não recebe destaque quando o mapa sobreposto está ligado.
            mapSlot.activeFrame.enabled = false;
            AnimateSlot(mapSlot, dt);

            int lives = ctx.Mission != null ? ctx.Mission.Lives : (ctx.Reference != null ? Services.Database.referenceScript.lives : 0);
            if (lives != lastLives) { lastLives = lives; livesText.Text = lives.ToString(); }
            var prog = player.Progression;
            if (prog != null)
            {
                int lvl = prog.ShownLevel;
                if (lvl != lastLevel) { lastLevel = lvl; levelText.Text = lvl.ToString(); }
                var r = xpFill.rectTransform;
                r.anchorMax = new Vector2(Mathf.Clamp01(prog.ShownFraction), 1f);
                if (prog.Emeralds != lastEmeralds) { lastEmeralds = prog.Emeralds; emeraldsText.Text = prog.Emeralds.ToString(); }
            }
            int arrows = player.Inventory.Arrows;
            if (arrows != lastArrows) { lastArrows = arrows; arrowsText.Text = arrows.ToString(); arrowsText.color = arrows == 0 ? BadgeB : Color.white; }

            for (int i = 0; i < damagePool.Count; i++)
            {
                var t = damagePool[i];
                if (!t.gameObject.activeSelf) continue;
                damageAge[i] += dt;
                float a = damageAge[i];
                var p = t.rectTransform.anchoredPosition;
                t.rectTransform.anchoredPosition = new Vector2(p.x, a * 40f);
                var c = t.color;
                c.a = Mathf.Clamp01(1.2f - a);
                t.color = c;
                if (a > 1.2f) t.gameObject.SetActive(false);
            }
        }

        static float Remap(float f) => f <= 0f ? 0f : Mathf.Lerp(0.1f, 0.9f, f);

        static void AnimateSlot(SlotView v, float dt)
        {
            Vector2 offset = Vector2.zero;
            if (v.shake > 0f)
            {
                v.shake -= dt;
                offset.x = Mathf.Sin(v.shake * 80f) * 4f * (v.shake / 0.25f);
            }
            float s = 1f;
            if (v.pulse > 0f)
            {
                v.pulse -= dt;
                s = 1f + 0.08f * Mathf.Sin((v.pulse / 0.25f) * Mathf.PI);
            }
            v.frame.rectTransform.anchoredPosition = offset;
            v.icon.rectTransform.anchoredPosition = offset;
            v.root.localScale = Vector3.one * s;
        }
    }
}

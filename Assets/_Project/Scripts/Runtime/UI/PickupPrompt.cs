using UnityEngine;
using UnityEngine.UI;

namespace Ruinas
{
    /// <summary>
    /// Dica de coleta ancorada ao coletável mais próximo que exige interação (poções): nome, descrição e o botão
    /// de pegar. Painel escuro translúcido logo abaixo do objeto, como no trecho de referência.
    /// </summary>
    public class PickupPrompt : MonoBehaviour
    {
        LevelContext ctx;
        RectTransform layer, panel;
        Image background;
        PixelText title, body, action;
        Pickup current;
        float alpha;

        public void Build(LevelContext context, RectTransform parent)
        {
            ctx = context;
            layer = UIFactory.Rect("DicaColeta", parent);
            layer.Stretch();
            panel = UIFactory.Rect("Painel", layer);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 1f);
            panel.sizeDelta = new Vector2(300f, 150f);
            background = UIFactory.Image("Fundo", panel, UIFactory.Skin != null ? UIFactory.Skin.white : null, new Color(0.035f, 0.04f, 0.05f, 0.86f), false);
            background.rectTransform.Stretch();
            title = UIFactory.Text("Titulo", panel, "", 2.3f, Color.white, TextAnchor.UpperCenter);
            title.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(290f, 26f));
            body = UIFactory.Text("Descricao", panel, "", 1.5f, new Color(0.78f, 0.79f, 0.8f), TextAnchor.UpperCenter);
            body.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(284f, 40f));
            body.Shadow = false;
            action = UIFactory.Text("Acao", panel, "", 2f, Color.white, TextAnchor.UpperCenter);
            action.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -104f), new Vector2(290f, 28f));
            panel.gameObject.SetActive(false);
        }

        Pickup FindNearest(Vector3 from)
        {
            Pickup best = null;
            float bestD = Pickup.PromptRadius;
            var list = Pickup.Active;
            for (int i = 0; i < list.Count; i++)
            {
                var p = list[i];
                if (p == null || !p.RequiresInteraction || p.Collected) continue;
                Vector3 d = p.transform.position - from;
                if (Mathf.Abs(d.y) > 2f) continue;
                d.y = 0f;
                float dist = d.magnitude;
                if (dist < bestD) { bestD = dist; best = p; }
            }
            return best;
        }

        void LateUpdate()
        {
            if (ctx == null || layer == null) return;
            var player = ctx.Player;
            bool hudOn = ctx.UI == null || ctx.UI.HudVisible;
            var target = player != null && !player.IsDefeated && hudOn ? FindNearest(player.transform.position) : null;
            if (target != null && target != current)
            {
                current = target;
                if (current.TryGetPrompt(out var t, out var d))
                {
                    title.Text = t.ToUpperInvariant();
                    body.Text = d;
                }
            }
            float goal = target != null ? 1f : 0f;
            alpha = Mathf.MoveTowards(alpha, goal, DeterministicVfx.UnscaledDeltaTime * 8f);
            bool visible = alpha > 0.01f && current != null && !current.Collected;
            if (panel.gameObject.activeSelf != visible) panel.gameObject.SetActive(visible);
            if (!visible) return;

            string key = Services.Input != null ? Services.Input.InteractLabel(Services.Input.DisplayDevice) : "E";
            action.Text = $"Pegar  {{#7CE05A}}[{key}]{{/}}";
            var cam = ctx.CameraRig != null ? ctx.CameraRig.cam : Camera.main;
            if (cam == null) return;
            Vector3 sp = cam.WorldToScreenPoint(current.transform.position + Vector3.up * 0.3f);
            if (sp.z < 0f) { panel.gameObject.SetActive(false); return; }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, sp, null, out var local);
            panel.anchoredPosition = local + new Vector2(0f, -70f);
            background.color = new Color(0.035f, 0.04f, 0.05f, 0.86f * alpha);
            var c = title.color; c.a = alpha; title.color = c;
            c = body.color; c.a = alpha; body.color = c;
            c = action.color; c.a = alpha; action.color = c;
        }

        public void Clear()
        {
            current = null;
            alpha = 0f;
            if (panel != null) panel.gameObject.SetActive(false);
        }
    }
}

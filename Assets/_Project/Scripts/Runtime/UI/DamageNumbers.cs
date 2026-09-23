using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ruinas
{
    /// <summary>
    /// Números de dano no mundo (pool). Brancos com contorno escuro; críticos sobre retângulo vermelho
    /// (como observado na referência). Afastamento automático evita sobreposição e mantém a leitura.
    /// </summary>
    public class DamageNumbers : MonoBehaviour
    {
        class Entry
        {
            public RectTransform root;
            public PixelText text;
            public Image box;
            public Vector3 world;
            public Vector2 offset;
            public float age;
            public float life;
            public bool active;
        }

        readonly List<Entry> pool = new List<Entry>();
        LevelContext ctx;
        RectTransform layer;

        public int ActiveCount
        {
            get
            {
                int n = 0;
                foreach (var e in pool) if (e.active) n++;
                return n;
            }
        }

        public void Build(LevelContext context, RectTransform parent)
        {
            ctx = context;
            layer = UIFactory.Rect("NumerosDeDano", parent);
            layer.Stretch();
            ctx.Events.Damaged += OnDamaged;
        }

        void OnDestroy()
        {
            if (ctx != null && ctx.Events != null) ctx.Events.Damaged -= OnDamaged;
        }

        void OnDamaged(DamageResult r)
        {
            if (!r.applied || (r.request.flags & DamageFlags.NoNumber) != 0) return;
            if (Services.Settings != null && !Services.Settings.Data.showDamageNumbers) return;
            var t = r.Target;
            if (t == null || t.team == Team.Player) return;
            Vector3 p = t.transform.position + Vector3.up * (t.height + 0.35f);
            bool status = (r.request.flags & DamageFlags.FromStatus) != 0;
            // No vídeo: golpes em branco; dano mágico/em área (artefatos) em vermelho.
            bool magic = !status && (r.request.kind == DamageKind.Magic || (r.request.flags & DamageFlags.Area) != 0);
            Show(p, r.amount, r.crit, status, magic);
        }

        static readonly Color MagicRed = new Color(0.93f, 0.12f, 0.12f);

        public void Show(Vector3 world, int amount, bool crit, bool minor, bool magic = false)
        {
            Entry e = null;
            foreach (var x in pool) if (!x.active) { e = x; break; }
            if (e == null)
            {
                if (pool.Count >= 40)
                {
                    e = pool[0];
                    foreach (var x in pool) if (x.age > e.age) e = x;
                }
                else e = Create();
            }
            e.world = world;
            e.age = 0f;
            e.life = crit ? 1.1f : 0.85f;
            e.active = true;
            e.text.Text = amount.ToString();
            e.text.PixelSize = crit ? 3.2f : (minor ? 2.4f : 2.8f);
            e.text.color = magic ? MagicRed : minor ? new Color(0.85f, 1f, 0.75f) : Color.white;
            e.box.enabled = crit && !magic;
            var size = e.text.MeasurePixels();
            e.box.rectTransform.sizeDelta = size + new Vector2(18f, 12f);
            e.offset = new Vector2(Random.Range(-14f, 14f), 0f);
            // Afastamento: sobe enquanto colidir com outro número recente próximo.
            var cam = ctx.CameraRig != null ? ctx.CameraRig.cam : Camera.main;
            if (cam != null)
            {
                Vector2 sp = cam.WorldToScreenPoint(world);
                for (int k = 0; k < 6; k++)
                {
                    bool clash = false;
                    foreach (var o in pool)
                    {
                        if (!o.active || o == e || o.age > 0.5f) continue;
                        Vector2 osp = (Vector2)cam.WorldToScreenPoint(o.world) + o.offset;
                        if (Mathf.Abs(osp.x - (sp.x + e.offset.x)) < 60f && Mathf.Abs(osp.y - (sp.y + e.offset.y)) < 28f) { clash = true; break; }
                    }
                    if (!clash) break;
                    e.offset.y += 30f;
                }
            }
            e.root.gameObject.SetActive(true);
            Place(e, cam);
        }

        Entry Create()
        {
            var e = new Entry();
            e.root = UIFactory.Rect("Numero", layer);
            e.root.sizeDelta = new Vector2(160f, 40f);
            e.box = UIFactory.Image("Caixa", e.root, UIFactory.Skin.damageBox, Color.white, true);
            e.box.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60f, 30f));
            e.text = UIFactory.Text("Valor", e.root, "", 2.8f, Color.white, TextAnchor.MiddleCenter);
            e.text.rectTransform.Stretch();
            e.text.Outline = true;
            e.text.Shadow = false;
            pool.Add(e);
            return e;
        }

        void Place(Entry e, Camera cam)
        {
            if (cam == null) return;
            Vector3 sp = cam.WorldToScreenPoint(e.world);
            if (sp.z < 0f) { e.root.gameObject.SetActive(false); return; }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, sp, null, out var local);
            float rise = e.age * 55f;
            e.root.anchoredPosition = local + e.offset + new Vector2(0f, rise);
            float pop = e.age < 0.08f ? Mathf.Lerp(1.35f, 1f, e.age / 0.08f) : 1f;
            e.root.localScale = Vector3.one * pop;
        }

        void LateUpdate()
        {
            var cam = ctx != null && ctx.CameraRig != null ? ctx.CameraRig.cam : Camera.main;
            float dt = Time.deltaTime;
            foreach (var e in pool)
            {
                if (!e.active) continue;
                e.age += dt;
                if (e.age >= e.life)
                {
                    e.active = false;
                    e.root.gameObject.SetActive(false);
                    continue;
                }
                float a = Mathf.Clamp01((e.life - e.age) / 0.25f);
                var c = e.text.color; c.a = a; e.text.color = c;
                var bc = e.box.color; bc.a = a; e.box.color = bc;
                Place(e, cam);
            }
        }

        public void ClearAll()
        {
            foreach (var e in pool)
            {
                e.active = false;
                e.root.gameObject.SetActive(false);
            }
        }
    }
}

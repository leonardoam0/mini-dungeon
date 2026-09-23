using UnityEngine;

namespace Ruinas
{
    public enum PickupKind { Item, Emeralds, Arrows, HealthOrb, Consumable }

    /// <summary>
    /// Coletável com coleta única. Itens mostram brilho localizado na cor da raridade (sem cobrir o piso);
    /// esmeraldas e flechas são atraídas quando o jogador passa perto; consumíveis (poções) exibem uma dica
    /// e só são pegos com o botão de interação.
    /// </summary>
    public class Pickup : MonoBehaviour
    {
        public const float PromptRadius = 2.6f;
        public const float InteractRadius = 2.1f;
        static readonly System.Collections.Generic.List<Pickup> active = new System.Collections.Generic.List<Pickup>();
        /// <summary>Coletáveis ativos na cena (para a dica de coleta).</summary>
        public static System.Collections.Generic.IReadOnlyList<Pickup> Active => active;

        public PickupKind kind;
        public ItemDefinition item;
        public ConsumableDefinition consumable;
        public int amount = 1;
        public int power = 1;
        [Tooltip("ID persistente para coletáveis posicionados no nível (vazio = drop dinâmico).")]
        public string persistentId;
        public Transform visual;

        public bool Collected { get; private set; }
        float age;
        Vector3 basePos;
        Vector3 hopFrom, hopTo;
        float hopT = 1f;
        PooledVfx beam;
        LootSystem owner;

        public void Setup(LootSystem system, PickupKind k, ItemDefinition def, int amt, int pwr, Vector3 from, Vector3 to)
        {
            owner = system;
            kind = k;
            item = def;
            amount = amt;
            power = pwr;
            Collected = false;
            age = 0f;
            hopFrom = from;
            hopTo = to;
            hopT = 0f;
            transform.position = from;
            basePos = to;
        }

        public void PlaceStatic()
        {
            basePos = transform.position;
            hopT = 1f;
        }

        void Start()
        {
            if (hopT >= 1f) basePos = transform.position;
            ShowBeam();
        }

        void ShowBeam()
        {
            if (beam != null) return;
            var ctx = LevelContext.Current;
            if (ctx == null) return;
            if (kind == PickupKind.Consumable && consumable != null)
            {
                beam = ctx.Vfx.Spawn("loot_beam", basePos, Quaternion.identity, -1f, new VfxParams { color = consumable.liquidColor * 1.6f, hasColor = true, height = 1.6f });
                return;
            }
            if (kind != PickupKind.Item || item == null) return;
            var color = ItemDefinition.RarityColor(item.rarity) * (item.rarity == Rarity.Common ? 1.2f : 2.2f);
            beam = ctx.Vfx.Spawn("loot_beam", basePos, Quaternion.identity, -1f, new VfxParams { color = color, hasColor = true, height = item.rarity == Rarity.Common ? 2.2f : 4.5f });
        }

        void OnEnable() { if (!active.Contains(this)) active.Add(this); }

        /// <summary>Nome e descrição exibidos na dica de coleta (vazio = sem dica).</summary>
        public bool TryGetPrompt(out string title, out string description)
        {
            title = description = "";
            if (Collected) return false;
            if (kind == PickupKind.Consumable && consumable != null) { title = consumable.displayName; description = consumable.description; return true; }
            if (kind == PickupKind.Item && item != null) { title = item.displayName; description = item.description; return true; }
            return false;
        }

        public bool RequiresInteraction => kind == PickupKind.Consumable;

        /// <summary>Coletável que exige interação mais próximo, dentro do raio (para interação contextual).</summary>
        public static Pickup NearestInteractive(Vector3 from, float radius)
        {
            Pickup best = null;
            float bestD = radius;
            foreach (var p in active)
            {
                if (p == null || p.Collected || !p.RequiresInteraction) continue;
                Vector3 d = p.transform.position - from;
                if (Mathf.Abs(d.y) > 2f) continue;
                d.y = 0f;
                float dist = d.magnitude;
                if (dist < bestD) { bestD = dist; best = p; }
            }
            return best;
        }

        void Update()
        {
            if (Collected) return;
            float dt = Time.deltaTime;
            age += dt;
            if (hopT < 1f)
            {
                hopT = Mathf.Min(1f, hopT + dt / 0.45f);
                Vector3 p = Vector3.Lerp(hopFrom, hopTo, hopT);
                p.y += Mathf.Sin(hopT * Mathf.PI) * 1.1f;
                transform.position = p;
                if (hopT >= 1f)
                {
                    basePos = hopTo;
                    ShowBeam();
                }
                return;
            }
            if (visual != null)
            {
                visual.localPosition = new Vector3(0f, 0.35f + Mathf.Sin(age * 2.4f) * 0.08f, 0f);
                visual.localRotation = Quaternion.Euler(0f, age * 70f, 0f);
            }

            var ctx = LevelContext.Current;
            var player = ctx != null ? ctx.Player : null;
            if (player == null || player.IsDefeated || age < 0.35f) return;
            Vector3 d = player.transform.position - transform.position;
            if (Mathf.Abs(d.y) > 2f) return;
            d.y = 0f;
            float dist = d.magnitude;
            if (RequiresInteraction)
            {
                if (dist <= InteractRadius && player.InteractRequested) Collect(player, ctx);
                return;
            }
            bool magnet = kind == PickupKind.Emeralds || kind == PickupKind.Arrows || kind == PickupKind.HealthOrb;
            if (magnet && dist < 3.2f && dist > 0.6f)
                transform.position = Vector3.MoveTowards(transform.position, player.transform.position, dt * (4f + (3.2f - dist) * 4f));
            if (dist <= (kind == PickupKind.Item ? 1.1f : 0.8f)) Collect(player, ctx);
        }

        public void Collect(PlayerController player, LevelContext ctx)
        {
            if (Collected) return;
            if (kind == PickupKind.HealthOrb && player.Actor.Receiver.Fraction >= 0.999f) return;
            Collected = true;
            switch (kind)
            {
                case PickupKind.Item:
                    var inst = player.Inventory.Add(item, power);
                    ctx.Events.RaiseItemCollected(inst);
                    Services.Audio?.Play("item_pickup", transform.position);
                    break;
                case PickupKind.Emeralds:
                    player.Progression.AddEmeralds(amount);
                    ctx.Events.RaiseEmeralds(amount);
                    Services.Audio?.Play("emerald", transform.position);
                    break;
                case PickupKind.Arrows:
                    player.Inventory.AddArrows(amount);
                    ctx.Events.RaiseArrows(amount);
                    Services.Audio?.Play("arrow_pickup", transform.position);
                    break;
                case PickupKind.HealthOrb:
                    ctx.Combat.Heal(player.Actor, player.Actor.Receiver.MaxHealth * 0.2f, "orb");
                    Services.Audio?.Play("heal", transform.position);
                    break;
                case PickupKind.Consumable:
                    if (consumable != null)
                    {
                        if (consumable.healFraction > 0f) ctx.Combat.Heal(player.Actor, player.Actor.Receiver.MaxHealth * consumable.healFraction, consumable.id);
                        if (consumable.status != null) player.Actor.Status?.Apply(consumable.status, player.Actor, consumable.statusDuration);
                        Services.Audio?.Play(consumable.pickupSfx, transform.position);
                        ctx.Events.Notify(consumable.displayName, consumable.description);
                    }
                    break;
            }
            ctx.Vfx.Spawn("pickup_sparkle", transform.position + Vector3.up * 0.4f, Quaternion.identity);
            if (!string.IsNullOrEmpty(persistentId)) ctx.Mission?.MarkPickupCollected(persistentId);
            Release();
        }

        public void Release()
        {
            if (beam != null) beam.StopAndRelease(0.2f);
            beam = null;
            if (owner != null) owner.OnPickupReleased(this);
            else gameObject.SetActive(false);
        }

        void OnDisable()
        {
            active.Remove(this);
            if (beam != null) beam.StopAndRelease(0.1f);
            beam = null;
        }
    }
}

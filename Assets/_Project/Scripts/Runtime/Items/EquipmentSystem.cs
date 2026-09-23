using System.Collections.Generic;
using UnityEngine;

namespace Ruinas
{
    /// <summary>
    /// Aplica o equipamento ao jogador: atributos (camada "equipment" do ActorStats), encantamentos
    /// por regras centralizadas, vida máxima, modelos na mão e aparência da armadura.
    /// </summary>
    public class EquipmentSystem : MonoBehaviour
    {
        PlayerController pc;
        InventorySystem inv;
        float healthOverride;
        GameObject meleeVisual;
        GameObject rangedVisual;
        ItemDefinition shownMelee, shownRanged;
        float toxicCooldown;
        readonly List<StatusApplication> meleeStatuses = new List<StatusApplication>();
        StatusApplication[] meleeStatusArray;
        Material baseSkin;

        public ItemInstance Melee => inv?.Melee;
        public ItemInstance Ranged => inv?.Ranged;
        public ItemInstance Armor => inv?.Armor;
        public StatusApplication[] MeleeStatuses => meleeStatusArray;
        public int ExtraPierce { get; private set; }

        public void Init(PlayerController controller, InventorySystem inventory, float maxHealthOverride)
        {
            pc = controller;
            inv = inventory;
            healthOverride = maxHealthOverride;
            if (pc.Rig != null && pc.Rig.renderers != null && pc.Rig.renderers.Length > 0 && pc.Rig.renderers[0] != null)
                baseSkin = pc.Rig.renderers[0].sharedMaterial;
            inv.EquipmentChanged += Refresh;
            pc.Actor.DealtHit += OnDealtHit;
            Refresh();
        }

        void OnDestroy()
        {
            if (inv != null) inv.EquipmentChanged -= Refresh;
            if (pc != null && pc.Actor != null) pc.Actor.DealtHit -= OnDealtHit;
        }

        public float ComputeMaxHealth()
        {
            if (healthOverride > 0f) return healthOverride;
            float baseHp = pc.Def.maxHealth * (pc.Progression != null ? pc.Progression.HealthBonus : 1f);
            return baseHp + pc.Actor.Stats.equipment.healthBonus;
        }

        IEnumerable<EnchantmentDefinition> Enchantments()
        {
            foreach (var it in new[] { inv.Melee, inv.Ranged, inv.Armor })
            {
                if (it?.def?.enchantments == null) continue;
                foreach (var e in it.def.enchantments) if (e != null) yield return e;
            }
        }

        public void Refresh()
        {
            if (pc == null) return;
            var m = pc.Actor.Stats.equipment;
            m.Reset();
            ExtraPierce = 0;
            meleeStatuses.Clear();

            var armor = inv.Armor;
            if (armor != null)
            {
                var d = armor.def;
                m.armor += d.damageReduction;
                m.healthBonus += d.healthBonus * armor.PowerMultiplier;
                m.moveSpeed *= 1f + d.moveSpeedBonus;
                m.cooldown *= 1f - d.cooldownReduction;
            }

            foreach (var e in Enchantments())
            {
                switch (e.kind)
                {
                    case EnchantKind.Sharpness: m.meleeDamage *= 1f + e.value; break;
                    case EnchantKind.Swiftness: m.moveSpeed *= 1f + e.value; break;
                    case EnchantKind.Recharge: m.cooldown *= 1f - e.value; break;
                    case EnchantKind.Vitality: m.healthBonus += e.value; break;
                    case EnchantKind.Guard: m.armor += e.value; break;
                    case EnchantKind.Piercing: ExtraPierce += Mathf.RoundToInt(e.value); break;
                    case EnchantKind.Fire:
                        if (e.status.effect != null) meleeStatuses.Add(e.status);
                        break;
                }
            }
            meleeStatusArray = meleeStatuses.Count > 0 ? meleeStatuses.ToArray() : null;

            if (pc.Actor.Receiver != null && pc.Actor.Receiver.MaxHealth > 0f)
                pc.Actor.Receiver.SetMaxHealth(ComputeMaxHealth(), true);

            for (int i = 0; i < 3; i++) pc.Artifacts.SetSlot(i, inv.Artifacts[i]);
            RefreshVisuals();
        }

        void RefreshVisuals()
        {
            var rig = pc.Rig;
            if (rig == null) return;
            var melee = inv.Melee?.def;
            if (melee != shownMelee)
            {
                if (meleeVisual != null) Destroy(meleeVisual);
                meleeVisual = CreateHeld(melee, rig.handR);
                shownMelee = melee;
                var trail = meleeVisual != null ? meleeVisual.GetComponentInChildren<TrailRenderer>(true) : null;
                if (trail == null && meleeVisual != null) trail = AddTrail(meleeVisual.transform, melee);
                pc.Actions.SetTrail(trail);
            }
            var ranged = inv.Ranged?.def;
            if (ranged != shownRanged)
            {
                if (rangedVisual != null) Destroy(rangedVisual);
                rangedVisual = CreateHeld(ranged, rig.back != null ? rig.back : rig.handL);
                shownRanged = ranged;
            }

            // Aparência da armadura: material/tinta própria por item (mantém a silhueta).
            var armor = inv.Armor?.def;
            if (rig.renderers != null)
            {
                foreach (var r in rig.renderers)
                {
                    if (r == null || r.gameObject.name.StartsWith("Held")) continue;
                    if (armor != null && armor.worldMaterial != null) r.sharedMaterial = armor.worldMaterial;
                    else if (baseSkin != null) r.sharedMaterial = baseSkin;
                }
            }
        }

        GameObject CreateHeld(ItemDefinition def, Transform parent)
        {
            if (def == null || def.worldMesh == null || parent == null) return null;
            var go = new GameObject("Held_" + def.id);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = def.holdOffset;
            go.transform.localRotation = Quaternion.Euler(def.holdEuler);
            go.transform.localScale = Vector3.one * def.holdScale;
            go.AddComponent<MeshFilter>().sharedMesh = def.worldMesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = def.worldMaterial != null && def.kind != ItemKind.Armor ? def.worldMaterial : Services.Database?.itemMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return go;
        }

        TrailRenderer AddTrail(Transform weapon, ItemDefinition def)
        {
            var tipGo = new GameObject("PontaRastro");
            tipGo.transform.SetParent(weapon, false);
            tipGo.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            var tr = tipGo.AddComponent<TrailRenderer>();
            tr.time = 0.11f;
            tr.minVertexDistance = 0.05f;
            tr.widthCurve = new AnimationCurve(new Keyframe(0f, 0.34f), new Keyframe(1f, 0f));
            tr.sharedMaterial = Services.Database?.vfx?.glowAdditive;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.95f, 0.85f), 0f), new GradientColorKey(new Color(0.8f, 0.6f, 1f), 1f) },
                new[] { new GradientAlphaKey(0.55f, 0f), new GradientAlphaKey(0f, 1f) });
            tr.colorGradient = grad;
            tr.emitting = false;
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return tr;
        }

        void Update()
        {
            if (toxicCooldown > 0f) toxicCooldown -= Time.deltaTime;
        }

        void OnDealtHit(Actor self, DamageResult r)
        {
            if (r.request.kind != DamageKind.Melee || r.Target == null) return;
            var ctx = LevelContext.Current;
            if (ctx == null) return;
            foreach (var e in Enchantments())
            {
                if (e.kind != EnchantKind.ToxicCloud || e.area == null || !e.area.enabled) continue;
                if (toxicCooldown > 0f) continue;
                if (!ctx.Random.Chance(e.chance <= 0f ? 1f : e.chance)) continue;
                toxicCooldown = e.cooldown;
                Vector3 p = r.Target.transform.position;
                ctx.Areas.Spawn(pc.Actor, Team.Player, p, e.area, pc.Actor.Stats.globalDamage);
                Services.Audio?.Play("toxic_puff", p);
            }
        }
    }
}

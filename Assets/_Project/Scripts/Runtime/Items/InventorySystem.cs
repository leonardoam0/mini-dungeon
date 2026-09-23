using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ruinas
{
    /// <summary>
    /// Inventário e equipamento do jogador (lógica pura, testável). Slots reais: corpo a corpo,
    /// distância, armadura e três artefatos. Serializa só IDs estáveis.
    /// </summary>
    public class InventorySystem
    {
        public readonly List<ItemInstance> Items = new List<ItemInstance>();
        public ItemInstance Melee { get; private set; }
        public ItemInstance Ranged { get; private set; }
        public ItemInstance Armor { get; private set; }
        public readonly ItemInstance[] Artifacts = new ItemInstance[3];
        public int Arrows { get; private set; }
        public int MaxArrows = 99;

        public event Action Changed;
        public event Action EquipmentChanged;
        public event Action<int> ArrowsChanged;

        public ItemInstance Add(ItemDefinition def, int power = 1)
        {
            if (def == null) return null;
            var it = ItemInstance.Create(def, power);
            Items.Add(it);
            Changed?.Invoke();
            return it;
        }

        public ItemInstance AddExisting(ItemInstance it)
        {
            if (it == null || it.def == null || Items.Contains(it)) return it;
            Items.Add(it);
            Changed?.Invoke();
            return it;
        }

        public ItemInstance Find(string uid)
        {
            if (string.IsNullOrEmpty(uid)) return null;
            foreach (var i in Items) if (i.uid == uid) return i;
            return null;
        }

        public bool IsEquipped(ItemInstance it)
        {
            if (it == null) return false;
            if (Melee == it || Ranged == it || Armor == it) return true;
            foreach (var a in Artifacts) if (a == it) return true;
            return false;
        }

        public int ArtifactSlotOf(ItemInstance it)
        {
            for (int i = 0; i < Artifacts.Length; i++) if (Artifacts[i] == it) return i;
            return -1;
        }

        /// <summary>Equipa no slot correspondente ao tipo. Artefatos: slot indicado, primeiro vazio ou o 1º.</summary>
        public bool Equip(ItemInstance it, int artifactSlot = -1)
        {
            if (it == null || it.def == null || !Items.Contains(it)) return false;
            switch (it.def.kind)
            {
                case ItemKind.Melee: Melee = it; break;
                case ItemKind.Ranged: Ranged = it; break;
                case ItemKind.Armor: Armor = it; break;
                case ItemKind.Artifact:
                    int current = ArtifactSlotOf(it);
                    int target = artifactSlot;
                    if (target < 0 || target >= Artifacts.Length)
                    {
                        if (current >= 0) return true;
                        target = Array.IndexOf(Artifacts, null);
                        if (target < 0) target = 0;
                    }
                    if (current >= 0 && current != target)
                    {
                        // Troca de posição entre slots.
                        Artifacts[current] = Artifacts[target];
                    }
                    Artifacts[target] = it;
                    break;
            }
            EquipmentChanged?.Invoke();
            Changed?.Invoke();
            return true;
        }

        public void Unequip(ItemKind kind, int artifactSlot = 0)
        {
            switch (kind)
            {
                case ItemKind.Melee: Melee = null; break;
                case ItemKind.Ranged: Ranged = null; break;
                case ItemKind.Armor: Armor = null; break;
                case ItemKind.Artifact:
                    if (artifactSlot >= 0 && artifactSlot < Artifacts.Length) Artifacts[artifactSlot] = null;
                    break;
            }
            EquipmentChanged?.Invoke();
            Changed?.Invoke();
        }

        public bool Remove(ItemInstance it)
        {
            if (it == null || !Items.Remove(it)) return false;
            bool eq = false;
            if (Melee == it) { Melee = null; eq = true; }
            if (Ranged == it) { Ranged = null; eq = true; }
            if (Armor == it) { Armor = null; eq = true; }
            for (int i = 0; i < Artifacts.Length; i++) if (Artifacts[i] == it) { Artifacts[i] = null; eq = true; }
            if (eq) EquipmentChanged?.Invoke();
            Changed?.Invoke();
            return true;
        }

        public bool UseArrow()
        {
            if (Arrows <= 0) return false;
            Arrows--;
            ArrowsChanged?.Invoke(Arrows);
            return true;
        }

        public void AddArrows(int n)
        {
            if (n == 0) return;
            Arrows = Mathf.Clamp(Arrows + n, 0, MaxArrows);
            ArrowsChanged?.Invoke(Arrows);
        }

        public void SetArrows(int n)
        {
            Arrows = Mathf.Clamp(n, 0, MaxArrows);
            ArrowsChanged?.Invoke(Arrows);
        }

        public InventoryData ToData()
        {
            var d = new InventoryData();
            foreach (var i in Items) d.items.Add(i.ToData());
            d.melee = Melee?.uid ?? "";
            d.ranged = Ranged?.uid ?? "";
            d.armor = Armor?.uid ?? "";
            d.artifacts = new string[3];
            for (int i = 0; i < 3; i++) d.artifacts[i] = Artifacts[i]?.uid ?? "";
            d.arrows = Arrows;
            return d;
        }

        public static InventorySystem FromData(InventoryData d, GameDatabase db)
        {
            var inv = new InventorySystem();
            if (d == null) return inv;
            if (d.items != null)
            {
                foreach (var e in d.items)
                {
                    var def = db != null ? db.GetItem(e.itemId) : null;
                    if (def == null || string.IsNullOrEmpty(e.uid)) continue;
                    inv.Items.Add(new ItemInstance { uid = e.uid, def = def, power = Mathf.Max(1, e.power) });
                }
            }
            inv.Melee = inv.Find(d.melee);
            inv.Ranged = inv.Find(d.ranged);
            inv.Armor = inv.Find(d.armor);
            if (d.artifacts != null)
                for (int i = 0; i < Mathf.Min(3, d.artifacts.Length); i++) inv.Artifacts[i] = inv.Find(d.artifacts[i]);
            // Um slot só aceita o tipo certo (defesa contra saves manipulados).
            if (inv.Melee != null && inv.Melee.def.kind != ItemKind.Melee) inv.Melee = null;
            if (inv.Ranged != null && inv.Ranged.def.kind != ItemKind.Ranged) inv.Ranged = null;
            if (inv.Armor != null && inv.Armor.def.kind != ItemKind.Armor) inv.Armor = null;
            for (int i = 0; i < 3; i++) if (inv.Artifacts[i] != null && inv.Artifacts[i].def.kind != ItemKind.Artifact) inv.Artifacts[i] = null;
            inv.Arrows = Mathf.Clamp(d.arrows, 0, inv.MaxArrows);
            return inv;
        }
    }
}

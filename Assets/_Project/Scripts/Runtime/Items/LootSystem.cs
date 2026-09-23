using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Ruinas
{
    /// <summary>Rola tabelas de loot configuráveis e cria coletáveis. Cada drop é um objeto de coleta única.</summary>
    public class LootSystem : MonoBehaviour
    {
        readonly List<Pickup> dynamic = new List<Pickup>();
        GameRandom rng;
        Transform root;

        public int ActiveDrops => dynamic.Count;
        public int ItemPowerBonus { get; set; }

        public void Init(GameRandom random)
        {
            rng = random;
            root = new GameObject("[Loot]").transform;
            root.SetParent(transform, false);
        }

        public void Roll(LootTable table, Vector3 origin, int minPower)
        {
            if (table == null || rng == null) return;
            if (table.guaranteed != null)
                foreach (var e in table.guaranteed) Spawn(e, origin, minPower);
            if (table.entries == null || table.entries.Length == 0) return;
            if (!rng.Chance(table.dropChance)) return;
            for (int r = 0; r < Mathf.Max(1, table.rolls); r++)
            {
                float total = 0f;
                foreach (var e in table.entries) total += Mathf.Max(0f, e.weight);
                float pick = rng.Value * total;
                foreach (var e in table.entries)
                {
                    pick -= Mathf.Max(0f, e.weight);
                    if (pick <= 0f) { Spawn(e, origin, minPower); break; }
                }
            }
        }

        void Spawn(LootEntry e, Vector3 origin, int minPower)
        {
            if (e == null) return;
            if (e.item != null) SpawnPickup(PickupKind.Item, origin, e.item, 1, Mathf.Max(1, minPower + ItemPowerBonus));
            if (e.emeraldsMax > 0)
            {
                int n = rng.Range(e.emeraldsMin, e.emeraldsMax + 1);
                if (n > 0) SpawnPickup(PickupKind.Emeralds, origin, null, n, 1);
            }
            if (e.arrows > 0) SpawnPickup(PickupKind.Arrows, origin, null, e.arrows, 1);
            if (e.healthOrb > 0f && rng.Chance(e.healthOrb)) SpawnPickup(PickupKind.HealthOrb, origin, null, 1, 1);
        }

        public Pickup SpawnPickup(PickupKind kind, Vector3 origin, ItemDefinition def, int amount, int power)
        {
            var go = new GameObject("Drop_" + kind);
            go.transform.SetParent(root, false);
            var p = go.AddComponent<Pickup>();
            p.visual = BuildVisual(go.transform, kind, def);
            Vector2 off = rng.InsideUnitCircle() * 1.3f;
            Vector3 target = origin + new Vector3(off.x, 0f, off.y);
            // O ponto de queda precisa estar no mesmo nível e alcançável a partir da origem (não no topo de pilares).
            if (!ReachableDrop(origin, target, out target) && NavMesh.SamplePosition(origin, out var hit, 3f, NavMesh.AllAreas)) target = hit.position;
            p.Setup(this, kind, def, amount, power, origin, target);
            dynamic.Add(p);
            return p;
        }

        // Criado sob demanda: a API de navegação não pode ser chamada em inicializadores estáticos.
        static NavMeshPath dropPath;

        static bool ReachableDrop(Vector3 origin, Vector3 desired, out Vector3 result)
        {
            result = desired;
            if (!NavMesh.SamplePosition(desired, out var hit, 2f, NavMesh.AllAreas)) return false;
            if (Mathf.Abs(hit.position.y - origin.y) > 1.1f) return false;
            if (!NavMesh.SamplePosition(origin, out var from, 2f, NavMesh.AllAreas)) { result = hit.position; return true; }
            if (dropPath == null) dropPath = new NavMeshPath();
            if (!NavMesh.CalculatePath(from.position, hit.position, NavMesh.AllAreas, dropPath) || dropPath.status != NavMeshPathStatus.PathComplete) return false;
            result = hit.position;
            return true;
        }

        /// <summary>Consumível posicionado (sem salto): usado pelo roteiro de referência e por baús.</summary>
        public Pickup SpawnConsumable(string id, Vector3 position)
        {
            var db = Services.Database;
            var def = db != null ? db.GetConsumable(id) : null;
            if (def == null) { Debug.LogWarning($"[Loot] Consumível desconhecido: {id}"); return null; }
            var go = new GameObject("Consumivel_" + id);
            go.transform.SetParent(root, false);
            Vector3 p = position;
            if (NavMesh.SamplePosition(position, out var hit, 1.5f, NavMesh.AllAreas)) p = hit.position;
            go.transform.position = p;
            var pk = go.AddComponent<Pickup>();
            pk.consumable = def;
            var v = new GameObject("Visual");
            v.transform.SetParent(go.transform, false);
            v.layer = Layers.Pickup;
            if (def.worldMesh != null)
            {
                v.AddComponent<MeshFilter>().sharedMesh = def.worldMesh;
                var mr = v.AddComponent<MeshRenderer>();
                mr.sharedMaterial = db.itemMaterial;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
            var lg = new GameObject("Brilho");
            lg.transform.SetParent(v.transform, false);
            lg.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = Color.Lerp(def.liquidColor, Color.white, 0.4f);
            l.range = 2.6f;
            l.intensity = 2.2f;
            l.shadows = LightShadows.None;
            pk.visual = v.transform;
            pk.Setup(this, PickupKind.Consumable, null, 1, 1, p, p);
            pk.PlaceStatic();
            dynamic.Add(pk);
            return pk;
        }

        public static Transform BuildVisual(Transform parent, PickupKind kind, ItemDefinition def)
        {
            var db = Services.Database;
            var v = new GameObject("Visual");
            v.transform.SetParent(parent, false);
            v.layer = Layers.Pickup;
            Mesh mesh = null;
            Material mat = db != null ? db.itemMaterial : null;
            float scale = 0.9f;
            switch (kind)
            {
                case PickupKind.Item:
                    mesh = def != null ? def.worldMesh : null;
                    if (def != null && def.kind == ItemKind.Armor) mat = db.itemMaterial;
                    else if (def != null && def.worldMaterial != null) mat = def.worldMaterial;
                    break;
                default:
                    if (db != null)
                        mesh = kind == PickupKind.Emeralds ? db.emeraldMesh : kind == PickupKind.Arrows ? db.arrowsMesh : db.healthOrbMesh;
                    scale = kind == PickupKind.Emeralds ? 0.55f : 0.7f;
                    break;
            }
            if (mesh != null)
            {
                v.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = v.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
            v.transform.localScale = Vector3.one * scale;
            return v.transform;
        }

        public void OnPickupReleased(Pickup p)
        {
            dynamic.Remove(p);
            if (p != null) Destroy(p.gameObject);
        }

        public void ClearDynamic()
        {
            for (int i = dynamic.Count - 1; i >= 0; i--)
            {
                var p = dynamic[i];
                if (p != null) Destroy(p.gameObject);
            }
            dynamic.Clear();
        }
    }
}

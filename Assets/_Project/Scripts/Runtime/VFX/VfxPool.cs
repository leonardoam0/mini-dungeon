using System.Collections.Generic;
using UnityEngine;

namespace Ruinas
{
    /// <summary>Pool de efeitos por chave (VfxLibrary) e de avisos no chão. Pertence à cena atual.</summary>
    public class VfxPool : MonoBehaviour
    {
        VfxLibrary library;
        readonly Dictionary<string, Stack<PooledVfx>> pools = new Dictionary<string, Stack<PooledVfx>>();
        readonly Dictionary<string, int> counts = new Dictionary<string, int>();
        readonly List<PooledVfx> active = new List<PooledVfx>();
        readonly Stack<Telegraph> telegraphPool = new Stack<Telegraph>();
        readonly List<Telegraph> telegraphs = new List<Telegraph>();
        Transform root;

        public float FlashScale { get; set; } = 1f;
        public int ActiveCount => active.Count;
        public int ActiveTelegraphs => telegraphs.Count;

        public void Init(VfxLibrary lib)
        {
            library = lib;
            root = new GameObject("[VFX]").transform;
            root.SetParent(transform, false);
            if (library?.entries == null) return;
            foreach (var e in library.entries)
            {
                if (e == null || e.prefab == null) continue;
                for (int i = 0; i < e.prewarm; i++)
                {
                    var v = Create(e);
                    if (v == null) break;
                    v.gameObject.SetActive(false);
                    GetStack(e.key).Push(v);
                }
            }
        }

        Stack<PooledVfx> GetStack(string key)
        {
            if (!pools.TryGetValue(key, out var s))
            {
                s = new Stack<PooledVfx>();
                pools[key] = s;
            }
            return s;
        }

        PooledVfx Create(VfxEntry e)
        {
            counts.TryGetValue(e.key, out int c);
            if (c >= e.maxInstances) return null;
            counts[e.key] = c + 1;
            var go = Instantiate(e.prefab, root);
            go.name = "VFX_" + e.key;
            go.layer = Layers.Vfx;
            var v = go.GetComponent<PooledVfx>();
            if (v == null) v = go.AddComponent<PooledVfx>();
            v.Bind(this, e.key);
            return v;
        }

        /// <summary>
        /// lifetime: 0 = duração padrão da biblioteca; negativo = até StopAndRelease; positivo = explícita.
        /// </summary>
        public PooledVfx Spawn(string key, Vector3 position, Quaternion rotation, float lifetime = 0f, VfxParams p = default)
        {
            if (library == null || string.IsNullOrEmpty(key)) return null;
            var e = library.Get(key);
            if (e == null || e.prefab == null) return null;
            var stack = GetStack(key);
            PooledVfx v = stack.Count > 0 ? stack.Pop() : Create(e);
            if (v == null)
            {
                // Limite atingido: recicla a instância mais antiga desta chave.
                for (int i = 0; i < active.Count; i++)
                {
                    if (active[i].Key != key) continue;
                    v = active[i];
                    active.RemoveAt(i);
                    break;
                }
                if (v == null) return null;
            }
            v.transform.SetParent(root, false);
            v.transform.SetPositionAndRotation(position, rotation);
            v.transform.localScale = Vector3.one;
            v.gameObject.SetActive(true);
            if (p.intensity <= 0f) p.intensity = 1f;
            v.Play(lifetime == 0f ? e.lifetime : lifetime, p);
            active.Add(v);
            return v;
        }

        public PooledVfx SpawnAttached(string key, Transform target, Vector3 offset, float lifetime = -1f, VfxParams p = default)
        {
            if (target == null) return null;
            var v = Spawn(key, target.position + offset, Quaternion.identity, lifetime, p);
            if (v != null) v.Follow(target, offset);
            return v;
        }

        public void Release(PooledVfx v)
        {
            if (v == null) return;
            active.Remove(v);
            v.gameObject.SetActive(false);
            GetStack(v.Key).Push(v);
        }

        public Telegraph ShowTelegraph(Vector3 position, float radius, float duration, Color color, bool dashed = false, bool fill = true)
        {
            Telegraph t;
            if (telegraphPool.Count > 0)
            {
                t = telegraphPool.Pop();
                t.gameObject.SetActive(true);
            }
            else
            {
                var go = new GameObject("Aviso");
                go.transform.SetParent(root, false);
                t = go.AddComponent<Telegraph>();
                t.Init(this, library != null ? library.quadMesh : null, library != null ? library.telegraphMaterial : null);
            }
            telegraphs.Add(t);
            t.Show(position, radius, duration, color, dashed, fill);
            return t;
        }

        public void ReleaseTelegraph(Telegraph t)
        {
            if (!telegraphs.Remove(t)) return;
            t.gameObject.SetActive(false);
            telegraphPool.Push(t);
        }

        public void ClearTelegraphs()
        {
            for (int i = telegraphs.Count - 1; i >= 0; i--) telegraphs[i].Hide();
        }

        public void ClearAll()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var v = active[i];
                active.RemoveAt(i);
                if (v == null) continue;
                v.gameObject.SetActive(false);
                GetStack(v.Key).Push(v);
            }
            for (int i = telegraphs.Count - 1; i >= 0; i--) telegraphs[i].Hide();
        }
    }
}

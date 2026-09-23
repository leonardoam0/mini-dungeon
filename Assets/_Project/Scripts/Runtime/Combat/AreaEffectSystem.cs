using System.Collections.Generic;
using UnityEngine;

namespace Ruinas
{
    /// <summary>Pool das áreas de efeito; ClearAll é chamado em reinícios e mortes para não deixar ataques órfãos.</summary>
    public class AreaEffectSystem : MonoBehaviour
    {
        readonly Stack<AreaEffect> pool = new Stack<AreaEffect>();
        readonly List<AreaEffect> active = new List<AreaEffect>();
        Transform root;

        public int ActiveCount => active.Count;
        public IReadOnlyList<AreaEffect> Active => active;

        public void Init()
        {
            root = new GameObject("[Areas]").transform;
            root.SetParent(transform, false);
        }

        public AreaEffect Spawn(Actor owner, Team team, Vector3 position, AreaEffectSpec spec, float damageScale = 1f)
        {
            if (spec == null) return null;
            AreaEffect a;
            if (pool.Count > 0)
            {
                a = pool.Pop();
                a.gameObject.SetActive(true);
            }
            else
            {
                var go = new GameObject("Area");
                go.transform.SetParent(root, false);
                a = go.AddComponent<AreaEffect>();
            }
            active.Add(a);
            a.Begin(this, owner, team, position, spec, damageScale);
            return a;
        }

        public void Release(AreaEffect a)
        {
            if (a == null || !active.Remove(a)) return;
            a.ForceRelease();
            a.gameObject.SetActive(false);
            pool.Push(a);
        }

        public void ClearAll()
        {
            for (int i = active.Count - 1; i >= 0; i--) Release(active[i]);
        }

        /// <summary>Remove áreas dentro de um raio (ex.: arena reiniciada).</summary>
        public void ClearInRadius(Vector3 center, float radius)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var a = active[i];
                Vector3 d = a.transform.position - center;
                d.y = 0f;
                if (d.magnitude <= radius) Release(a);
            }
        }
    }
}

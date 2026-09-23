using System.Collections.Generic;
using UnityEngine;

namespace Ruinas
{
    /// <summary>Limites de câmera de uma sala: o foco é mantido dentro da caixa (plano XZ).</summary>
    public class RoomBounds : MonoBehaviour
    {
        public static readonly List<RoomBounds> All = new List<RoomBounds>();

        public Vector3 size = new Vector3(16f, 10f, 16f);
        public int priority;
        public float margin = 1f;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public Bounds WorldBounds => new Bounds(transform.position, size);

        public bool Contains(Vector3 p)
        {
            var b = WorldBounds;
            return p.x >= b.min.x && p.x <= b.max.x && p.z >= b.min.z && p.z <= b.max.z && p.y >= b.min.y - 2f && p.y <= b.max.y + 2f;
        }

        public Vector3 Clamp(Vector3 p)
        {
            var b = WorldBounds;
            p.x = Mathf.Clamp(p.x, b.min.x + margin, Mathf.Max(b.min.x + margin, b.max.x - margin));
            p.z = Mathf.Clamp(p.z, b.min.z + margin, Mathf.Max(b.min.z + margin, b.max.z - margin));
            return p;
        }

        public static RoomBounds Find(Vector3 p)
        {
            RoomBounds best = null;
            foreach (var r in All)
                if (r.Contains(p) && (best == null || r.priority > best.priority)) best = r;
            return best;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.5f);
            Gizmos.DrawWireCube(transform.position, size);
        }
    }
}

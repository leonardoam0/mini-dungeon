using UnityEngine;
using UnityEngine.AI;

namespace Ruinas
{
    /// <summary>
    /// Espaços de ataque ao redor do jogador: anel corpo a corpo e anel de disparo. Cada inimigo reserva um
    /// espaço e caminha até ele, evitando que o grupo se funda num único ponto.
    /// </summary>
    public class AttackSlotManager : MonoBehaviour
    {
        public int meleeSlots = 6;
        public float meleeRadius = 1.35f;
        public int rangedSlots = 8;
        public float rangedRadius = 7.5f;

        Object[] meleeOwners;
        Object[] rangedOwners;

        void Awake()
        {
            meleeOwners = new Object[meleeSlots];
            rangedOwners = new Object[rangedSlots];
        }

        public int Request(Object owner, bool ranged, Vector3 fromPosition)
        {
            var owners = ranged ? rangedOwners : meleeOwners;
            for (int i = 0; i < owners.Length; i++) if (owners[i] == owner) return i;
            int best = -1;
            float bestD = float.MaxValue;
            for (int i = 0; i < owners.Length; i++)
            {
                if (owners[i] != null) continue;
                float d = (SlotPosition(i, ranged) - fromPosition).sqrMagnitude;
                if (d < bestD) { bestD = d; best = i; }
            }
            if (best >= 0) owners[best] = owner;
            return best;
        }

        public void Release(Object owner)
        {
            if (meleeOwners == null) return;
            for (int i = 0; i < meleeOwners.Length; i++) if (meleeOwners[i] == owner) meleeOwners[i] = null;
            for (int i = 0; i < rangedOwners.Length; i++) if (rangedOwners[i] == owner) rangedOwners[i] = null;
        }

        public void ReleaseAll()
        {
            if (meleeOwners == null) return;
            for (int i = 0; i < meleeOwners.Length; i++) meleeOwners[i] = null;
            for (int i = 0; i < rangedOwners.Length; i++) rangedOwners[i] = null;
        }

        public Vector3 SlotPosition(int index, bool ranged)
        {
            int n = ranged ? rangedSlots : meleeSlots;
            float r = ranged ? rangedRadius : meleeRadius;
            float a = (index / (float)n) * Mathf.PI * 2f + (ranged ? 0.3f : 0f);
            Vector3 p = transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r;
            if (NavMesh.SamplePosition(p, out var hit, 1.5f, NavMesh.AllAreas)) return hit.position;
            return p;
        }

        public int OccupiedMelee
        {
            get
            {
                int c = 0;
                if (meleeOwners != null) foreach (var o in meleeOwners) if (o != null) c++;
                return c;
            }
        }
    }
}

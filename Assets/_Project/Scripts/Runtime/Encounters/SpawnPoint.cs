using UnityEngine;

namespace Ruinas
{
    /// <summary>Ponto de surgimento de inimigos de um encontro (grupo opcional).</summary>
    public class SpawnPoint : MonoBehaviour
    {
        public string group = "";
        public float radius = 1.2f;

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.1f, radius);
        }
    }
}

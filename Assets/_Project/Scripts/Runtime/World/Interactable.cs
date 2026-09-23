using System.Collections.Generic;
using UnityEngine;

namespace Ruinas
{
    /// <summary>Base de objetos com interação (tecla E / gatilho esquerdo / clique).</summary>
    public abstract class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();

        public float radius = 1.9f;
        public string prompt = "Interagir";
        public Transform highlightTarget;

        public bool Highlighted { get; private set; }

        protected virtual void OnEnable() => All.Add(this);
        protected virtual void OnDisable() => All.Remove(this);

        public virtual bool CanInteract(PlayerController player) => isActiveAndEnabled;
        public abstract void Interact(PlayerController player);
        public virtual string Prompt => prompt;

        public virtual void SetHighlighted(bool on) => Highlighted = on;

        public static Interactable FindNear(Vector3 position, float maxDistance, PlayerController player = null)
        {
            Interactable best = null;
            float bestD = maxDistance;
            foreach (var it in All)
            {
                if (it == null || (player != null && !it.CanInteract(player))) continue;
                Vector3 d = it.transform.position - position;
                if (Mathf.Abs(d.y) > 2.5f) continue;
                d.y = 0f;
                float dist = d.magnitude;
                if (dist <= Mathf.Min(bestD, player != null ? it.radius : maxDistance)) { bestD = dist; best = it; }
            }
            return best;
        }
    }
}

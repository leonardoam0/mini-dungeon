using System;
using UnityEngine;

namespace Ruinas
{
    public enum RouteAction { GoTo, OpenChest, ClearEncounter, Exit }

    [Serializable]
    public class RouteStep
    {
        public string label;
        public Transform point;
        public RouteAction action;
        public Chest chest;
        public ArenaDirector encounter;
    }

    /// <summary>Rota do piloto automático de validação (percorre a missão do início ao fim).</summary>
    public class AutoPilotRoute : MonoBehaviour
    {
        public RouteStep[] steps;

        void OnDrawGizmosSelected()
        {
            if (steps == null) return;
            Gizmos.color = Color.cyan;
            for (int i = 0; i < steps.Length - 1; i++)
                if (steps[i]?.point != null && steps[i + 1]?.point != null)
                    Gizmos.DrawLine(steps[i].point.position + Vector3.up, steps[i + 1].point.position + Vector3.up);
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ruinas
{
    public enum ZoneAction { None, ActivateEncounter, Objective, Hint, Checkpoint }

    /// <summary>
    /// Volume de gatilho verificado por posição (sem física): ativa encontros, objetivos e dicas.
    /// Determinístico e sem dependência de Rigidbody.
    /// </summary>
    public class TriggerZone : MonoBehaviour
    {
        public static readonly List<TriggerZone> All = new List<TriggerZone>();

        public string zoneId;
        public Vector3 size = new Vector3(4f, 4f, 4f);
        public ZoneAction action;
        public ArenaDirector encounter;
        [TextArea] public string text;
        public bool once = true;

        public bool Fired { get; private set; }
        public bool PlayerInside { get; private set; }
        public event Action<TriggerZone> Entered;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public Bounds WorldBounds => new Bounds(transform.position, size);

        public bool Contains(Vector3 p)
        {
            var b = WorldBounds;
            return p.x >= b.min.x && p.x <= b.max.x && p.y >= b.min.y && p.y <= b.max.y && p.z >= b.min.z && p.z <= b.max.z;
        }

        public void ResetZone()
        {
            Fired = false;
            PlayerInside = false;
        }

        public void MarkFired() => Fired = true;

        void Update()
        {
            var ctx = LevelContext.Current;
            var player = ctx != null ? ctx.Player : null;
            if (player == null || player.IsDefeated) return;
            bool inside = Contains(player.transform.position);
            if (inside && !PlayerInside)
            {
                PlayerInside = true;
                if (!(once && Fired))
                {
                    Fired = true;
                    Fire(ctx);
                }
            }
            else if (!inside) PlayerInside = false;
        }

        void Fire(LevelContext ctx)
        {
            Entered?.Invoke(this);
            switch (action)
            {
                case ZoneAction.ActivateEncounter:
                    if (encounter != null) encounter.Activate();
                    break;
                case ZoneAction.Objective:
                    ctx.Mission?.OnObjectiveZone(this);
                    break;
                case ZoneAction.Hint:
                    ctx.Events.ShowHint(zoneId, text);
                    break;
            }
        }

        void OnDrawGizmos()
        {
            Gizmos.color = action == ZoneAction.ActivateEncounter ? new Color(1f, 0.3f, 0.3f, 0.4f) : new Color(1f, 0.9f, 0.3f, 0.35f);
            Gizmos.DrawWireCube(transform.position, size);
        }
    }
}

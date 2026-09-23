using System.Collections.Generic;
using UnityEngine;

namespace Ruinas
{
    /// <summary>Ponto de retorno após derrota. Ativado por proximidade; o MissionDirector salva o progresso.</summary>
    public class Checkpoint : MonoBehaviour
    {
        public static readonly List<Checkpoint> All = new List<Checkpoint>();

        public string checkpointId;
        public int order;
        public float activationRadius = 2.6f;
        public Light flame;
        public Renderer banner;
        public bool Activated { get; private set; }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public Vector3 SpawnPosition => transform.position + transform.forward * 1.2f;

        public void SetActivated(bool on, bool silent)
        {
            if (Activated == on) return;
            Activated = on;
            if (flame != null) flame.enabled = on;
            if (on && !silent)
            {
                var ctx = LevelContext.Current;
                ctx?.Vfx.Spawn("checkpoint_flare", transform.position + Vector3.up * 1.2f, Quaternion.identity);
                Services.Audio?.Play("checkpoint", transform.position);
            }
        }

        public static Checkpoint Find(string id)
        {
            foreach (var c in All) if (c != null && c.checkpointId == id) return c;
            return null;
        }
    }
}

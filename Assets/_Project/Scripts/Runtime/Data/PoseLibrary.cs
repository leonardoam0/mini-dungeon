using System;
using UnityEngine;

namespace Ruinas
{
    public enum Ease { Linear, InQuad, OutQuad, InOutQuad, OutBack, Hold }

    /// <summary>Uma pose-chave: rotações locais (graus) de cada articulação do rig de peças rígidas.</summary>
    [Serializable]
    public struct PoseKey
    {
        public float time;
        public Vector3 body;
        public Vector3 head;
        public Vector3 armL;
        public Vector3 armR;
        public Vector3 legL;
        public Vector3 legR;
        public Vector3 rootOffset;
        public Vector3 rootTilt;
        public Ease ease;
    }

    [Serializable]
    public class PoseClip
    {
        public string id;
        public float duration = 0.5f;
        public bool loop;
        [Tooltip("Mantém as pernas na locomoção (ex.: disparo andando, beber poção).")] public bool upperBodyOnly;
        public float blendIn = 0.06f;
        public float blendOut = 0.1f;
        public PoseKey[] keys;
    }

    [CreateAssetMenu(menuName = "Ruinas/Pose Library")]
    public class PoseLibrary : ScriptableObject
    {
        public PoseClip[] clips;

        [NonSerialized] System.Collections.Generic.Dictionary<string, PoseClip> map;

        public PoseClip Get(string id)
        {
            if (string.IsNullOrEmpty(id) || clips == null) return null;
            if (map == null)
            {
                map = new System.Collections.Generic.Dictionary<string, PoseClip>();
                foreach (var c in clips) if (c != null && !string.IsNullOrEmpty(c.id)) map[c.id] = c;
            }
            return map.TryGetValue(id, out var clip) ? clip : null;
        }

        public void InvalidateCache() => map = null;
    }
}

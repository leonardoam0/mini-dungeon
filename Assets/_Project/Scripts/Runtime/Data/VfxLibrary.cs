using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ruinas
{
    [Serializable]
    public class VfxEntry
    {
        public string key;
        public GameObject prefab;
        public float lifetime = 1.5f;
        public int prewarm = 2;
        public int maxInstances = 24;
    }

    [CreateAssetMenu(menuName = "Ruinas/Vfx Library")]
    public class VfxLibrary : ScriptableObject
    {
        public VfxEntry[] entries;
        [Header("Materiais compartilhados")]
        public Material telegraphMaterial;
        public Material glowAdditive;
        public Material particleCube;
        public Mesh cubeMesh;
        public Mesh quadMesh;
        public Mesh sphereMesh;

        [NonSerialized] Dictionary<string, VfxEntry> map;

        public VfxEntry Get(string key)
        {
            if (string.IsNullOrEmpty(key) || entries == null) return null;
            if (map == null)
            {
                map = new Dictionary<string, VfxEntry>();
                foreach (var e in entries) if (e != null && !string.IsNullOrEmpty(e.key)) map[e.key] = e;
            }
            return map.TryGetValue(key, out var v) ? v : null;
        }
    }
}

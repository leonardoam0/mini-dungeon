using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ruinas
{
    public enum AudioCategory { Sfx, Ui, Music, Ambience }

    [Serializable]
    public class SfxEntry
    {
        public string key;
        public AudioClip[] clips;
        public AudioCategory category = AudioCategory.Sfx;
        [Range(0, 1.5f)] public float volume = 0.8f;
        public float pitchMin = 0.95f;
        public float pitchMax = 1.05f;
        public int maxVoices = 4;
        [Range(0, 1)] public float spatial = 0.65f;
        public float minDistance = 4f;
        public float maxDistance = 40f;
        [Tooltip("Menor = mais importante (alertas de jogabilidade).")] [Range(0, 256)] public int priority = 128;
        public bool loop;
        [Tooltip("Tempo mínimo entre duas reproduções desta chave (evita duplicação por evento).")] public float minInterval = 0.03f;
    }

    [CreateAssetMenu(menuName = "Ruinas/Sfx Library")]
    public class SfxLibrary : ScriptableObject
    {
        public SfxEntry[] entries;
        [NonSerialized] Dictionary<string, SfxEntry> map;

        public SfxEntry Get(string key)
        {
            if (string.IsNullOrEmpty(key) || entries == null) return null;
            if (map == null)
            {
                map = new Dictionary<string, SfxEntry>();
                foreach (var e in entries) if (e != null && !string.IsNullOrEmpty(e.key)) map[e.key] = e;
            }
            return map.TryGetValue(key, out var v) ? v : null;
        }
    }
}

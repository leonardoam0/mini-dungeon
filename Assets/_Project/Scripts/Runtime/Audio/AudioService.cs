using System.Collections.Generic;
using UnityEngine;

namespace Ruinas
{
    /// <summary>
    /// Reprodução centralizada: vozes limitadas por chave, intervalo mínimo por chave (evita duplicação
    /// por evento), variação discreta de tom/volume, categorias com volume separado e pausa coerente.
    /// </summary>
    public class AudioService : MonoBehaviour
    {
        const int PoolSize = 28;

        class Voice
        {
            public AudioSource source;
            public string key;
            public float endTime;
            public int priority;
            public AudioCategory category;
            public float baseVolume;
        }

        SfxLibrary library;
        SettingsService settings;
        readonly List<Voice> voices = new List<Voice>();
        readonly Dictionary<string, float> lastPlay = new Dictionary<string, float>();
        readonly Dictionary<string, int> activeCount = new Dictionary<string, int>();
        AudioSource musicA, musicB, ambience;
        float musicBase = 0.6f, ambienceBase = 0.6f;
        string currentMusic, currentAmbience;
        AudioSource activeMusic;
        float musicFade = 1f;
        float musicFadeSpeed = 0.7f;
        GameRandom rng = new GameRandom(4242);

        public void Init(SfxLibrary lib, SettingsService settingsService)
        {
            library = lib;
            settings = settingsService;
            for (int i = 0; i < PoolSize; i++)
            {
                var go = new GameObject("Voz" + i);
                go.transform.SetParent(transform, false);
                var s = go.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.rolloffMode = AudioRolloffMode.Linear;
                s.dopplerLevel = 0f;
                voices.Add(new Voice { source = s });
            }
            musicA = CreateLoopSource("MusicaA");
            musicB = CreateLoopSource("MusicaB");
            ambience = CreateLoopSource("Ambiente");
            activeMusic = musicA;
            if (settings != null) settings.Changed += ApplyVolumes;
        }

        AudioSource CreateLoopSource(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.loop = true;
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            s.ignoreListenerPause = true;
            return s;
        }

        float CategoryVolume(AudioCategory c)
        {
            if (settings == null) return 1f;
            var d = settings.Data;
            switch (c)
            {
                case AudioCategory.Music: return d.masterVolume * d.musicVolume;
                case AudioCategory.Ambience: return d.masterVolume * d.ambienceVolume;
                case AudioCategory.Ui: return d.masterVolume * d.uiVolume;
                default: return d.masterVolume * d.sfxVolume;
            }
        }

        void ApplyVolumes()
        {
            foreach (var v in voices)
                if (v.source.isPlaying) v.source.volume = v.baseVolume * CategoryVolume(v.category);
            UpdateMusicVolumes();
            if (ambience != null) ambience.volume = ambienceBase * CategoryVolume(AudioCategory.Ambience);
        }

        public void PlayUi(string key) => PlayInternal(key, null, 1f, 1f, true);

        public void Play(string key, Vector3 position, float volumeScale = 1f, float pitchScale = 1f)
            => PlayInternal(key, position, volumeScale, pitchScale, false);

        public void Play2D(string key, float volumeScale = 1f, float pitchScale = 1f)
            => PlayInternal(key, null, volumeScale, pitchScale, false);

        void PlayInternal(string key, Vector3? position, float volumeScale, float pitchScale, bool ui)
        {
            if (library == null || string.IsNullOrEmpty(key)) return;
            var e = library.Get(key);
            if (e == null || e.clips == null || e.clips.Length == 0) return;

            float now = Time.unscaledTime;
            if (lastPlay.TryGetValue(key, out var last) && now - last < e.minInterval) return;
            activeCount.TryGetValue(key, out int count);
            if (count >= e.maxVoices) return;

            var voice = FindVoice(e.priority);
            if (voice == null) return;
            if (voice.source.isPlaying && voice.key != null) Decrement(voice.key);

            var clip = e.clips[e.clips.Length == 1 ? 0 : rng.Range(0, e.clips.Length)];
            var s = voice.source;
            s.clip = clip;
            s.loop = false;
            s.priority = e.priority;
            s.pitch = rng.Range(e.pitchMin, e.pitchMax) * pitchScale;
            voice.baseVolume = e.volume * volumeScale * rng.Range(0.92f, 1f);
            voice.category = ui ? AudioCategory.Ui : e.category;
            s.volume = voice.baseVolume * CategoryVolume(voice.category);
            s.ignoreListenerPause = ui || e.category == AudioCategory.Ui;
            if (position.HasValue && !ui)
            {
                s.spatialBlend = e.spatial;
                s.minDistance = e.minDistance;
                s.maxDistance = e.maxDistance;
                s.transform.position = position.Value;
            }
            else
            {
                s.spatialBlend = 0f;
                s.transform.localPosition = Vector3.zero;
            }
            s.Play();
            voice.key = key;
            voice.priority = e.priority;
            voice.endTime = now + clip.length / Mathf.Max(0.1f, Mathf.Abs(s.pitch));
            lastPlay[key] = now;
            activeCount[key] = count + 1;
        }

        Voice FindVoice(int priority)
        {
            Voice best = null;
            foreach (var v in voices)
                if (!v.source.isPlaying) return v;
            // Rouba a voz menos importante que termina primeiro.
            foreach (var v in voices)
            {
                if (v.priority < priority) continue;
                if (best == null || v.priority > best.priority || (v.priority == best.priority && v.endTime < best.endTime)) best = v;
            }
            if (best != null) best.source.Stop();
            return best;
        }

        void Decrement(string key)
        {
            if (activeCount.TryGetValue(key, out int c)) activeCount[key] = Mathf.Max(0, c - 1);
        }

        void Update()
        {
            float now = Time.unscaledTime;
            foreach (var v in voices)
            {
                if (v.key != null && !v.source.isPlaying && !AudioListener.pause)
                {
                    Decrement(v.key);
                    v.key = null;
                }
                else if (v.key != null && now > v.endTime + 0.5f && !v.source.isPlaying)
                {
                    Decrement(v.key);
                    v.key = null;
                }
            }

            if (musicFade < 1f)
            {
                musicFade = Mathf.Min(1f, musicFade + Time.unscaledDeltaTime * musicFadeSpeed);
                UpdateMusicVolumes();
                if (musicFade >= 1f)
                {
                    var other = activeMusic == musicA ? musicB : musicA;
                    other.Stop();
                }
            }
        }

        void UpdateMusicVolumes()
        {
            float vol = musicBase * CategoryVolume(AudioCategory.Music);
            var other = activeMusic == musicA ? musicB : musicA;
            if (activeMusic != null) activeMusic.volume = vol * musicFade;
            if (other != null) other.volume = vol * (1f - musicFade);
        }

        public void PlayMusic(string key, float fadeSeconds = 1.5f)
        {
            if (key == currentMusic) return;
            currentMusic = key;
            var e = library != null ? library.Get(key) : null;
            var next = activeMusic == musicA ? musicB : musicA;
            if (e == null || e.clips == null || e.clips.Length == 0)
            {
                next.Stop();
                activeMusic = next;
            }
            else
            {
                musicBase = e.volume;
                next.clip = e.clips[0];
                next.volume = 0f;
                next.Play();
                activeMusic = next;
            }
            musicFade = 0f;
            musicFadeSpeed = 1f / Mathf.Max(0.05f, fadeSeconds);
        }

        public void PlayAmbience(string key)
        {
            if (key == currentAmbience) return;
            currentAmbience = key;
            var e = library != null ? library.Get(key) : null;
            if (e == null || e.clips == null || e.clips.Length == 0)
            {
                ambience.Stop();
                return;
            }
            ambienceBase = e.volume;
            ambience.clip = e.clips[0];
            ambience.volume = ambienceBase * CategoryVolume(AudioCategory.Ambience);
            ambience.Play();
        }

        public void StopAllSfx()
        {
            foreach (var v in voices)
            {
                if (v.source.isPlaying) v.source.Stop();
                if (v.key != null) Decrement(v.key);
                v.key = null;
            }
        }
    }
}

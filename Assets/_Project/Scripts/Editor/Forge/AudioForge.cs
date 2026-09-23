using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Ruinas.EditorTools
{
    /// <summary>
    /// Direção sonora PROPOSTA (não houve identificação confiável do áudio original): efeitos e trilhas
    /// sintetizados por código, com variações por chave e volumes por categoria.
    /// </summary>
    public static class AudioForge
    {
        const string Dir = "Assets/_Project/Audio";
        const string LibraryPath = "Assets/_Project/Data/Audio/SfxLibrary.asset";

        class Def
        {
            public string key;
            public AudioCategory cat = AudioCategory.Sfx;
            public float volume = 0.8f, pitchMin = 0.95f, pitchMax = 1.05f, spatial = 0.6f;
            public int maxVoices = 4, priority = 128, variants = 1;
            public bool loop;
            public float minInterval = 0.03f;
            public Func<int, float[]> make;
        }

        static float[] Out(Synth s, float peak = 0.85f) { s.Normalize(peak); return s.Buf; }

        static readonly float[] Scale = { 146.83f, 174.61f, 196f, 220f, 261.63f, 293.66f, 349.23f, 392f, 440f, 523.25f };

        static List<Def> Defs()
        {
            var d = new List<Def>();
            void Add(string key, Func<int, float[]> make, float vol = 0.8f, int variants = 1, AudioCategory cat = AudioCategory.Sfx,
                int voices = 4, float pmin = 0.95f, float pmax = 1.05f, float spatial = 0.55f, int priority = 128, bool loop = false, float minInterval = 0.03f)
                => d.Add(new Def { key = key, make = make, volume = vol, variants = variants, cat = cat, maxVoices = voices, pitchMin = pmin, pitchMax = pmax, spatial = spatial, priority = priority, loop = loop, minInterval = minInterval });

            // Combate
            Add("swing", v => { var s = new Synth(0.24f, 10 + v); s.Noise(0, 0.24f, 1f, 0.05f, 0.08f); s.BandSweep(700f + v * 90f, 2600f, 1.4f); return Out(s, 0.7f); }, 0.55f, 3, voices: 4, pmin: 0.9f, pmax: 1.12f);
            Add("hit_flesh", v => { var s = new Synth(0.2f, 20 + v); s.Tone(0, 0.16f, 160f, 60f, 1f, Synth.Wave.Sine, 0.002f, 0.05f); s.Noise(0, 0.08f, 0.6f, 0.001f, 0.02f); s.LowPass(2200f); s.SoftClip(1.6f); return Out(s); }, 0.75f, 3, voices: 5, priority: 90);
            Add("hit_magic", v => { var s = new Synth(0.3f, 30 + v); s.Tone(0, 0.25f, 900f, 300f, 0.6f, Synth.Wave.Tri, 0.002f, 0.08f); s.Noise(0, 0.12f, 0.35f, 0.001f, 0.03f); s.Reverb(0.2f); return Out(s, 0.7f); }, 0.55f, 2, voices: 4);
            Add("arrow_hit", v => { var s = new Synth(0.16f, 40 + v); s.Noise(0, 0.05f, 1f, 0.001f, 0.012f); s.Tone(0, 0.1f, 420f, 180f, 0.5f, Synth.Wave.Tri, 0.001f, 0.03f); s.LowPass(3500f); return Out(s, 0.75f); }, 0.6f, 2, voices: 4);
            Add("bow_draw", v => { var s = new Synth(0.32f, 50); s.Noise(0, 0.3f, 0.5f, 0.12f, 0.2f); s.BandSweep(300f, 900f, 3f); s.Tone(0, 0.3f, 90f, 120f, 0.2f, Synth.Wave.Saw, 0.1f, 0.3f); s.LowPass(1500f); return Out(s, 0.5f); }, 0.4f, 1, voices: 2);
            Add("bow_release", v => { var s = new Synth(0.4f, 60 + v); s.Pluck(0, 110f + v * 8f, 0.35f, 1f, 0.985f); s.Noise(0, 0.06f, 0.4f, 0.001f, 0.02f); s.LowPass(4000f); return Out(s, 0.8f); }, 0.6f, 2, voices: 3);
            Add("roll", v => { var s = new Synth(0.3f, 70); s.Noise(0, 0.3f, 0.8f, 0.03f, 0.12f); s.BandSweep(1200f, 400f, 1.2f); s.Tone(0.2f, 0.08f, 120f, 70f, 0.5f, Synth.Wave.Sine, 0.001f, 0.03f); return Out(s, 0.6f); }, 0.45f, 1, voices: 2);
            Add("footstep", v => { var s = new Synth(0.09f, 80 + v); s.Noise(0, 0.07f, 1f, 0.001f, 0.018f); s.LowPass(900f + v * 150f); s.Tone(0, 0.05f, 110f, 80f, 0.4f, Synth.Wave.Sine, 0.001f, 0.015f); return Out(s, 0.6f); }, 0.25f, 4, voices: 2, pmin: 0.85f, pmax: 1.15f, minInterval: 0.1f);
            Add("player_death", v => { var s = new Synth(1.2f, 90); s.Tone(0, 1f, 330f, 110f, 0.6f, Synth.Wave.Tri, 0.01f, 0.4f); s.Tone(0.05f, 1f, 220f, 73f, 0.4f, Synth.Wave.Sine, 0.01f, 0.5f); s.Reverb(0.35f); return Out(s); }, 0.7f, 1, voices: 1, priority: 20);
            Add("heal", v => { var s = new Synth(0.8f, 100); for (int i = 0; i < 4; i++) s.Tone(i * 0.08f, 0.5f, Scale[4 + i], Scale[4 + i] * 1.01f, 0.35f, Synth.Wave.Sine, 0.005f, 0.2f); s.Reverb(0.35f); return Out(s, 0.7f); }, 0.6f, 1, voices: 2);
            Add("denied", v => { var s = new Synth(0.14f, 110); s.Tone(0, 0.12f, 180f, 150f, 0.6f, Synth.Wave.Square, 0.001f, 0.05f); s.LowPass(900f); return Out(s, 0.5f); }, 0.35f, 1, AudioCategory.Ui, voices: 1, minInterval: 0.12f);

            // Artefatos
            Add("feather_leap", v => { var s = new Synth(0.5f, 120); s.Noise(0, 0.45f, 1f, 0.05f, 0.18f); s.BandSweep(600f, 3200f, 1.5f); for (int i = 0; i < 3; i++) s.Tone(i * 0.05f, 0.3f, 880f * (1 + i * 0.25f), 1400f, 0.15f, Synth.Wave.Sine, 0.002f, 0.1f); return Out(s, 0.7f); }, 0.6f);
            Add("feather_land", v => { var s = new Synth(0.6f, 130); s.Tone(0, 0.4f, 140f, 45f, 1f, Synth.Wave.Sine, 0.002f, 0.14f); s.Noise(0, 0.3f, 0.6f, 0.002f, 0.08f); s.LowPass(1400f); s.Reverb(0.2f); s.SoftClip(1.4f); return Out(s); }, 0.75f, priority: 60);
            Add("pulse_charge", v => { var s = new Synth(0.35f, 140); s.Tone(0, 0.32f, 220f, 880f, 0.6f, Synth.Wave.Saw, 0.2f, 0.3f); s.LowPass(2000f); s.Tone(0, 0.32f, 440f, 1760f, 0.25f, Synth.Wave.Sine, 0.2f, 0.3f); return Out(s, 0.6f); }, 0.5f);
            Add("pulse_blast", v => { var s = new Synth(1.3f, 150); s.Tone(0, 0.9f, 180f, 60f, 1f, Synth.Wave.Sine, 0.002f, 0.3f); s.Noise(0, 0.6f, 0.7f, 0.002f, 0.15f); s.BandSweep(3000f, 400f, 0.9f); s.Tone(0, 1.1f, 660f, 640f, 0.25f, Synth.Wave.Tri, 0.01f, 0.5f, 0.01f); s.Reverb(0.4f); s.SoftClip(1.3f); return Out(s); }, 0.8f, priority: 50);
            Add("summon", v => { var s = new Synth(1.0f, 160); int[] arp = { 4, 6, 7, 9 }; for (int i = 0; i < arp.Length; i++) s.Pluck(i * 0.09f, Scale[arp[i]], 0.6f, 0.6f, 0.993f); s.Noise(0, 0.4f, 0.2f, 0.1f, 0.2f); s.Reverb(0.4f); return Out(s, 0.75f); }, 0.6f);
            Add("toxic_puff", v => { var s = new Synth(0.5f, 170); s.Noise(0, 0.45f, 1f, 0.02f, 0.15f); s.LowPass(700f); s.Tone(0, 0.2f, 90f, 60f, 0.4f, Synth.Wave.Sine, 0.005f, 0.1f); return Out(s, 0.6f); }, 0.5f, voices: 2, minInterval: 0.2f);
            Add("llama_spit", v => { var s = new Synth(0.25f, 180); s.Noise(0, 0.2f, 1f, 0.005f, 0.05f); s.BandSweep(2500f, 900f, 2f); return Out(s, 0.6f); }, 0.5f, voices: 2);
            // Explosão e labareda (eventos da arena de referência).
            Add("explosion", v => { var s = new Synth(1.5f, 311); s.Noise(0, 1.3f, 1f, 0.004f, 0.55f); s.LowPass(850f); s.Tone(0, 0.9f, 85f, 32f, 1f, Synth.Wave.Sine, 0.003f, 0.45f); s.Noise(0, 0.25f, 0.5f, 0.002f, 0.06f); s.SoftClip(2.2f); s.Reverb(0.3f); return Out(s); }, 0.85f, voices: 2, priority: 60);
            Add("fire_roar", v => { var s = new Synth(1.1f, 312); s.Noise(0, 1f, 0.9f, 0.12f, 0.45f); s.BandSweep(500f, 1800f, 1.2f); s.Tone(0, 0.9f, 95f, 80f, 0.3f, Synth.Wave.Saw, 0.15f, 0.45f, 0.02f); s.LowPass(1500f); return Out(s, 0.7f); }, 0.6f, voices: 2);

            // Inimigos
            Add("zombie_groan", v => { var s = new Synth(0.8f, 190 + v); s.Tone(0, 0.75f, 95f + v * 10f, 70f, 0.8f, Synth.Wave.Saw, 0.08f, 0.4f, 0.03f); s.Noise(0, 0.7f, 0.25f, 0.05f, 0.3f); s.BandSweep(400f, 700f, 2.5f); s.SoftClip(1.5f); return Out(s, 0.7f); }, 0.45f, 3, voices: 2, pmin: 0.85f, pmax: 1.1f, minInterval: 0.3f);
            Add("skeleton_rattle", v => { var s = new Synth(0.4f, 200 + v); for (int i = 0; i < 6; i++) { float t = i * 0.05f + s.Rand01() * 0.02f; s.Tone(t, 0.04f, 1800f + s.Rand01() * 900f, 1200f, 0.4f, Synth.Wave.Square, 0.001f, 0.01f); } s.HighPass(600f); return Out(s, 0.6f); }, 0.4f, 2, voices: 2, minInterval: 0.3f);
            Add("brute_roar", v => { var s = new Synth(1.2f, 210); s.Tone(0, 1.1f, 70f, 50f, 1f, Synth.Wave.Saw, 0.1f, 0.6f, 0.04f); s.Noise(0, 1f, 0.4f, 0.1f, 0.5f); s.LowPass(800f); s.SoftClip(2f); s.Reverb(0.25f); return Out(s); }, 0.7f, voices: 1, priority: 40);
            Add("enemy_death", v => { var s = new Synth(0.5f, 220 + v); s.Noise(0, 0.4f, 1f, 0.005f, 0.1f); s.LowPass(1600f); s.Tone(0, 0.3f, 300f, 90f, 0.5f, Synth.Wave.Tri, 0.002f, 0.1f); for (int i = 0; i < 5; i++) s.Tone(0.05f + i * 0.04f, 0.05f, 900f + i * 150f, 700f, 0.12f, Synth.Wave.Square, 0.001f, 0.015f); return Out(s, 0.7f); }, 0.55f, 2, voices: 3);
            Add("vine_emerge", v => { var s = new Synth(0.7f, 230); s.Noise(0, 0.65f, 1f, 0.1f, 0.3f); s.LowPass(500f); s.Tone(0, 0.6f, 60f, 110f, 0.5f, Synth.Wave.Saw, 0.1f, 0.3f); s.LowPass(700f); return Out(s, 0.7f); }, 0.55f);
            Add("vine_charge", v => { var s = new Synth(0.9f, 240); s.Tone(0, 0.9f, 120f, 260f, 0.5f, Synth.Wave.Tri, 0.4f, 0.5f, 0.05f); s.Noise(0, 0.9f, 0.2f, 0.5f, 0.4f); s.LowPass(1200f); return Out(s, 0.55f); }, 0.45f, voices: 2);
            Add("vine_spit", v => { var s = new Synth(0.3f, 250); s.Tone(0, 0.12f, 300f, 120f, 0.8f, Synth.Wave.Sine, 0.001f, 0.04f); s.Noise(0, 0.15f, 0.6f, 0.001f, 0.04f); s.LowPass(1800f); return Out(s, 0.7f); }, 0.5f, voices: 2);
            Add("spore_splat", v => { var s = new Synth(0.5f, 260); s.Noise(0, 0.4f, 1f, 0.002f, 0.1f); s.LowPass(900f); s.Tone(0, 0.15f, 140f, 60f, 0.6f, Synth.Wave.Sine, 0.001f, 0.05f); return Out(s, 0.7f); }, 0.55f, voices: 3);

            // Mundo
            Add("chest_open", v => { var s = new Synth(1.0f, 270); s.Noise(0, 0.35f, 0.6f, 0.05f, 0.15f); s.BandSweep(300f, 800f, 4f); s.Tone(0.3f, 0.7f, Scale[5], Scale[5], 0.25f, Synth.Wave.Sine, 0.005f, 0.3f); s.Tone(0.38f, 0.6f, Scale[7], Scale[7], 0.25f, Synth.Wave.Sine, 0.005f, 0.3f); s.Tone(0.46f, 0.6f, Scale[9], Scale[9], 0.25f, Synth.Wave.Sine, 0.005f, 0.3f); s.Reverb(0.3f); return Out(s, 0.75f); }, 0.7f, voices: 1);
            Add("item_pickup", v => { var s = new Synth(0.5f, 280); s.Tone(0, 0.25f, Scale[7], Scale[7], 0.5f, Synth.Wave.Tri, 0.002f, 0.1f); s.Tone(0.07f, 0.35f, Scale[9], Scale[9], 0.5f, Synth.Wave.Tri, 0.002f, 0.15f); s.Reverb(0.2f); return Out(s, 0.7f); }, 0.6f, voices: 2);
            Add("emerald", v => { var s = new Synth(0.25f, 290 + v); s.Tone(0, 0.2f, 1568f + v * 120f, 1568f, 0.5f, Synth.Wave.Sine, 0.001f, 0.06f); s.Tone(0.04f, 0.18f, 2093f, 2093f, 0.3f, Synth.Wave.Sine, 0.001f, 0.05f); return Out(s, 0.6f); }, 0.45f, 3, voices: 3, minInterval: 0.05f);
            Add("arrow_pickup", v => { var s = new Synth(0.2f, 300); s.Noise(0, 0.08f, 0.6f, 0.001f, 0.02f); s.Tone(0, 0.12f, 900f, 1200f, 0.3f, Synth.Wave.Tri, 0.001f, 0.04f); return Out(s, 0.6f); }, 0.45f, voices: 2);
            Add("checkpoint", v => { var s = new Synth(1.4f, 310); int[] n = { 0, 2, 4, 7 }; for (int i = 0; i < n.Length; i++) s.Note(i * 0.12f, 0.6f, Scale[n[i]] * 2f, 0.2f, Synth.Wave.Tri, 0.01f, 0.2f, 0.5f, 0.5f); s.Reverb(0.45f); return Out(s, 0.7f); }, 0.6f, voices: 1);
            Add("portal", v => { var s = new Synth(1.8f, 320); s.Tone(0, 1.7f, 110f, 440f, 0.6f, Synth.Wave.Saw, 0.3f, 1f, 0.02f); s.LowPass(1500f); s.Noise(0, 1.5f, 0.3f, 0.5f, 0.8f); s.Reverb(0.5f); return Out(s, 0.75f); }, 0.7f, voices: 1);
            Add("gate_open", v => { var s = new Synth(1.6f, 330); s.Noise(0, 1.5f, 1f, 0.1f, 0.9f); s.LowPass(400f); s.Tone(0, 1.5f, 55f, 48f, 0.6f, Synth.Wave.Saw, 0.1f, 1f); s.LowPass(500f); s.SoftClip(1.5f); return Out(s, 0.8f); }, 0.7f, voices: 2, spatial: 0.4f);
            Add("gate_close", v => { var s = new Synth(1.0f, 340); s.Noise(0, 0.8f, 1f, 0.05f, 0.4f); s.LowPass(500f); s.Tone(0.7f, 0.3f, 90f, 40f, 1f, Synth.Wave.Sine, 0.001f, 0.1f); s.SoftClip(1.5f); return Out(s, 0.8f); }, 0.7f, voices: 2, spatial: 0.4f);
            Add("obelisk_activate", v => { var s = new Synth(2.4f, 350); s.Tone(0, 2.3f, 55f, 110f, 0.8f, Synth.Wave.Saw, 0.6f, 1.5f, 0.01f); s.LowPass(900f); s.Tone(0.5f, 1.8f, 440f, 880f, 0.2f, Synth.Wave.Sine, 0.5f, 1f); s.Reverb(0.5f); return Out(s, 0.8f); }, 0.75f, voices: 1, priority: 40, spatial: 0.3f);
            Add("obelisk_beam", v => { var s = new Synth(1.6f, 360); s.Tone(0, 1.5f, 330f, 340f, 0.4f, Synth.Wave.Tri, 0.15f, 0.8f, 0.03f); s.Tone(0, 1.5f, 495f, 500f, 0.25f, Synth.Wave.Sine, 0.15f, 0.8f); s.Noise(0, 1.4f, 0.15f, 0.2f, 0.8f); s.HighPass(200f); s.Reverb(0.45f); return Out(s, 0.65f); }, 0.5f, voices: 2, spatial: 0.35f);
            Add("encounter_clear", v => { var s = new Synth(2.2f, 370); int[] ch = { 0, 4, 7 }; foreach (var n in ch) s.Note(0, 1.2f, Scale[n + 1] * 2f, 0.2f, Synth.Wave.Tri, 0.02f, 0.3f, 0.6f, 0.9f, 0.004f); s.Tone(0.2f, 1.8f, Scale[9] * 2f, Scale[9] * 2f, 0.12f, Synth.Wave.Sine, 0.01f, 0.8f); s.Reverb(0.5f); return Out(s, 0.75f); }, 0.7f, voices: 1, priority: 30, spatial: 0.2f);
            Add("level_up", v => { var s = new Synth(1.4f, 380); int[] n = { 0, 2, 4, 7, 9 }; for (int i = 0; i < n.Length; i++) s.Tone(i * 0.08f, 0.6f, Scale[n[i]] * 2f, Scale[n[i]] * 2f, 0.35f, Synth.Wave.Tri, 0.003f, 0.25f); s.Reverb(0.4f); return Out(s, 0.8f); }, 0.6f, voices: 1, spatial: 0f);

            // Interface
            Add("ui_move", v => { var s = new Synth(0.05f, 390); s.Tone(0, 0.04f, 1200f, 1100f, 0.5f, Synth.Wave.Square, 0.001f, 0.012f); s.LowPass(3000f); return Out(s, 0.4f); }, 0.3f, 1, AudioCategory.Ui, voices: 2, minInterval: 0.04f);
            Add("ui_select", v => { var s = new Synth(0.12f, 400); s.Tone(0, 0.1f, 880f, 1320f, 0.5f, Synth.Wave.Square, 0.001f, 0.04f); s.LowPass(3500f); return Out(s, 0.5f); }, 0.35f, 1, AudioCategory.Ui, voices: 2);
            Add("ui_back", v => { var s = new Synth(0.12f, 410); s.Tone(0, 0.1f, 900f, 600f, 0.5f, Synth.Wave.Square, 0.001f, 0.04f); s.LowPass(3000f); return Out(s, 0.5f); }, 0.35f, 1, AudioCategory.Ui, voices: 2);
            Add("ui_open", v => { var s = new Synth(0.2f, 420); s.Noise(0, 0.15f, 0.5f, 0.02f, 0.05f); s.BandSweep(500f, 1500f, 2f); return Out(s, 0.45f); }, 0.35f, 1, AudioCategory.Ui, voices: 1);
            Add("notify", v => { var s = new Synth(0.35f, 430); s.Tone(0, 0.3f, 1046f, 1046f, 0.4f, Synth.Wave.Sine, 0.002f, 0.1f); s.Tone(0.06f, 0.3f, 1318f, 1318f, 0.3f, Synth.Wave.Sine, 0.002f, 0.1f); return Out(s, 0.5f); }, 0.35f, 1, AudioCategory.Ui, voices: 1, minInterval: 0.2f);
            Add("equip", v => { var s = new Synth(0.25f, 440); s.Noise(0, 0.08f, 0.6f, 0.001f, 0.02f); s.Tone(0, 0.2f, 700f, 500f, 0.4f, Synth.Wave.Tri, 0.001f, 0.06f); return Out(s, 0.55f); }, 0.45f, 1, AudioCategory.Ui, voices: 1);
            Add("salvage", v => { var s = new Synth(0.35f, 450); s.Noise(0, 0.25f, 0.7f, 0.001f, 0.06f); s.LowPass(2000f); s.Tone(0.1f, 0.2f, 1568f, 1568f, 0.3f, Synth.Wave.Sine, 0.001f, 0.06f); return Out(s, 0.55f); }, 0.45f, 1, AudioCategory.Ui, voices: 1);
            Add("buy", v => { var s = new Synth(0.3f, 460); s.Tone(0, 0.12f, 1318f, 1318f, 0.4f, Synth.Wave.Sine, 0.001f, 0.05f); s.Tone(0.08f, 0.2f, 1760f, 1760f, 0.4f, Synth.Wave.Sine, 0.001f, 0.06f); return Out(s, 0.55f); }, 0.45f, 1, AudioCategory.Ui, voices: 1);
            Add("upgrade", v => { var s = new Synth(0.6f, 470); for (int i = 0; i < 3; i++) s.Tone(i * 0.07f, 0.3f, 660f * Mathf.Pow(1.26f, i), 660f * Mathf.Pow(1.26f, i), 0.35f, Synth.Wave.Tri, 0.002f, 0.1f); s.Noise(0, 0.1f, 0.4f, 0.001f, 0.03f); s.Reverb(0.3f); return Out(s, 0.6f); }, 0.5f, 1, AudioCategory.Ui, voices: 1);

            // Ambiente e música (loops sem emenda)
            Add("amb_temple", v => Ambience(), 0.55f, 1, AudioCategory.Ambience, voices: 1, loop: true, spatial: 0f);
            Add("music_explore", v => MusicExplore(), 0.45f, 1, AudioCategory.Music, voices: 1, loop: true, spatial: 0f);
            Add("music_combat", v => MusicCombat(), 0.5f, 1, AudioCategory.Music, voices: 1, loop: true, spatial: 0f);
            Add("music_menu", v => MusicMenu(), 0.45f, 1, AudioCategory.Music, voices: 1, loop: true, spatial: 0f);
            return d;
        }

        static float[] Ambience()
        {
            float loop = 20f;
            var s = new Synth(loop + 3f, 900);
            // Vento: ruído filtrado com respiração lenta.
            var wind = new Synth(loop + 3f, 901);
            wind.Noise(0, loop + 3f, 0.6f, 0.01f, 999f);
            wind.LowPass(380f);
            for (int i = 0; i < wind.Len; i++) wind.Buf[i] *= 0.55f + 0.45f * Mathf.Sin(i / (float)Synth.SR * 2f * Mathf.PI / 10f);
            for (int i = 0; i < s.Len; i++) s.Buf[i] += wind.Buf[i];
            // Zumbido grave do templo.
            s.Note(0, loop + 2f, 55f, 0.18f, Synth.Wave.Sine, 2f, 0.1f, 1f, 1f, 0.003f);
            s.Note(0, loop + 2f, 82.4f, 0.08f, Synth.Wave.Tri, 2f, 0.1f, 1f, 1f, 0.002f);
            // Gotas esparsas.
            for (int i = 0; i < 12; i++)
            {
                float t = s.Rand01() * loop;
                float f = 1200f + s.Rand01() * 900f;
                s.Tone(t, 0.12f, f, f * 0.7f, 0.12f, Synth.Wave.Sine, 0.001f, 0.03f);
            }
            s.Reverb(0.45f, 0.86f);
            s.Normalize(0.6f);
            return s.Loop(loop);
        }

        static float[] MusicExplore()
        {
            float bpm = 76f, beat = 60f / bpm, bar = beat * 4f;
            int bars = 12;
            float loop = bar * bars;
            var s = new Synth(loop + 4f, 910);
            // Ré menor: Dm – Bb – F – C (graus do Scale: D=146.83 ... ).
            float[][] chords =
            {
                new[] { 146.83f, 174.61f, 220f },
                new[] { 116.54f, 146.83f, 174.61f },
                new[] { 174.61f, 220f, 261.63f },
                new[] { 130.81f, 164.81f, 196f },
            };
            for (int b = 0; b < bars; b++)
            {
                var ch = chords[b % 4];
                foreach (var f in ch) s.Note(b * bar, bar * 0.95f, f, 0.07f, Synth.Wave.Saw, 0.8f, 0.4f, 0.7f, 1.2f, 0.004f);
                s.Note(b * bar, bar * 0.9f, ch[0] * 0.5f, 0.12f, Synth.Wave.Tri, 0.1f, 0.5f, 0.6f, 0.6f);
                // Dedilhado esparso (pentatônica).
                for (int k = 0; k < 4; k++)
                {
                    if (s.Rand01() < 0.35f) continue;
                    float t = b * bar + k * beat + (s.Rand01() < 0.3f ? beat * 0.5f : 0f);
                    float f = Scale[4 + (int)(s.Rand01() * 5f)];
                    s.Pluck(t, f, 1.2f, 0.18f, 0.994f);
                }
            }
            s.LowPass(2600f);
            s.Reverb(0.4f, 0.84f);
            s.Normalize(0.7f);
            return s.Loop(loop);
        }

        static float[] MusicCombat()
        {
            float bpm = 118f, beat = 60f / bpm, bar = beat * 4f;
            int bars = 12;
            float loop = bar * bars;
            var s = new Synth(loop + 3f, 920);
            float[] bass = { 73.42f, 73.42f, 87.31f, 65.41f };
            for (int b = 0; b < bars; b++)
            {
                float root = bass[(b / 2) % 4];
                for (int k = 0; k < 8; k++)
                {
                    float t = b * bar + k * beat * 0.5f;
                    s.Note(t, beat * 0.4f, k % 4 == 3 ? root * 1.5f : root, 0.22f, Synth.Wave.Saw, 0.005f, 0.08f, 0.4f, 0.05f);
                }
                for (int k = 0; k < 4; k++)
                {
                    float t = b * bar + k * beat;
                    s.Tone(t, 0.25f, 120f, 45f, k % 2 == 0 ? 0.9f : 0.5f, Synth.Wave.Sine, 0.001f, 0.08f); // bumbo
                    s.Noise(t + beat * 0.5f, 0.06f, 0.18f, 0.001f, 0.015f);                               // chimbal
                    if (k == 1 || k == 3) s.Noise(t, 0.15f, 0.35f, 0.001f, 0.05f);                          // caixa
                }
                if (b % 2 == 0) s.Note(b * bar, bar * 1.8f, root * 4f, 0.05f, Synth.Wave.Square, 0.3f, 0.5f, 0.5f, 0.5f, 0.005f);
            }
            s.LowPass(3200f);
            s.Reverb(0.22f, 0.78f);
            s.SoftClip(1.2f);
            s.Normalize(0.72f);
            return s.Loop(loop);
        }

        static float[] MusicMenu()
        {
            float loop = 24f;
            var s = new Synth(loop + 5f, 930);
            float[] chord = { 146.83f, 220f, 293.66f, 349.23f };
            foreach (var f in chord) s.Note(0, loop, f, 0.06f, Synth.Wave.Tri, 4f, 1f, 0.8f, 4f, 0.003f);
            for (int i = 0; i < 14; i++)
            {
                float t = i * (loop / 14f) + s.Rand01() * 0.4f;
                float bell = Scale[5 + (int)(s.Rand01() * 5f)] * 2f;
                s.Tone(t, 2.5f, bell, bell, 0.12f, Synth.Wave.Sine, 0.002f, 0.8f);
            }
            for (int i = 0; i < s.Len; i++) if (float.IsNaN(s.Buf[i])) s.Buf[i] = 0f;
            s.Reverb(0.55f, 0.88f);
            s.Normalize(0.65f);
            return s.Loop(loop);
        }

        public static void Generate()
        {
            var defs = Defs();
            var entries = new List<SfxEntry>();
            foreach (var def in defs)
            {
                var clips = new List<string>();
                for (int v = 0; v < Mathf.Max(1, def.variants); v++)
                {
                    string sub = def.cat == AudioCategory.Music ? "Music" : def.cat == AudioCategory.Ambience ? "Ambience" : def.cat == AudioCategory.Ui ? "UI" : "Sfx";
                    string path = $"{Dir}/{sub}/{def.key}{(def.variants > 1 ? "_" + (v + 1) : "")}.wav";
                    Synth.WriteWav(path, def.make(v));
                    clips.Add(path);
                }
                entries.Add(new SfxEntry
                {
                    key = def.key,
                    category = def.cat,
                    volume = def.volume,
                    pitchMin = def.pitchMin,
                    pitchMax = def.pitchMax,
                    maxVoices = def.maxVoices,
                    spatial = def.spatial,
                    priority = def.priority,
                    loop = def.loop,
                    minInterval = def.minInterval,
                    clips = new AudioClip[0],
                });
                entries[entries.Count - 1].clips = new AudioClip[clips.Count];
                for (int i = 0; i < clips.Count; i++) entries[entries.Count - 1].clips[i] = null;
                pendingPaths[def.key] = clips;
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            foreach (var e in entries)
            {
                var paths = pendingPaths[e.key];
                for (int i = 0; i < paths.Count; i++)
                {
                    ConfigureImporter(paths[i], e.category);
                    e.clips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(paths[i]);
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(LibraryPath));
            var lib = AssetDatabase.LoadAssetAtPath<SfxLibrary>(LibraryPath);
            if (lib == null)
            {
                lib = ScriptableObject.CreateInstance<SfxLibrary>();
                AssetDatabase.CreateAsset(lib, LibraryPath);
            }
            lib.entries = entries.ToArray();
            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
        }

        static readonly Dictionary<string, List<string>> pendingPaths = new Dictionary<string, List<string>>();

        static void ConfigureImporter(string path, AudioCategory cat)
        {
            var ai = AssetImporter.GetAtPath(path) as AudioImporter;
            if (ai == null) return;
            ai.forceToMono = true;
            ai.loadInBackground = cat == AudioCategory.Music || cat == AudioCategory.Ambience;
            var s = ai.defaultSampleSettings;
            bool longClip = cat == AudioCategory.Music || cat == AudioCategory.Ambience;
            s.loadType = longClip ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            s.compressionFormat = longClip ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
            s.quality = 0.6f;
            ai.defaultSampleSettings = s;
            ai.SaveAndReimport();
        }

        public static SfxLibrary Library => AssetDatabase.LoadAssetAtPath<SfxLibrary>(LibraryPath);
    }
}

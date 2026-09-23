using System;
using System.IO;
using UnityEngine;

namespace Ruinas.EditorTools
{
    /// <summary>Síntese simples (osciladores, ruído, envelopes, filtros, Karplus-Strong, reverb) e gravação WAV.</summary>
    public class Synth
    {
        public const int SR = 44100;
        public readonly float[] Buf;
        readonly System.Random rng;

        public Synth(float seconds, int seed = 1)
        {
            Buf = new float[Mathf.Max(1, Mathf.CeilToInt(seconds * SR))];
            rng = new System.Random(seed);
        }

        public int Len => Buf.Length;
        public float Rand() => (float)rng.NextDouble() * 2f - 1f;
        public float Rand01() => (float)rng.NextDouble();
        static int S(float t) => Mathf.Max(0, Mathf.RoundToInt(t * SR));

        public static float Env(float t, float a, float d, float s, float sustainLevel, float r, float len)
        {
            if (t < 0f) return 0f;
            if (t < a) return t / Mathf.Max(1e-4f, a);
            t -= a;
            if (t < d) return Mathf.Lerp(1f, sustainLevel, t / Mathf.Max(1e-4f, d));
            t -= d;
            float sLen = Mathf.Max(0f, len - a - d - r);
            if (t < sLen) return sustainLevel;
            t -= sLen;
            return Mathf.Lerp(sustainLevel, 0f, Mathf.Clamp01(t / Mathf.Max(1e-4f, r)));
        }

        /// <summary>Decaimento exponencial simples.</summary>
        public static float Perc(float t, float attack, float decay) => t < 0f ? 0f : t < attack ? t / Mathf.Max(1e-4f, attack) : Mathf.Exp(-(t - attack) / Mathf.Max(1e-4f, decay));

        public enum Wave { Sine, Tri, Saw, Square }

        static float Osc(Wave w, float phase)
        {
            phase -= Mathf.Floor(phase);
            switch (w)
            {
                case Wave.Tri: return 4f * Mathf.Abs(phase - 0.5f) - 1f;
                case Wave.Saw: return 2f * phase - 1f;
                case Wave.Square: return phase < 0.5f ? 1f : -1f;
                default: return Mathf.Sin(phase * Mathf.PI * 2f);
            }
        }

        /// <summary>Tom com varredura exponencial de frequência e envelope percussivo.</summary>
        public void Tone(float start, float dur, float f0, float f1, float amp, Wave w = Wave.Sine, float attack = 0.005f, float decay = -1f, float vibrato = 0f)
        {
            int s0 = S(start), n = S(dur);
            double phase = 0;
            if (decay < 0f) decay = dur * 0.35f;
            for (int i = 0; i < n && s0 + i < Len; i++)
            {
                float t = i / (float)SR;
                float u = t / Mathf.Max(1e-4f, dur);
                float f = f0 * Mathf.Pow(f1 / f0, u);
                if (vibrato > 0f) f *= 1f + vibrato * Mathf.Sin(t * 6f * Mathf.PI * 2f);
                phase += f / SR;
                float e = Perc(t, attack, decay) * Mathf.Clamp01((dur - t) / 0.01f);
                Buf[s0 + i] += Osc(w, (float)phase) * amp * e;
            }
        }

        /// <summary>Nota sustentada com ADSR (pads, baixo).</summary>
        public void Note(float start, float dur, float freq, float amp, Wave w, float a, float d, float s, float r, float detune = 0f)
        {
            int s0 = S(start), n = S(dur + r);
            double p1 = Rand01(), p2 = Rand01();
            for (int i = 0; i < n && s0 + i < Len; i++)
            {
                float t = i / (float)SR;
                p1 += freq / SR;
                p2 += freq * (1f + detune) / SR;
                float v = Osc(w, (float)p1);
                if (detune != 0f) v = (v + Osc(w, (float)p2)) * 0.5f;
                Buf[s0 + i] += v * amp * Env(t, a, d, s, s, r, dur + r);
            }
        }

        public void Noise(float start, float dur, float amp, float attack, float decay)
        {
            int s0 = S(start), n = S(dur);
            for (int i = 0; i < n && s0 + i < Len; i++)
            {
                float t = i / (float)SR;
                Buf[s0 + i] += Rand() * amp * Perc(t, attack, decay) * Mathf.Clamp01((dur - t) / 0.01f);
            }
        }

        /// <summary>Corda dedilhada (Karplus-Strong): cordas do arco, dedilhados da trilha.</summary>
        public void Pluck(float start, float freq, float dur, float amp, float damping = 0.996f)
        {
            int period = Mathf.Max(2, Mathf.RoundToInt(SR / freq));
            var ring = new float[period];
            for (int i = 0; i < period; i++) ring[i] = Rand();
            int s0 = S(start), n = S(dur);
            int idx = 0;
            for (int i = 0; i < n && s0 + i < Len; i++)
            {
                float v = ring[idx];
                int next = (idx + 1) % period;
                ring[idx] = (v + ring[next]) * 0.5f * damping;
                idx = next;
                Buf[s0 + i] += v * amp * Mathf.Clamp01((dur - i / (float)SR) / 0.02f);
            }
        }

        public void LowPass(float cutoff, int from = 0, int to = -1)
        {
            if (to < 0) to = Len;
            float rc = 1f / (cutoff * 2f * Mathf.PI), dt = 1f / SR, a = dt / (rc + dt);
            float y = 0f;
            for (int i = from; i < to; i++) { y += a * (Buf[i] - y); Buf[i] = y; }
        }

        public void HighPass(float cutoff)
        {
            float rc = 1f / (cutoff * 2f * Mathf.PI), dt = 1f / SR, a = rc / (rc + dt);
            float y = 0f, prev = 0f;
            for (int i = 0; i < Len; i++) { float x = Buf[i]; y = a * (y + x - prev); prev = x; Buf[i] = y; }
        }

        /// <summary>Passa-faixa com centro variável ao longo do tempo (sopros e ventos).</summary>
        public void BandSweep(float f0, float f1, float q)
        {
            float low = 0f, band = 0f;
            for (int i = 0; i < Len; i++)
            {
                float u = i / (float)Len;
                float fc = Mathf.Lerp(f0, f1, u);
                float f = 2f * Mathf.Sin(Mathf.PI * Mathf.Min(fc, SR * 0.2f) / SR);
                float high = Buf[i] - low - band / q;
                band += f * high;
                low += f * band;
                Buf[i] = band;
            }
        }

        public void Gain(float g)
        {
            for (int i = 0; i < Len; i++) Buf[i] *= g;
        }

        public void Normalize(float peak = 0.89f)
        {
            float m = 1e-6f;
            for (int i = 0; i < Len; i++) m = Mathf.Max(m, Mathf.Abs(Buf[i]));
            float g = peak / m;
            for (int i = 0; i < Len; i++) Buf[i] *= g;
        }

        public void SoftClip(float drive = 1.2f)
        {
            for (int i = 0; i < Len; i++) Buf[i] = (float)Math.Tanh(Buf[i] * drive) / (float)Math.Tanh(drive);
        }

        /// <summary>Reverb de pentes + passa-tudo (Schroeder), mistura seca/úmida.</summary>
        public void Reverb(float wet, float room = 0.82f)
        {
            int[] combs = { 1557, 1617, 1491, 1422, 1277, 1356 };
            int[] alls = { 225, 556, 441 };
            var output = new float[Len];
            foreach (var c in combs)
            {
                var buf = new float[c];
                int idx = 0;
                float filt = 0f;
                for (int i = 0; i < Len; i++)
                {
                    float y = buf[idx];
                    filt = y * 0.7f + filt * 0.3f;
                    buf[idx] = Buf[i] + filt * room;
                    idx = (idx + 1) % c;
                    output[i] += y / combs.Length;
                }
            }
            foreach (var a in alls)
            {
                var buf = new float[a];
                int idx = 0;
                for (int i = 0; i < Len; i++)
                {
                    float b = buf[idx];
                    float y = -output[i] + b;
                    buf[idx] = output[i] + b * 0.5f;
                    idx = (idx + 1) % a;
                    output[i] = y;
                }
            }
            for (int i = 0; i < Len; i++) Buf[i] = Buf[i] * (1f - wet) + output[i] * wet;
        }

        /// <summary>Soma a cauda depois de loopSeconds ao início (loop sem emenda) e corta.</summary>
        public float[] Loop(float loopSeconds)
        {
            int n = S(loopSeconds);
            var outBuf = new float[n];
            for (int i = 0; i < Len; i++) outBuf[i % n] += Buf[i];
            // Pequeno cruzamento nas bordas.
            int fade = Mathf.Min(512, n / 4);
            for (int i = 0; i < fade; i++)
            {
                float k = i / (float)fade;
                float a = outBuf[i], b = outBuf[n - fade + i];
                outBuf[n - fade + i] = Mathf.Lerp(b, a, k * 0.5f);
            }
            return outBuf;
        }

        public static void WriteWav(string path, float[] data, int sampleRate = SR)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var fs = new FileStream(path, FileMode.Create))
            using (var w = new BinaryWriter(fs))
            {
                int bytes = data.Length * 2;
                w.Write(new[] { 'R', 'I', 'F', 'F' });
                w.Write(36 + bytes);
                w.Write(new[] { 'W', 'A', 'V', 'E' });
                w.Write(new[] { 'f', 'm', 't', ' ' });
                w.Write(16);
                w.Write((short)1);
                w.Write((short)1);
                w.Write(sampleRate);
                w.Write(sampleRate * 2);
                w.Write((short)2);
                w.Write((short)16);
                w.Write(new[] { 'd', 'a', 't', 'a' });
                w.Write(bytes);
                foreach (var s in data) w.Write((short)Mathf.Clamp(Mathf.RoundToInt(s * 32767f), -32768, 32767));
            }
        }
    }
}

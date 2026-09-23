using UnityEngine;

namespace Ruinas
{
    public struct PoseSample
    {
        public Vector3 body, head, armL, armR, legL, legR, rootOffset, rootTilt;

        public static PoseSample Lerp(in PoseSample a, in PoseSample b, float t)
        {
            return new PoseSample
            {
                body = Vector3.LerpUnclamped(a.body, b.body, t),
                head = Vector3.LerpUnclamped(a.head, b.head, t),
                armL = Vector3.LerpUnclamped(a.armL, b.armL, t),
                armR = Vector3.LerpUnclamped(a.armR, b.armR, t),
                legL = Vector3.LerpUnclamped(a.legL, b.legL, t),
                legR = Vector3.LerpUnclamped(a.legR, b.legR, t),
                rootOffset = Vector3.LerpUnclamped(a.rootOffset, b.rootOffset, t),
                rootTilt = Vector3.LerpUnclamped(a.rootTilt, b.rootTilt, t),
            };
        }

        /// <summary>Mistura só tronco, cabeça e braços (pernas continuam na locomoção).</summary>
        public static PoseSample LerpUpper(in PoseSample lower, in PoseSample upper, float t)
        {
            var r = lower;
            r.body = Vector3.LerpUnclamped(lower.body, upper.body, t);
            r.head = Vector3.LerpUnclamped(lower.head, upper.head, t);
            r.armL = Vector3.LerpUnclamped(lower.armL, upper.armL, t);
            r.armR = Vector3.LerpUnclamped(lower.armR, upper.armR, t);
            return r;
        }

        public static PoseSample FromKey(in PoseKey k)
        {
            return new PoseSample
            {
                body = k.body, head = k.head, armL = k.armL, armR = k.armR,
                legL = k.legL, legR = k.legR, rootOffset = k.rootOffset, rootTilt = k.rootTilt,
            };
        }
    }

    public static class PoseMath
    {
        public static float Apply(Ease ease, float t)
        {
            t = Mathf.Clamp01(t);
            switch (ease)
            {
                case Ease.InQuad: return t * t;
                case Ease.OutQuad: return 1f - (1f - t) * (1f - t);
                case Ease.InOutQuad: return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;
                case Ease.OutBack:
                    const float c1 = 1.70158f, c3 = c1 + 1f;
                    return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
                case Ease.Hold: return 0f;
                default: return t;
            }
        }

        /// <summary>Amostra o clip no tempo t (segundos). A curva de cada segmento é a do quadro final.</summary>
        public static PoseSample Sample(PoseClip clip, float t)
        {
            var keys = clip.keys;
            if (keys == null || keys.Length == 0) return default;
            if (clip.loop && clip.duration > 0f) t = Mathf.Repeat(t, clip.duration);
            if (t <= keys[0].time) return PoseSample.FromKey(keys[0]);
            for (int i = 0; i < keys.Length - 1; i++)
            {
                var a = keys[i];
                var b = keys[i + 1];
                if (t <= b.time)
                {
                    float span = Mathf.Max(1e-4f, b.time - a.time);
                    float u = Apply(b.ease, (t - a.time) / span);
                    return PoseSample.Lerp(PoseSample.FromKey(a), PoseSample.FromKey(b), u);
                }
            }
            return PoseSample.FromKey(keys[keys.Length - 1]);
        }
    }
}

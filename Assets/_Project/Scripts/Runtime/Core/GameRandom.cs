using UnityEngine;

namespace Ruinas
{
    /// <summary>
    /// Gerador xorshift com semente explícita. Usado em IA, loot e efeitos para permitir
    /// repetições controladas no modo de referência (sem prometer determinismo bit a bit).
    /// </summary>
    public class GameRandom
    {
        uint state;

        public GameRandom(int seed) { Reset(seed); }

        public void Reset(int seed)
        {
            state = (uint)seed ^ 0x9E3779B9u;
            if (state == 0) state = 0xA341316Cu;
            for (int i = 0; i < 4; i++) NextUInt();
        }

        public uint NextUInt()
        {
            uint x = state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            state = x;
            return x;
        }

        /// <summary>[0, 1)</summary>
        public float Value => (NextUInt() >> 8) * (1f / 16777216f);

        public float Range(float min, float max) => min + (max - min) * Value;

        /// <summary>[min, max)</summary>
        public int Range(int min, int max) => max <= min ? min : min + (int)(NextUInt() % (uint)(max - min));

        public bool Chance(float p) => Value < p;

        public Vector2 InsideUnitCircle()
        {
            float a = Value * Mathf.PI * 2f;
            float r = Mathf.Sqrt(Value);
            return new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
        }

        public Vector3 OnUnitSphere()
        {
            float z = Range(-1f, 1f);
            float a = Value * Mathf.PI * 2f;
            float r = Mathf.Sqrt(1f - z * z);
            return new Vector3(r * Mathf.Cos(a), r * Mathf.Sin(a), z);
        }

        /// <summary>Hash estável para variações de blocos e decoração.</summary>
        public static uint Hash(int x, int y, int z, int salt = 0)
        {
            unchecked
            {
                uint h = (uint)(x * 73856093) ^ (uint)(y * 19349663) ^ (uint)(z * 83492791) ^ (uint)(salt * 2654435761u);
                h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15;
                return h;
            }
        }

        public static float Hash01(int x, int y, int z, int salt = 0) => (Hash(x, y, z, salt) & 0xFFFFFF) / 16777216f;
    }
}

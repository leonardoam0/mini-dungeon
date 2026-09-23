using UnityEngine;

namespace Ruinas
{
    public struct VfxParams
    {
        public float radius;
        public float duration;
        public float height;
        public float intensity;
        public Color color;
        public bool hasColor;
    }

    public interface IVfxBehaviour
    {
        void OnVfxSpawn(PooledVfx vfx);
        void OnVfxStop();
    }

    /// <summary>
    /// Instância reutilizável de efeito. Ao reutilizar, todos os sistemas de partículas, luzes e comportamentos
    /// são reinicializados; ao parar, a emissão cessa e a instância só volta ao pool quando nada mais está visível.
    /// </summary>
    public class PooledVfx : MonoBehaviour
    {
        public string Key { get; private set; }
        public VfxParams Params { get; private set; }
        public float Lifetime { get; private set; }
        public float Age { get; private set; }
        public bool IsStopping => stopping;

        ParticleSystem[] systems;
        bool[] autoSeed;
        Light[] lights;
        float[] lightBase;
        IVfxBehaviour[] behaviours;
        VfxPool pool;
        Transform follow;
        Vector3 followOffset;
        bool following;
        bool stopping;
        float stopTimer;
        float fadeTime;

        public void Bind(VfxPool owner, string key)
        {
            pool = owner;
            Key = key;
            systems = GetComponentsInChildren<ParticleSystem>(true);
            lights = GetComponentsInChildren<Light>(true);
            lightBase = new float[lights.Length];
            for (int i = 0; i < lights.Length; i++) lightBase[i] = lights[i].intensity;
            behaviours = GetComponentsInChildren<IVfxBehaviour>(true);
            autoSeed = new bool[systems.Length];
            for (int i = 0; i < systems.Length; i++)
            {
                var main = systems[i].main;
                main.playOnAwake = false;
                autoSeed[i] = systems[i].useAutoRandomSeed;
            }
        }

        public void Play(float lifetime, VfxParams p)
        {
            Params = p;
            Lifetime = lifetime;
            Age = 0f;
            stopping = false;
            stopTimer = 0f;
            following = false;
            follow = null;
            for (int i = 0; i < lights.Length; i++)
            {
                lights[i].intensity = lightBase[i] * (p.intensity > 0f ? p.intensity : 1f);
                if (p.hasColor) lights[i].color = p.color;
                lights[i].enabled = true;
            }
            // Na captura determinística cada instância recebe semente fixa; fora dela volta à semente automática.
            uint seed = DeterministicVfx.Enabled ? DeterministicVfx.NextSpawnSeed(Key) : 0u;
            for (int i = 0; i < systems.Length; i++)
            {
                var ps = systems[i];
                if (p.hasColor)
                {
                    var main = ps.main;
                    main.startColor = new ParticleSystem.MinMaxGradient(p.color);
                }
                ps.Clear(false);
                if (DeterministicVfx.Enabled) DeterministicVfx.Seed(ps, seed + (uint)i * 7919u);
                else if (ps.useAutoRandomSeed != autoSeed[i])
                {
                    ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.useAutoRandomSeed = autoSeed[i];
                }
                ps.Play(false);
            }
            foreach (var b in behaviours) b.OnVfxSpawn(this);
        }

        /// <summary>Captura determinística iniciada com o efeito já ativo: reinicia as partículas com semente fixa.</summary>
        public void Reseed(uint seed)
        {
            if (systems == null) return;
            for (int i = 0; i < systems.Length; i++)
            {
                var ps = systems[i];
                bool emitting = ps.isEmitting;
                DeterministicVfx.Seed(ps, seed + (uint)i * 7919u);
                if (emitting) ps.Play(false);
            }
        }

        public void Follow(Transform target, Vector3 offset)
        {
            follow = target;
            followOffset = offset;
            following = target != null;
            if (following) transform.position = target.position + offset;
        }

        public void StopAndRelease(float fade = 0.4f)
        {
            if (stopping || !gameObject.activeSelf) return;
            stopping = true;
            stopTimer = 0f;
            fadeTime = Mathf.Max(0.01f, fade);
            foreach (var ps in systems) ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            foreach (var b in behaviours) b.OnVfxStop();
        }

        bool AnyParticlesAlive()
        {
            foreach (var ps in systems) if (ps.particleCount > 0) return true;
            return false;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            Age += dt;
            if (following)
            {
                if (follow == null || !follow.gameObject.activeInHierarchy) { following = false; StopAndRelease(0.2f); }
                else transform.position = follow.position + followOffset;
            }

            if (!stopping && Lifetime > 0f && Age >= Lifetime) StopAndRelease(0.3f);

            if (stopping)
            {
                stopTimer += dt;
                float k = 1f - Mathf.Clamp01(stopTimer / fadeTime);
                for (int i = 0; i < lights.Length; i++) lights[i].intensity = lightBase[i] * k * (Params.intensity > 0f ? Params.intensity : 1f);
                bool done = stopTimer >= fadeTime && !AnyParticlesAlive();
                if (done || stopTimer > fadeTime + 4f) pool.Release(this);
            }
        }
    }
}

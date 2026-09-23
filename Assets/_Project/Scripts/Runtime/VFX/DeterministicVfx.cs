using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ruinas
{
    /// <summary>
    /// Captura determinística da sequência de referência: partículas com sementes fixas, relógio das animações
    /// visuais (scripts e shaders de efeito) que começa em zero com o roteiro e gerador global reiniciado. Fora da
    /// captura as sementes continuam automáticas (variedade na jogabilidade) e o relógio visual é o próprio Time.time.
    /// </summary>
    public static class DeterministicVfx
    {
        public static bool Enabled { get; private set; }
        /// <summary>Muda a cada início de captura; quem guarda fase própria (temporizadores) se realinha.</summary>
        public static int Generation { get; private set; }

        static readonly int ShaderTimeId = Shader.PropertyToID("_RuinasTime");
        static int beginFrame;
        static float step = 1f / 30f;
        static uint spawns;

        /// <summary>
        /// Relógio das animações puramente visuais (oscilação de luzes, pulsos de brilho). Na captura conta os passos
        /// fixos desde o início do roteiro, sem depender da base de tempo acumulada durante o carregamento.
        /// </summary>
        public static float VisualTime => Enabled ? (Time.frameCount - beginFrame) * step : Time.time;

        /// <summary>
        /// Passo das animações de interface, do mapa e da névoa: na captura é o passo fixo, sem depender do tempo não
        /// escalado (que variou entre execuções nas medições de repetibilidade).
        /// </summary>
        public static float UnscaledDeltaTime => Enabled ? step : Time.unscaledDeltaTime;

        // Os shaders de efeito (rolagem de feixes e bolhas, giro dos anéis) leem _RuinasTime em vez de _Time,
        // que inclui o tempo de carregamento e mudaria de uma captura para outra.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void HookShaderTime()
        {
            RenderPipelineManager.beginContextRendering -= SetShaderTime;
            RenderPipelineManager.beginContextRendering += SetShaderTime;
        }

        static void SetShaderTime(ScriptableRenderContext context, List<Camera> cameras) => Shader.SetGlobalFloat(ShaderTimeId, VisualTime);

        public static void Begin(int seed)
        {
            Enabled = true;
            Generation++;
            step = Time.captureDeltaTime > 0f ? Time.captureDeltaTime : 1f / 30f;
            beginFrame = Time.frameCount;
            spawns = 0;
            UnityEngine.Random.InitState(seed);
            // Partículas já presentes na cena (ambiente, obelisco, tochas): reiniciadas com semente fixa pela posição
            // na hierarquia, mantendo o estado de emissão.
            foreach (var ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include))
            {
                if (ps.GetComponentInParent<PooledVfx>(true) != null) continue;
                bool emitting = ps.isEmitting;
                Seed(ps, Hash(PathOf(ps.transform), (uint)seed));
                if (emitting) ps.Play(false);
            }
            // Efeitos do pool já ativos (por exemplo, status aplicados na preparação da cena).
            foreach (var vfx in Object.FindObjectsByType<PooledVfx>(FindObjectsInactive.Exclude))
                vfx.Reseed(Hash(vfx.Key + "|" + PathOf(vfx.transform), (uint)seed));
        }

        public static void End() => Enabled = false;

        /// <summary>Semente da próxima instância de efeito do pool: nome do efeito e ordem de criação no roteiro.</summary>
        public static uint NextSpawnSeed(string key) => Hash(key, ++spawns * 2654435761u);

        public static void Seed(ParticleSystem ps, uint seed)
        {
            ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.useAutoRandomSeed = false;
            ps.randomSeed = seed;
        }

        static string PathOf(Transform t)
        {
            string s = t.name;
            for (var p = t.parent; p != null; p = p.parent) s = p.name + "/" + s;
            Vector3 w = t.position;
            return s + "@" + Mathf.RoundToInt(w.x * 10f) + "," + Mathf.RoundToInt(w.y * 10f) + "," + Mathf.RoundToInt(w.z * 10f);
        }

        static uint Hash(string s, uint salt)
        {
            uint h = 2166136261u ^ salt;
            foreach (char c in s) { h ^= c; h *= 16777619u; }
            return h;
        }
    }
}

using UnityEngine;

namespace Ruinas
{
    /// <summary>Oscilação suave de luz (tochas, braseiros, fogo localizado). Sem efeito de estroboscópio.</summary>
    [RequireComponent(typeof(Light))]
    public class LightFlicker : MonoBehaviour
    {
        public float baseIntensity = 2.4f;
        public float amplitude = 0.18f;
        public float speed = 7f;
        public float rangeAmplitude = 0.06f;

        Light lightComp;
        float baseRange;
        float seed;

        void Awake()
        {
            lightComp = GetComponent<Light>();
            baseRange = lightComp.range;
            seed = (transform.position.x * 13.1f + transform.position.z * 7.7f) % 100f;
            if (baseIntensity <= 0f) baseIntensity = lightComp.intensity;
        }

        void Update()
        {
            float t = DeterministicVfx.VisualTime * speed + seed;
            float n = (Mathf.PerlinNoise(t, seed) - 0.5f) * 2f;
            float n2 = Mathf.Sin(t * 0.37f) * 0.3f;
            lightComp.intensity = baseIntensity * (1f + amplitude * (n * 0.8f + n2 * 0.2f));
            lightComp.range = baseRange * (1f + rangeAmplitude * n);
        }
    }
}

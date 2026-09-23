using UnityEngine;

namespace Ruinas
{
    /// <summary>
    /// Nuvem de cubos: o raio de emissão acompanha o raio da área de dano; ao parar, a emissão cessa
    /// e as partículas encolhem, sinalizando o fim do perigo.
    /// </summary>
    public class CloudFx : MonoBehaviour, IVfxBehaviour
    {
        public ParticleSystem cubes;
        public ParticleSystem wisps;
        public float fillFactor = 0.82f;
        public float particlesPerArea = 7f;
        public float defaultRadius = 1.8f;

        public void OnVfxSpawn(PooledVfx vfx)
        {
            float r = vfx.Params.radius > 0f ? vfx.Params.radius : defaultRadius;
            Configure(cubes, r, particlesPerArea);
            Configure(wisps, r, particlesPerArea * 0.25f);
        }

        void Configure(ParticleSystem ps, float r, float density)
        {
            if (ps == null) return;
            var shape = ps.shape;
            shape.radius = r * fillFactor;
            var emission = ps.emission;
            float area = Mathf.PI * r * r;
            emission.rateOverTime = Mathf.Clamp(area * density, 6f, 90f);
        }

        public void OnVfxStop() { }
    }
}

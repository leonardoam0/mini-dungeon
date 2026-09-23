using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Ruinas
{
    /// <summary>
    /// Aplica o perfil de iluminação da cena e mantém os parâmetros globais dinâmicos: névoa de altura
    /// relativa ao piso do jogador e recorte de oclusão. Também aplica "reduzir flashes" ao bloom.
    /// </summary>
    public class AtmosphereController : MonoBehaviour
    {
        static readonly int HeightFogParamsId = Shader.PropertyToID("_RuinasHeightFogParams");
        static readonly int HeightFogColorId = Shader.PropertyToID("_RuinasHeightFogColor");
        static readonly int SeeThroughParamsId = Shader.PropertyToID("_RuinasSeeThroughParams");

        public LightingProfile profile;
        public Light sun;
        public Volume volume;

        Transform follow;
        float heightRef;
        float heightVel;
        bool hasRef;
        int fogGeneration;
        float baseBloom = -1f;

        public void Init(LightingProfile p, Transform player)
        {
            if (p != null) profile = p;
            follow = player;
            ApplyStatic();
            ApplyAccessibility();
            if (Services.Settings != null) Services.Settings.Changed += ApplyAccessibility;
        }

        void OnDestroy()
        {
            if (Services.Settings != null) Services.Settings.Changed -= ApplyAccessibility;
        }

        public void SetFollow(Transform t)
        {
            follow = t;
            hasRef = false;
        }

        public void ApplyStatic()
        {
            if (profile == null) return;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = profile.ambientSky * profile.ambientIntensity;
            RenderSettings.ambientEquatorColor = profile.ambientEquator * profile.ambientIntensity;
            RenderSettings.ambientGroundColor = profile.ambientGround * profile.ambientIntensity;
            RenderSettings.fog = profile.fog;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = profile.fogColor;
            RenderSettings.fogStartDistance = profile.fogStart;
            RenderSettings.fogEndDistance = profile.fogEnd;
            DynamicGI.UpdateEnvironment();

            if (sun != null)
            {
                sun.color = profile.sunColor;
                sun.intensity = profile.sunIntensity;
                sun.transform.rotation = Quaternion.Euler(profile.sunEuler);
                sun.shadowStrength = profile.shadowStrength;
            }
            var cam = Camera.main;
            if (cam != null) cam.backgroundColor = profile.cameraBackground;
            Shader.SetGlobalColor(HeightFogColorId, profile.heightFogColor);
        }

        public void ApplyAccessibility()
        {
            bool reduce = Services.Settings != null && Services.Settings.Data.reduceFlashes;
            var ctx = LevelContext.Current;
            if (ctx != null && ctx.Vfx != null) ctx.Vfx.FlashScale = reduce ? 0.45f : 1f;
            // volume.profile cria uma cópia em runtime: o asset do projeto não é alterado.
            if (volume != null && volume.sharedProfile != null && volume.profile.TryGet<Bloom>(out var bloom))
            {
                if (baseBloom < 0f) baseBloom = bloom.intensity.value;
                bloom.intensity.Override(reduce ? baseBloom * 0.55f : baseBloom);
            }
        }

        void Update()
        {
            if (profile == null) return;
            if (follow != null)
            {
                float target = follow.position.y - profile.heightFogBelowPlayer;
                if (fogGeneration != DeterministicVfx.Generation) { fogGeneration = DeterministicVfx.Generation; hasRef = false; }
                if (!hasRef) { heightRef = target; heightVel = 0f; hasRef = true; }
                heightRef = Mathf.SmoothDamp(heightRef, target, ref heightVel, 0.6f, Mathf.Infinity, DeterministicVfx.UnscaledDeltaTime);
            }
            Shader.SetGlobalVector(HeightFogParamsId, new Vector4(heightRef, profile.heightFogRange, profile.heightFogMax, hasRef ? 1f : 0f));
            var cam = Camera.main;
            float aspect = cam != null ? cam.aspect : 16f / 9f;
            Shader.SetGlobalVector(SeeThroughParamsId, new Vector4(follow != null ? 1f : 0f, aspect, profile.seeThroughMinHeight, profile.seeThroughDepthMargin));
        }
    }
}

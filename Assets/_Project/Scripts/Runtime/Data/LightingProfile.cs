using UnityEngine;

namespace Ruinas
{
    /// <summary>
    /// Atmosfera de uma cena. Paleta inicial PROPOSTA pelo documento (#082823, #345954, #8A624A,
    /// #FFB34D, #B9FFF1, #C568EF, #4AD83C), ajustada por comparação de capturas.
    /// </summary>
    [CreateAssetMenu(menuName = "Ruinas/Lighting Profile")]
    public class LightingProfile : ScriptableObject
    {
        [Header("Ambiente (trilight)")]
        public Color ambientSky = new Color(0.20f, 0.36f, 0.36f);
        public Color ambientEquator = new Color(0.11f, 0.22f, 0.21f);
        public Color ambientGround = new Color(0.05f, 0.09f, 0.08f);
        public float ambientIntensity = 1f;

        [Header("Luz principal (lua)")]
        public Color sunColor = new Color(0.55f, 0.78f, 0.82f);
        public float sunIntensity = 0.55f;
        public Vector3 sunEuler = new Vector3(58f, 150f, 0f);
        [Range(0, 1)] public float shadowStrength = 0.85f;

        [Header("Névoa por distância")]
        public bool fog = true;
        public Color fogColor = new Color(0.031f, 0.157f, 0.137f);
        public float fogStart = 34f;
        public float fogEnd = 78f;
        public Color cameraBackground = new Color(0.02f, 0.07f, 0.065f);

        [Header("Névoa de altura (níveis inferiores)")]
        public Color heightFogColor = new Color(0.02f, 0.085f, 0.075f);
        public float heightFogBelowPlayer = 1.6f;
        public float heightFogRange = 9f;
        [Range(0, 1)] public float heightFogMax = 0.82f;

        [Header("Recorte de oclusão")]
        public float seeThroughRadius = 0.12f;
        public float seeThroughMinHeight = 0.9f;
        public float seeThroughDepthMargin = 1.2f;

        [Header("Pós-processamento")]
        public float bloomThreshold = 1.0f;
        public float bloomIntensity = 0.9f;
        public float bloomScatter = 0.62f;
        public float postExposure = 0.15f;
        [Tooltip("Balanço de branco (negativo = mais frio).")] public float whiteBalanceTemperature = 0f;
        [Tooltip("Desfoque gaussiano do fundo: início/fim (distância da câmera) e raio máximo. 0 = desligado.")]
        public float dofStart = 0f;
        public float dofEnd = 0f;
        public float dofMaxRadius = 1f;
        public float contrast = 14f;
        public float saturation = 12f;
        public Color splitShadows = new Color(0.30f, 0.62f, 0.60f);
        public Color splitHighlights = new Color(0.95f, 0.66f, 0.40f);
        public float splitBalance = -12f;
        public float vignette = 0.28f;
        public float ssaoIntensity = 1.1f;
        public float ssaoRadius = 0.4f;
    }
}

using UnityEngine;

namespace Ruinas
{
    /// <summary>
    /// Perfil de comparação com o vídeo (540×960). A imagem é renderizada em 16:9 e recortada:
    /// o vídeo aparenta ser um recorte vertical de uma captura horizontal. Crop e deslocamento são editáveis.
    /// </summary>
    [CreateAssetMenu(menuName = "Ruinas/Reference Capture Profile")]
    public class ReferenceCaptureProfile : ScriptableObject
    {
        public int outputWidth = 540;
        public int outputHeight = 960;
        [Tooltip("Centro do recorte no quadro renderizado (0..1).")] public Vector2 cropCenter = new Vector2(0.5f, 0.5f);
        [Tooltip("Altura do recorte como fração da altura renderizada.")] [Range(0.3f, 1f)] public float cropHeightFraction = 1f;
        [Tooltip("Câmera usada na captura (vazio = a mesma da jogabilidade).")] public CameraProfile cameraOverride;
        public float[] captureTimes = { 0f, 0.5f, 1f, 2.5f, 4f, 5.5f, 7f, 9.5f, 12f, 14f };

        public RectInt CropRect(int srcWidth, int srcHeight)
        {
            int h = Mathf.RoundToInt(srcHeight * cropHeightFraction);
            int w = Mathf.RoundToInt(h * (outputWidth / (float)outputHeight));
            w = Mathf.Min(w, srcWidth);
            int cx = Mathf.RoundToInt(cropCenter.x * srcWidth);
            int cy = Mathf.RoundToInt(cropCenter.y * srcHeight);
            int x = Mathf.Clamp(cx - w / 2, 0, srcWidth - w);
            int y = Mathf.Clamp(cy - h / 2, 0, srcHeight - h);
            return new RectInt(x, y, w, h);
        }
    }
}

using UnityEngine;

namespace Ruinas
{
    /// <summary>
    /// Parâmetros editáveis da câmera. Valores iniciais ESTIMADOS a partir do vídeo de referência
    /// (azimute ~45°, elevação ~45°, perspectiva de FOV estreito); não são valores oficiais.
    /// </summary>
    [CreateAssetMenu(menuName = "Ruinas/Camera Profile")]
    public class CameraProfile : ScriptableObject
    {
        [Header("Projeção")]
        public float yaw = 45f;
        public float pitch = 45f;
        public float distance = 34f;
        public float fieldOfView = 25f;
        public bool orthographic;
        public float orthographicSize = 11.5f;
        public float nearClip = 4f;
        public float farClip = 140f;

        [Header("Acompanhamento")]
        public Vector3 targetOffset = new Vector3(0f, 0.9f, 0f);
        [Tooltip("Deslocamento do alvo na tela (fração da altura), positivo = alvo mais abaixo.")]
        public float screenOffsetY = 0f;
        public float followSmoothTime = 0.12f;
        public float verticalSmoothTime = 0.35f;
        public float lookAhead = 1.1f;
        public float lookAheadSmoothTime = 0.45f;
        public bool useRoomBounds = true;

        [Header("Impacto")]
        public float shakeScale = 1f;
        public float maxShake = 0.25f;
    }
}

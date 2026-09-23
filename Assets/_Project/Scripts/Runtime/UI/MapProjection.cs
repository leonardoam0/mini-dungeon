using UnityEngine;

namespace Ruinas
{
    /// <summary>
    /// Transformação única mundo → mapa: o mapa é o mundo escalado em torno do jogador (mesma orientação
    /// da câmera), com a altura relativa também escalada. Depois é projetado pela câmera de jogo, então
    /// a geometria acompanha o movimento e o enquadramento sem offsets por objeto. Testado em EditMode.
    /// </summary>
    public static class MapProjection
    {
        public static Vector3 WorldToMap(Vector3 world, Vector3 pivot, float scale)
        {
            return pivot + (world - pivot) * scale;
        }

        public static Vector3 MapToWorld(Vector3 map, Vector3 pivot, float scale)
        {
            return pivot + (map - pivot) / Mathf.Max(1e-4f, scale);
        }

        /// <summary>Centro da célula (x, z) na altura indicada.</summary>
        public static Vector3 CellCorner(MapData data, float cx, float cz, float height)
        {
            return new Vector3(data.origin.x + cx, height, data.origin.z + cz);
        }
    }
}

using UnityEngine;

namespace Ruinas
{
    [System.Flags]
    public enum MapCellFlags : byte
    {
        None = 0,
        Walkable = 1,
        Stairs = 2,
        Water = 4,
        Door = 8,
        Hidden = 16,
    }

    /// <summary>
    /// Grade caminhável gerada a partir do mundo voxel. Base do mapa sobreposto: a transformação
    /// mundo→mapa é única (célula = 1 unidade, origem em <see cref="origin"/>).
    /// </summary>
    [CreateAssetMenu(menuName = "Ruinas/Map Data")]
    public class MapData : ScriptableObject
    {
        public int width;
        public int depth;
        public Vector3 origin;
        [Tooltip("Altura da superfície caminhável em meias unidades (-1 = sem piso).")] public short[] heights;
        public byte[] flags;

        public bool InBounds(int x, int z) => x >= 0 && z >= 0 && x < width && z < depth;
        public int Index(int x, int z) => z * width + x;

        public bool IsWalkable(int x, int z)
        {
            if (!InBounds(x, z) || flags == null) return false;
            var f = (MapCellFlags)flags[Index(x, z)];
            return (f & MapCellFlags.Walkable) != 0 && (f & MapCellFlags.Hidden) == 0;
        }

        public float Height(int x, int z) => InBounds(x, z) && heights != null ? heights[Index(x, z)] * 0.5f : -1f;

        public MapCellFlags Flags(int x, int z) => InBounds(x, z) && flags != null ? (MapCellFlags)flags[Index(x, z)] : MapCellFlags.None;

        public Vector2Int WorldToCell(Vector3 world)
        {
            return new Vector2Int(Mathf.FloorToInt(world.x - origin.x), Mathf.FloorToInt(world.z - origin.z));
        }

        public Vector3 CellToWorld(float cx, float cz, float y) => new Vector3(origin.x + cx, y, origin.z + cz);
    }
}

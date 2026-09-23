using UnityEngine;

namespace Ruinas
{
    /// <summary>Grade de blocos (1 unidade por bloco). Fora dos limites é ar.</summary>
    public class VoxelGrid
    {
        public readonly int SizeX, SizeY, SizeZ;
        public readonly byte[] Blocks;

        public VoxelGrid(int sx, int sy, int sz)
        {
            SizeX = sx;
            SizeY = sy;
            SizeZ = sz;
            Blocks = new byte[sx * sy * sz];
        }

        public bool InBounds(int x, int y, int z) => x >= 0 && y >= 0 && z >= 0 && x < SizeX && y < SizeY && z < SizeZ;
        int Index(int x, int y, int z) => (y * SizeZ + z) * SizeX + x;

        public byte Get(int x, int y, int z) => InBounds(x, y, z) ? Blocks[Index(x, y, z)] : (byte)0;

        public void Set(int x, int y, int z, byte b)
        {
            if (InBounds(x, y, z)) Blocks[Index(x, y, z)] = b;
        }

        /// <summary>Preenche a caixa [x0,x1]×[y0,y1]×[z0,z1] (inclusiva).</summary>
        public void Fill(int x0, int y0, int z0, int x1, int y1, int z1, byte b)
        {
            if (x0 > x1) (x0, x1) = (x1, x0);
            if (y0 > y1) (y0, y1) = (y1, y0);
            if (z0 > z1) (z0, z1) = (z1, z0);
            for (int y = y0; y <= y1; y++)
                for (int z = z0; z <= z1; z++)
                    for (int x = x0; x <= x1; x++)
                        Set(x, y, z, b);
        }

        /// <summary>Topo do bloco sólido mais alto da coluna (y da superfície), ou -1.</summary>
        public int TopSolid(int x, int z, BlockRegistry reg, int fromY = -1)
        {
            int start = fromY < 0 ? SizeY - 1 : Mathf.Min(fromY, SizeY - 1);
            for (int y = start; y >= 0; y--)
            {
                var d = reg[Get(x, y, z)];
                if (d.shape != BlockShape.Empty && d.solid) return y;
            }
            return -1;
        }
    }
}

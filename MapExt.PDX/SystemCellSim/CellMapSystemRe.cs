using Colossal.Mathematics;
using Unity.Entities;
using Unity.Mathematics;

namespace MapExt
{
    //[DisableAutoCreation]
    //仅用作静态方法库；
    public static class CellMapSystemRe
    {
        
        public static readonly int kMapSize = 57344;
        //protected JobHandle m_ReadDependencies;
        //protected JobHandle m_WriteDependencies;
        //protected NativeArray<T> m_Map;
        //protected int2 m_TextureSize;

        //only for instance;
        
        ///static methods:
                
        public static float3 GetCellCenter(int index, int textureSize)
        {
            int num = index % textureSize;
            int num2 = index / textureSize;
            int num3 = 57344 / textureSize;
            return new float3(-0.5f * 57344 + (num + 0.5f) * num3, 0f, -0.5f * 57344 + (num2 + 0.5f) * num3);
        }
        
        public static float3 GetCellCenter2(int2 cell, int textureSize)
        {
            int num = 57344 / textureSize;
            return new float3(-0.5f * 57344 + (cell.x + 0.5f) * num, 0f, -0.5f * 57344 + (cell.y + 0.5f) * num);
        }

        public static Bounds3 GetCellBounds(int index, int textureSize)
        {
            int num = index % textureSize;
            int num2 = index / textureSize;
            int num3 = 57344 / textureSize;
            return new Bounds3(new float3(-0.5f * 57344 + num * num3, -100000f, -0.5f * 57344 + num2 * num3), new float3(-0.5f * 57344 + (num + 1f) * num3, 100000f, -0.5f * 57344 + (num2 + 1f) * num3));
        }

        public static float2 GetCellCoords(float3 position, int mapSize, int textureSize)
        {
            return (0.5f + position.xz / mapSize) * textureSize;
        }

        public static int2 GetCell(float3 position, int mapSize, int textureSize)
        {
            return (int2)math.floor(GetCellCoords(position, mapSize, textureSize));
        }
        
    }//class;
}//namespace;

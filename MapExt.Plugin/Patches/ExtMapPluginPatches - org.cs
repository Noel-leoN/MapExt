using Game.Simulation;
using HarmonyLib;
using Unity.Collections;
using Colossal.Mathematics;
using Colossal.Collections;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using System;



namespace MapExtPlugin.Patches
{
    public static class CellMapSystemRe
    {

        public static readonly int kMapSize = 57344;
        //protected JobHandle m_ReadDependencies;
        //protected JobHandle m_WriteDependencies;
        //protected NativeArray<T> m_Map;
        //protected int2 m_TextureSize;

        ///static methods:

        public static float3 GetCellCenter(int index, int textureSize)
        {
            int num = index % textureSize;
            int num2 = index / textureSize;
            int num3 = kMapSize / textureSize;
            return new float3(-0.5f * kMapSize + (num + 0.5f) * num3, 0f, -0.5f * kMapSize + (num2 + 0.5f) * num3);
        }

        public static float3 GetCellCenter(int2 cell, int textureSize)
        {
            int num = kMapSize / textureSize;
            return new float3(-0.5f * kMapSize + (cell.x + 0.5f) * num, 0f, -0.5f * kMapSize + (cell.y + 0.5f) * num);
        }

        public static Bounds3 GetCellBounds(int index, int textureSize)
        {
            int num = index % textureSize;
            int num2 = index / textureSize;
            int num3 = kMapSize / textureSize;
            return new Bounds3(new float3(-0.5f * kMapSize + num * num3, -100000f, -0.5f * kMapSize + num2 * num3), new float3(-0.5f * kMapSize + (num + 1f) * num3, 100000f, -0.5f * kMapSize + (num2 + 1f) * num3));
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

    //AirPollution class;bc cell;
    [HarmonyPatch]
    internal static class AirPollutionSystemPatch
    {   
        [HarmonyPatch(typeof(CellMapSystem<AirPollution>), nameof(AirPollutionSystem.GetData))]
        [HarmonyPostfix]
        //引用mapsize method;
        public static void GetData(CellMapSystem<AirPollution> __instance, ref CellMapData<AirPollution> __result, bool readOnly, ref JobHandle dependencies)
        {
            //__result.m_CellSize *= 4;
        }

        //原系统method;
        [HarmonyPatch(typeof(AirPollutionSystem), nameof(AirPollutionSystem.GetCellCenter))]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result, int index)
        {
            __result = CellMapSystemRe.GetCellCenter(index, AirPollutionSystem.kTextureSize);
        }

        //原系统method;
        [HarmonyPatch(typeof(AirPollutionSystem), nameof(AirPollutionSystem.GetPollution))]
        [HarmonyPostfix]
        public static void GetPollution(ref AirPollution __result, float3 position, ref NativeArray<AirPollution> pollutionMap)
        {
            AirPollution result = default(AirPollution);
            float num = (float)CellMapSystemRe.kMapSize / (float)AirPollutionSystem.kTextureSize;
            int2 cell = CellMapSystemRe.GetCell(position - new float3(num / 2f, 0f, num / 2f), CellMapSystemRe.kMapSize, AirPollutionSystem.kTextureSize);
            float2 @float = CellMapSystemRe.GetCellCoords(position, CellMapSystemRe.kMapSize, AirPollutionSystem.kTextureSize) - new float2(0.5f, 0.5f);
            cell = math.clamp(cell, 0, AirPollutionSystem.kTextureSize - 2);
            short pollution = pollutionMap[cell.x + AirPollutionSystem.kTextureSize * cell.y].m_Pollution;
            short pollution2 = pollutionMap[cell.x + 1 + AirPollutionSystem.kTextureSize * cell.y].m_Pollution;
            short pollution3 = pollutionMap[cell.x + AirPollutionSystem.kTextureSize * (cell.y + 1)].m_Pollution;
            short pollution4 = pollutionMap[cell.x + 1 + AirPollutionSystem.kTextureSize * (cell.y + 1)].m_Pollution;
            result.m_Pollution = (short)math.round(math.lerp(math.lerp(pollution, pollution2, @float.x - (float)cell.x), math.lerp(pollution3, pollution4, @float.x - (float)cell.x), @float.y - (float)cell.y));
            __result = result;
        }

    }//airpollution system class;

    //AvailabilityInfoToGridSystem;bc cell;
    [HarmonyPatch]
    internal static class AvailabilityInfoToGridSystemPatch
    {
        [HarmonyPatch(typeof(CellMapSystem<AvailabilityInfoCell>), nameof(AvailabilityInfoToGridSystem.GetData))]
        [HarmonyPostfix]
        public static void GetData(CellMapSystem<AvailabilityInfoCell> __instance, ref CellMapData<AvailabilityInfoCell> __result, bool readOnly, ref JobHandle dependencies)
        {
            //__result.m_CellSize *= 4;
        }

        [HarmonyPatch(typeof(AvailabilityInfoToGridSystem), nameof(AvailabilityInfoToGridSystem.GetCellCenter))]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result, int index)
        {
            __result = CellMapSystemRe.GetCellCenter(index, AvailabilityInfoToGridSystem.kTextureSize);
        }

        
    }//AvailabilityInfoToGridSystem class


    [HarmonyPatch]
    internal static class GroundPollutionSystemPatch
    {
        [HarmonyPatch(typeof(CellMapSystem<GroundPollution>), nameof(GroundPollutionSystem.GetData))]
        [HarmonyPostfix]
        public static void GetData(CellMapSystem<GroundPollution> __instance, ref CellMapData<GroundPollution> __result, bool readOnly, ref JobHandle dependencies)
        {
            //__result.m_CellSize *= 4;
        }

        [HarmonyPatch(typeof(GroundPollutionSystem), nameof(GroundPollutionSystem.GetCellCenter))]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result, int index)
        {
            __result = CellMapSystemRe.GetCellCenter(index, GroundPollutionSystem.kTextureSize);
        }

        [HarmonyPatch(typeof(GroundPollutionSystem), nameof(GroundPollutionSystem.GetPollution))]
        [HarmonyPostfix]
        public static void GetPollution(ref GroundPollution __result, float3 position, ref NativeArray<GroundPollution> pollutionMap)
        {
            GroundPollution result = default(GroundPollution);
            int2 cell = CellMapSystemRe.GetCell(position, CellMapSystemRe.kMapSize, GroundPollutionSystem.kTextureSize);
            float2 cellCoords = CellMapSystemRe.GetCellCoords(position, CellMapSystem<GroundPollution>.kMapSize, GroundPollutionSystem.kTextureSize);
            if (cell.x < 0 || cell.x >= GroundPollutionSystem.kTextureSize || cell.y < 0 || cell.y >= GroundPollutionSystem.kTextureSize)
            {
                __result = result;
            }
            GroundPollution groundPollution = pollutionMap[cell.x + GroundPollutionSystem.kTextureSize * cell.y];
            GroundPollution groundPollution2 = ((cell.x < GroundPollutionSystem.kTextureSize - 1) ? pollutionMap[cell.x + 1 + GroundPollutionSystem.kTextureSize * cell.y] : default(GroundPollution));
            GroundPollution groundPollution3 = ((cell.y < GroundPollutionSystem.kTextureSize - 1) ? pollutionMap[cell.x + GroundPollutionSystem.kTextureSize * (cell.y + 1)] : default(GroundPollution));
            GroundPollution groundPollution4 = ((cell.x < GroundPollutionSystem.kTextureSize - 1 && cell.y < GroundPollutionSystem.kTextureSize - 1) ? pollutionMap[cell.x + 1 + GroundPollutionSystem.kTextureSize * (cell.y + 1)] : default(GroundPollution));
            result.m_Pollution = (short)Mathf.RoundToInt(math.lerp(math.lerp(groundPollution.m_Pollution, groundPollution2.m_Pollution, cellCoords.x - (float)cell.x), math.lerp(groundPollution3.m_Pollution, groundPollution4.m_Pollution, cellCoords.x - (float)cell.x), cellCoords.y - (float)cell.y));
            result.m_Previous = (short)Mathf.RoundToInt(math.lerp(math.lerp(groundPollution.m_Previous, groundPollution2.m_Previous, cellCoords.x - (float)cell.x), math.lerp(groundPollution3.m_Previous, groundPollution4.m_Previous, cellCoords.x - (float)cell.x), cellCoords.y - (float)cell.y));
            __result = result;
        }
    }//GroundPollutionSystem class

    [HarmonyPatch]
    internal static class GroundWaterSystemPatch
    {
        [HarmonyPatch(typeof(CellMapSystem<GroundWater>), nameof(GroundPollutionSystem.GetData))]
        [HarmonyPostfix]
        public static void GetData(CellMapSystem<GroundWater> __instance, ref CellMapData<GroundWater> __result, bool readOnly, ref JobHandle dependencies)
        {
            //__result.m_CellSize *= 4;
        }

        [HarmonyPatch(typeof(GroundWaterSystem), nameof(GroundWaterSystem.GetCellCenter))]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result, int index)
        {
            __result = CellMapSystemRe.GetCellCenter(index, GroundWaterSystem.kTextureSize);
        }

        [HarmonyPatch(typeof(GroundWaterSystem), nameof(GroundWaterSystem.TryGetCell))]
        [HarmonyPostfix]
        public static void TryGetCell(ref bool __result, float3 position, ref int2 cell)
        {
            cell = CellMapSystemRe.GetCell(position, CellMapSystemRe.kMapSize, GroundWaterSystem.kTextureSize);
            __result = GroundWaterSystem.IsValidCell(cell);
        }

        [HarmonyPatch(typeof(GroundWaterSystem), nameof(GroundWaterSystem.GetGroundWater), new Type[] { typeof(float3), typeof(NativeArray<GroundWater>) })]
        [HarmonyPostfix]
        public static void GetGroundWater(ref GroundWater __result, float3 position, NativeArray<GroundWater> groundWaterMap)
        {
            float2 @float = CellMapSystemRe.GetCellCoords(position, CellMapSystemRe.kMapSize, GroundWaterSystem.kTextureSize) - new float2(0.5f, 0.5f);
            int2 cell = new int2(Mathf.FloorToInt(@float.x), Mathf.FloorToInt(@float.y));
            int2 cell2 = new int2(cell.x + 1, cell.y);
            int2 cell3 = new int2(cell.x, cell.y + 1);
            int2 cell4 = new int2(cell.x + 1, cell.y + 1);
            GroundWater groundWater = GetGroundWater2(ref groundWaterMap, ref cell);
            GroundWater groundWater2 = GetGroundWater2(ref groundWaterMap, ref cell2);
            GroundWater groundWater3 = GetGroundWater2(ref groundWaterMap, ref cell3);
            GroundWater groundWater4 = GetGroundWater2(ref groundWaterMap, ref cell4);
            float sx = @float.x - (float)cell.x;
            float sy = @float.y - (float)cell.y;
            GroundWater result = default(GroundWater);
            result.m_Amount = (short)math.round(Bilinear(groundWater.m_Amount, groundWater2.m_Amount, groundWater3.m_Amount, groundWater4.m_Amount, sx, sy));
            result.m_Polluted = (short)math.round(Bilinear(groundWater.m_Polluted, groundWater2.m_Polluted, groundWater3.m_Polluted, groundWater4.m_Polluted, sx, sy));
            result.m_Max = (short)math.round(Bilinear(groundWater.m_Max, groundWater2.m_Max, groundWater3.m_Max, groundWater4.m_Max, sx, sy));
            __result = result;


        }
        private static GroundWater GetGroundWater2(ref NativeArray<GroundWater> groundWaterMap,ref int2 cell)
        {
            if (!GroundWaterSystem.IsValidCell(cell))
            {
                return default(GroundWater);
            }
            return groundWaterMap[cell.x + GroundWaterSystem.kTextureSize * cell.y];
        }

        private static float Bilinear(short v00, short v10, short v01, short v11, float sx, float sy)
        {
            return math.lerp(math.lerp(v00, v10, sx), math.lerp(v01, v11, sx), sy);
        }       

        [HarmonyPatch(typeof(GroundWaterSystem), nameof(GroundWaterSystem.ConsumeGroundWater))]
        [HarmonyPostfix]
        public static void ConsumeGroundWater(float3 position, NativeArray<GroundWater> groundWaterMap, int amount)
        {

            Unity.Assertions.Assert.IsTrue(amount >= 0);
            float2 @float = CellMapSystemRe.GetCellCoords(position, CellMapSystemRe.kMapSize, GroundWaterSystem.kTextureSize) - new float2(0.5f, 0.5f);
            int2 cell = new int2(Mathf.FloorToInt(@float.x), Mathf.FloorToInt(@float.y));
            int2 cell2 = new int2(cell.x + 1, cell.y);
            int2 cell3 = new int2(cell.x, cell.y + 1);
            int2 cell4 = new int2(cell.x + 1, cell.y + 1);
            GroundWater gw2 = GetGroundWater2(ref groundWaterMap, ref cell);
            GroundWater gw3 = GetGroundWater2(ref groundWaterMap, ref cell2);
            GroundWater gw4 = GetGroundWater2(ref groundWaterMap, ref cell3);
            GroundWater gw5 = GetGroundWater2(ref groundWaterMap, ref cell4);
            float sx = @float.x - (float)cell.x;
            float sy = @float.y - (float)cell.y;
            float num = math.ceil(Bilinear(gw2.m_Amount, 0, 0, 0, sx, sy));
            float num2 = math.ceil(Bilinear(0, gw3.m_Amount, 0, 0, sx, sy));
            float num3 = math.ceil(Bilinear(0, 0, gw4.m_Amount, 0, sx, sy));
            float num4 = math.ceil(Bilinear(0, 0, 0, gw5.m_Amount, sx, sy));
            float totalAvailable = num + num2 + num3 + num4;
            float totalConsumed = math.min(amount, totalAvailable);
            if (totalAvailable < (float)amount)
            {
                UnityEngine.Debug.LogWarning($"Trying to consume more groundwater than available! amount: {amount}, available: {totalAvailable}");
            }
            ConsumeFraction(ref gw2, num);
            ConsumeFraction(ref gw3, num2);
            ConsumeFraction(ref gw4, num3);
            ConsumeFraction(ref gw5, num4);
            Unity.Assertions.Assert.IsTrue(Mathf.Approximately(totalAvailable, 0f));
            Unity.Assertions.Assert.IsTrue(Mathf.Approximately(totalConsumed, 0f));            
            SetGroundWater(ref groundWaterMap,ref  cell, gw2);
            SetGroundWater(ref groundWaterMap, ref cell2, gw3);
            SetGroundWater(ref groundWaterMap,ref  cell3, gw4);
            SetGroundWater(ref groundWaterMap,ref cell4, gw5);
            void ConsumeFraction(ref GroundWater gw, float cellAvailable)
            {
                if (!(totalAvailable < 0.5f))
                {
                    float num5 = cellAvailable / totalAvailable;
                    totalAvailable -= cellAvailable;
                    float num6 = math.max(y: math.max(0f, totalConsumed - totalAvailable), x: math.round(num5 * totalConsumed));
                    Unity.Assertions.Assert.IsTrue(num6 <= (float)gw.m_Amount);
                    gw.Consume((int)num6);
                    totalConsumed -= num6;
                }
            }
        }

        private static void SetGroundWater(ref NativeArray<GroundWater> groundWaterMap, ref int2 cell, GroundWater gw)
        {
            if (GroundWaterSystem.IsValidCell(cell))
            {
                groundWaterMap[cell.x + GroundWaterSystem.kTextureSize * cell.y] = gw;
            }
        }

    }//class

    [HarmonyPatch]
    internal static class NaturalResourceSystemPatch
    {
        [HarmonyPatch(typeof(CellMapSystem<NaturalResourceCell>), nameof(NaturalResourceSystem.GetData))]
        [HarmonyPostfix]
        public static void GetData(CellMapSystem<NaturalResourceCell> __instance, ref CellMapData<NaturalResourceCell> __result, bool readOnly, ref JobHandle dependencies)
        {
            //__result.m_CellSize *= 4;
        }

        /*
        [HarmonyPatch(typeof(CellMapSystem<NaturalResourceCell>), nameof(CellMapSystem<NaturalResourceCell>.GetCellCenter))]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result, int index)
        {
            __result = CellMapSystemRe.GetCellCenter(index, NaturalResourceSystem.kTextureSize);
        }*/

        [HarmonyPatch(typeof(NaturalResourceSystem), nameof(NaturalResourceSystem.ResourceAmountToArea))]
        [HarmonyPostfix]
        public static void ResourceAmountToArea(NaturalResourceSystem __instance, ref float __result,float amount)
        {
            float2 @float = CellMapSystemRe.kMapSize / (float2)__instance.TextureSize;
            __result = amount * @float.x * @float.y / 10000f;
        }
        
    }//NatualResourceSystem class;

    [HarmonyPatch]
    internal static class NoisePollutionSystemPatch
    {
        [HarmonyPatch(typeof(CellMapSystem<NoisePollution>), nameof(NoisePollutionSystem.GetData))]
        [HarmonyPostfix]
        public static void GetData(CellMapSystem<NoisePollution> __instance, ref CellMapData<NoisePollution> __result, bool readOnly, ref JobHandle dependencies)
        {
            //__result.m_CellSize *= 4;
        }

        [HarmonyPatch(typeof(NoisePollutionSystem), nameof(NoisePollutionSystem.GetCellCenter))]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result, int index)
        {
            __result = CellMapSystemRe.GetCellCenter(index, NoisePollutionSystem.kTextureSize);
        }       

        [HarmonyPatch(typeof(NoisePollutionSystem), nameof(NoisePollutionSystem.GetPollution))]
        [HarmonyPostfix]
        public static void GetPollution(ref NoisePollution __result, float3 position, ref NativeArray<NoisePollution> pollutionMap)
        {
            NoisePollution result = default(NoisePollution);
            float num = CellMapSystemRe.kMapSize / (float)NoisePollutionSystem.kTextureSize;
            int2 cell = CellMapSystemRe.GetCell(position - new float3(num / 2f, 0f, num / 2f), CellMapSystemRe.kMapSize, NoisePollutionSystem.kTextureSize);
            float2 @float = CellMapSystemRe.GetCellCoords(position, CellMapSystemRe.kMapSize, NoisePollutionSystem.kTextureSize) - new float2(0.5f, 0.5f);
            cell = math.clamp(cell, 0, NoisePollutionSystem.kTextureSize - 2);
            short pollution = pollutionMap[cell.x + NoisePollutionSystem.kTextureSize * cell.y].m_Pollution;
            short pollution2 = pollutionMap[cell.x + 1 + NoisePollutionSystem.kTextureSize * cell.y].m_Pollution;
            short pollution3 = pollutionMap[cell.x + NoisePollutionSystem.kTextureSize * (cell.y + 1)].m_Pollution;
            short pollution4 = pollutionMap[cell.x + 1 + NoisePollutionSystem.kTextureSize * (cell.y + 1)].m_Pollution;
            result.m_Pollution = (short)Mathf.RoundToInt(math.lerp(math.lerp(pollution, pollution2, @float.x - (float)cell.x), math.lerp(pollution3, pollution4, @float.x - (float)cell.x), @float.y - (float)cell.y));
            __result = result;
        }
    }//NoisePollutionSystem class;


    [HarmonyPatch]
    internal static class PopulationToGridSystemPatch
    {
        [HarmonyPatch(typeof(CellMapSystem<PopulationCell>), nameof(PopulationToGridSystem.GetData))]
        [HarmonyPostfix]
        public static void GetData(CellMapSystem<PopulationCell> __instance, ref CellMapData<PopulationCell> __result, bool readOnly, ref JobHandle dependencies)
        {
            //__result.m_CellSize *= 4;
        }

        [HarmonyPatch(typeof(PopulationToGridSystem), nameof(PopulationToGridSystem.GetCellCenter))]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result, int index)
        {
            __result = CellMapSystemRe.GetCellCenter(index, PopulationToGridSystem.kTextureSize);
        }

        [HarmonyPatch(typeof(PopulationToGridSystem), nameof(PopulationToGridSystem.GetPopulation))]
        [HarmonyPostfix]
        public static void GetPopulation(ref PopulationCell __result, float3 position, ref NativeArray<PopulationCell> populationMap)
        {            
            PopulationCell result = default(PopulationCell);
            int2 cell = CellMapSystemRe.GetCell(position, CellMapSystemRe.kMapSize, PopulationToGridSystem.kTextureSize);
            float2 cellCoords = CellMapSystemRe.GetCellCoords(position, CellMapSystemRe.kMapSize, PopulationToGridSystem.kTextureSize);
            if (cell.x < 0 || cell.x >= PopulationToGridSystem.kTextureSize || cell.y < 0 || cell.y >= PopulationToGridSystem.kTextureSize)
            {
                __result = result;
            }
            float population = populationMap[cell.x + PopulationToGridSystem.kTextureSize * cell.y].m_Population;
            float y = ((cell.x < PopulationToGridSystem.kTextureSize - 1) ? populationMap[cell.x + 1 + PopulationToGridSystem.kTextureSize * cell.y].m_Population : 0f);
            float x = ((cell.y < PopulationToGridSystem.kTextureSize - 1) ? populationMap[cell.x + PopulationToGridSystem.kTextureSize * (cell.y + 1)].m_Population : 0f);
            float y2 = ((cell.x < PopulationToGridSystem.kTextureSize - 1 && cell.y < PopulationToGridSystem.kTextureSize - 1) ? populationMap[cell.x + 1 + PopulationToGridSystem.kTextureSize * (cell.y + 1)].m_Population : 0f);
            result.m_Population = math.lerp(math.lerp(population, y, cellCoords.x - (float)cell.x), math.lerp(x, y2, cellCoords.x - (float)cell.x), cellCoords.y - (float)cell.y);
            __result = result;
        }
    }//class;

    [HarmonyPatch]
    internal static class SoilWaterSystemPatch
    {
        [HarmonyPatch(typeof(CellMapSystem<SoilWater>), nameof(SoilWaterSystem.GetData))]
        [HarmonyPostfix]
        public static void GetData(CellMapSystem<SoilWater> __instance, ref CellMapData<SoilWater> __result, bool readOnly, ref JobHandle dependencies)
        {
            //__result.m_CellSize *= 4;
        }

        [HarmonyPatch(typeof(SoilWaterSystem), nameof(SoilWaterSystem.GetCellCenter))]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result, int index)
        {
            __result = CellMapSystemRe.GetCellCenter(index, SoilWaterSystem.kTextureSize);
        }

        [HarmonyPatch(typeof(SoilWaterSystem), nameof(SoilWaterSystem.GetSoilWater))]
        [HarmonyPostfix]
        public static void GetSoilWater(ref SoilWater __result, float3 position, ref NativeArray<SoilWater> soilWaterMap)
        {
            SoilWater result = default(SoilWater);
            int2 cell = CellMapSystemRe.GetCell(position, CellMapSystemRe.kMapSize, SoilWaterSystem.kTextureSize);
            float2 cellCoords = CellMapSystemRe.GetCellCoords(position, CellMapSystemRe.kMapSize, SoilWaterSystem.kTextureSize);
            if (cell.x < 0 || cell.x >= SoilWaterSystem.kTextureSize || cell.y < 0 || cell.y >= SoilWaterSystem.kTextureSize)
            {
                __result = result;
            }
            float x = soilWaterMap[cell.x + SoilWaterSystem.kTextureSize * cell.y].m_Amount;
            float y = ((cell.x < SoilWaterSystem.kTextureSize - 1) ? soilWaterMap[cell.x + 1 + SoilWaterSystem.kTextureSize * cell.y].m_Amount : 0);
            float x2 = ((cell.y < SoilWaterSystem.kTextureSize - 1) ? soilWaterMap[cell.x + SoilWaterSystem.kTextureSize * (cell.y + 1)].m_Amount : 0);
            float y2 = ((cell.x < SoilWaterSystem.kTextureSize - 1 && cell.y < SoilWaterSystem.kTextureSize - 1) ? soilWaterMap[cell.x + 1 + SoilWaterSystem.kTextureSize * (cell.y + 1)].m_Amount : 0);
            result.m_Amount = (short)Mathf.RoundToInt(math.lerp(math.lerp(x, y, cellCoords.x - (float)cell.x), math.lerp(x2, y2, cellCoords.x - (float)cell.x), cellCoords.y - (float)cell.y));
            __result = result;
        }
    }//class;

    [HarmonyPatch]
    internal static class TelecomCoverageSystemPatch
    {
        [HarmonyPatch(typeof(CellMapSystem<TelecomCoverage>), nameof(TelecomCoverageSystem.GetData))]
        [HarmonyPostfix]
        public static void GetData(CellMapSystem<TelecomCoverage> __instance, ref CellMapData<TelecomCoverage> __result, bool readOnly, ref JobHandle dependencies)
        {
            //__result.m_CellSize *= 4;
        }       
                
    }//class;

    [HarmonyPatch]
    internal static class TerrainAttractivenessSystemPatch
    {
        [HarmonyPatch(typeof(CellMapSystem<TerrainAttractiveness>), nameof(TerrainAttractivenessSystem.GetData))]
        [HarmonyPostfix]
        public static void GetData(CellMapSystem<TerrainAttractiveness> __instance, ref CellMapData<TerrainAttractiveness> __result, bool readOnly, ref JobHandle dependencies)
        {
            //__result.m_CellSize *= 4;
        }

        [HarmonyPatch(typeof(TerrainAttractivenessSystem), nameof(TerrainAttractivenessSystem.GetCellCenter))]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result, int index)
        {
            __result = CellMapSystemRe.GetCellCenter(index, TerrainAttractivenessSystem.kTextureSize);
        }
        
        /*
        [HarmonyPatch(typeof(TerrainAttractivenessSystem), nameof(TerrainAttractivenessSystem.EvaluateAttractiveness), new Type[] { typeof(float), typeof(TerrainAttractiveness), typeof(AttractivenessParameterData) })]
        [HarmonyPostfix]
        public static void EvaluateAttractiveness(ref float __result, float terrainHeight, TerrainAttractiveness attractiveness, AttractivenessParameterData parameters)
        {
            
            float num = parameters.m_ForestEffect * attractiveness.m_ForestBonus;
            float num2 = parameters.m_ShoreEffect * attractiveness.m_ShoreBonus;
            float num3 = math.min(parameters.m_HeightBonus.z, math.max(0f, terrainHeight - parameters.m_HeightBonus.x) * parameters.m_HeightBonus.y);
            __result = num + num2 + num3;
            
        }

        [HarmonyPatch(typeof(TerrainAttractivenessSystem), nameof(TerrainAttractivenessSystem.EvaluateAttractiveness), new Type[] { typeof(float3), typeof(CellMapData<TerrainAttractiveness>), typeof(TerrainHeightData), typeof(AttractivenessParameterData), typeof(NativeArray<int>) })]
        [HarmonyPostfix]
        public static void EvaluateAttractiveness(ref float __result, float3 position, CellMapData<TerrainAttractiveness> data, TerrainHeightData heightData, AttractivenessParameterData parameters, NativeArray<int> factors)
        {
            float num = TerrainUtils.SampleHeight(ref heightData, position);
            TerrainAttractiveness attractiveness = TerrainAttractivenessSystem.GetAttractiveness(position, data.m_Buffer);
            float num2 = parameters.m_ForestEffect * attractiveness.m_ForestBonus;
            AttractionSystem.SetFactor(factors, AttractionSystem.AttractivenessFactor.Forest, num2);
            float num3 = parameters.m_ShoreEffect * attractiveness.m_ShoreBonus;
            AttractionSystem.SetFactor(factors, AttractionSystem.AttractivenessFactor.Beach, num3);
            float num4 = math.min(parameters.m_HeightBonus.z, math.max(0f, num - parameters.m_HeightBonus.x) * parameters.m_HeightBonus.y);
            AttractionSystem.SetFactor(factors, AttractionSystem.AttractivenessFactor.Height, num4);
            __result = num2 + num3 + num4;
            
            __result = Systems.TerrainAttractivenessSystem.EvaluateAttractiveness(position, data, heightData, parameters, factors);
            return false;
        }*/

        [HarmonyPatch(typeof(TerrainAttractivenessSystem), nameof(TerrainAttractivenessSystem.GetAttractiveness))]
        [HarmonyPostfix]
        public static void GetAttractiveness(ref TerrainAttractiveness __result, float3 position, ref NativeArray<TerrainAttractiveness> attractivenessMap)
        {
            TerrainAttractiveness result = default(TerrainAttractiveness);
            int2 cell = CellMapSystemRe.GetCell(position, CellMapSystem<TerrainAttractiveness>.kMapSize, TerrainAttractivenessSystem.kTextureSize);
            float2 cellCoords = CellMapSystemRe.GetCellCoords(position, CellMapSystemRe.kMapSize, TerrainAttractivenessSystem.kTextureSize);
            if (cell.x < 0 || cell.x >= TerrainAttractivenessSystem.kTextureSize || cell.y < 0 || cell.y >= TerrainAttractivenessSystem.kTextureSize)
            {
                __result = result;
            }
            TerrainAttractiveness terrainAttractiveness = attractivenessMap[cell.x + TerrainAttractivenessSystem.kTextureSize * cell.y];
            TerrainAttractiveness terrainAttractiveness2 = ((cell.x < TerrainAttractivenessSystem.kTextureSize - 1) ? attractivenessMap[cell.x + 1 + TerrainAttractivenessSystem.kTextureSize * cell.y] : default(TerrainAttractiveness));
            TerrainAttractiveness terrainAttractiveness3 = ((cell.y < TerrainAttractivenessSystem.kTextureSize - 1) ? attractivenessMap[cell.x + TerrainAttractivenessSystem.kTextureSize * (cell.y + 1)] : default(TerrainAttractiveness));
            TerrainAttractiveness terrainAttractiveness4 = ((cell.x < TerrainAttractivenessSystem.kTextureSize - 1 && cell.y < TerrainAttractivenessSystem.kTextureSize - 1) ? attractivenessMap[cell.x + 1 + TerrainAttractivenessSystem.kTextureSize * (cell.y + 1)] : default(TerrainAttractiveness));
            result.m_ForestBonus = (short)Mathf.RoundToInt(math.lerp(math.lerp(terrainAttractiveness.m_ForestBonus, terrainAttractiveness2.m_ForestBonus, cellCoords.x - (float)cell.x), math.lerp(terrainAttractiveness3.m_ForestBonus, terrainAttractiveness4.m_ForestBonus, cellCoords.x - (float)cell.x), cellCoords.y - (float)cell.y));
            result.m_ShoreBonus = (short)Mathf.RoundToInt(math.lerp(math.lerp(terrainAttractiveness.m_ShoreBonus, terrainAttractiveness2.m_ShoreBonus, cellCoords.x - (float)cell.x), math.lerp(terrainAttractiveness3.m_ShoreBonus, terrainAttractiveness4.m_ShoreBonus, cellCoords.x - (float)cell.x), cellCoords.y - (float)cell.y));
            __result = result;
        }                
    }//class;

    [HarmonyPatch]
    internal static class TrafficAmbienceSystemPatch
    {
        /*
        [HarmonyPatch(typeof(TrafficAmbienceSystem), nameof(TrafficAmbienceSystem.GetData))]
        [HarmonyPostfix]
        public static void GetData(CellMapSystem<TrafficAmbienceCell> __instance, ref CellMapData<TrafficAmbienceCell> __result, bool readOnly, ref JobHandle dependencies)
        {
            __result.m_CellSize *= 4;
        }
        */

        [HarmonyPatch(typeof(TrafficAmbienceSystem), nameof(TrafficAmbienceSystem.GetCellCenter))]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result, int index)
        {
            __result = CellMapSystemRe.GetCellCenter(index, TrafficAmbienceSystem.kTextureSize);
        }

        [HarmonyPatch(typeof(TrafficAmbienceSystem), nameof(TrafficAmbienceSystem.GetTrafficAmbience2))]
        [HarmonyPostfix]
        public static void GetTrafficAmbience2(ref TrafficAmbienceCell __result, float3 position, ref NativeArray<TrafficAmbienceCell> trafficAmbienceMap, float maxPerCell)
        {
            TrafficAmbienceCell result = default(TrafficAmbienceCell);
            int2 cell = CellMapSystemRe.GetCell(position, CellMapSystemRe.kMapSize, TrafficAmbienceSystem.kTextureSize);
            float num = 0f;
            float num2 = 0f;
            for (int i = cell.x - 2; i <= cell.x + 2; i++)
            {
                for (int j = cell.y - 2; j <= cell.y + 2; j++)
                {
                    if (i >= 0 && i < TrafficAmbienceSystem.kTextureSize && j >= 0 && j < TrafficAmbienceSystem.kTextureSize)
                    {
                        int index = i + TrafficAmbienceSystem.kTextureSize * j;
                        float num3 = math.max(1f, math.distancesq(TrafficAmbienceSystem.GetCellCenter(index), position));
                        num += math.min(maxPerCell, trafficAmbienceMap[index].m_Traffic) / num3;
                        num2 += 1f / num3;
                    }
                }
            }
            result.m_Traffic = num / num2;
            __result = result;
        }
    }//class;

    [HarmonyPatch]
    internal static class WindSimulationSystemPatch
    {
        [HarmonyPatch(typeof(WindSimulationSystem), nameof(WindSimulationSystem.GetCellCenter))]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result, int index)
        {
            int3 @int = new int3(index % WindSimulationSystem.kResolution.x, index / WindSimulationSystem.kResolution.x % WindSimulationSystem.kResolution.y, index / (WindSimulationSystem.kResolution.x * WindSimulationSystem.kResolution.y));
            float3 result = CellMapSystemRe.kMapSize * new float3(((float)@int.x + 0.5f) / (float)WindSimulationSystem.kResolution.x, 0f, ((float)@int.y + 0.5f) / (float)WindSimulationSystem.kResolution.y) - CellMapSystemRe.kMapSize / 2;
            result.y = 100f + 1024f * ((float)@int.z + 0.5f) / (float)WindSimulationSystem.kResolution.z;
            __result = result;
        }

    }//class;


    [HarmonyPatch]
    internal static class WindSystemPatch
    {
        [HarmonyPatch(typeof(CellMapSystem<Wind>), "GetData")]
        [HarmonyPostfix]
        public static void GetData(CellMapSystem<Wind> __instance, ref CellMapData<Wind> __result, bool readOnly, ref JobHandle dependencies)
        {
            //__result.m_CellSize *= 4;
        }

        [HarmonyPatch(typeof(WindSystem), nameof(WindSystem.GetCellCenter))]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result, int index)
        {
            __result = CellMapSystemRe.GetCellCenter(index, WindSystem.kTextureSize);
        }

        [HarmonyPatch(typeof(WindSystem), nameof(WindSystem.GetWind))]
        [HarmonyPostfix]
        public static void GetWind(ref Wind __result, float3 position, ref NativeArray<Wind> windMap)
        {
            int2 cell = CellMapSystemRe.GetCell(position, CellMapSystemRe.kMapSize, WindSystem.kTextureSize);
            cell = math.clamp(cell, 0, WindSystem.kTextureSize - 1);
            float2 cellCoords = CellMapSystemRe.GetCellCoords(position, CellMapSystemRe.kMapSize, WindSystem.kTextureSize);
            int num = math.min(WindSystem.kTextureSize - 1, cell.x + 1);
            int num2 = math.min(WindSystem.kTextureSize - 1, cell.y + 1);
            Wind result = default(Wind);
            result.m_Wind = math.lerp(math.lerp(windMap[cell.x + WindSystem.kTextureSize * cell.y].m_Wind, windMap[num + WindSystem.kTextureSize * cell.y].m_Wind, cellCoords.x - (float)cell.x), math.lerp(windMap[cell.x + WindSystem.kTextureSize * num2].m_Wind, windMap[num + WindSystem.kTextureSize * num2].m_Wind, cellCoords.x - (float)cell.x), cellCoords.y - (float)cell.y);
            __result = result;
        }    

    }//class;

    [HarmonyPatch]
    internal static class ZoneAmbienceSystemPatch
    {
        [HarmonyPatch(typeof(CellMapSystem<ZoneAmbienceCell>), "GetData")]
        [HarmonyPostfix]
        public static void GetData(CellMapSystem<ZoneAmbienceCell> __instance, ref CellMapData<ZoneAmbienceCell> __result, bool readOnly, ref JobHandle dependencies)
        {
            //__result.m_CellSize *= 4;
        }

        [HarmonyPatch(typeof(ZoneAmbienceSystem), nameof(ZoneAmbienceSystem.GetCellCenter))]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result, int index)
        {
            __result = CellMapSystemRe.GetCellCenter(index, ZoneAmbienceSystem.kTextureSize);
        }    

    }//class;


    [HarmonyPatch]
    internal static class LandValueSystemPatch
    {
        [HarmonyPatch(typeof(CellMapSystem<LandValueCell>), "GetData")]
        [HarmonyPostfix]
        public static void GetData(CellMapSystem<LandValueCell> __instance, ref CellMapData<LandValueCell> __result, bool readOnly, ref JobHandle dependencies)
        {
            //__result.m_CellSize *= 4;
        }

        [HarmonyPatch(typeof(LandValueSystem), nameof(LandValueSystem.GetCellCenter))]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result, int index)
        {
            __result = CellMapSystemRe.GetCellCenter(index, LandValueSystem.kTextureSize);
        }
    }//LV class;



}//namespace
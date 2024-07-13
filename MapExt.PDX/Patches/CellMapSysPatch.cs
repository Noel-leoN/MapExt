using Game.Simulation;
using HarmonyLib;
using Unity.Collections;
using Colossal.Mathematics;
using Colossal.Collections;
using Colossal.Serialization.Entities;
using Unity.Mathematics;
using Game.Prefabs;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using System;

namespace MapExt.Patches
{
    /// <summary>
    /// 
    /// </summary>
    public static class CellMapStatic
    {
        public static float3 GetCellCenter(int index, int textureSize)
        {
            int num = index % textureSize;
            int num2 = index / textureSize;
            int num3 = 57344 / textureSize;
            return new float3(-0.5f * 57344 + ((float)num + 0.5f) * (float)num3, 0f, -0.5f * 57344 + ((float)num2 + 0.5f) * (float)num3);
        }

        public static float3 GetCellCenter2(int2 cell, int textureSize)
        {
            int num = 57344 / textureSize;
            return new float3(-0.5f * (float)57344 + ((float)cell.x + 0.5f) * (float)num, 0f, -0.5f * (float)57344 + ((float)cell.y + 0.5f) * (float)num);
        }
        public static Bounds3 GetCellBounds(int index, int textureSize)
        {
            int num = index % textureSize;
            int num2 = index / textureSize;
            int num3 = 57344 / textureSize;
            return new Bounds3(new float3(-0.5f * 57344 + (float)(num * num3), -100000f, -0.5f * 57344 + (float)(num2 * num3)), new float3(-0.5f * 57344 + ((float)num + 1f) * (float)num3, 100000f, -0.5f * 57344 + ((float)num2 + 1f) * (float)num3));
        }

        public static float2 GetCellCoords(float3 position, int mapSize, int textureSize)
        {
            return (0.5f + position.xz / mapSize) * textureSize;
        }

        public static int2 GetCell(float3 position, int mapSize, int textureSize)
        {
            return (int2)math.floor((0.5f + position.xz / mapSize) * textureSize);
        }

    }    

    //AirPollution class;bc cell;
    [HarmonyPatch]
    internal static class AirPollutionSystemPatch
    {
        //原系统method;

        [HarmonyPatch(typeof(AirPollutionSystem), "GetPollution")]
        [HarmonyPostfix]
        public static void GetPollution(ref AirPollution __result, float3 position, NativeArray<AirPollution> pollutionMap)
        {           
            __result = Systems.AirPollutionSystem.GetPollution(position, pollutionMap);
            //Mod.log.Info($"AirPollutionSystem.GetPollution {__result.m_Pollution} ");            
        }

        //原系统method;
        [HarmonyPatch(typeof(AirPollutionSystem), "GetCellCenter")]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result, int index)
        {
            __result = CellMapStatic.GetCellCenter(index, Systems.AirPollutionSystem.kTextureSize);
        }

        //cellmap method;instance;no cell       
        [HarmonyPatch(typeof(CellMapSystem<AirPollution>), "GetMap")]
        [HarmonyPrefix]
        public static bool GetMap(CellMapSystem<AirPollution> __instance, ref NativeArray<AirPollution> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == "Game.Simulation.AirPollutionSystem")
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.AirPollutionSystem>().GetMap(readOnly, out dependencies);
                return false;
            }
            return true;

        }

        //cellmap method;instance;cell        
        [HarmonyPatch(typeof(CellMapSystem<AirPollution>), "GetData")]
        [HarmonyPrefix]
        public static bool GetData(CellMapSystem<AirPollution> __instance, ref CellMapData<AirPollution> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == "Game.Simulation.AirPollutionSystem")
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.AirPollutionSystem>().GetData(readOnly, out dependencies);

                //debug;
                //Mod.log.Info($"AirPollutionGetData {__result.m_CellSize * __result.m_TextureSize}");
                //float2 cell = __result.m_CellSize * __result.m_TextureSize;
                //Mod.log.Info($"CellMapSystem<AirPollution>.GetData.harmony {cell} ");
                return false;
            }
            
            return true;
        }       

        //cellmap method;instance;no cell;
        [HarmonyPatch(typeof(CellMapSystem<AirPollution>), "AddReader")]
        [HarmonyPrefix]
        public static bool AddReader(CellMapSystem<AirPollution> __instance, JobHandle jobHandle)
        {
            if (__instance.GetType().FullName == "Game.Simulation.AirPollutionSystem")
            {
                __instance.World.GetExistingSystemManaged<Systems.AirPollutionSystem>().AddReader(jobHandle);
                return false;
            }
            return true;
        }
        //cellmap method;instance;no cell;
        [HarmonyPatch(typeof(CellMapSystem<AirPollution>), "AddWriter")]
        [HarmonyPrefix]
        public static bool AddWriter(CellMapSystem<AirPollution> __instance, JobHandle jobHandle)
        {
            if (__instance.GetType().FullName == "Game.Simulation.AirPollutionSystem")
            {
                __instance.World.GetExistingSystemManaged<Systems.AirPollutionSystem>().AddWriter(jobHandle);
                return false;
            }
            return true;
        }
    }//airpollution system class;

    //AvailabilityInfoToGridSystem;bc cell;
    [HarmonyPatch]
    class AvailabilityInfoToGridSystemPatch
    {
        //sys method;static;
        [HarmonyPatch(typeof(AvailabilityInfoToGridSystem), "GetCellCenter")]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result, int index)
        {
            __result = CellMapStatic.GetCellCenter(index, Systems.AvailabilityInfoToGridSystem.kTextureSize);
           
        }

        //sys method;static;
        [HarmonyPatch(typeof(AvailabilityInfoToGridSystem), "GetAvailabilityInfo")]
        [HarmonyPostfix]
        public static void GetAvailabilityInfo(ref AvailabilityInfoCell __result,float3 position, NativeArray<AvailabilityInfoCell> AvailabilityInfoMap)
        {
            __result = Systems.AvailabilityInfoToGridSystem.GetAvailabilityInfo(position, AvailabilityInfoMap);            
        }

        [HarmonyPatch(typeof(CellMapSystem<AvailabilityInfoCell>), "GetData")]
        [HarmonyPrefix]
        public static bool GetData(CellMapSystem<AvailabilityInfoCell> __instance, ref CellMapData<AvailabilityInfoCell> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == "Game.Simulation.AvailabilityInfoToGridSystem")
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.AvailabilityInfoToGridSystem>().GetData(readOnly, out dependencies);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<AvailabilityInfoCell>), "GetMap")]
        [HarmonyPrefix]
        public static bool GetMap(CellMapSystem<AvailabilityInfoCell> __instance, ref NativeArray<AvailabilityInfoCell> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == "Game.Simulation.AvailabilityInfoToGridSystem")
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.AvailabilityInfoToGridSystem>().GetMap(readOnly, out dependencies);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<AvailabilityInfoCell>), "AddReader")]
        [HarmonyPrefix]
        public static bool AddReader(CellMapSystem<AvailabilityInfoCell> __instance, JobHandle jobHandle)
        {
            if (__instance.GetType().FullName == "Game.Simulation.AvailabilityInfoToGridSystem")
            {
                __instance.World.GetExistingSystemManaged<Systems.AvailabilityInfoToGridSystem>().AddReader(jobHandle);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<AvailabilityInfoCell>), "AddWriter")]
        [HarmonyPrefix]
        public static bool AddWriter(CellMapSystem<AvailabilityInfoCell> __instance, JobHandle jobHandle)
        {
            if (__instance.GetType().FullName == "Game.Simulation.AvailabilityInfoToGridSystem")
            {
                __instance.World.GetExistingSystemManaged<Systems.AvailabilityInfoToGridSystem>().AddWriter(jobHandle);
                return false;
            }
            return true;
        }
    }//AvailabilityInfoToGridSystem class

    /*
    [HarmonyPatch]
    internal static class GroundPollutionSystemPatch
    {
        [HarmonyPatch(typeof(GroundPollutionSystem), nameof(GroundPollutionSystem.GetPollution))]
        [HarmonyPrefix]
        public static bool GetPollution(ref GroundPollution __result, float3 position, NativeArray<GroundPollution> pollutionMap)
        {           
            __result = Systems.GroundPollutionSystem.GetPollution(position, pollutionMap);
            return false;
        }


        [HarmonyPatch(typeof(GroundPollutionSystem), nameof(GroundPollutionSystem.GetCellCenter))]
        [HarmonyPrefix]
        public static bool GetCellCenter(ref float3 __result, int index)
        {
            __result = CellMapStatic.GetCellCenter(index, Systems.GroundPollutionSystem.kTextureSize);
            return false;
        }

        [HarmonyPatch(typeof(CellMapSystem<GroundPollution>), nameof(CellMapSystem<GroundPollution>.GetMap))]
        [HarmonyPrefix]
        public static bool GetMap(CellMapSystem<GroundPollution> __instance, ref NativeArray<GroundPollution> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == nameof(GroundPollutionSystem))
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.GroundPollutionSystem>().GetMap(readOnly, out dependencies);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<GroundPollution>), nameof(CellMapSystem<GroundPollution>.AddReader))]
        [HarmonyPrefix]
        public static bool AddReader(CellMapSystem<GroundPollution> __instance, JobHandle jobHandle)
        {
            if (__instance.GetType().FullName == nameof(GroundPollutionSystem))
            {
                __instance.World.GetExistingSystemManaged<Systems.GroundPollutionSystem>().AddReader(jobHandle);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<GroundPollution>), nameof(CellMapSystem<GroundPollution>.GetData))]
        [HarmonyPrefix]
        public static bool GetData(CellMapSystem<GroundPollution> __instance, ref CellMapData<GroundPollution> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == nameof(GroundPollutionSystem))
            {
                __result = __instance.World.GetExistingSystemManaged<ExtMap57km.Systems.GroundPollutionSystem>().GetData(readOnly, out dependencies);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<GroundPollution>), nameof(CellMapSystem<GroundPollution>.AddWriter))]
        [HarmonyPrefix]
        public static bool AddWriter(CellMapSystem<GroundPollution> __instance, JobHandle jobHandle)
        {
            if (__instance.GetType().FullName == nameof(GroundPollutionSystem))
            {
                __instance.World.GetExistingSystemManaged<Systems.GroundPollutionSystem>().AddWriter(jobHandle);
                return false;
            }
            return true;
        }
    }//class
    */

    /*
    [HarmonyPatch]
    internal static class GroundWaterSystemPatch
    {
        [HarmonyPatch(typeof(GroundWaterSystem), nameof(GroundWaterSystem.GetGroundWater), new Type[] { typeof(float3), typeof(NativeArray<GroundWater>) })]
        [HarmonyPrefix]
        public static bool GetGroundWater(ref GroundWater __result, float3 position, NativeArray<GroundWater> groundWaterMap)
        {            
            __result = Systems.GroundWaterSystem.GetGroundWater(position, groundWaterMap);
            return false;
        }

        [HarmonyPatch(typeof(GroundWaterSystem), nameof(GroundWaterSystem.TryGetCell))]
        [HarmonyPrefix]
        public static bool TryGetCell(ref bool __result, float3 position, ref int2 cell)
        {
            cell = CellMapSystem<GroundWater>.GetCell(position, CellMapSystem<GroundWater>.kMapSize, Systems.GroundWaterSystem.kTextureSize);
            __result = Systems.GroundWaterSystem.IsValidCell(cell);
            return false;
        }

        [HarmonyPatch(typeof(GroundWaterSystem), nameof(GroundWaterSystem.IsValidCell))]
        [HarmonyPrefix]
        public static bool IsValidCell(ref bool __result, int2 cell)
        {           
            __result = Systems.GroundWaterSystem.IsValidCell(cell);
            return false;
        }

        [HarmonyPatch(typeof(GroundWaterSystem), nameof(GroundWaterSystem.ConsumeGroundWater))]
        [HarmonyPrefix]
        public static bool ConsumeGroundWater(GroundWaterSystem __instance,float3 position, NativeArray<GroundWater> groundWaterMap, int amount)
        {            
            Unity.Assertions.Assert.IsTrue(amount >= 0);
            float2 @float = CellMapSystem<GroundWater>.GetCellCoords(position, CellMapSystem<GroundWater>.kMapSize, GroundWaterSystem.kTextureSize) - new float2(0.5f, 0.5f);
            int2 cell = new int2(Mathf.FloorToInt(@float.x), Mathf.FloorToInt(@float.y));
            int2 cell2 = new int2(cell.x + 1, cell.y);
            int2 cell3 = new int2(cell.x, cell.y + 1);
            int2 cell4 = new int2(cell.x + 1, cell.y + 1);
            GroundWater gw2 = Systems.GroundWaterSystem.GetGroundWater(groundWaterMap, cell);
            GroundWater gw3 = Systems.GroundWaterSystem.GetGroundWater(groundWaterMap, cell2);
            GroundWater gw4 = Systems.GroundWaterSystem.GetGroundWater(groundWaterMap, cell3);
            GroundWater gw5 = Systems.GroundWaterSystem.GetGroundWater(groundWaterMap, cell4);
            float sx = @float.x - (float)cell.x;
            float sy = @float.y - (float)cell.y;
            float num = math.ceil(Systems.GroundWaterSystem.Bilinear(gw2.m_Amount, 0, 0, 0, sx, sy));
            float num2 = math.ceil(Systems.GroundWaterSystem.Bilinear(0, gw3.m_Amount, 0, 0, sx, sy));
            float num3 = math.ceil(Systems.GroundWaterSystem.Bilinear(0, 0, gw4.m_Amount, 0, sx, sy));
            float num4 = math.ceil(Systems.GroundWaterSystem.Bilinear(0, 0, 0, gw5.m_Amount, sx, sy));
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
            Systems.GroundWaterSystem.SetGroundWater(groundWaterMap, cell, gw2);
            Systems.GroundWaterSystem.SetGroundWater(groundWaterMap, cell2, gw3);
            Systems.GroundWaterSystem.SetGroundWater(groundWaterMap, cell3, gw4);
            Systems.GroundWaterSystem.SetGroundWater(groundWaterMap, cell4, gw5);
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
            return false;
        }

        [HarmonyPatch(typeof(CellMapSystem<GroundWater>), nameof(CellMapSystem<GroundWater>.GetMap))]
        [HarmonyPrefix]
        public static bool GetMap(CellMapSystem<GroundWater> __instance, ref NativeArray<GroundWater> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == nameof(GroundWaterSystem))
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.GroundWaterSystem>().GetMap(readOnly, out dependencies);
                return false;
            }
            return true;
        }            

        [HarmonyPatch(typeof(CellMapSystem<GroundWater>), nameof(CellMapSystem<GroundWater>.GetData))]
        [HarmonyPrefix]
        public static bool GetData(CellMapSystem<GroundWater> __instance, ref CellMapData<GroundWater> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == nameof(GroundWaterSystem))
            {
                __result = __instance.World.GetExistingSystemManaged<ExtMap57km.Systems.GroundWaterSystem>().GetData(readOnly, out dependencies);
                return false;
            }
            return true;
        }

        //may not necessary if make ecs "postfix"(updateafter) of this sim system;下同；
        [HarmonyPatch(typeof(CellMapSystem<GroundWater>), nameof(CellMapSystem<GroundWater>.AddReader))]
        [HarmonyPrefix]
        public static bool AddReader(CellMapSystem<GroundWater> __instance, JobHandle jobHandle)
        {
            if (__instance.GetType().FullName == nameof(GroundWaterSystem))
            {
                __instance.World.GetExistingSystemManaged<Systems.GroundWaterSystem>().AddReader(jobHandle);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<GroundWater>), nameof(CellMapSystem<GroundWater>.AddWriter))]
        [HarmonyPrefix]
        public static bool AddWriter(CellMapSystem<GroundWater> __instance, JobHandle jobHandle)
        {
            if (__instance.GetType().FullName == nameof(GroundWaterSystem))
            {
                __instance.World.GetExistingSystemManaged<Systems.GroundWaterSystem>().AddWriter(jobHandle);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(GroundWaterSystem), nameof(GroundWaterSystem.GetCellCenter))]
        [HarmonyPrefix]
        public static bool GetCellCenter(ref float3 __result, int index)
        {
            __result = CellMapStatic.GetCellCenter(index, Systems.GroundWaterSystem.kTextureSize);
            return false;
        }
    }//class
    */

    /*
    [HarmonyPatch]
    internal static class NaturalResourceSystemPatch
    {       
        [HarmonyPatch(typeof(NaturalResourceSystem), nameof(NaturalResourceSystem.ResourceAmountToArea))]
        [HarmonyPrefix]
        public static bool ResourceAmountToArea(NaturalResourceSystem __instance,float __result, float amount)
        {
            if (__instance.GetType().FullName == nameof(NaturalResourceSystem))
            {
                float2 @float = 57344 / (float2)__instance.TextureSize;
                __result = amount * @float.x * @float.y / 10000f;
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<NaturalResourceCell>), nameof(CellMapSystem<NaturalResourceCell>.GetData))]
        [HarmonyPrefix]
        public static bool GetData(CellMapSystem<NaturalResourceCell> __instance, ref CellMapData<NaturalResourceCell> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == nameof(NaturalResourceSystem))
            {
                __result = __instance.World.GetExistingSystemManaged<ExtMap57km.Systems.NaturalResourceSystem>().GetData(readOnly, out dependencies);
                return false;
            }
            return true;
        }

        //may not necessary if make ecs "postfix"(updateafter) of this sim system;下同；
        [HarmonyPatch(typeof(CellMapSystem<NaturalResourceCell>), nameof(CellMapSystem<NaturalResourceCell>.AddReader))]
        [HarmonyPrefix]
        public static bool AddReader(CellMapSystem<NaturalResourceCell> __instance, JobHandle jobHandle)
        {
            if (__instance.GetType().FullName == nameof(NaturalResourceSystem))
            {
                __instance.World.GetExistingSystemManaged<Systems.NaturalResourceSystem>().AddReader(jobHandle);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<NaturalResourceCell>), nameof(CellMapSystem<NaturalResourceCell>.AddWriter))]
        [HarmonyPrefix]
        public static bool AddWriter(CellMapSystem<NaturalResourceCell> __instance, JobHandle jobHandle)
        {
            if (__instance.GetType().FullName == nameof(NaturalResourceSystem))
            {
                __instance.World.GetExistingSystemManaged<Systems.NaturalResourceSystem>().AddWriter(jobHandle);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<NaturalResourceCell>), nameof(CellMapSystem<NaturalResourceCell>.GetMap))]
        [HarmonyPrefix]
        public static bool GetMap(CellMapSystem<NaturalResourceCell> __instance, ref NativeArray<NaturalResourceCell> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == nameof(NaturalResourceSystem))
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.NaturalResourceSystem>().GetMap(readOnly, out dependencies);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<NaturalResourceCell>), nameof(CellMapSystem<NaturalResourceCell>.GetCellCenter), new Type[] { typeof(int2), typeof(int) })]
        [HarmonyPrefix]
        public static bool GetCellCenter(ref float3 __result, int2 cell,int textureSize)
        {
            __result = CellMapStatic.GetCellCenter2(cell, textureSize);
            return false;
        }

        [HarmonyPatch(typeof(CellMapSystem<NaturalResourceCell>), nameof(CellMapSystem<NaturalResourceCell>.GetCellCenter), new Type[] { typeof(int), typeof(int) })]
        [HarmonyPrefix]
        public static bool GetCellCenter(ref float3 __result, int index, int textureSize)
        {
            __result = CellMapStatic.GetCellCenter(index, textureSize);
            return false;
        }


    }//NatualResourceSystem class;
    */

    /*
    [HarmonyPatch]
    class NoisePollutionSystemPatch
    {
        [HarmonyPatch(typeof(NoisePollutionSystem), "GetCellCenter")]
        [HarmonyPrefix]
        public static bool GetCellCenter(CellMapSystem<NoisePollution> __instance, ref float3 __result, int index)
        {
            if (__instance.GetType().FullName == "Game.Simulation.NoisePollutionSystem")
            {
                __result = CellMapStatic.GetCellCenter(index, Systems.NoisePollutionSystem.kTextureSize);
            return false;
            }
            return true;
        }

        
        [HarmonyPatch(typeof(NoisePollutionSystem), "GetPollution")]
        [HarmonyPrefix]
        public static bool GetPollution(CellMapSystem<NoisePollution> __instance, ref NoisePollution __result, float3 position, NativeArray<NoisePollution> pollutionMap)
        {
            if (__instance.GetType().FullName == "Game.Simulation.NoisePollutionSystem")
            {
                __result = Systems.NoisePollutionSystem.GetPollution(position, pollutionMap);
            return false;
            }
            return true;
        }
        
        [HarmonyPatch(typeof(CellMapSystem<NoisePollution>), "GetData")]
        [HarmonyPrefix]
        public static bool GetData(CellMapSystem<NoisePollution> __instance, ref CellMapData<NoisePollution> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == "Game.Simulation.NoisePollutionSystem")
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.NoisePollutionSystem>().GetData(readOnly, out dependencies);
                return false;
            }
            return true;
        }

        
        [HarmonyPatch(typeof(CellMapSystem<NoisePollution>), "GetMap")]
        [HarmonyPrefix]
        public static bool GetMap(CellMapSystem<NoisePollution> __instance, ref NativeArray<NoisePollution> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == "Game.Simulation.NoisePollutionSystem")
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.NoisePollutionSystem>().GetMap(readOnly, out dependencies);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<NoisePollution>), "AddReader")]
        [HarmonyPrefix]
        public static bool AddReader(CellMapSystem<NoisePollution> __instance, JobHandle jobHandle)
        {
            if (__instance.GetType().FullName == "Game.Simulation.NoisePollutionSystem")
            {
                __instance.World.GetExistingSystemManaged<Systems.NoisePollutionSystem>().AddReader(jobHandle);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<NoisePollution>), "AddWriter")]
        [HarmonyPrefix]
        public static bool AddWriter(CellMapSystem<NoisePollution> __instance, JobHandle jobHandle)
        {
            if (__instance.GetType().FullName == "Game.Simulation.NoisePollutionSystem")
            {
                __instance.World.GetExistingSystemManaged<Systems.NoisePollutionSystem>().AddWriter(jobHandle);
                return false;
            }
            return true;
        }

    }//NoisePollutionSystem class;
    */


    [HarmonyPatch]
    class PopulationToGridSystemPatch
    {
        [HarmonyPatch(typeof(PopulationToGridSystem), "GetCellCenter")]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result, int index)
        {
            __result = CellMapStatic.GetCellCenter(index, Systems.PopulationToGridSystem.kTextureSize);
        }

        [HarmonyPatch(typeof(PopulationToGridSystem), nameof(PopulationToGridSystem.GetPopulation))]
        [HarmonyPostfix]
        public static void GetPopulation(ref PopulationCell __result, float3 position, NativeArray<PopulationCell> populationMap)
        {
            __result = Systems.PopulationToGridSystem.GetPopulation(position, populationMap);
        }

        [HarmonyPatch(typeof(CellMapSystem<PopulationCell>), "GetData")]
        [HarmonyPrefix]
        public static bool GetData(CellMapSystem<PopulationCell> __instance, ref CellMapData<PopulationCell> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == "Game.Simulation.PopulationToGridSystem")
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.PopulationToGridSystem>().GetData(readOnly, out dependencies);
                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<PopulationCell>), "GetMap")]
        [HarmonyPrefix]
        public static bool GetMap(CellMapSystem<PopulationCell> __instance, ref NativeArray<PopulationCell> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == "Game.Simulation.PopulationToGridSystem")
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.PopulationToGridSystem>().GetMap(readOnly, out dependencies);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<PopulationCell>), "AddReader")]
        [HarmonyPrefix]
        public static bool AddReader(CellMapSystem<PopulationCell> __instance, JobHandle jobHandle)
        {
            if (__instance.GetType().FullName == "Game.Simulation.PopulationToGridSystem")
            {
                __instance.World.GetExistingSystemManaged<Systems.PopulationToGridSystem>().AddReader(jobHandle);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<PopulationCell>), "AddWriter")]
        [HarmonyPrefix]
        public static bool AddWriter(CellMapSystem<PopulationCell> __instance, JobHandle jobHandle)
        {
            if (__instance.GetType().FullName == "Game.Simulation.PopulationToGridSystem")
            {
                __instance.World.GetExistingSystemManaged<Systems.PopulationToGridSystem>().AddWriter(jobHandle);
                return false;
            }
            return true;
        }

    }//class;

    [HarmonyPatch]
    class SoilWaterSystemPatch
    {
        [HarmonyPatch(typeof(SoilWaterSystem), "GetCellCenter")]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result, int index)
        {
            __result = CellMapStatic.GetCellCenter(index, Systems.SoilWaterSystem.kTextureSize);
        }

        [HarmonyPatch(typeof(SoilWaterSystem), "GetSoilWater")]
        [HarmonyPostfix]
        public static void GetSoilWater(ref SoilWater __result, float3 position, NativeArray<SoilWater> soilWaterMap)
        {
            __result = Systems.SoilWaterSystem.GetSoilWater(position, soilWaterMap);
        }

        [HarmonyPatch(typeof(CellMapSystem<SoilWater>), "GetMap")]
        [HarmonyPrefix]
        public static bool GetMap(CellMapSystem<SoilWater> __instance, ref NativeArray<SoilWater> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == "Game.Simulation.SoilWaterSystem")
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.SoilWaterSystem>().GetMap(readOnly, out dependencies);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<SoilWater>), "GetData")]
        [HarmonyPrefix]
        public static bool GetData(CellMapSystem<SoilWater> __instance, ref CellMapData<SoilWater> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == "Game.Simulation.SoilWaterSystem")
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.SoilWaterSystem>().GetData(readOnly, out dependencies);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<SoilWater>), "AddReader")]
        [HarmonyPrefix]
        public static bool AddReader(CellMapSystem<SoilWater> __instance, JobHandle jobHandle)
        {
            if (__instance.GetType().FullName == "Game.Simulation.SoilWaterSystem")
            {
                __instance.World.GetExistingSystemManaged<Systems.SoilWaterSystem>().AddReader(jobHandle);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<SoilWater>), "AddWriter")]
        [HarmonyPrefix]
        public static bool AddWriter(CellMapSystem<SoilWater> __instance, JobHandle jobHandle)
        {
            if (__instance.GetType().FullName == "Game.Simulation.SoilWaterSystem")
            {
                __instance.World.GetExistingSystemManaged<Systems.SoilWaterSystem>().AddWriter(jobHandle);
                return false;
            }
            return true;
        }
    }//class;

    [HarmonyPatch]
    internal static class TelecomCoverageSystemPatch
    { 
        [HarmonyPatch(typeof(CellMapSystem<TelecomCoverage>), "GetData")]
        [HarmonyPrefix]
        public static bool GetData(CellMapSystem<TelecomCoverage> __instance, ref CellMapData<TelecomCoverage> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == "Game.Simulation.TelecomCoverageSystem")
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.TelecomCoverageSystem>().GetData(readOnly, out dependencies);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<TelecomCoverage>), "GetMap")]
        [HarmonyPrefix]
        public static bool GetMap(CellMapSystem<TelecomCoverage> __instance, ref NativeArray<TelecomCoverage> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == "Game.Simulation.TelecomCoverageSystem")
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.TelecomCoverageSystem>().GetMap(readOnly, out dependencies);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<TelecomCoverage>), "AddReader")]
        [HarmonyPrefix]
        public static bool AddReader(CellMapSystem<TelecomCoverage> __instance, JobHandle jobHandle)
        {
            if (__instance.GetType().FullName == "Game.Simulation.TelecomCoverageSystem")
            {
                __instance.World.GetExistingSystemManaged<Systems.TelecomCoverageSystem>().AddReader(jobHandle);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<TelecomCoverage>), "AddWriter")]
        [HarmonyPrefix]
        public static bool AddWriter(CellMapSystem<TelecomCoverage> __instance, JobHandle jobHandle)
        {
            if (__instance.GetType().FullName == "Game.Simulation.TelecomCoverageSystem")
            {
                __instance.World.GetExistingSystemManaged<Systems.TelecomCoverageSystem>().AddWriter(jobHandle);
                return false;
            }
            return true;
        }

    }//class;


    [HarmonyPatch]
    internal static class TelecomPreviewSystemPatch
    {
        [HarmonyPatch(typeof(CellMapSystem<TelecomCoverage>), "GetData")]
        [HarmonyPrefix]
        public static bool GetData(CellMapSystem<TelecomCoverage> __instance, ref CellMapData<TelecomCoverage> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == "Game.Tools.TelecomPreviewSystem")
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.TelecomPreviewSystem>().GetData(readOnly, out dependencies);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<TelecomCoverage>), "GetMap")]
        [HarmonyPrefix]
        public static bool GetMap(CellMapSystem<TelecomCoverage> __instance, ref NativeArray<TelecomCoverage> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == "Game.Tools.TelecomPreviewSystem")
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.TelecomPreviewSystem>().GetMap(readOnly, out dependencies);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<TelecomCoverage>), "AddReader")]
        [HarmonyPrefix]
        public static bool AddReader(CellMapSystem<TelecomCoverage> __instance, JobHandle jobHandle)
        {
            if (__instance.GetType().FullName == "Game.Tools.TelecomPreviewSystem")
            {
                __instance.World.GetExistingSystemManaged<Systems.TelecomPreviewSystem>().AddReader(jobHandle);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<TelecomCoverage>), "AddWriter")]
        [HarmonyPrefix]
        public static bool AddWriter(CellMapSystem<TelecomCoverage> __instance, JobHandle jobHandle)
        {
            if (__instance.GetType().FullName == "Game.Tools.TelecomPreviewSystem")
            {
                __instance.World.GetExistingSystemManaged<Systems.TelecomPreviewSystem>().AddWriter(jobHandle);
                return false;
            }
            return true;
        }
    }//TelecomPreView class;

    [HarmonyPatch]
    internal static class TerrainAttractivenessSystemPatch
    {
        [HarmonyPatch(typeof(TerrainAttractivenessSystem), "GetCellCenter")]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result, int index)
        {
            __result = CellMapStatic.GetCellCenter(index, Systems.TerrainAttractivenessSystem.kTextureSize);
        }

        [HarmonyPatch(typeof(TerrainAttractivenessSystem), nameof(TerrainAttractivenessSystem.EvaluateAttractiveness),new Type[] {typeof(float),typeof(TerrainAttractiveness),typeof(AttractivenessParameterData) })]
        [HarmonyPostfix]
        public static void EvaluateAttractiveness(ref float __result, float terrainHeight, TerrainAttractiveness attractiveness, AttractivenessParameterData parameters)
        {
            __result = Systems.TerrainAttractivenessSystem.EvaluateAttractiveness(terrainHeight, attractiveness, parameters);
        }

        [HarmonyPatch(typeof(TerrainAttractivenessSystem), nameof(TerrainAttractivenessSystem.EvaluateAttractiveness), new Type[] {typeof(float3),typeof(CellMapData<TerrainAttractiveness>),typeof(TerrainHeightData),typeof(AttractivenessParameterData),typeof(NativeArray<int>) })]
        [HarmonyPostfix]
        public static void EvaluateAttractiveness(ref float __result, float3 position, CellMapData<TerrainAttractiveness> data, TerrainHeightData heightData, AttractivenessParameterData parameters, NativeArray<int> factors)
        {
            __result = Systems.TerrainAttractivenessSystem.EvaluateAttractiveness(position, data, heightData, parameters, factors);
        }

        [HarmonyPatch(typeof(TerrainAttractivenessSystem), nameof(TerrainAttractivenessSystem.GetAttractiveness))]
        [HarmonyPostfix]
        public static void GetAttractiveness(ref TerrainAttractiveness __result, float3 position, NativeArray<TerrainAttractiveness> attractivenessMap)
        {
            __result = Systems.TerrainAttractivenessSystem.GetAttractiveness(position, attractivenessMap);
        }

        [HarmonyPatch(typeof(CellMapSystem<TerrainAttractiveness>), "GetData")]
        [HarmonyPrefix]
        public static bool GetData(CellMapSystem<TerrainAttractiveness> __instance, ref CellMapData<TerrainAttractiveness> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == "Game.Simulation.TerrainAttractivenessSystem")
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.TerrainAttractivenessSystem>().GetData(readOnly, out dependencies);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<TerrainAttractiveness>), "GetMap")]
        [HarmonyPrefix]
        public static bool GetMap(CellMapSystem<TerrainAttractiveness> __instance, ref NativeArray<TerrainAttractiveness> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == "Game.Simulation.TerrainAttractivenessSystem")
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.TerrainAttractivenessSystem>().GetMap(readOnly, out dependencies);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<TerrainAttractiveness>), "AddReader")]
        [HarmonyPrefix]
        public static bool AddReader(CellMapSystem<TerrainAttractiveness> __instance, JobHandle jobHandle)
        {
            if (__instance.GetType().FullName == "Game.Simulation.TerrainAttractivenessSystem")
            {
                __instance.World.GetExistingSystemManaged<Systems.TerrainAttractivenessSystem>().AddReader(jobHandle);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<TerrainAttractiveness>), "AddWriter")]
        [HarmonyPrefix]
        public static bool AddWriter(CellMapSystem<TerrainAttractiveness> __instance, JobHandle jobHandle)
        {
            if (__instance.GetType().FullName == "Game.Simulation.TerrainAttractivenessSystem")
            {
                __instance.World.GetExistingSystemManaged<Systems.TerrainAttractivenessSystem>().AddWriter(jobHandle);
                return false;
            }
            return true;
        }
    }//class;

    /*
    [HarmonyPatch]
    internal static class TrafficAmbienceSystemPatch
    {
        [HarmonyPatch(typeof(TrafficAmbienceSystem), nameof(TrafficAmbienceSystem.GetCellCenter))]
        [HarmonyPrefix]
        public static bool GetCellCenter(ref float3 __result, int index)
        {
            __result = CellMapStatic.GetCellCenter(index, Systems.TrafficAmbienceSystem.kTextureSize);
            return false;
        }

        [HarmonyPatch(typeof(TrafficAmbienceSystem), nameof(TrafficAmbienceSystem.GetTrafficAmbience2))]
        [HarmonyPrefix]
        public static bool GetTrafficAmbience2(ref TrafficAmbienceCell __result,float3 position, NativeArray<TrafficAmbienceCell> trafficAmbienceMap, float maxPerCell)
        {            
            __result = Systems.TrafficAmbienceSystem.GetTrafficAmbience2(position, trafficAmbienceMap, maxPerCell);
            return false;
        }

        [HarmonyPatch(typeof(CellMapSystem<TrafficAmbienceCell>), nameof(CellMapSystem<TrafficAmbienceCell>.GetData))]
        [HarmonyPrefix]
        public static bool GetData(CellMapSystem<TrafficAmbienceCell> __instance, ref CellMapData<TrafficAmbienceCell> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == nameof(TrafficAmbienceSystem))
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.TrafficAmbienceSystem>().GetData(readOnly, out dependencies);
                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<TrafficAmbienceCell>), nameof(CellMapSystem<TrafficAmbienceCell>.GetMap))]
        [HarmonyPrefix]
        public static bool GetMap(CellMapSystem<TrafficAmbienceCell> __instance, ref NativeArray<TrafficAmbienceCell> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == nameof(TrafficAmbienceSystem))
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.TrafficAmbienceSystem>().GetMap(readOnly, out dependencies);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<TrafficAmbienceCell>), nameof(CellMapSystem<TrafficAmbienceCell>.AddReader))]
        [HarmonyPrefix]
        public static bool AddReader(CellMapSystem<TrafficAmbienceCell> __instance, JobHandle jobHandle)
        {
            string name = __instance.GetType().FullName;
            if (name == nameof(TrafficAmbienceSystem))
            {
                __instance.World.GetExistingSystemManaged<Systems.TrafficAmbienceSystem>().AddReader(jobHandle);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<TrafficAmbienceCell>), nameof(CellMapSystem<TrafficAmbienceCell>.AddWriter))]
        [HarmonyPrefix]
        public static bool AddWriter(CellMapSystem<TrafficAmbienceCell> __instance, JobHandle jobHandle)
        {
            if (__instance.GetType().FullName == nameof(TrafficAmbienceSystem))
            {
                __instance.World.GetExistingSystemManaged<Systems.TrafficAmbienceSystem>().AddWriter(jobHandle);
                return false;
            }
            return true;
        }
    }//class;
    */
    
    [HarmonyPatch]
    internal static class WindSystemPatch
    {
        [HarmonyPatch(typeof(WindSystem), "GetCellCenter")]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result, int index)
        {
            __result = CellMapStatic.GetCellCenter(index, Systems.WindSystem.kTextureSize);
        }

        [HarmonyPatch(typeof(WindSystem), "GetWind")]
        [HarmonyPostfix]
        public static void GetWind(ref Wind __result, float3 position, NativeArray<Wind> windMap)
        {
            __result = Systems.WindSystem.GetWind(position, windMap);
        }

        [HarmonyPatch(typeof(CellMapSystem<Wind>), "GetData")]
        [HarmonyPrefix]
        public static bool GetData(CellMapSystem<Wind> __instance, ref CellMapData<Wind> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == "Game.Simulation.WindSystem")
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.WindSystem>().GetData(readOnly, out dependencies);
                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<Wind>), "GetMap")]
        [HarmonyPrefix]
        public static bool GetMap(CellMapSystem<Wind> __instance, ref NativeArray<Wind> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == "Game.Simulation.WindSystem")
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.WindSystem>().GetMap(readOnly, out dependencies);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<Wind>), "AddReader")]
        [HarmonyPrefix]
        public static bool AddReader(CellMapSystem<Wind> __instance, JobHandle jobHandle)
        {
                if (__instance.GetType().FullName == "Game.Simulation.WindSystem")
            {
                    __instance.World.GetExistingSystemManaged<Systems.WindSystem>().AddReader(jobHandle);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<Wind>),"AddWriter")]
        public static bool AddWriter(CellMapSystem<Wind> __instance, JobHandle jobHandle)
        {
            if (__instance.GetType().FullName == "Game.Simulation.WindSystem")
            {
                __instance.World.GetExistingSystemManaged<Systems.WindSystem>().AddWriter(jobHandle);
                return false;
            }
            return true;
        }

    }//class;

    /*
    [HarmonyPatch]
    internal static class ZoneAmbienceSystemPatch
    {
        [HarmonyPatch(typeof(CellMapSystem<ZoneAmbienceCell>), nameof(CellMapSystem<ZoneAmbienceCell>.AddReader))]
        [HarmonyPrefix]
        public static bool AddReader(CellMapSystem<ZoneAmbienceCell> __instance, JobHandle jobHandle)
        {
            if (__instance.GetType().FullName == nameof(ZoneAmbienceSystem))
            {
                __instance.World.GetExistingSystemManaged<Systems.ZoneAmbienceSystem>().AddReader(jobHandle);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<ZoneAmbienceCell>), nameof(CellMapSystem<ZoneAmbienceCell>.AddWriter))]
        [HarmonyPrefix]
        public static bool AddWriter(CellMapSystem<ZoneAmbienceCell> __instance, JobHandle jobHandle)
        {
            if (__instance.GetType().FullName == nameof(ZoneAmbienceSystem))
            {
                __instance.World.GetExistingSystemManaged<Systems.ZoneAmbienceSystem>().AddWriter(jobHandle);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<ZoneAmbienceCell>), nameof(CellMapSystem<ZoneAmbienceCell>.GetData))]
        [HarmonyPrefix]
        public static bool GetData(CellMapSystem<ZoneAmbienceCell> __instance, ref CellMapData<ZoneAmbienceCell> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == nameof(ZoneAmbienceSystem))
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.ZoneAmbienceSystem>().GetData(readOnly, out dependencies);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<ZoneAmbienceCell>), nameof(CellMapSystem<ZoneAmbienceCell>.GetMap))]
        [HarmonyPrefix]
        public static bool GetMap(CellMapSystem<ZoneAmbienceCell> __instance, ref NativeArray<ZoneAmbienceCell> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == nameof(ZoneAmbienceSystem))
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.ZoneAmbienceSystem>().GetMap(readOnly, out dependencies);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(ZoneAmbienceSystem), nameof(ZoneAmbienceSystem.GetCellCenter))]
        [HarmonyPrefix]
        public static bool GetCellCenter(ref float3 __result, int index)
        {
            __result = CellMapStatic.GetCellCenter(index, Systems.ZoneAmbienceSystem.kTextureSize);
            return false;
        }      
    }// class;
    */



    [HarmonyPatch]
    internal static class LandValueSystemPatch
    {
        [HarmonyPatch(typeof(CellMapSystem<LandValueCell>), "AddReader")]
        [HarmonyPrefix]
        public static bool AddReader(CellMapSystem<LandValueCell> __instance, JobHandle jobHandle)
        {
            if (__instance.GetType().FullName == "Game.Simulation.LandValueSystem")
            {
                __instance.World.GetExistingSystemManaged<Systems.LandValueSystem>().AddReader(jobHandle);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<LandValueCell>), "AddWriter")]
        [HarmonyPrefix]
        public static bool AddWriter(CellMapSystem<LandValueCell> __instance, JobHandle jobHandle)
        {
            if (__instance.GetType().FullName == "Game.Simulation.LandValueSystem")
            {
                __instance.World.GetExistingSystemManaged<Systems.LandValueSystem>().AddWriter(jobHandle);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<LandValueCell>), "GetData")]
        [HarmonyPrefix]
        public static bool GetData(CellMapSystem<LandValueCell> __instance, ref CellMapData<LandValueCell> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == "Game.Simulation.LandValueSystem")
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.LandValueSystem>().GetData(readOnly, out dependencies);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<LandValueCell>), "GetMap")]
        [HarmonyPrefix]
        public static bool GetMap(CellMapSystem<LandValueCell> __instance, ref NativeArray<LandValueCell> __result, bool readOnly, ref JobHandle dependencies)
        {
            if (__instance.GetType().FullName == "Game.Simulation.LandValueSystem")
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.LandValueSystem>().GetMap(readOnly, out dependencies);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(LandValueSystem), "GetCellCenter")]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result, int index)
        {           
            __result = CellMapStatic.GetCellCenter(index, Systems.LandValueSystem.kTextureSize);
            
        }

        [HarmonyPatch(typeof(LandValueSystem), "GetCellIndex")]
        [HarmonyPostfix]
        public static void GetCellIndex(ref int __result, float3 pos)
        {
            __result = Systems.LandValueSystem.GetCellIndex(pos);
        }
        
    }//LV class;


    [HarmonyPatch]
    class WindSimulationSystemPatch
    {
        [HarmonyPatch(typeof(WindSimulationSystem), "GetCellCenter")]
        [HarmonyPostfix]
        public static void GetCellCenter(ref float3 __result, int index)
        {
            int3 @int = new int3(index % WindSimulationSystem.kResolution.x, index / WindSimulationSystem.kResolution.x % WindSimulationSystem.kResolution.y, index / (WindSimulationSystem.kResolution.x * WindSimulationSystem.kResolution.y));
            float3 result = 57344 * new float3(((float)@int.x + 0.5f) / (float)WindSimulationSystem.kResolution.x, 0f, ((float)@int.y + 0.5f) / (float)WindSimulationSystem.kResolution.y) - 57344 / 2;
            result.y = 100f + 1024f * ((float)@int.z + 0.5f) / (float)WindSimulationSystem.kResolution.z;
            __result = result;
        }


        [HarmonyPatch(typeof(WindSimulationSystem), "GetCells")]
        [HarmonyPrefix]
        public static bool GetCells(WindSimulationSystem __instance, ref JobHandle deps)
        {
            if (__instance.GetType().FullName == "Game.Simulation.WindSimulationSystem")
            {
                __instance.World.GetExistingSystemManaged<Systems.WindSimulationSystem>().GetCells(out deps);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(WindSimulationSystem), "AddReader")]
        [HarmonyPrefix]
        public static bool AddReader(WindSimulationSystem __instance, JobHandle reader)
        {
            if (__instance.GetType().FullName == "Game.Simulation.WindSimulationSystem")
            {
                __instance.World.GetExistingSystemManaged<Systems.WindSimulationSystem>().AddReader(reader);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(WindSimulationSystem), "SetWind")]
        [HarmonyPrefix]
        public static bool SetWind(WindSimulationSystem __instance, float2 direction, float pressure)
        {
            if (__instance.GetType().FullName == "Game.Simulation.WindSimulationSystem")
            {
                __instance.World.GetExistingSystemManaged<Systems.WindSimulationSystem>().SetWind(direction, pressure);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(WindSimulationSystem), "SetDefaults")]
        [HarmonyPrefix]
        public static bool SetDefaults(WindSimulationSystem __instance, Context context)
        {
            if (__instance.GetType().FullName == "Game.Simulation.WindSimulationSystem")
            {
                __instance.World.GetExistingSystemManaged<Systems.WindSimulationSystem>().SetDefaults(context);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(WindSimulationSystem), "DebugLoad")]
        [HarmonyPrefix]
        public static bool DebugLoad(WindSimulationSystem __instance)
        {
            if (__instance.GetType().FullName == "Game.Simulation.WindSimulationSystem")
            {
                __instance.World.GetExistingSystemManaged<Systems.WindSimulationSystem>().DebugLoad();
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(WindSimulationSystem), "DebugSave")]
        [HarmonyPrefix]
        public static bool DebugSave(WindSimulationSystem __instance)
        {
            if (__instance.GetType().FullName == "Game.Simulation.WindSimulationSystem")
            {
                __instance.World.GetExistingSystemManaged<Systems.WindSimulationSystem>().DebugSave();
                return false;
            }
            return true;
        }

    }


}//namespace
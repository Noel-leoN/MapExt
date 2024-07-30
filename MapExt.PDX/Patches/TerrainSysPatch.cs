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
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using System.Reflection;
using UnityEngine.Rendering.HighDefinition;

namespace MapExt.Patches
{
    /// <summary>
    /// 
    /// </summary>

    [HarmonyPatch]
    internal static class TerrainSysPatch
    {
        //原系统method;

        [HarmonyPatch(typeof(TerrainSystem), "FinalizeTerrainData")]
        [HarmonyPrefix]
        public static bool FinalizeTerrainData(TerrainSystem __instance,Texture2D map, Texture2D worldMap, float2 heightScaleOffset, float2 inMapCorner, float2 inMapSize, float2 inWorldCorner, float2 inWorldSize, float2 inWorldHeightMinMax)
        {
            {
                __instance.heightScaleOffset = heightScaleOffset;
                /*
                if (math.all(inWorldSize == inMapSize) || __instance.worldHeightmap == null)
                {
                    TerrainSystem.baseLod = 0;
                    __instance.playableArea = inMapSize;
                    __instance.worldSize = inMapSize;
                    __instance.playableOffset = inMapCorner;
                    __instance.worldOffset = inMapCorner;                   
                }
                else
                {
                    TerrainSystem.baseLod = 0;//org=1;
                    __instance.playableArea = inMapSize;
                    __instance.worldSize = inWorldSize;
                    __instance.playableOffset = inMapCorner;
                    __instance.worldOffset = inWorldCorner;
                }*/
                //更改为baselod=0,世界地图与可玩地图；
                Traverse.Create(__instance).Method(nameof(TerrainSystem.baseLod)).SetValue(0);
                Traverse.Create(__instance).Method(nameof(TerrainSystem.playableArea)).SetValue(inMapSize);
                Traverse.Create(__instance).Method(nameof(TerrainSystem.worldSize)).SetValue(inWorldSize);
                Traverse.Create(__instance).Method(nameof(TerrainSystem.playableOffset)).SetValue(inMapCorner);
                Traverse.Create(__instance).Method(nameof(TerrainSystem.worldOffset)).SetValue(inWorldCorner);

                __instance.m_NewMap = true;
                __instance.m_NewMapThisFrame = true;

                //__instance.worldHeightMinMax = inWorldHeightMinMax;
                Traverse.Create(__instance).Method(nameof(TerrainSystem.worldHeightMinMax)).SetValue(inWorldHeightMinMax);

                __instance.m_WorldOffsetScale = new float4((__instance.playableOffset - __instance.worldOffset) / __instance.worldSize, __instance.playableArea / __instance.worldSize);
                float3 @float = new float3(__instance.playableArea.x, heightScaleOffset.x, __instance.playableArea.y);
                float3 xyz = 1f / @float;
                float3 xyz2 = -__instance.positionOffset;
                __instance.m_MapOffsetScale = new Vector4(0f - __instance.positionOffset.x, 0f - __instance.positionOffset.z, 1f / @float.x, 1f / @float.z);
                if (__instance.m_HeightmapCascade == null || __instance.m_HeightmapCascade.width != __instance.heightmap.width || __instance.m_HeightmapCascade.height != __instance.heightmap.height)
                {
                    if (__instance.m_HeightmapCascade != null)
                    {
                        __instance.m_HeightmapCascade.Release();
                        UnityEngine.Object.Destroy(__instance.m_HeightmapCascade);
                        __instance.m_HeightmapCascade = null;
                    }
                    __instance.m_HeightmapCascade = new RenderTexture(__instance.heightmap.width, __instance.heightmap.height, 0, GraphicsFormat.R16_UNorm)
                    {
                        hideFlags = HideFlags.HideAndDontSave,
                        enableRandomWrite = false,
                        name = "TerrainHeightsCascade",
                        filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Clamp,
                        dimension = TextureDimension.Tex2DArray,
                        volumeDepth = 4
                    };
                    __instance.m_HeightmapCascade.Create();
                }
                if (__instance.m_HeightmapDepth == null || __instance.m_HeightmapDepth.width != __instance.heightmap.width || __instance.m_HeightmapDepth.height != __instance.heightmap.height)
                {
                    if (__instance.m_HeightmapDepth != null)
                    {
                        __instance.m_HeightmapDepth.Release();
                        UnityEngine.Object.Destroy(__instance.m_HeightmapDepth);
                        __instance.m_HeightmapDepth = null;
                    }
                    __instance.m_HeightmapDepth = new RenderTexture(__instance.heightmap.width, __instance.heightmap.height, 16, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear)
                    {
                        name = "HeightmapDepth"
                    };
                    __instance.m_HeightmapDepth.Create();
                }
                if (map != null)
                {
                    Graphics.CopyTexture(map, 0, 0, __instance.m_HeightmapCascade, TerrainSystem.baseLod, 0);
                }
                __instance.m_CascadeRanges = new float4[4];
                __instance.m_ShaderCascadeRanges = new Vector4[4];
                for (int i = 0; i < 4; i++)
                {
                    __instance.m_CascadeRanges[i] = new float4(0f, 0f, 0f, 0f);
                }
                __instance.m_CascadeRanges[TerrainSystem.baseLod] = new float4(__instance.playableOffset, __instance.playableOffset + __instance.playableArea);
                if (TerrainSystem.baseLod > 0)
                {
                    __instance.m_CascadeRanges[0] = new float4(__instance.worldOffset, __instance.worldOffset + __instance.worldSize);
                    if (worldMap != null)
                    {
                        Graphics.CopyTexture(worldMap, 0, 0, __instance.m_HeightmapCascade, 0, 0);
                    }
                }
                __instance.m_UpdateArea = new float4(__instance.m_CascadeRanges[TerrainSystem.baseLod]);
                Shader.SetGlobalTexture("colossal_TerrainTexture", __instance.m_Heightmap);
                Shader.SetGlobalVector("colossal_TerrainScale", new float4(xyz, 0f));
                Shader.SetGlobalVector("colossal_TerrainOffset", new float4(xyz2, 0f));
                Shader.SetGlobalVector("colossal_TerrainCascadeLimit", new float4(0.5f / (float)__instance.m_HeightmapCascade.width, 0.5f / (float)__instance.m_HeightmapCascade.height, 0f, 0f));
                Shader.SetGlobalTexture("colossal_TerrainTextureArray", __instance.m_HeightmapCascade);
                Shader.SetGlobalInt("colossal_TerrainTextureArrayBaseLod", TerrainSystem.baseLod);
                if (map != null)
                {
                    __instance.m_CPUHeightReaders.Complete();
                    __instance.m_CPUHeightReaders = default(JobHandle);
                    //
                    //__instance.WriteCPUHeights(map.GetRawTextureData<ushort>());
                    //
                    Traverse.Create(__instance).Method("TerrainSystem.WriteCPUHeights", map.GetRawTextureData<ushort>());
                }

                //__instance.m_TerrainMinMax.Init((__instance.worldHeightmap != null) ? 1024 : 512, (__instance.worldHeightmap != null) ? __instance.worldHeightmap.width : __instance.m_Heightmap.width);
                //__instance.m_TerrainMinMax.UpdateMap(__instance, __instance.m_Heightmap, __instance.worldHeightmap);

                Traverse.Create(__instance).Method("TerrainSystem.m_TerrainMinMax.Init", (__instance.worldHeightmap != null) ? 1024 : 512, (__instance.worldHeightmap != null) ? __instance.worldHeightmap.width : __instance.m_Heightmap.width);
                Traverse.Create(__instance).Method("TerrainSystem.m_TerrainMinMax.UpdateMap", __instance, __instance.m_Heightmap, __instance.worldHeightmap);


            }
            return false;
        }


    }//class;

  


}//namespace
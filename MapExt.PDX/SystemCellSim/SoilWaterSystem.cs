using System;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Events;
using Game.Prefabs;
using Game.Serialization;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;
using Game;
using Game.Simulation;

namespace MapExt.Systems
{
    /// <summary>
    /// bcjob£¡£¡£¡£»
    /// </summary>
    public partial class SoilWaterSystem : CellMapSystem<SoilWater>, IJobSerializable, IPostDeserialize
    {
        [BurstCompile]
        private struct SoilWaterTickJob : IJob
        {
            public NativeArray<SoilWater> m_SoilWaterMap;

            [ReadOnly]
            public TerrainHeightData m_TerrainHeightData;

            [ReadOnly]
            public WaterSurfaceData m_WaterSurfaceData;

            public NativeArray<float> m_SoilWaterTextureData;

            public SoilWaterParameterData m_SoilWaterParameters;

            public ComponentLookup<WaterLevelChange> m_Changes;

            public ComponentLookup<FloodCounterData> m_FloodCounterDatas;

            [ReadOnly]
            public ComponentLookup<EventData> m_Events;

            [ReadOnly]
            public NativeList<Entity> m_FloodEntities;

            [ReadOnly]
            public NativeList<Entity> m_FloodPrefabEntities;

            public EntityCommandBuffer m_CommandBuffer;

            public Entity m_FloodCounterEntity;

            public float m_Weather;

            public int m_ShaderUpdatesPerSoilUpdate;

            public int m_LoadDistributionIndex;

            private void HandleInterface(int index, int otherIndex, NativeArray<int> tmp, ref SoilWaterParameterData soilWaterParameters)
            {
                SoilWater soilWater = this.m_SoilWaterMap[index];
                SoilWater soilWater2 = this.m_SoilWaterMap[otherIndex];
                int num = tmp[index];
                int num2 = tmp[otherIndex];
                float num3 = soilWater2.m_Surface - soilWater.m_Surface;
                float num4 = (float)soilWater2.m_Amount / (float)soilWater2.m_Max - (float)soilWater.m_Amount / (float)soilWater.m_Max;
                float num5 = soilWaterParameters.m_HeightEffect * num3 / (float)(CellMapSystemRe.kMapSize / SoilWaterSystem.kTextureSize) + 0.25f * num4;
                num5 = ((!(num5 >= 0f)) ? math.max(0f - soilWaterParameters.m_MaxDiffusion, num5) : math.min(soilWaterParameters.m_MaxDiffusion, num5));
                int num6 = Mathf.RoundToInt(num5 * (float)((num5 > 0f) ? soilWater2.m_Amount : soilWater.m_Amount));
                num += num6;
                num2 -= num6;
                tmp[index] = num;
                tmp[otherIndex] = num2;
            }

            private void StartFlood()
            {
                if (this.m_FloodPrefabEntities.Length > 0)
                {
                    EntityArchetype archetype = this.m_Events[this.m_FloodPrefabEntities[0]].m_Archetype;
                    Entity e = this.m_CommandBuffer.CreateEntity(archetype);
                    this.m_CommandBuffer.SetComponent(e, new PrefabRef
                    {
                        m_Prefab = this.m_FloodPrefabEntities[0]
                    });
                    this.m_CommandBuffer.SetComponent(e, new WaterLevelChange
                    {
                        m_DangerHeight = 0f,
                        m_Direction = new float2(0f, 0f),
                        m_Intensity = 0f,
                        m_MaxIntensity = 0f
                    });
                }
            }

            private void StopFlood()
            {
                this.m_CommandBuffer.AddComponent<Deleted>(this.m_FloodEntities[0]);
            }

            public void Execute()
            {
                NativeArray<int> tmp = new NativeArray<int>(this.m_SoilWaterMap.Length, Allocator.Temp);
                for (int i = 0; i < this.m_SoilWaterMap.Length; i++)
                {
                    int num = i % SoilWaterSystem.kTextureSize;
                    int num2 = i / SoilWaterSystem.kTextureSize;
                    if (num < SoilWaterSystem.kTextureSize - 1)
                    {
                        this.HandleInterface(i, i + 1, tmp, ref this.m_SoilWaterParameters);
                    }
                    if (num2 < SoilWaterSystem.kTextureSize - 1)
                    {
                        this.HandleInterface(i, i + SoilWaterSystem.kTextureSize, tmp, ref this.m_SoilWaterParameters);
                    }
                }
                float num3 = math.max(0f, math.pow(2f * math.max(0f, this.m_Weather - 0.5f), 2f));
                float num4 = 1f / (2f * this.m_SoilWaterParameters.m_MaximumWaterDepth);
                int2 @int = this.m_WaterSurfaceData.resolution.xz / SoilWaterSystem.kTextureSize;
                FloodCounterData value = this.m_FloodCounterDatas[this.m_FloodCounterEntity];
                value.m_FloodCounter = math.max(0f, 0.98f * value.m_FloodCounter + 2f * num3 - 0.1f);
                if (value.m_FloodCounter > 20f && this.m_FloodEntities.Length == 0)
                {
                    this.StartFlood();
                }
                else if (this.m_FloodEntities.Length > 0)
                {
                    if (value.m_FloodCounter == 0f)
                    {
                        this.StopFlood();
                    }
                    else
                    {
                        WaterLevelChange value2 = this.m_Changes[this.m_FloodEntities[0]];
                        value2.m_Intensity = math.max(0f, (value.m_FloodCounter - 20f) / 80f);
                        this.m_Changes[this.m_FloodEntities[0]] = value2;
                    }
                }
                this.m_FloodCounterDatas[this.m_FloodCounterEntity] = value;
                int num5 = 0;
                int num6 = 0;
                int num7 = 0;
                int num8 = this.m_LoadDistributionIndex * SoilWaterSystem.kTextureSize / SoilWaterSystem.kLoadDistribution;
                int num9 = num8 + SoilWaterSystem.kTextureSize / SoilWaterSystem.kLoadDistribution;
                for (int j = num8 * SoilWaterSystem.kTextureSize; j < num9 * SoilWaterSystem.kTextureSize; j++)
                {
                    SoilWater value3 = this.m_SoilWaterMap[j];
                    value3.m_Amount = (short)math.max(0, value3.m_Amount + tmp[j] + Mathf.RoundToInt(this.m_SoilWaterParameters.m_RainMultiplier * num3));
                    float num10 = (value3.m_Surface = TerrainUtils.SampleHeight(ref this.m_TerrainHeightData, SoilWaterSystem.GetCellCenter(j)));
                    short num11 = (short)Mathf.RoundToInt(math.max(0f, 0.1f * (0.5f * (float)value3.m_Max - (float)value3.m_Amount)));
                    float x = (float)num11 * this.m_SoilWaterParameters.m_WaterPerUnit / (float)value3.m_Max;
                    int num12 = 0;
                    int num13 = 0;
                    float num14 = 0f;
                    float num15 = 0f;
                    int num16 = j % SoilWaterSystem.kTextureSize * @int.x + j / SoilWaterSystem.kTextureSize * this.m_WaterSurfaceData.resolution.x * @int.y;
                    for (int k = 0; k < @int.x; k += 4)
                    {
                        for (int l = 0; l < @int.y; l += 4)
                        {
                            float depth = this.m_WaterSurfaceData.depths[num16 + k + l * this.m_WaterSurfaceData.resolution.z].m_Depth;
                            if (depth > 0.01f)
                            {
                                num12++;
                                num14 += math.min(this.m_SoilWaterParameters.m_MaximumWaterDepth, depth);
                                num15 += math.min(x, depth);
                            }
                            num13++;
                        }
                    }
                    num11 = (short)Math.Min(num11, Mathf.RoundToInt((float)value3.m_Max * 10f * num15));
                    x = (float)num11 * this.m_SoilWaterParameters.m_WaterPerUnit / (float)value3.m_Max;
                    float num17 = (1f - num4 * num14 / (float)num13) * (float)value3.m_Max;
                    short num18 = (short)Mathf.RoundToInt(math.max(0f, this.m_SoilWaterParameters.m_OverflowRate * ((float)value3.m_Amount - num17)));
                    float num19 = 0f;
                    if ((float)num18 > 0f)
                    {
                        num19 = (float)value3.m_Amount / (float)value3.m_Max;
                        x = 0f;
                    }
                    if (num12 == 0)
                    {
                        x = 0f;
                    }
                    value3.m_Amount += num11;
                    value3.m_Amount -= num18;
                    short num20 = (short)Mathf.RoundToInt(math.sign(value3.m_Max / 8 - value3.m_Amount));
                    value3.m_Amount += num20;
                    num6 += num11 + Math.Max((short)0, num20);
                    num5 += num18 + Math.Max(0, -num20);
                    num7 += value3.m_Amount;
                    this.m_SoilWaterTextureData[j] = (0f - x) / (float)this.m_ShaderUpdatesPerSoilUpdate + num19;
                    this.m_SoilWaterMap[j] = value3;
                }
                tmp.Dispose();
            }
        }

        private struct TypeHandle
        {
            public ComponentLookup<WaterLevelChange> __Game_Events_WaterLevelChange_RW_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<EventData> __Game_Prefabs_EventData_RO_ComponentLookup;

            public ComponentLookup<FloodCounterData> __Game_Simulation_FloodCounterData_RW_ComponentLookup;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                this.__Game_Events_WaterLevelChange_RW_ComponentLookup = state.GetComponentLookup<WaterLevelChange>();
                this.__Game_Prefabs_EventData_RO_ComponentLookup = state.GetComponentLookup<EventData>(isReadOnly: true);
                this.__Game_Simulation_FloodCounterData_RW_ComponentLookup = state.GetComponentLookup<FloodCounterData>();
            }
        }

        public static readonly int kTextureSize = 128;

        public static readonly int kUpdatesPerDay = 1024;

        public static readonly int kLoadDistribution = 8;

        private SimulationSystem m_SimulationSystem;

        private TerrainSystem m_TerrainSystem;

        private WaterSystem m_WaterSystem;

        private ClimateSystem m_ClimateSystem;

        private EndFrameBarrier m_EndFrameBarrier;

        private Texture2D m_SoilWaterTexture;

        private EntityQuery m_SoilWaterParameterQuery;

        private EntityQuery m_FloodQuery;

        private EntityQuery m_FloodPrefabQuery;

        private TypeHandle __TypeHandle;

        private EntityQuery __query_336595330_0;

        public int2 TextureSize => new int2(SoilWaterSystem.kTextureSize, SoilWaterSystem.kTextureSize);

        public Texture soilTexture => this.m_SoilWaterTexture;

        public static float3 GetCellCenter(int index)
        {
            return CellMapSystemRe.GetCellCenter(index, SoilWaterSystem.kTextureSize);
        }

        public static SoilWater GetSoilWater(float3 position, NativeArray<SoilWater> soilWaterMap)
        {
            SoilWater result = default(SoilWater);
            int2 cell = CellMapSystemRe.GetCell(position, CellMapSystemRe.kMapSize, SoilWaterSystem.kTextureSize);
            float2 cellCoords = CellMapSystemRe.GetCellCoords(position, CellMapSystemRe.kMapSize, SoilWaterSystem.kTextureSize);
            if (cell.x < 0 || cell.x >= SoilWaterSystem.kTextureSize || cell.y < 0 || cell.y >= SoilWaterSystem.kTextureSize)
            {
                return result;
            }
            float x = soilWaterMap[cell.x + SoilWaterSystem.kTextureSize * cell.y].m_Amount;
            float y = ((cell.x < SoilWaterSystem.kTextureSize - 1) ? soilWaterMap[cell.x + 1 + SoilWaterSystem.kTextureSize * cell.y].m_Amount : 0);
            float x2 = ((cell.y < SoilWaterSystem.kTextureSize - 1) ? soilWaterMap[cell.x + SoilWaterSystem.kTextureSize * (cell.y + 1)].m_Amount : 0);
            float y2 = ((cell.x < SoilWaterSystem.kTextureSize - 1 && cell.y < SoilWaterSystem.kTextureSize - 1) ? soilWaterMap[cell.x + 1 + SoilWaterSystem.kTextureSize * (cell.y + 1)].m_Amount : 0);
            result.m_Amount = (short)Mathf.RoundToInt(math.lerp(math.lerp(x, y, cellCoords.x - (float)cell.x), math.lerp(x2, y2, cellCoords.x - (float)cell.x), cellCoords.y - (float)cell.y));
            return result;
        }

        private void CreateFloodCounter()
        {
            base.EntityManager.CreateEntity(base.EntityManager.CreateArchetype(ComponentType.ReadWrite<FloodCounterData>()));
        }

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            this.m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
            this.m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
            this.m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
            this.m_ClimateSystem = base.World.GetOrCreateSystemManaged<ClimateSystem>();
            this.m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
            this.m_SoilWaterParameterQuery = base.GetEntityQuery(ComponentType.ReadOnly<SoilWaterParameterData>());
            this.m_FloodQuery = base.GetEntityQuery(ComponentType.ReadOnly<Flood>());
            this.m_FloodPrefabQuery = base.GetEntityQuery(ComponentType.ReadOnly<FloodData>());
            this.CreateFloodCounter();
            base.CreateTextures(SoilWaterSystem.kTextureSize);
            this.m_SoilWaterTexture = new Texture2D(SoilWaterSystem.kTextureSize, SoilWaterSystem.kTextureSize, TextureFormat.RFloat, mipChain: false, linear: true)
            {
                name = "SoilWaterTexture",
                hideFlags = HideFlags.HideAndDontSave
            };
            NativeArray<float> rawTextureData = this.m_SoilWaterTexture.GetRawTextureData<float>();
            for (int i = 0; i < base.m_Map.Length; i++)
            {
                _ = (float)(i % SoilWaterSystem.kTextureSize) / (float)SoilWaterSystem.kTextureSize;
                _ = (float)(i / SoilWaterSystem.kTextureSize) / (float)SoilWaterSystem.kTextureSize;
                SoilWater soilWater = default(SoilWater);
                soilWater.m_Amount = 1024;
                soilWater.m_Max = 8192;
                SoilWater value = soilWater;
                base.m_Map[i] = value;
                rawTextureData[i] = 0f;
            }
            this.m_SoilWaterTexture.Apply();
            base.RequireForUpdate(this.m_SoilWaterParameterQuery);
        }

        [Preserve]
        protected override void OnUpdate()
        {
            TerrainHeightData heightData = this.m_TerrainSystem.GetHeightData();
            if (heightData.isCreated)
            {
                this.m_SoilWaterTexture.Apply();
                float value = this.m_ClimateSystem.precipitation.value;
                int shaderUpdatesPerSoilUpdate = 262144 / (SoilWaterSystem.kUpdatesPerDay / SoilWaterSystem.kLoadDistribution) / this.m_WaterSystem.SimulationCycleSteps;
                int loadDistributionIndex = (int)((long)this.m_SimulationSystem.frameIndex / (long)(262144 / SoilWaterSystem.kUpdatesPerDay) % SoilWaterSystem.kLoadDistribution);
                this.__TypeHandle.__Game_Simulation_FloodCounterData_RW_ComponentLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Prefabs_EventData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Events_WaterLevelChange_RW_ComponentLookup.Update(ref base.CheckedStateRef);
                SoilWaterTickJob soilWaterTickJob = default(SoilWaterTickJob);
                soilWaterTickJob.m_SoilWaterMap = base.m_Map;
                soilWaterTickJob.m_TerrainHeightData = heightData;
                soilWaterTickJob.m_WaterSurfaceData = this.m_WaterSystem.GetSurfaceData(out var deps);
                soilWaterTickJob.m_SoilWaterTextureData = this.m_SoilWaterTexture.GetRawTextureData<float>();
                soilWaterTickJob.m_SoilWaterParameters = this.m_SoilWaterParameterQuery.GetSingleton<SoilWaterParameterData>();
                soilWaterTickJob.m_FloodEntities = this.m_FloodQuery.ToEntityListAsync(base.World.UpdateAllocator.ToAllocator, out var outJobHandle);
                soilWaterTickJob.m_FloodPrefabEntities = this.m_FloodPrefabQuery.ToEntityListAsync(base.World.UpdateAllocator.ToAllocator, out var outJobHandle2);
                soilWaterTickJob.m_Changes = this.__TypeHandle.__Game_Events_WaterLevelChange_RW_ComponentLookup;
                soilWaterTickJob.m_Events = this.__TypeHandle.__Game_Prefabs_EventData_RO_ComponentLookup;
                soilWaterTickJob.m_FloodCounterDatas = this.__TypeHandle.__Game_Simulation_FloodCounterData_RW_ComponentLookup;
                soilWaterTickJob.m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer();
                soilWaterTickJob.m_FloodCounterEntity = this.__query_336595330_0.GetSingletonEntity();
                soilWaterTickJob.m_Weather = value;
                soilWaterTickJob.m_ShaderUpdatesPerSoilUpdate = shaderUpdatesPerSoilUpdate;
                soilWaterTickJob.m_LoadDistributionIndex = loadDistributionIndex;
                SoilWaterTickJob jobData = soilWaterTickJob;
                base.Dependency = IJobExtensions.Schedule(jobData, JobUtils.CombineDependencies(base.m_WriteDependencies, base.m_ReadDependencies, outJobHandle, outJobHandle2, deps, base.Dependency));
                base.AddWriter(base.Dependency);
                this.m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
                this.m_TerrainSystem.AddCPUHeightReader(base.Dependency);
                this.m_WaterSystem.AddSurfaceReader(base.Dependency);
                base.Dependency = JobHandle.CombineDependencies(base.m_ReadDependencies, base.m_WriteDependencies, base.Dependency);
            }
        }

        public void PostDeserialize(Context context)
        {
            EntityQuery entityQuery = base.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<FloodCounterData>());
            try
            {
                if (entityQuery.CalculateEntityCount() == 0)
                {
                    this.CreateFloodCounter();
                }
            }
            finally
            {
                entityQuery.Dispose();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void __AssignQueries(ref SystemState state)
        {
            this.__query_336595330_0 = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[1] { ComponentType.ReadOnly<FloodCounterData>() },
                Any = new ComponentType[0],
                None = new ComponentType[0],
                Disabled = new ComponentType[0],
                Absent = new ComponentType[0],
                Options = EntityQueryOptions.IncludeSystems
            });
        }

        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();
            this.__AssignQueries(ref base.CheckedStateRef);
            this.__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
        }

        [Preserve]
        public SoilWaterSystem()
        {
        }

        public new NativeArray<SoilWater> GetMap(bool readOnly, out JobHandle dependencies)
        {
            dependencies = (readOnly ? this.m_WriteDependencies : JobHandle.CombineDependencies(this.m_ReadDependencies, this.m_WriteDependencies));
            return this.m_Map;
        }

        public new CellMapData<SoilWater> GetData(bool readOnly, out JobHandle dependencies)
        {
            dependencies = (readOnly ? this.m_WriteDependencies : JobHandle.CombineDependencies(this.m_ReadDependencies, this.m_WriteDependencies));
            CellMapData<SoilWater> result = default(CellMapData<SoilWater>);
            result.m_Buffer = this.m_Map;
            result.m_CellSize = CellMapSystemRe.kMapSize / (float2)this.m_TextureSize;
            result.m_TextureSize = this.m_TextureSize;
            return result;
        }

        public new void AddReader(JobHandle jobHandle)
        {
            this.m_ReadDependencies = JobHandle.CombineDependencies(this.m_ReadDependencies, jobHandle);
        }

        public new void AddWriter(JobHandle jobHandle)
        {
            this.m_WriteDependencies = jobHandle;
        }
    }
}

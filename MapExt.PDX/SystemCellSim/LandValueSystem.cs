#define UNITY_ASSERTIONS
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;
using Game.Simulation;
using Game;

namespace MapExt.Systems
{
    /// <summary>
    /// bc cell!; loginfo;
    /// </summary>
    public partial class LandValueSystem : CellMapSystem<LandValueCell>, IJobSerializable
    {
        private struct NetIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
        {
            public int m_TotalCount;

            public float m_TotalLandValueBonus;

            public Bounds3 m_Bounds;

            public ComponentLookup<LandValue> m_LandValueData;

            public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

            public bool Intersect(QuadTreeBoundsXZ bounds)
            {
                return MathUtils.Intersect(bounds.m_Bounds, this.m_Bounds);
            }

            public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
            {
                if (MathUtils.Intersect(bounds.m_Bounds, this.m_Bounds) && this.m_LandValueData.HasComponent(entity) && this.m_EdgeGeometryData.HasComponent(entity))
                {
                    LandValue landValue = this.m_LandValueData[entity];
                    if (landValue.m_LandValue > 0f)
                    {
                        this.m_TotalLandValueBonus += landValue.m_LandValue;
                        this.m_TotalCount++;
                    }
                }
            }
        }

        [BurstCompile]
        private struct LandValueMapUpdateJob : IJobParallelFor
        {
            public NativeArray<LandValueCell> m_LandValueMap;

            [ReadOnly]
            public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

            [ReadOnly]
            public NativeArray<TerrainAttractiveness> m_AttractiveMap;

            [ReadOnly]
            public NativeArray<GroundPollution> m_GroundPollutionMap;

            [ReadOnly]
            public NativeArray<AirPollution> m_AirPollutionMap;

            [ReadOnly]
            public NativeArray<NoisePollution> m_NoisePollutionMap;

            [ReadOnly]
            public NativeArray<AvailabilityInfoCell> m_AvailabilityInfoMap;

            [ReadOnly]
            public CellMapData<TelecomCoverage> m_TelecomCoverageMap;

            [ReadOnly]
            public WaterSurfaceData m_WaterSurfaceData;

            [ReadOnly]
            public TerrainHeightData m_TerrainHeightData;

            [ReadOnly]
            public ComponentLookup<LandValue> m_LandValueData;

            [ReadOnly]
            public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

            [ReadOnly]
            public AttractivenessParameterData m_AttractivenessParameterData;

            [ReadOnly]
            public LandValueParameterData m_LandValueParameterData;

            public float m_CellSize;

            public void Execute(int index)
            {
                float3 cellCenter = CellMapSystem<LandValueCell>.GetCellCenter(index, LandValueSystem.kTextureSize);
                if (WaterUtils.SampleDepth(ref this.m_WaterSurfaceData, cellCenter) > 1f)
                {
                    this.m_LandValueMap[index] = new LandValueCell
                    {
                        m_LandValue = this.m_LandValueParameterData.m_LandValueBaseline
                    };
                    return;
                }
                NetIterator netIterator = default(NetIterator);
                netIterator.m_TotalCount = 0;
                netIterator.m_TotalLandValueBonus = 0f;
                netIterator.m_Bounds = new Bounds3(cellCenter - new float3(1.5f * this.m_CellSize, 10000f, 1.5f * this.m_CellSize), cellCenter + new float3(1.5f * this.m_CellSize, 10000f, 1.5f * this.m_CellSize));
                netIterator.m_EdgeGeometryData = this.m_EdgeGeometryData;
                netIterator.m_LandValueData = this.m_LandValueData;
                NetIterator iterator = netIterator;
                this.m_NetSearchTree.Iterate(ref iterator);
                float num = GroundPollutionSystem.GetPollution(cellCenter, this.m_GroundPollutionMap).m_Pollution;
                float num2 = AirPollutionSystem.GetPollution(cellCenter, this.m_AirPollutionMap).m_Pollution;
                float num3 = NoisePollutionSystem.GetPollution(cellCenter, this.m_NoisePollutionMap).m_Pollution;
                float x = AvailabilityInfoToGridSystem.GetAvailabilityInfo(cellCenter, this.m_AvailabilityInfoMap).m_AvailabilityInfo.x;
                float num4 = TelecomCoverage.SampleNetworkQuality(this.m_TelecomCoverageMap, cellCenter);
                LandValueCell value = this.m_LandValueMap[index];
                float num5 = (((float)iterator.m_TotalCount > 0f) ? (iterator.m_TotalLandValueBonus / (float)iterator.m_TotalCount) : 0f);
                float num6 = math.min((x - 5f) * this.m_LandValueParameterData.m_AttractivenessBonusMultiplier, this.m_LandValueParameterData.m_CommonFactorMaxBonus);
                float num7 = math.min(num4 * this.m_LandValueParameterData.m_TelecomCoverageBonusMultiplier, this.m_LandValueParameterData.m_CommonFactorMaxBonus);
                num5 += num6 + num7;
                float num8 = WaterUtils.SamplePolluted(ref this.m_WaterSurfaceData, cellCenter);
                float num9 = 0f;
                if (num8 <= 0f && num <= 0f)
                {
                    num9 = TerrainAttractivenessSystem.EvaluateAttractiveness(TerrainUtils.SampleHeight(ref this.m_TerrainHeightData, cellCenter), this.m_AttractiveMap[index], this.m_AttractivenessParameterData);
                    num5 += math.min(math.max(num9 - 5f, 0f) * this.m_LandValueParameterData.m_AttractivenessBonusMultiplier, this.m_LandValueParameterData.m_CommonFactorMaxBonus);
                }
                float num10 = num * this.m_LandValueParameterData.m_GroundPollutionPenaltyMultiplier + num2 * this.m_LandValueParameterData.m_AirPollutionPenaltyMultiplier + num3 * this.m_LandValueParameterData.m_NoisePollutionPenaltyMultiplier;
                float num11 = math.max(this.m_LandValueParameterData.m_LandValueBaseline, this.m_LandValueParameterData.m_LandValueBaseline + num5 - num10);
                if (math.abs(value.m_LandValue - num11) >= 0.1f)
                {
                    value.m_LandValue = math.lerp(value.m_LandValue, num11, 0.4f);
                }
                this.m_LandValueMap[index] = value;
            }
        }

        [BurstCompile]
        private struct EdgeUpdateJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle m_EntityType;

            [ReadOnly]
            public ComponentTypeHandle<Edge> m_EdgeType;

            [ReadOnly]
            public BufferTypeHandle<Game.Net.ServiceCoverage> m_ServiceCoverageType;

            [ReadOnly]
            public BufferTypeHandle<ResourceAvailability> m_AvailabilityType;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<LandValue> m_LandValues;

            [ReadOnly]
            public LandValueParameterData m_LandValueParameterData;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> nativeArray = chunk.GetNativeArray(this.m_EntityType);
                NativeArray<Edge> nativeArray2 = chunk.GetNativeArray(ref this.m_EdgeType);
                BufferAccessor<Game.Net.ServiceCoverage> bufferAccessor = chunk.GetBufferAccessor(ref this.m_ServiceCoverageType);
                BufferAccessor<ResourceAvailability> bufferAccessor2 = chunk.GetBufferAccessor(ref this.m_AvailabilityType);
                for (int i = 0; i < nativeArray2.Length; i++)
                {
                    Entity entity = nativeArray[i];
                    float num = 0f;
                    float num2 = 0f;
                    float num3 = 0f;
                    if (bufferAccessor.Length > 0)
                    {
                        DynamicBuffer<Game.Net.ServiceCoverage> dynamicBuffer = bufferAccessor[i];
                        Game.Net.ServiceCoverage serviceCoverage = dynamicBuffer[0];
                        num = math.lerp(serviceCoverage.m_Coverage.x, serviceCoverage.m_Coverage.y, 0.5f) * this.m_LandValueParameterData.m_HealthCoverageBonusMultiplier;
                        Game.Net.ServiceCoverage serviceCoverage2 = dynamicBuffer[5];
                        num2 = math.lerp(serviceCoverage2.m_Coverage.x, serviceCoverage2.m_Coverage.y, 0.5f) * this.m_LandValueParameterData.m_EducationCoverageBonusMultiplier;
                        Game.Net.ServiceCoverage serviceCoverage3 = dynamicBuffer[2];
                        num3 = math.lerp(serviceCoverage3.m_Coverage.x, serviceCoverage3.m_Coverage.y, 0.5f) * this.m_LandValueParameterData.m_PoliceCoverageBonusMultiplier;
                    }
                    float num4 = 0f;
                    float num5 = 0f;
                    float num6 = 0f;
                    if (bufferAccessor2.Length > 0)
                    {
                        DynamicBuffer<ResourceAvailability> dynamicBuffer2 = bufferAccessor2[i];
                        ResourceAvailability resourceAvailability = dynamicBuffer2[1];
                        num4 = math.lerp(resourceAvailability.m_Availability.x, resourceAvailability.m_Availability.y, 0.5f) * this.m_LandValueParameterData.m_CommercialServiceBonusMultiplier;
                        ResourceAvailability resourceAvailability2 = dynamicBuffer2[31];
                        num5 = math.lerp(resourceAvailability2.m_Availability.x, resourceAvailability2.m_Availability.y, 0.5f) * this.m_LandValueParameterData.m_BusBonusMultiplier;
                        ResourceAvailability resourceAvailability3 = dynamicBuffer2[32];
                        num6 = math.lerp(resourceAvailability3.m_Availability.x, resourceAvailability3.m_Availability.y, 0.5f) * this.m_LandValueParameterData.m_TramSubwayBonusMultiplier;
                    }
                    LandValue value = this.m_LandValues[entity];
                    float num7 = math.max(num + num2 + num3 + num4 + num5 + num6, 0f);
                    if (math.abs(value.m_LandValue - num7) >= 0.1f)
                    {
                        float x = math.lerp(value.m_LandValue, num7, 0.6f);
                        value.m_LandValue = math.max(x, 0f);
                        this.m_LandValues[entity] = value;
                    }
                }
            }

            void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                this.Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
            }
        }

        private struct TypeHandle
        {
            [ReadOnly]
            public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

            [ReadOnly]
            public ComponentTypeHandle<Edge> __Game_Net_Edge_RO_ComponentTypeHandle;

            [ReadOnly]
            public BufferTypeHandle<Game.Net.ServiceCoverage> __Game_Net_ServiceCoverage_RO_BufferTypeHandle;

            [ReadOnly]
            public BufferTypeHandle<ResourceAvailability> __Game_Net_ResourceAvailability_RO_BufferTypeHandle;

            public ComponentLookup<LandValue> __Game_Net_LandValue_RW_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<LandValue> __Game_Net_LandValue_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<EdgeGeometry> __Game_Net_EdgeGeometry_RO_ComponentLookup;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                this.__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                this.__Game_Net_Edge_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Edge>(isReadOnly: true);
                this.__Game_Net_ServiceCoverage_RO_BufferTypeHandle = state.GetBufferTypeHandle<Game.Net.ServiceCoverage>(isReadOnly: true);
                this.__Game_Net_ResourceAvailability_RO_BufferTypeHandle = state.GetBufferTypeHandle<ResourceAvailability>(isReadOnly: true);
                this.__Game_Net_LandValue_RW_ComponentLookup = state.GetComponentLookup<LandValue>();
                this.__Game_Net_LandValue_RO_ComponentLookup = state.GetComponentLookup<LandValue>(isReadOnly: true);
                this.__Game_Net_EdgeGeometry_RO_ComponentLookup = state.GetComponentLookup<EdgeGeometry>(isReadOnly: true);
            }
        }

        public static readonly int kTextureSize = 128;

        public static readonly int kUpdatesPerDay = 32;

        private EntityQuery m_EdgeGroup;

        private EntityQuery m_NodeGroup;

        private EntityQuery m_AttractivenessParameterQuery;

        private EntityQuery m_LandValueParameterQuery;

        private GroundPollutionSystem m_GroundPollutionSystem;

        private AirPollutionSystem m_AirPollutionSystem;

        private NoisePollutionSystem m_NoisePollutionSystem;

        private AvailabilityInfoToGridSystem m_AvailabilityInfoToGridSystem;

        private SearchSystem m_NetSearchSystem;

        private TerrainAttractivenessSystem m_TerrainAttractivenessSystem;

        private TerrainSystem m_TerrainSystem;

        private WaterSystem m_WaterSystem;

        private TelecomCoverageSystem m_TelecomCoverageSystem;

        private TypeHandle __TypeHandle;

        public int2 TextureSize => new int2(LandValueSystem.kTextureSize, LandValueSystem.kTextureSize);

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 262144 / LandValueSystem.kUpdatesPerDay;
        }

        public static float3 GetCellCenter(int index)
        {
            return CellMapSystemRe.GetCellCenter(index, LandValueSystem.kTextureSize);
        }

        public static int GetCellIndex(float3 pos)
        {
            int num = CellMapSystemRe.kMapSize / LandValueSystem.kTextureSize;
            return Mathf.FloorToInt(((float)(CellMapSystemRe.kMapSize / 2) + pos.x) / (float)num) + Mathf.FloorToInt(((float)(CellMapSystemRe.kMapSize / 2) + pos.z) / (float)num) * LandValueSystem.kTextureSize;
        }

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            Assert.IsTrue(LandValueSystem.kTextureSize == TerrainAttractivenessSystem.kTextureSize);
            base.CreateTextures(LandValueSystem.kTextureSize);
            this.m_NetSearchSystem = base.World.GetOrCreateSystemManaged<SearchSystem>();
            this.m_GroundPollutionSystem = base.World.GetOrCreateSystemManaged<GroundPollutionSystem>();
            this.m_AirPollutionSystem = base.World.GetOrCreateSystemManaged<AirPollutionSystem>();
            this.m_NoisePollutionSystem = base.World.GetOrCreateSystemManaged<NoisePollutionSystem>();
            this.m_TerrainAttractivenessSystem = base.World.GetOrCreateSystemManaged<TerrainAttractivenessSystem>();
            this.m_AvailabilityInfoToGridSystem = base.World.GetOrCreateSystemManaged<AvailabilityInfoToGridSystem>();
            this.m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
            this.m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
            this.m_TelecomCoverageSystem = base.World.GetOrCreateSystemManaged<TelecomCoverageSystem>();
            this.m_AttractivenessParameterQuery = base.GetEntityQuery(ComponentType.ReadOnly<AttractivenessParameterData>());
            this.m_LandValueParameterQuery = base.GetEntityQuery(ComponentType.ReadOnly<LandValueParameterData>());
            this.m_EdgeGroup = base.GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[3]
                {
                    ComponentType.ReadOnly<Edge>(),
                    ComponentType.ReadWrite<LandValue>(),
                    ComponentType.ReadOnly<Curve>()
                },
                Any = new ComponentType[0],
                None = new ComponentType[2]
                {
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Temp>()
                }
            });
            base.RequireAnyForUpdate(this.m_EdgeGroup);
        }

        [Preserve]
        protected override void OnUpdate()
        {
            if (!this.m_EdgeGroup.IsEmptyIgnoreFilter)
            {
                this.__TypeHandle.__Game_Net_LandValue_RW_ComponentLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Net_ResourceAvailability_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Net_ServiceCoverage_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Net_Edge_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
                EdgeUpdateJob edgeUpdateJob = default(EdgeUpdateJob);
                edgeUpdateJob.m_EntityType = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle;
                edgeUpdateJob.m_EdgeType = this.__TypeHandle.__Game_Net_Edge_RO_ComponentTypeHandle;
                edgeUpdateJob.m_ServiceCoverageType = this.__TypeHandle.__Game_Net_ServiceCoverage_RO_BufferTypeHandle;
                edgeUpdateJob.m_AvailabilityType = this.__TypeHandle.__Game_Net_ResourceAvailability_RO_BufferTypeHandle;
                edgeUpdateJob.m_LandValues = this.__TypeHandle.__Game_Net_LandValue_RW_ComponentLookup;
                edgeUpdateJob.m_LandValueParameterData = this.m_LandValueParameterQuery.GetSingleton<LandValueParameterData>();
                EdgeUpdateJob jobData = edgeUpdateJob;
                base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, this.m_EdgeGroup, base.Dependency);
            }
            this.__TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Net_LandValue_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            LandValueMapUpdateJob landValueMapUpdateJob = default(LandValueMapUpdateJob);
            landValueMapUpdateJob.m_NetSearchTree = this.m_NetSearchSystem.GetNetSearchTree(readOnly: true, out var dependencies);
            landValueMapUpdateJob.m_AttractiveMap = this.m_TerrainAttractivenessSystem.GetMap(readOnly: true, out var dependencies2);
            landValueMapUpdateJob.m_GroundPollutionMap = this.m_GroundPollutionSystem.GetMap(readOnly: true, out var dependencies3);
            landValueMapUpdateJob.m_AirPollutionMap = this.m_AirPollutionSystem.GetMap(readOnly: true, out var dependencies4);
            landValueMapUpdateJob.m_NoisePollutionMap = this.m_NoisePollutionSystem.GetMap(readOnly: true, out var dependencies5);
            landValueMapUpdateJob.m_AvailabilityInfoMap = this.m_AvailabilityInfoToGridSystem.GetMap(readOnly: true, out var dependencies6);
            landValueMapUpdateJob.m_TelecomCoverageMap = this.m_TelecomCoverageSystem.GetData(readOnly: true, out var dependencies7);
            landValueMapUpdateJob.m_LandValueMap = base.m_Map;
            landValueMapUpdateJob.m_LandValueData = this.__TypeHandle.__Game_Net_LandValue_RO_ComponentLookup;
            landValueMapUpdateJob.m_TerrainHeightData = this.m_TerrainSystem.GetHeightData();
            landValueMapUpdateJob.m_WaterSurfaceData = this.m_WaterSystem.GetSurfaceData(out var deps);
            landValueMapUpdateJob.m_EdgeGeometryData = this.__TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup;
            landValueMapUpdateJob.m_AttractivenessParameterData = this.m_AttractivenessParameterQuery.GetSingleton<AttractivenessParameterData>();
            landValueMapUpdateJob.m_LandValueParameterData = this.m_LandValueParameterQuery.GetSingleton<LandValueParameterData>();
            landValueMapUpdateJob.m_CellSize = CellMapSystemRe.kMapSize / (float)LandValueSystem.kTextureSize;
            LandValueMapUpdateJob jobData2 = landValueMapUpdateJob;
            base.Dependency = IJobParallelForExtensions.Schedule(jobData2, LandValueSystem.kTextureSize * LandValueSystem.kTextureSize, LandValueSystem.kTextureSize, JobHandle.CombineDependencies(dependencies, dependencies2, JobHandle.CombineDependencies(base.m_WriteDependencies, base.m_ReadDependencies, JobHandle.CombineDependencies(base.Dependency, deps, JobHandle.CombineDependencies(dependencies3, dependencies5, JobHandle.CombineDependencies(dependencies6, dependencies4, dependencies7))))));
            base.AddWriter(base.Dependency);
            this.m_NetSearchSystem.AddNetSearchTreeReader(base.Dependency);
            this.m_WaterSystem.AddSurfaceReader(base.Dependency);
            this.m_TerrainAttractivenessSystem.AddReader(base.Dependency);
            this.m_GroundPollutionSystem.AddReader(base.Dependency);
            this.m_AirPollutionSystem.AddReader(base.Dependency);
            this.m_NoisePollutionSystem.AddReader(base.Dependency);
            this.m_AvailabilityInfoToGridSystem.AddReader(base.Dependency);
            this.m_TelecomCoverageSystem.AddReader(base.Dependency);
            base.Dependency = JobHandle.CombineDependencies(base.m_ReadDependencies, base.m_WriteDependencies, base.Dependency);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void __AssignQueries(ref SystemState state)
        {
        }

        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();
            this.__AssignQueries(ref base.CheckedStateRef);
            this.__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
        }

        [Preserve]
        public LandValueSystem()
        {
        }

        public new NativeArray<LandValueCell> GetMap(bool readOnly, out JobHandle dependencies)
        {
            dependencies = (readOnly ? this.m_WriteDependencies : JobHandle.CombineDependencies(this.m_ReadDependencies, this.m_WriteDependencies));
            return this.m_Map;
        }

        public new CellMapData<LandValueCell> GetData(bool readOnly, out JobHandle dependencies)
        {
            dependencies = (readOnly ? this.m_WriteDependencies : JobHandle.CombineDependencies(this.m_ReadDependencies, this.m_WriteDependencies));
            CellMapData<LandValueCell> result = default(CellMapData<LandValueCell>);
            result.m_Buffer = this.m_Map;
            result.m_CellSize = 57344 / (float2)this.m_TextureSize;
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

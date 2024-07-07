using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Net;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;
using Game;
using Game.Simulation;

namespace MapExt.Systems
{
    public partial class AvailabilityInfoToGridSystem : CellMapSystem<AvailabilityInfoCell>, IJobSerializable
    {
        private struct NetIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
        {
            public AvailabilityInfoCell m_TotalWeight;

            public AvailabilityInfoCell m_Result;

            public float m_CellSize;

            public Bounds3 m_Bounds;

            public BufferLookup<ResourceAvailability> m_Availabilities;

            public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

            public bool Intersect(QuadTreeBoundsXZ bounds)
            {
                return MathUtils.Intersect(bounds.m_Bounds, this.m_Bounds);
            }

            private void AddData(float2 attractiveness2, float2 uneducated2, float2 educated2, float2 services2, float2 workplaces2, float2 t, float3 curvePos, float weight)
            {
                float num = math.lerp(attractiveness2.x, attractiveness2.y, t.y);
                float num2 = 0.5f * math.lerp(uneducated2.x + educated2.x, uneducated2.y + educated2.y, t.y);
                float num3 = math.lerp(services2.x, services2.y, t.y);
                float num4 = math.lerp(workplaces2.x, workplaces2.y, t.y);
                this.m_Result.AddAttractiveness(weight * num);
                this.m_TotalWeight.AddAttractiveness(weight);
                this.m_Result.AddConsumers(weight * num2);
                this.m_TotalWeight.AddConsumers(weight);
                this.m_Result.AddServices(weight * num3);
                this.m_TotalWeight.AddServices(weight);
                this.m_Result.AddWorkplaces(weight * num4);
                this.m_TotalWeight.AddWorkplaces(weight);
            }

            public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
            {
                if (MathUtils.Intersect(bounds.m_Bounds, this.m_Bounds) && this.m_Availabilities.HasBuffer(entity) && this.m_EdgeGeometryData.HasComponent(entity))
                {
                    DynamicBuffer<ResourceAvailability> dynamicBuffer = this.m_Availabilities[entity];
                    float2 availability = dynamicBuffer[18].m_Availability;
                    float2 availability2 = dynamicBuffer[2].m_Availability;
                    float2 availability3 = dynamicBuffer[3].m_Availability;
                    float2 availability4 = dynamicBuffer[1].m_Availability;
                    float2 availability5 = dynamicBuffer[0].m_Availability;
                    EdgeGeometry edgeGeometry = this.m_EdgeGeometryData[entity];
                    int num = (int)math.ceil(edgeGeometry.m_Start.middleLength * 0.05f);
                    int num2 = (int)math.ceil(edgeGeometry.m_End.middleLength * 0.05f);
                    float3 @float = 0.5f * (this.m_Bounds.min + this.m_Bounds.max);
                    for (int i = 1; i <= num; i++)
                    {
                        float2 t = i / new float2(num, num + num2);
                        float3 curvePos = math.lerp(MathUtils.Position(edgeGeometry.m_Start.m_Left, t.x), MathUtils.Position(edgeGeometry.m_Start.m_Right, t.x), 0.5f);
                        float weight = math.max(0f, 1f - math.distance(@float.xz, curvePos.xz) / (1.5f * this.m_CellSize));
                        this.AddData(availability, availability2, availability3, availability4, availability5, t, curvePos, weight);
                    }
                    for (int j = 1; j <= num2; j++)
                    {
                        float2 t2 = new float2(j, num + j) / new float2(num2, num + num2);
                        float3 curvePos2 = math.lerp(MathUtils.Position(edgeGeometry.m_End.m_Left, t2.x), MathUtils.Position(edgeGeometry.m_End.m_Right, t2.x), 0.5f);
                        float weight2 = math.max(0f, 1f - math.distance(@float.xz, curvePos2.xz) / (1.5f * this.m_CellSize));
                        this.AddData(availability, availability2, availability3, availability4, availability5, t2, curvePos2, weight2);
                    }
                }
            }
        }

        [BurstCompile]
        private struct AvailabilityInfoToGridJob : IJobParallelFor
        {
            public NativeArray<AvailabilityInfoCell> m_AvailabilityInfoMap;

            [ReadOnly]
            public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

            [ReadOnly]
            public BufferLookup<ResourceAvailability> m_AvailabilityData;

            [ReadOnly]
            public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

            public float m_CellSize;

            public void Execute(int index)
            {
                float3 cellCenter = CellMapSystem<AvailabilityInfoCell>.GetCellCenter(index, AvailabilityInfoToGridSystem.kTextureSize);
                NetIterator netIterator = default(NetIterator);
                netIterator.m_TotalWeight = default(AvailabilityInfoCell);
                netIterator.m_Result = default(AvailabilityInfoCell);
                netIterator.m_Bounds = new Bounds3(cellCenter - new float3(1.5f * this.m_CellSize, 10000f, 1.5f * this.m_CellSize), cellCenter + new float3(1.5f * this.m_CellSize, 10000f, 1.5f * this.m_CellSize));
                netIterator.m_CellSize = this.m_CellSize;
                netIterator.m_EdgeGeometryData = this.m_EdgeGeometryData;
                netIterator.m_Availabilities = this.m_AvailabilityData;
                NetIterator iterator = netIterator;
                this.m_NetSearchTree.Iterate(ref iterator);
                AvailabilityInfoCell value = this.m_AvailabilityInfoMap[index];
                value.m_AvailabilityInfo = math.select(iterator.m_Result.m_AvailabilityInfo / iterator.m_TotalWeight.m_AvailabilityInfo, 0f, iterator.m_TotalWeight.m_AvailabilityInfo == 0f);
                this.m_AvailabilityInfoMap[index] = value;
            }
        }

        private struct TypeHandle
        {
            [ReadOnly]
            public BufferLookup<ResourceAvailability> __Game_Net_ResourceAvailability_RO_BufferLookup;

            [ReadOnly]
            public ComponentLookup<EdgeGeometry> __Game_Net_EdgeGeometry_RO_ComponentLookup;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                this.__Game_Net_ResourceAvailability_RO_BufferLookup = state.GetBufferLookup<ResourceAvailability>(isReadOnly: true);
                this.__Game_Net_EdgeGeometry_RO_ComponentLookup = state.GetComponentLookup<EdgeGeometry>(isReadOnly: true);
            }
        }

        public static readonly int kTextureSize = 128;

        public static readonly int kUpdatesPerDay = 32;

        private SearchSystem m_NetSearchSystem;

        private TypeHandle __TypeHandle;

        public int2 TextureSize => new int2(AvailabilityInfoToGridSystem.kTextureSize, AvailabilityInfoToGridSystem.kTextureSize);

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 262144 / AvailabilityInfoToGridSystem.kUpdatesPerDay;
        }

       

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            base.CreateTextures(AvailabilityInfoToGridSystem.kTextureSize);
            this.m_NetSearchSystem = base.World.GetOrCreateSystemManaged<SearchSystem>();
        }

        [Preserve]
        protected override void OnUpdate()
        {
            this.__TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Net_ResourceAvailability_RO_BufferLookup.Update(ref base.CheckedStateRef);
            AvailabilityInfoToGridJob availabilityInfoToGridJob = default(AvailabilityInfoToGridJob);
            availabilityInfoToGridJob.m_NetSearchTree = this.m_NetSearchSystem.GetNetSearchTree(readOnly: true, out var dependencies);
            availabilityInfoToGridJob.m_AvailabilityInfoMap = base.m_Map;
            availabilityInfoToGridJob.m_AvailabilityData = this.__TypeHandle.__Game_Net_ResourceAvailability_RO_BufferLookup;
            availabilityInfoToGridJob.m_EdgeGeometryData = this.__TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup;
            availabilityInfoToGridJob.m_CellSize = (float)CellMapSystem<AvailabilityInfoCell>.kMapSize / (float)AvailabilityInfoToGridSystem.kTextureSize;
            AvailabilityInfoToGridJob jobData = availabilityInfoToGridJob;
            base.Dependency = IJobParallelForExtensions.Schedule(jobData, AvailabilityInfoToGridSystem.kTextureSize * AvailabilityInfoToGridSystem.kTextureSize, AvailabilityInfoToGridSystem.kTextureSize, JobHandle.CombineDependencies(dependencies, JobHandle.CombineDependencies(base.m_WriteDependencies, base.m_ReadDependencies, base.Dependency)));
            base.AddWriter(base.Dependency);
            this.m_NetSearchSystem.AddNetSearchTreeReader(base.Dependency);
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
        public AvailabilityInfoToGridSystem()
        {
        }

        //泛型非静态方法重定向，配合harmony patch;
        public new NativeArray<AvailabilityInfoCell> GetMap(bool readOnly, out JobHandle dependencies)
        {
            dependencies = (readOnly ? this.m_WriteDependencies : JobHandle.CombineDependencies(this.m_ReadDependencies, this.m_WriteDependencies));
            return this.m_Map;
        }

        public new CellMapData<AvailabilityInfoCell> GetData(bool readOnly, out JobHandle dependencies)
        {
            dependencies = (readOnly ? this.m_WriteDependencies : JobHandle.CombineDependencies(this.m_ReadDependencies, this.m_WriteDependencies));
            CellMapData<AvailabilityInfoCell> result = default(CellMapData<AvailabilityInfoCell>);
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

        public static float3 GetCellCenter(int index)
        {
            return CellMapSystemRe.GetCellCenter(index, AvailabilityInfoToGridSystem.kTextureSize);
        }

        public static AvailabilityInfoCell GetAvailabilityInfo(float3 position, NativeArray<AvailabilityInfoCell> AvailabilityInfoMap)
        {
            AvailabilityInfoCell result = default(AvailabilityInfoCell);
            int2 cell = CellMapSystemRe.GetCell(position, CellMapSystemRe.kMapSize, AvailabilityInfoToGridSystem.kTextureSize);
            float2 cellCoords = CellMapSystemRe.GetCellCoords(position, CellMapSystemRe.kMapSize, AvailabilityInfoToGridSystem.kTextureSize);
            if (cell.x < 0 || cell.x >= AvailabilityInfoToGridSystem.kTextureSize || cell.y < 0 || cell.y >= AvailabilityInfoToGridSystem.kTextureSize)
            {
                return default(AvailabilityInfoCell);
            }
            float4 availabilityInfo = AvailabilityInfoMap[cell.x + AvailabilityInfoToGridSystem.kTextureSize * cell.y].m_AvailabilityInfo;
            float4 y = ((cell.x < AvailabilityInfoToGridSystem.kTextureSize - 1) ? AvailabilityInfoMap[cell.x + 1 + AvailabilityInfoToGridSystem.kTextureSize * cell.y].m_AvailabilityInfo : ((float4)0));
            float4 x = ((cell.y < AvailabilityInfoToGridSystem.kTextureSize - 1) ? AvailabilityInfoMap[cell.x + AvailabilityInfoToGridSystem.kTextureSize * (cell.y + 1)].m_AvailabilityInfo : ((float4)0));
            float4 y2 = ((cell.x < AvailabilityInfoToGridSystem.kTextureSize - 1 && cell.y < AvailabilityInfoToGridSystem.kTextureSize - 1) ? AvailabilityInfoMap[cell.x + 1 + AvailabilityInfoToGridSystem.kTextureSize * (cell.y + 1)].m_AvailabilityInfo : ((float4)0));
            result.m_AvailabilityInfo = math.lerp(math.lerp(availabilityInfo, y, cellCoords.x - (float)cell.x), math.lerp(x, y2, cellCoords.x - (float)cell.x), cellCoords.y - (float)cell.y);
            return result;
        }


    }
}

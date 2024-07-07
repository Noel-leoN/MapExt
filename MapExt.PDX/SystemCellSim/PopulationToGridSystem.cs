using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Objects;
using Game.Tools;
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
    /// <summary>
    /// bcjob£¡£¡£¡£»
    /// </summary>
	public partial class PopulationToGridSystem : CellMapSystem<PopulationCell>, IJobSerializable
    {
        [BurstCompile]
        private struct PopulationToGridJob : IJob
        {
            [ReadOnly]
            public NativeList<Entity> m_Entities;

            public NativeArray<PopulationCell> m_PopulationMap;

            [ReadOnly]
            public BufferLookup<Renter> m_Renters;

            [ReadOnly]
            public ComponentLookup<Transform> m_Transforms;

            [ReadOnly]
            public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

            public void Execute()
            {
                for (int i = 0; i < PopulationToGridSystem.kTextureSize * PopulationToGridSystem.kTextureSize; i++)
                {
                    this.m_PopulationMap[i] = default(PopulationCell);
                }
                for (int j = 0; j < this.m_Entities.Length; j++)
                {
                    Entity entity = this.m_Entities[j];
                    int num = 0;
                    DynamicBuffer<Renter> dynamicBuffer = this.m_Renters[entity];
                    for (int k = 0; k < dynamicBuffer.Length; k++)
                    {
                        Entity renter = dynamicBuffer[k].m_Renter;
                        if (this.m_HouseholdCitizens.HasBuffer(renter))
                        {
                            num += this.m_HouseholdCitizens[renter].Length;
                        }
                    }
                    int2 cell = CellMapSystem<PopulationCell>.GetCell(this.m_Transforms[entity].m_Position, CellMapSystem<PopulationCell>.kMapSize, PopulationToGridSystem.kTextureSize);
                    if (cell.x >= 0 && cell.y >= 0 && cell.x < PopulationToGridSystem.kTextureSize && cell.y < PopulationToGridSystem.kTextureSize)
                    {
                        int index = cell.x + cell.y * PopulationToGridSystem.kTextureSize;
                        PopulationCell value = this.m_PopulationMap[index];
                        value.m_Population += num;
                        this.m_PopulationMap[index] = value;
                    }
                }
            }
        }

        private struct TypeHandle
        {
            [ReadOnly]
            public BufferLookup<Renter> __Game_Buildings_Renter_RO_BufferLookup;

            [ReadOnly]
            public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

            [ReadOnly]
            public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                this.__Game_Buildings_Renter_RO_BufferLookup = state.GetBufferLookup<Renter>(isReadOnly: true);
                this.__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
                this.__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
            }
        }

        public static readonly int kTextureSize = 64;

        public static readonly int kUpdatesPerDay = 32;

        private EntityQuery m_ResidentialPropertyQuery;

        private TypeHandle __TypeHandle;

        public int2 TextureSize => new int2(PopulationToGridSystem.kTextureSize, PopulationToGridSystem.kTextureSize);

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 262144 / PopulationToGridSystem.kUpdatesPerDay;
        }

        public static float3 GetCellCenter(int index)
        {
            return CellMapSystemRe.GetCellCenter(index, PopulationToGridSystem.kTextureSize);
        }

        public static PopulationCell GetPopulation(float3 position, NativeArray<PopulationCell> populationMap)
        {
            PopulationCell result = default(PopulationCell);
            int2 cell = CellMapSystemRe.GetCell(position, CellMapSystemRe.kMapSize, PopulationToGridSystem.kTextureSize);
            float2 cellCoords = CellMapSystemRe.GetCellCoords(position, CellMapSystemRe.kMapSize, PopulationToGridSystem.kTextureSize);
            if (cell.x < 0 || cell.x >= PopulationToGridSystem.kTextureSize || cell.y < 0 || cell.y >= PopulationToGridSystem.kTextureSize)
            {
                return result;
            }
            float population = populationMap[cell.x + PopulationToGridSystem.kTextureSize * cell.y].m_Population;
            float y = ((cell.x < PopulationToGridSystem.kTextureSize - 1) ? populationMap[cell.x + 1 + PopulationToGridSystem.kTextureSize * cell.y].m_Population : 0f);
            float x = ((cell.y < PopulationToGridSystem.kTextureSize - 1) ? populationMap[cell.x + PopulationToGridSystem.kTextureSize * (cell.y + 1)].m_Population : 0f);
            float y2 = ((cell.x < PopulationToGridSystem.kTextureSize - 1 && cell.y < PopulationToGridSystem.kTextureSize - 1) ? populationMap[cell.x + 1 + PopulationToGridSystem.kTextureSize * (cell.y + 1)].m_Population : 0f);
            result.m_Population = math.lerp(math.lerp(population, y, cellCoords.x - (float)cell.x), math.lerp(x, y2, cellCoords.x - (float)cell.x), cellCoords.y - (float)cell.y);
            return result;
        }

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            base.CreateTextures(PopulationToGridSystem.kTextureSize);
            this.m_ResidentialPropertyQuery = base.GetEntityQuery(ComponentType.ReadOnly<ResidentialProperty>(), ComponentType.ReadOnly<Renter>(), ComponentType.ReadOnly<Transform>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
        }

        [Preserve]
        protected override void OnUpdate()
        {
            this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_Renter_RO_BufferLookup.Update(ref base.CheckedStateRef);
            PopulationToGridJob populationToGridJob = default(PopulationToGridJob);
            populationToGridJob.m_Entities = this.m_ResidentialPropertyQuery.ToEntityListAsync(base.World.UpdateAllocator.ToAllocator, out var outJobHandle);
            populationToGridJob.m_PopulationMap = base.m_Map;
            populationToGridJob.m_Renters = this.__TypeHandle.__Game_Buildings_Renter_RO_BufferLookup;
            populationToGridJob.m_HouseholdCitizens = this.__TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup;
            populationToGridJob.m_Transforms = this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup;
            PopulationToGridJob jobData = populationToGridJob;
            base.Dependency = IJobExtensions.Schedule(jobData, JobUtils.CombineDependencies(outJobHandle, base.m_WriteDependencies, base.m_ReadDependencies, base.Dependency));
            base.AddWriter(base.Dependency);
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
        public PopulationToGridSystem()
        {
        }

        public new NativeArray<PopulationCell> GetMap(bool readOnly, out JobHandle dependencies)
        {
            dependencies = (readOnly ? this.m_WriteDependencies : JobHandle.CombineDependencies(this.m_ReadDependencies, this.m_WriteDependencies));
            return this.m_Map;
        }

        public new CellMapData<PopulationCell> GetData(bool readOnly, out JobHandle dependencies)
        {
            dependencies = (readOnly ? this.m_WriteDependencies : JobHandle.CombineDependencies(this.m_ReadDependencies, this.m_WriteDependencies));
            CellMapData<PopulationCell> result = default(CellMapData<PopulationCell>);
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

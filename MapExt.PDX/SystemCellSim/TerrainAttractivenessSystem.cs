using Colossal.Serialization.Entities;
using Game.Prefabs;
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
    public partial class TerrainAttractivenessSystem : CellMapSystem<TerrainAttractiveness>, IJobSerializable
    {
        [BurstCompile]
        private struct TerrainAttractivenessPrepareJob : IJobParallelForBatch
        {
            [ReadOnly]
            public TerrainHeightData m_TerrainData;

            [ReadOnly]
            public WaterSurfaceData m_WaterData;

            [ReadOnly]
            public CellMapData<ZoneAmbienceCell> m_ZoneAmbienceData;

            public NativeArray<float3> m_AttractFactorData;

            public void Execute(int startIndex, int count)
            {
                for (int i = startIndex; i < startIndex + count; i++)
                {
                    float3 cellCenter = TerrainAttractivenessSystem.GetCellCenter(i);
                    this.m_AttractFactorData[i] = new float3(WaterUtils.SampleDepth(ref this.m_WaterData, cellCenter), TerrainUtils.SampleHeight(ref this.m_TerrainData, cellCenter), ZoneAmbienceSystem.GetZoneAmbience(GroupAmbienceType.Forest, cellCenter, this.m_ZoneAmbienceData.m_Buffer, 1f));
                }
            }
        }

        [BurstCompile]
        private struct TerrainAttractivenessJob : IJobParallelForBatch
        {
            [ReadOnly]
            public NativeArray<float3> m_AttractFactorData;

            [ReadOnly]
            public float m_Scale;

            public NativeArray<TerrainAttractiveness> m_AttractivenessMap;

            public AttractivenessParameterData m_AttractivenessParameters;

            public void Execute(int startIndex, int count)
            {
                for (int i = startIndex; i < startIndex + count; i++)
                {
                    float3 cellCenter = TerrainAttractivenessSystem.GetCellCenter(i);
                    float2 @float = 0;
                    int num = Mathf.CeilToInt(math.max(this.m_AttractivenessParameters.m_ForestDistance, this.m_AttractivenessParameters.m_ShoreDistance) / this.m_Scale);
                    for (int j = -num; j <= num; j++)
                    {
                        for (int k = -num; k <= num; k++)
                        {
                            int num2 = math.min(TerrainAttractivenessSystem.kTextureSize - 1, math.max(0, i % TerrainAttractivenessSystem.kTextureSize + j));
                            int num3 = math.min(TerrainAttractivenessSystem.kTextureSize - 1, math.max(0, i / TerrainAttractivenessSystem.kTextureSize + k));
                            int index = num2 + num3 * TerrainAttractivenessSystem.kTextureSize;
                            float3 float2 = this.m_AttractFactorData[index];
                            float num4 = math.distance(TerrainAttractivenessSystem.GetCellCenter(index), cellCenter);
                            @float.x = math.max(@float.x, math.saturate(1f - num4 / this.m_AttractivenessParameters.m_ForestDistance) * float2.z);
                            @float.y = math.max(@float.y, math.saturate(1f - num4 / this.m_AttractivenessParameters.m_ShoreDistance) * ((float2.x > 2f) ? 1f : 0f));
                        }
                    }
                    this.m_AttractivenessMap[i] = new TerrainAttractiveness
                    {
                        m_ForestBonus = @float.x,
                        m_ShoreBonus = @float.y
                    };
                }
            }
        }

        public static readonly int kTextureSize = 128;

        public static readonly int kUpdatesPerDay = 16;

        private TerrainSystem m_TerrainSystem;

        private WaterSystem m_WaterSystem;

        private ZoneAmbienceSystem m_ZoneAmbienceSystem;

        private EntityQuery m_AttractivenessParameterGroup;

        private NativeArray<float3> m_AttractFactorData;

        public int2 TextureSize => new int2(TerrainAttractivenessSystem.kTextureSize, TerrainAttractivenessSystem.kTextureSize);

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 262144 / TerrainAttractivenessSystem.kUpdatesPerDay;
        }

        public static float3 GetCellCenter(int index)
        {
            return CellMapSystemRe.GetCellCenter(index, TerrainAttractivenessSystem.kTextureSize);
        }

        public static float EvaluateAttractiveness(float terrainHeight, TerrainAttractiveness attractiveness, AttractivenessParameterData parameters)
        {
            float num = parameters.m_ForestEffect * attractiveness.m_ForestBonus;
            float num2 = parameters.m_ShoreEffect * attractiveness.m_ShoreBonus;
            float num3 = math.min(parameters.m_HeightBonus.z, math.max(0f, terrainHeight - parameters.m_HeightBonus.x) * parameters.m_HeightBonus.y);
            return num + num2 + num3;
        }

        public static float EvaluateAttractiveness(float3 position, CellMapData<TerrainAttractiveness> data, TerrainHeightData heightData, AttractivenessParameterData parameters, NativeArray<int> factors)
        {
            float num = TerrainUtils.SampleHeight(ref heightData, position);
            TerrainAttractiveness attractiveness = TerrainAttractivenessSystem.GetAttractiveness(position, data.m_Buffer);
            float num2 = parameters.m_ForestEffect * attractiveness.m_ForestBonus;
            AttractionSystem.SetFactor(factors, AttractionSystem.AttractivenessFactor.Forest, num2);
            float num3 = parameters.m_ShoreEffect * attractiveness.m_ShoreBonus;
            AttractionSystem.SetFactor(factors, AttractionSystem.AttractivenessFactor.Beach, num3);
            float num4 = math.min(parameters.m_HeightBonus.z, math.max(0f, num - parameters.m_HeightBonus.x) * parameters.m_HeightBonus.y);
            AttractionSystem.SetFactor(factors, AttractionSystem.AttractivenessFactor.Height, num4);
            return num2 + num3 + num4;
        }

        public static TerrainAttractiveness GetAttractiveness(float3 position, NativeArray<TerrainAttractiveness> attractivenessMap)
        {
            TerrainAttractiveness result = default(TerrainAttractiveness);
            int2 cell = CellMapSystemRe.GetCell(position, CellMapSystemRe.kMapSize, TerrainAttractivenessSystem.kTextureSize);
            float2 cellCoords = CellMapSystemRe.GetCellCoords(position, CellMapSystemRe.kMapSize, TerrainAttractivenessSystem.kTextureSize);
            if (cell.x < 0 || cell.x >= TerrainAttractivenessSystem.kTextureSize || cell.y < 0 || cell.y >= TerrainAttractivenessSystem.kTextureSize)
            {
                return result;
            }
            TerrainAttractiveness terrainAttractiveness = attractivenessMap[cell.x + TerrainAttractivenessSystem.kTextureSize * cell.y];
            TerrainAttractiveness terrainAttractiveness2 = ((cell.x < TerrainAttractivenessSystem.kTextureSize - 1) ? attractivenessMap[cell.x + 1 + TerrainAttractivenessSystem.kTextureSize * cell.y] : default(TerrainAttractiveness));
            TerrainAttractiveness terrainAttractiveness3 = ((cell.y < TerrainAttractivenessSystem.kTextureSize - 1) ? attractivenessMap[cell.x + TerrainAttractivenessSystem.kTextureSize * (cell.y + 1)] : default(TerrainAttractiveness));
            TerrainAttractiveness terrainAttractiveness4 = ((cell.x < TerrainAttractivenessSystem.kTextureSize - 1 && cell.y < TerrainAttractivenessSystem.kTextureSize - 1) ? attractivenessMap[cell.x + 1 + TerrainAttractivenessSystem.kTextureSize * (cell.y + 1)] : default(TerrainAttractiveness));
            result.m_ForestBonus = (short)Mathf.RoundToInt(math.lerp(math.lerp(terrainAttractiveness.m_ForestBonus, terrainAttractiveness2.m_ForestBonus, cellCoords.x - (float)cell.x), math.lerp(terrainAttractiveness3.m_ForestBonus, terrainAttractiveness4.m_ForestBonus, cellCoords.x - (float)cell.x), cellCoords.y - (float)cell.y));
            result.m_ShoreBonus = (short)Mathf.RoundToInt(math.lerp(math.lerp(terrainAttractiveness.m_ShoreBonus, terrainAttractiveness2.m_ShoreBonus, cellCoords.x - (float)cell.x), math.lerp(terrainAttractiveness3.m_ShoreBonus, terrainAttractiveness4.m_ShoreBonus, cellCoords.x - (float)cell.x), cellCoords.y - (float)cell.y));
            return result;
        }

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            base.CreateTextures(TerrainAttractivenessSystem.kTextureSize);
            this.m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
            this.m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
            this.m_ZoneAmbienceSystem = base.World.GetOrCreateSystemManaged<ZoneAmbienceSystem>();
            this.m_AttractivenessParameterGroup = base.GetEntityQuery(ComponentType.ReadOnly<AttractivenessParameterData>());
            this.m_AttractFactorData = new NativeArray<float3>(base.m_Map.Length, Allocator.Persistent);
        }

        [Preserve]
        protected override void OnDestroy()
        {
            this.m_AttractFactorData.Dispose();
            base.OnDestroy();
        }

        [Preserve]
        protected override void OnUpdate()
        {
            TerrainHeightData heightData = this.m_TerrainSystem.GetHeightData();
            TerrainAttractivenessPrepareJob terrainAttractivenessPrepareJob = default(TerrainAttractivenessPrepareJob);
            terrainAttractivenessPrepareJob.m_AttractFactorData = this.m_AttractFactorData;
            terrainAttractivenessPrepareJob.m_TerrainData = heightData;
            terrainAttractivenessPrepareJob.m_WaterData = this.m_WaterSystem.GetSurfaceData(out var deps);
            terrainAttractivenessPrepareJob.m_ZoneAmbienceData = this.m_ZoneAmbienceSystem.GetData(readOnly: true, out var dependencies);
            TerrainAttractivenessPrepareJob jobData = terrainAttractivenessPrepareJob;
            TerrainAttractivenessJob jobData2 = new TerrainAttractivenessJob
            {
                m_Scale = heightData.scale.x * (float)TerrainAttractivenessSystem.kTextureSize,
                m_AttractFactorData = this.m_AttractFactorData,
                m_AttractivenessMap = base.m_Map,
                m_AttractivenessParameters = this.m_AttractivenessParameterGroup.GetSingleton<AttractivenessParameterData>()
            };
            JobHandle jobHandle = jobData.ScheduleBatch(base.m_Map.Length, 4, JobHandle.CombineDependencies(deps, dependencies, base.Dependency));
            this.m_TerrainSystem.AddCPUHeightReader(jobHandle);
            this.m_ZoneAmbienceSystem.AddReader(jobHandle);
            this.m_WaterSystem.AddSurfaceReader(jobHandle);
            base.Dependency = jobData2.ScheduleBatch(base.m_Map.Length, 4, JobHandle.CombineDependencies(base.m_WriteDependencies, base.m_ReadDependencies, jobHandle));
            base.AddWriter(base.Dependency);
            base.Dependency = JobHandle.CombineDependencies(base.m_ReadDependencies, base.m_WriteDependencies, base.Dependency);
        }

        [Preserve]
        public TerrainAttractivenessSystem()
        {
        }

        public new NativeArray<TerrainAttractiveness> GetMap(bool readOnly, out JobHandle dependencies)
        {
            dependencies = (readOnly ? this.m_WriteDependencies : JobHandle.CombineDependencies(this.m_ReadDependencies, this.m_WriteDependencies));
            return this.m_Map;
        }

        public new CellMapData<TerrainAttractiveness> GetData(bool readOnly, out JobHandle dependencies)
        {
            dependencies = (readOnly ? this.m_WriteDependencies : JobHandle.CombineDependencies(this.m_ReadDependencies, this.m_WriteDependencies));
            CellMapData<TerrainAttractiveness> result = default(CellMapData<TerrainAttractiveness>);
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

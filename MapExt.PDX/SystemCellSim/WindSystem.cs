using System;
using Colossal.Serialization.Entities;
using Game.Rendering;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;
using Game;
using Game.Simulation;

namespace MapExt.Systems
{
    /// <summary>
    /// bcjob£¡£¡£¡£»µ÷ÓÃwindsimulation£»
    /// </summary>
    public partial class WindSystem : CellMapSystem<Wind>, IJobSerializable
    {
        [BurstCompile]
        private struct WindCopyJob : IJobFor
        {
            public NativeArray<Wind> m_WindMap;

            [ReadOnly]
            public NativeArray<WindSimulationSystem.WindCell> m_Source;

            [ReadOnly]
            public TerrainHeightData m_TerrainHeightData;

            public void Execute(int index)
            {
                float3 cellCenter = WindSimulationSystem.GetCellCenter(index);
                cellCenter.y = TerrainUtils.SampleHeight(ref this.m_TerrainHeightData, cellCenter) + 25f;
                float num = math.max(0f, (float)WindSimulationSystem.kResolution.z * (cellCenter.y - TerrainUtils.ToWorldSpace(ref this.m_TerrainHeightData, 0f)) / TerrainUtils.ToWorldSpace(ref this.m_TerrainHeightData, 65535f) - 0.5f);
                int3 cell = new int3(index % WindSystem.kTextureSize, index / WindSystem.kTextureSize, Math.Min(Mathf.FloorToInt(num), WindSimulationSystem.kResolution.z - 1));
                int3 cell2 = new int3(cell.x, cell.y, Math.Min(cell.z + 1, WindSimulationSystem.kResolution.z - 1));
                float2 xy = WindSimulationSystem.GetCenterVelocity(cell, this.m_Source).xy;
                float2 xy2 = WindSimulationSystem.GetCenterVelocity(cell2, this.m_Source).xy;
                float2 wind = math.lerp(xy, xy2, math.frac(num));
                this.m_WindMap[index] = new Wind
                {
                    m_Wind = wind
                };
            }
        }

        public static readonly int kTextureSize = 64;

        public static readonly int kUpdateInterval = 512;

        public WindSimulationSystem m_WindSimulationSystem;

        public WindTextureSystem m_WindTextureSystem;

        public TerrainSystem m_TerrainSystem;

        public int2 TextureSize => new int2(WindSystem.kTextureSize, WindSystem.kTextureSize);

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            if (phase != SystemUpdatePhase.GameSimulation)
            {
                return 1;
            }
            return WindSystem.kUpdateInterval;
        }

        public static float3 GetCellCenter(int index)
        {
            return CellMapSystem<Wind>.GetCellCenter(index, WindSystem.kTextureSize);
        }

        public static Wind GetWind(float3 position, NativeArray<Wind> windMap)
        {
            int2 cell = CellMapSystemRe.GetCell(position, CellMapSystemRe.kMapSize, WindSystem.kTextureSize);
            cell = math.clamp(cell, 0, WindSystem.kTextureSize - 1);
            float2 cellCoords = CellMapSystemRe.GetCellCoords(position, CellMapSystemRe.kMapSize, WindSystem.kTextureSize);
            int num = math.min(WindSystem.kTextureSize - 1, cell.x + 1);
            int num2 = math.min(WindSystem.kTextureSize - 1, cell.y + 1);
            Wind result = default(Wind);
            result.m_Wind = math.lerp(math.lerp(windMap[cell.x + WindSystem.kTextureSize * cell.y].m_Wind, windMap[num + WindSystem.kTextureSize * cell.y].m_Wind, cellCoords.x - (float)cell.x), math.lerp(windMap[cell.x + WindSystem.kTextureSize * num2].m_Wind, windMap[num + WindSystem.kTextureSize * num2].m_Wind, cellCoords.x - (float)cell.x), cellCoords.y - (float)cell.y);
            return result;
        }

        public override JobHandle Deserialize<TReader>(EntityReaderData readerData, JobHandle inputDeps)
        {
            this.m_WindTextureSystem.RequireUpdate();
            if (readerData.GetReader<TReader>().context.version > Game.Version.cellMapLengths)
            {
                return base.Deserialize<TReader>(readerData, inputDeps);
            }
            base.m_Map.Dispose();
            base.m_Map = new NativeArray<Wind>(65536, Allocator.Persistent);
            inputDeps = base.Deserialize<TReader>(readerData, inputDeps);
            inputDeps.Complete();
            base.m_Map.Dispose();
            base.m_Map = new NativeArray<Wind>(WindSystem.kTextureSize * WindSystem.kTextureSize, Allocator.Persistent);
            return inputDeps;
        }

        public override JobHandle SetDefaults(Context context)
        {
            this.m_WindTextureSystem.RequireUpdate();
            for (int i = 0; i < base.m_Map.Length; i++)
            {
                base.m_Map[i] = new Wind
                {
                    m_Wind = this.m_WindSimulationSystem.constantWind
                };
            }
            return default(JobHandle);
        }

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            this.m_WindSimulationSystem = base.World.GetOrCreateSystemManaged<WindSimulationSystem>();
            this.m_WindTextureSystem = base.World.GetOrCreateSystemManaged<WindTextureSystem>();
            this.m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
            base.CreateTextures(WindSystem.kTextureSize);
            for (int i = 0; i < base.m_Map.Length; i++)
            {
                base.m_Map[i] = new Wind
                {
                    m_Wind = this.m_WindSimulationSystem.constantWind
                };
            }
        }

        [Preserve]
        protected override void OnUpdate()
        {
            TerrainHeightData heightData = this.m_TerrainSystem.GetHeightData();
            if (heightData.isCreated)
            {
                WindCopyJob windCopyJob = default(WindCopyJob);
                windCopyJob.m_WindMap = base.m_Map;
                windCopyJob.m_Source = this.m_WindSimulationSystem.GetCells(out var deps);
                windCopyJob.m_TerrainHeightData = heightData;
                WindCopyJob jobData = windCopyJob;
                base.Dependency = jobData.Schedule(base.m_Map.Length, JobHandle.CombineDependencies(deps, JobHandle.CombineDependencies(base.m_WriteDependencies, base.m_ReadDependencies, base.Dependency)));
                base.AddWriter(base.Dependency);
                this.m_TerrainSystem.AddCPUHeightReader(base.Dependency);
                this.m_WindSimulationSystem.AddReader(base.Dependency);
                this.m_WindTextureSystem.RequireUpdate();
            }
        }

        [Preserve]
        public WindSystem()
        {
        }

        public new NativeArray<Wind> GetMap(bool readOnly, out JobHandle dependencies)
        {
            dependencies = (readOnly ? this.m_WriteDependencies : JobHandle.CombineDependencies(this.m_ReadDependencies, this.m_WriteDependencies));
            return this.m_Map;
        }

        public new CellMapData<Wind> GetData(bool readOnly, out JobHandle dependencies)
        {
            dependencies = (readOnly ? this.m_WriteDependencies : JobHandle.CombineDependencies(this.m_ReadDependencies, this.m_WriteDependencies));
            CellMapData<Wind> result = default(CellMapData<Wind>);
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

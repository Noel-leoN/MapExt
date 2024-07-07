using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Common;
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
    /// bcjob未引用mapsize;可不替换；
    /// </summary>
    public partial class GroundPollutionSystem : CellMapSystem<GroundPollution>, IJobSerializable
    {
        [BurstCompile]
        private struct PollutionFadeJob : IJob
        {
            public NativeArray<GroundPollution> m_PollutionMap;

            public PollutionParameterData m_PollutionParameters;

            public RandomSeed m_Random;

            public uint m_Frame;

            public void Execute()
            {
                Unity.Mathematics.Random random = this.m_Random.GetRandom((int)this.m_Frame);
                for (int i = 0; i < this.m_PollutionMap.Length; i++)
                {
                    GroundPollution value = this.m_PollutionMap[i];
                    value.m_Previous = value.m_Pollution;
                    if (value.m_Pollution > 0)
                    {
                        value.m_Pollution = (short)math.max(0, this.m_PollutionMap[i].m_Pollution - MathUtils.RoundToIntRandom(ref random, (float)this.m_PollutionParameters.m_GroundFade / (float)GroundPollutionSystem.kUpdatesPerDay));
                    }
                    this.m_PollutionMap[i] = value;
                }
            }
        }

        public static readonly int kTextureSize = 256;

        public static readonly int kUpdatesPerDay = 128;

        private SimulationSystem m_SimulationSystem;

        private EntityQuery m_PollutionParameterGroup;

        public int2 TextureSize => new int2(GroundPollutionSystem.kTextureSize, GroundPollutionSystem.kTextureSize);

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 262144 / GroundPollutionSystem.kUpdatesPerDay;
        }

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            this.m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
            base.CreateTextures(GroundPollutionSystem.kTextureSize);
            this.m_PollutionParameterGroup = base.GetEntityQuery(ComponentType.ReadOnly<PollutionParameterData>());
            base.RequireForUpdate(this.m_PollutionParameterGroup);
        }

        [Preserve]
        protected override void OnUpdate()
        {
            PollutionFadeJob pollutionFadeJob = default(PollutionFadeJob);
            pollutionFadeJob.m_PollutionMap = base.m_Map;
            pollutionFadeJob.m_PollutionParameters = this.m_PollutionParameterGroup.GetSingleton<PollutionParameterData>();
            pollutionFadeJob.m_Random = RandomSeed.Next();
            pollutionFadeJob.m_Frame = this.m_SimulationSystem.frameIndex;
            PollutionFadeJob jobData = pollutionFadeJob;
            base.Dependency = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(base.m_WriteDependencies, base.m_ReadDependencies, base.Dependency));
            base.AddWriter(base.Dependency);
            base.Dependency = JobHandle.CombineDependencies(base.m_ReadDependencies, base.m_WriteDependencies, base.Dependency);
        }

        [Preserve]
        public GroundPollutionSystem()
        {
        }

        //泛型非静态方法重定向，配合harmony patch;
        public new NativeArray<GroundPollution> GetMap(bool readOnly, out JobHandle dependencies)
        {
            dependencies = (readOnly ? this.m_WriteDependencies : JobHandle.CombineDependencies(this.m_ReadDependencies, this.m_WriteDependencies));
            return this.m_Map;
        }

        public new CellMapData<GroundPollution> GetData(bool readOnly, out JobHandle dependencies)
        {
            dependencies = (readOnly ? this.m_WriteDependencies : JobHandle.CombineDependencies(this.m_ReadDependencies, this.m_WriteDependencies));
            CellMapData<GroundPollution> result = default(CellMapData<GroundPollution>);
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
            return CellMapSystemRe.GetCellCenter(index, GroundPollutionSystem.kTextureSize);
        }

        public static GroundPollution GetPollution(float3 position, NativeArray<GroundPollution> pollutionMap)
        {
            GroundPollution result = default(GroundPollution);
            int2 cell = CellMapSystemRe.GetCell(position, CellMapSystemRe.kMapSize, GroundPollutionSystem.kTextureSize);
            float2 cellCoords = CellMapSystemRe.GetCellCoords(position, CellMapSystemRe.kMapSize, GroundPollutionSystem.kTextureSize);
            if (cell.x < 0 || cell.x >= GroundPollutionSystem.kTextureSize || cell.y < 0 || cell.y >= GroundPollutionSystem.kTextureSize)
            {
                return result;
            }
            GroundPollution groundPollution = pollutionMap[cell.x + GroundPollutionSystem.kTextureSize * cell.y];
            GroundPollution groundPollution2 = ((cell.x < GroundPollutionSystem.kTextureSize - 1) ? pollutionMap[cell.x + 1 + GroundPollutionSystem.kTextureSize * cell.y] : default(GroundPollution));
            GroundPollution groundPollution3 = ((cell.y < GroundPollutionSystem.kTextureSize - 1) ? pollutionMap[cell.x + GroundPollutionSystem.kTextureSize * (cell.y + 1)] : default(GroundPollution));
            GroundPollution groundPollution4 = ((cell.x < GroundPollutionSystem.kTextureSize - 1 && cell.y < GroundPollutionSystem.kTextureSize - 1) ? pollutionMap[cell.x + 1 + GroundPollutionSystem.kTextureSize * (cell.y + 1)] : default(GroundPollution));
            result.m_Pollution = (short)Mathf.RoundToInt(math.lerp(math.lerp(groundPollution.m_Pollution, groundPollution2.m_Pollution, cellCoords.x - (float)cell.x), math.lerp(groundPollution3.m_Pollution, groundPollution4.m_Pollution, cellCoords.x - (float)cell.x), cellCoords.y - (float)cell.y));
            result.m_Previous = (short)Mathf.RoundToInt(math.lerp(math.lerp(groundPollution.m_Previous, groundPollution2.m_Previous, cellCoords.x - (float)cell.x), math.lerp(groundPollution3.m_Previous, groundPollution4.m_Previous, cellCoords.x - (float)cell.x), cellCoords.y - (float)cell.y));
            return result;
        }
    }
}

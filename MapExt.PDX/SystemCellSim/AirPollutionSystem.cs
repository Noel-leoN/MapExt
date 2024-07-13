using Colossal.Entities;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Prefabs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;
using Game;
using Game.Simulation;
using Mono.Cecil;
using Mono.Cecil.Cil;
using HarmonyLib;
using System;

namespace MapExt.Systems
{
    /// <summary>
    /// bc job 调用cellmap，需覆写系统；
    /// </summary>
	public partial class AirPollutionSystem : CellMapSystem<AirPollution>, IJobSerializable
    {
        [BurstCompile]
        private struct AirPollutionMoveJob : IJob
        {
            public NativeArray<AirPollution> m_PollutionMap;

            [ReadOnly]
            public NativeArray<Wind> m_WindMap;

            public PollutionParameterData m_PollutionParameters;

            public RandomSeed m_Random;

            public uint m_Frame;

            public void Execute()
            {
                NativeArray<AirPollution> nativeArray = new NativeArray<AirPollution>(this.m_PollutionMap.Length, Allocator.Temp);
                Unity.Mathematics.Random random = this.m_Random.GetRandom((int)this.m_Frame);
                for (int i = 0; i < this.m_PollutionMap.Length; i++)
                {
                    float3 cellCenter = AirPollutionSystem.GetCellCenter(i);
                    Wind wind = WindSystem.GetWind(cellCenter, this.m_WindMap);
                    short pollution = AirPollutionSystem.GetPollution(cellCenter - this.m_PollutionParameters.m_WindAdvectionSpeed * new float3(wind.m_Wind.x, 0f, wind.m_Wind.y), this.m_PollutionMap).m_Pollution;
                    nativeArray[i] = new AirPollution
                    {
                        m_Pollution = pollution
                    };
                }
                float value = (float)this.m_PollutionParameters.m_AirFade / (float)AirPollutionSystem.kUpdatesPerDay;
                for (int j = 0; j < AirPollutionSystem.kTextureSize; j++)
                {
                    for (int k = 0; k < AirPollutionSystem.kTextureSize; k++)
                    {
                        int num = j * AirPollutionSystem.kTextureSize + k;
                        int pollution2 = nativeArray[num].m_Pollution;
                        pollution2 += ((k > 0) ? (nativeArray[num - 1].m_Pollution >> AirPollutionSystem.kSpread) : 0);
                        pollution2 += ((k < AirPollutionSystem.kTextureSize - 1) ? (nativeArray[num + 1].m_Pollution >> AirPollutionSystem.kSpread) : 0);
                        pollution2 += ((j > 0) ? (nativeArray[num - AirPollutionSystem.kTextureSize].m_Pollution >> AirPollutionSystem.kSpread) : 0);
                        pollution2 += ((j < AirPollutionSystem.kTextureSize - 1) ? (nativeArray[num + AirPollutionSystem.kTextureSize].m_Pollution >> AirPollutionSystem.kSpread) : 0);
                        pollution2 -= (nativeArray[num].m_Pollution >> AirPollutionSystem.kSpread - 2) + MathUtils.RoundToIntRandom(ref random, value);
                        pollution2 = math.clamp(pollution2, 0, 32767);
                        this.m_PollutionMap[num] = new AirPollution
                        {
                            m_Pollution = (short)pollution2
                        };
                    }
                }
                nativeArray.Dispose();
            }
        }

        private static readonly int kSpread = 3;

        public static readonly int kTextureSize = 256;

        public static readonly int kUpdatesPerDay = 128;

        private WindSystem m_WindSystem;

        private SimulationSystem m_SimulationSystem;

        private EntityQuery m_PollutionParameterQuery;

        public int2 TextureSize => new int2(AirPollutionSystem.kTextureSize, AirPollutionSystem.kTextureSize);

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 262144 / AirPollutionSystem.kUpdatesPerDay;
        }        

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            base.CreateTextures(AirPollutionSystem.kTextureSize);
            this.m_WindSystem = base.World.GetOrCreateSystemManaged<WindSystem>();
            this.m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
            this.m_PollutionParameterQuery = base.GetEntityQuery(ComponentType.ReadOnly<PollutionParameterData>());
            base.RequireForUpdate(this.m_PollutionParameterQuery);
        }

        [Preserve]
        protected override void OnUpdate()
        {
            AirPollutionMoveJob airPollutionMoveJob = default(AirPollutionMoveJob);
            airPollutionMoveJob.m_PollutionMap = base.m_Map;
            airPollutionMoveJob.m_WindMap = this.m_WindSystem.GetMap(readOnly: true, out var dependencies);
            airPollutionMoveJob.m_PollutionParameters = this.m_PollutionParameterQuery.GetSingleton<PollutionParameterData>();
            airPollutionMoveJob.m_Random = RandomSeed.Next();
            airPollutionMoveJob.m_Frame = this.m_SimulationSystem.frameIndex;
            AirPollutionMoveJob jobData = airPollutionMoveJob;
            base.Dependency = IJobExtensions.Schedule(jobData, JobUtils.CombineDependencies(dependencies, base.m_WriteDependencies, base.m_ReadDependencies, base.Dependency));
            this.m_WindSystem.AddReader(base.Dependency);
            base.AddWriter(base.Dependency);
            base.Dependency = JobHandle.CombineDependencies(base.m_ReadDependencies, base.m_WriteDependencies, base.Dependency);
           
        }

        [Preserve]
        public AirPollutionSystem()
        {
        }

        //泛型非静态方法重定向，配合harmony patch;
        public new NativeArray<AirPollution> GetMap(bool readOnly, out JobHandle dependencies)
        {
            dependencies = (readOnly ? this.m_WriteDependencies : JobHandle.CombineDependencies(this.m_ReadDependencies, this.m_WriteDependencies));

            //debug;
            //Console.WriteLine("MapExt.Systems.AirPollutionSystem.GetMap: " + this.m_Map);

            return this.m_Map;
            

        }

        public new CellMapData<AirPollution> GetData(bool readOnly, out JobHandle dependencies)
        {
            dependencies = (readOnly ? this.m_WriteDependencies : JobHandle.CombineDependencies(this.m_ReadDependencies, this.m_WriteDependencies));
            CellMapData<AirPollution> result = default(CellMapData<AirPollution>);
            result.m_Buffer = this.m_Map;
            result.m_CellSize = 57344 / (float2)this.m_TextureSize;
            result.m_TextureSize = this.m_TextureSize;

            //debug;
            //Console.WriteLine("MapExt.Systems.AirPollutionSystem.GetData: " + result.m_CellSize * this.m_TextureSize);

            return result;
        }

        public new void AddReader(JobHandle jobHandle)
        {
            this.m_ReadDependencies = JobHandle.CombineDependencies(this.m_ReadDependencies, jobHandle);

            //debug;
            //Console.WriteLine("MapExt.Systems.AirPollutionSystem.AddReader: " + this.m_ReadDependencies);
        }

        public new void AddWriter(JobHandle jobHandle)
        {
            this.m_WriteDependencies = jobHandle;

            //debug;
            //Console.WriteLine("MapExt.Systems.AirPollutionSystem.AddWriter: " + this.m_WriteDependencies);
        }
       
        //原系统方法改写(防止修改gamedll编译未生效)
        public static float3 GetCellCenter(int index)
        {
            return CellMapSystemRe.GetCellCenter(index, AirPollutionSystem.kTextureSize);
        }

        public static AirPollution GetPollution(float3 position, NativeArray<AirPollution> pollutionMap)
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
            return result;
        }
    }//class;
        
}//namespace;

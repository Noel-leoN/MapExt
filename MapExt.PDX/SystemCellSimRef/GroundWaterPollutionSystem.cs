using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;
using Game;
using Game.Simulation;

namespace MapExt.Systems
{
    /// <summary>
    /// bcjobµ÷ÓÃ£¡£¡£¡£»
    /// </summary>
    public partial class GroundWaterPollutionSystem : GameSystemBase
	{
		[BurstCompile]
		private struct PolluteGroundWaterJob : IJob
		{
			public NativeArray<GroundWater> m_GroundWaterMap;

			[ReadOnly]
			public NativeArray<GroundPollution> m_PollutionMap;

			public void Execute()
			{
				for (int i = 0; i < this.m_GroundWaterMap.Length; i++)
				{
					GroundWater value = this.m_GroundWaterMap[i];
					GroundPollution pollution = GroundPollutionSystem.GetPollution(GroundWaterSystem.GetCellCenter(i), this.m_PollutionMap);
					if (pollution.m_Pollution > 0)
					{
						value.m_Polluted = (short)math.min(value.m_Amount, value.m_Polluted + pollution.m_Pollution / 200);
						this.m_GroundWaterMap[i] = value;
					}
				}
			}
		}

		private GroundWaterSystem m_GroundWaterSystem;

		private GroundPollutionSystem m_GroundPollutionSystem;

		public override int GetUpdateInterval(SystemUpdatePhase phase)
		{
			return 128;
		}

		public override int GetUpdateOffset(SystemUpdatePhase phase)
		{
			return 64;
		}

		[Preserve]
		protected override void OnCreate()
		{
			base.OnCreate();
			this.m_GroundWaterSystem = base.World.GetOrCreateSystemManaged<GroundWaterSystem>();
			this.m_GroundPollutionSystem = base.World.GetOrCreateSystemManaged<GroundPollutionSystem>();
		}

		[Preserve]
		protected override void OnUpdate()
		{
			PolluteGroundWaterJob polluteGroundWaterJob = default(PolluteGroundWaterJob);
			polluteGroundWaterJob.m_GroundWaterMap = this.m_GroundWaterSystem.GetMap(readOnly: false, out var dependencies);
			polluteGroundWaterJob.m_PollutionMap = this.m_GroundPollutionSystem.GetMap(readOnly: true, out var dependencies2);
			PolluteGroundWaterJob jobData = polluteGroundWaterJob;
			base.Dependency = jobData.Schedule(JobHandle.CombineDependencies(base.Dependency, dependencies, dependencies2));
			this.m_GroundWaterSystem.AddWriter(base.Dependency);
			this.m_GroundPollutionSystem.AddReader(base.Dependency);
		}

		[Preserve]
		public GroundWaterPollutionSystem()
		{
		}
	}
}

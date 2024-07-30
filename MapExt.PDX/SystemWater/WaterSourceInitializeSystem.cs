using System.Runtime.CompilerServices;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;
using Game;
using Game.Simulation;

namespace MapExt.Systems
{
	//[CompilerGenerated]
	public partial class WaterSourceInitializeSystem : GameSystemBase
	{
		[BurstCompile]
		private struct InitializeWaterSourcesJob : IJobChunk
		{
			[ReadOnly]
			public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

			public ComponentTypeHandle<Game.Simulation.WaterSourceData> m_SourceType;

			[ReadOnly]
			public ComponentTypeHandle<Transform> m_TransformType;

			[ReadOnly]
			public ComponentLookup<Game.Prefabs.WaterSourceData> m_PrefabSourceDatas;

			public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				NativeArray<Game.Simulation.WaterSourceData> nativeArray = chunk.GetNativeArray(ref this.m_SourceType);
				NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref this.m_PrefabRefType);
				NativeArray<Transform> nativeArray3 = chunk.GetNativeArray(ref this.m_TransformType);
				for (int i = 0; i < chunk.Count; i++)
				{
					Game.Prefabs.WaterSourceData waterSourceData = this.m_PrefabSourceDatas[nativeArray2[i].m_Prefab];
                    Game.Simulation.WaterSourceData waterSourceData2 = nativeArray[i];
					waterSourceData2.m_Amount = waterSourceData.m_Amount;
					waterSourceData2.m_Radius = waterSourceData.m_Radius;
					if (waterSourceData2.m_ConstantDepth != 2 && waterSourceData2.m_ConstantDepth != 3)
					{
						waterSourceData2.m_Multiplier = WaterSystem.CalculateSourceMultiplier(waterSourceData2, nativeArray3[i].m_Position);
					}
					waterSourceData2.m_Polluted = waterSourceData.m_InitialPolluted;
					nativeArray[i] = waterSourceData2;
				}
			}

			void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				this.Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
			}
		}

		private struct TypeHandle
		{
			public ComponentTypeHandle<Game.Simulation.WaterSourceData> __Game_Simulation_WaterSourceData_RW_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentLookup<Game.Prefabs.WaterSourceData> __Game_Prefabs_WaterSourceData_RO_ComponentLookup;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void __AssignHandles(ref SystemState state)
			{
				this.__Game_Simulation_WaterSourceData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Simulation.WaterSourceData>();
				this.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
				this.__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
				this.__Game_Prefabs_WaterSourceData_RO_ComponentLookup = state.GetComponentLookup<Game.Prefabs.WaterSourceData>(isReadOnly: true);
			}
		}

		private EntityQuery m_WaterSourceQuery;

		private TypeHandle __TypeHandle;

		[Preserve]
		protected override void OnCreate()
		{
			base.OnCreate();
			this.m_WaterSourceQuery = base.GetEntityQuery(ComponentType.ReadOnly<Game.Simulation.WaterSourceData>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<Created>(), ComponentType.Exclude<Temp>());
			base.RequireForUpdate(this.m_WaterSourceQuery);
		}

		[Preserve]
		protected override void OnUpdate()
		{
			this.__TypeHandle.__Game_Prefabs_WaterSourceData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Simulation_WaterSourceData_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			InitializeWaterSourcesJob initializeWaterSourcesJob = default(InitializeWaterSourcesJob);
			initializeWaterSourcesJob.m_SourceType = this.__TypeHandle.__Game_Simulation_WaterSourceData_RW_ComponentTypeHandle;
			initializeWaterSourcesJob.m_PrefabRefType = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;
			initializeWaterSourcesJob.m_TransformType = this.__TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle;
			initializeWaterSourcesJob.m_PrefabSourceDatas = this.__TypeHandle.__Game_Prefabs_WaterSourceData_RO_ComponentLookup;
			InitializeWaterSourcesJob jobData = initializeWaterSourcesJob;
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, this.m_WaterSourceQuery, base.Dependency);
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
		public WaterSourceInitializeSystem()
		{
		}
	}
}

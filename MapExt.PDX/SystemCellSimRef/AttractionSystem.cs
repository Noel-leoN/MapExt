using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
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
	//[CompilerGenerated]
	public partial class AttractionSystem : GameSystemBase
	{
		public enum AttractivenessFactor
		{
			Efficiency = 0,
			Maintenance = 1,
			Forest = 2,
			Beach = 3,
			Height = 4,
			Count = 5
		}

		[BurstCompile]
		private struct AttractivenessJob : IJobChunk
		{
			public ComponentTypeHandle<AttractivenessProvider> m_AttractivenessType;

			[ReadOnly]
			public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

			[ReadOnly]
			public ComponentTypeHandle<PrefabRef> m_PrefabType;

			[ReadOnly]
			public BufferTypeHandle<Efficiency> m_EfficiencyType;

			[ReadOnly]
			public ComponentTypeHandle<Signature> m_SignatureType;

			[ReadOnly]
			public ComponentTypeHandle<Game.Buildings.Park> m_ParkType;

			[ReadOnly]
			public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

			[ReadOnly]
			public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;

			[ReadOnly]
			public ComponentLookup<AttractionData> m_AttractionDatas;

			[ReadOnly]
			public ComponentLookup<ParkData> m_ParkDatas;

			[ReadOnly]
			public ComponentLookup<PrefabRef> m_PrefabRefData;

			[ReadOnly]
			public CellMapData<TerrainAttractiveness> m_TerrainMap;

			[ReadOnly]
			public TerrainHeightData m_HeightData;

			public AttractivenessParameterData m_Parameters;

			public uint m_UpdateFrameIndex;

			public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				if (chunk.GetSharedComponent(this.m_UpdateFrameType).m_Index != this.m_UpdateFrameIndex)
				{
					return;
				}
				NativeArray<PrefabRef> nativeArray = chunk.GetNativeArray(ref this.m_PrefabType);
				NativeArray<AttractivenessProvider> nativeArray2 = chunk.GetNativeArray(ref this.m_AttractivenessType);
				BufferAccessor<Efficiency> bufferAccessor = chunk.GetBufferAccessor(ref this.m_EfficiencyType);
				NativeArray<Game.Buildings.Park> nativeArray3 = chunk.GetNativeArray(ref this.m_ParkType);
				NativeArray<Game.Objects.Transform> nativeArray4 = chunk.GetNativeArray(ref this.m_TransformType);
				BufferAccessor<InstalledUpgrade> bufferAccessor2 = chunk.GetBufferAccessor(ref this.m_InstalledUpgradeType);
				bool flag = chunk.Has(ref this.m_SignatureType);
				for (int i = 0; i < chunk.Count; i++)
				{
					Entity prefab = nativeArray[i].m_Prefab;
					AttractionData result = default(AttractionData);
					if (this.m_AttractionDatas.HasComponent(prefab))
					{
						result = this.m_AttractionDatas[prefab];
					}
					if (bufferAccessor2.Length != 0)
					{
						UpgradeUtils.CombineStats(ref result, bufferAccessor2[i], ref this.m_PrefabRefData, ref this.m_AttractionDatas);
					}
					float num = result.m_Attractiveness;
					if (!flag)
					{
						num *= BuildingUtils.GetEfficiency(bufferAccessor, i);
					}
					if (chunk.Has(ref this.m_ParkType) && this.m_ParkDatas.HasComponent(prefab))
					{
						Game.Buildings.Park park = nativeArray3[i];
						ParkData parkData = this.m_ParkDatas[prefab];
						float num2 = ((parkData.m_MaintenancePool > 0) ? ((float)park.m_Maintenance / (float)parkData.m_MaintenancePool) : 0f);
						num *= 0.8f + 0.2f * num2;
					}
					if (chunk.Has(ref this.m_TransformType))
					{
						float3 position = nativeArray4[i].m_Position;
						num *= 1f + 0.01f * TerrainAttractivenessSystem.EvaluateAttractiveness(position, this.m_TerrainMap, this.m_HeightData, this.m_Parameters, default(NativeArray<int>));
					}
					AttractivenessProvider attractivenessProvider = default(AttractivenessProvider);
					attractivenessProvider.m_Attractiveness = Mathf.RoundToInt(num);
					AttractivenessProvider value = attractivenessProvider;
					nativeArray2[i] = value;
				}
			}

			void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				this.Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
			}
		}

		private struct TypeHandle
		{
			public ComponentTypeHandle<AttractivenessProvider> __Game_Buildings_AttractivenessProvider_RW_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

			[ReadOnly]
			public BufferTypeHandle<Efficiency> __Game_Buildings_Efficiency_RO_BufferTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<Signature> __Game_Buildings_Signature_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<Game.Buildings.Park> __Game_Buildings_Park_RO_ComponentTypeHandle;

			[ReadOnly]
			public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

			public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

			[ReadOnly]
			public ComponentLookup<AttractionData> __Game_Prefabs_AttractionData_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<ParkData> __Game_Prefabs_ParkData_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void __AssignHandles(ref SystemState state)
			{
				this.__Game_Buildings_AttractivenessProvider_RW_ComponentTypeHandle = state.GetComponentTypeHandle<AttractivenessProvider>();
				this.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
				this.__Game_Buildings_Efficiency_RO_BufferTypeHandle = state.GetBufferTypeHandle<Efficiency>(isReadOnly: true);
				this.__Game_Buildings_Signature_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Signature>(isReadOnly: true);
				this.__Game_Buildings_Park_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.Park>(isReadOnly: true);
				this.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
				this.__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Transform>(isReadOnly: true);
				this.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
				this.__Game_Prefabs_AttractionData_RO_ComponentLookup = state.GetComponentLookup<AttractionData>(isReadOnly: true);
				this.__Game_Prefabs_ParkData_RO_ComponentLookup = state.GetComponentLookup<ParkData>(isReadOnly: true);
				this.__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			}
		}

		private SimulationSystem m_SimulationSystem;

		private TerrainAttractivenessSystem m_TerrainAttractivenessSystem;

		private TerrainSystem m_TerrainSystem;

		private EntityQuery m_BuildingGroup;

		private EntityQuery m_SettingsQuery;

		private TypeHandle __TypeHandle;

		public override int GetUpdateInterval(SystemUpdatePhase phase)
		{
			return 16;
		}

		public static void SetFactor(NativeArray<int> factors, AttractivenessFactor factor, float attractiveness)
		{
			if (factors.IsCreated && factors.Length == 5)
			{
				factors[(int)factor] = Mathf.RoundToInt(attractiveness);
			}
		}

		[Preserve]
		protected override void OnCreate()
		{
			base.OnCreate();
			this.m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
			this.m_TerrainAttractivenessSystem = base.World.GetOrCreateSystemManaged<TerrainAttractivenessSystem>();
			this.m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
			this.m_SettingsQuery = base.GetEntityQuery(ComponentType.ReadOnly<AttractivenessParameterData>());
			this.m_BuildingGroup = base.GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[3]
				{
					ComponentType.ReadWrite<AttractivenessProvider>(),
					ComponentType.ReadOnly<PrefabRef>(),
					ComponentType.ReadOnly<UpdateFrame>()
				},
				None = new ComponentType[3]
				{
					ComponentType.ReadOnly<Destroyed>(),
					ComponentType.ReadOnly<Deleted>(),
					ComponentType.ReadOnly<Temp>()
				}
			});
		}

		[Preserve]
		protected override void OnUpdate()
		{
			uint updateFrameWithInterval = SimulationUtils.GetUpdateFrameWithInterval(this.m_SimulationSystem.frameIndex, (uint)this.GetUpdateInterval(SystemUpdatePhase.GameSimulation), 16);
			this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_ParkData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_AttractionData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Buildings_Park_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Buildings_Signature_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Buildings_Efficiency_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Buildings_AttractivenessProvider_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			AttractivenessJob attractivenessJob = default(AttractivenessJob);
			attractivenessJob.m_AttractivenessType = this.__TypeHandle.__Game_Buildings_AttractivenessProvider_RW_ComponentTypeHandle;
			attractivenessJob.m_PrefabType = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;
			attractivenessJob.m_EfficiencyType = this.__TypeHandle.__Game_Buildings_Efficiency_RO_BufferTypeHandle;
			attractivenessJob.m_SignatureType = this.__TypeHandle.__Game_Buildings_Signature_RO_ComponentTypeHandle;
			attractivenessJob.m_ParkType = this.__TypeHandle.__Game_Buildings_Park_RO_ComponentTypeHandle;
			attractivenessJob.m_InstalledUpgradeType = this.__TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;
			attractivenessJob.m_TransformType = this.__TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle;
			attractivenessJob.m_UpdateFrameType = this.__TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle;
			attractivenessJob.m_AttractionDatas = this.__TypeHandle.__Game_Prefabs_AttractionData_RO_ComponentLookup;
			attractivenessJob.m_ParkDatas = this.__TypeHandle.__Game_Prefabs_ParkData_RO_ComponentLookup;
			attractivenessJob.m_PrefabRefData = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup;
			attractivenessJob.m_TerrainMap = this.m_TerrainAttractivenessSystem.GetData(readOnly: true, out var dependencies);
			attractivenessJob.m_HeightData = this.m_TerrainSystem.GetHeightData();
			attractivenessJob.m_Parameters = this.m_SettingsQuery.GetSingleton<AttractivenessParameterData>();
			attractivenessJob.m_UpdateFrameIndex = updateFrameWithInterval;
			AttractivenessJob jobData = attractivenessJob;
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, this.m_BuildingGroup, JobHandle.CombineDependencies(base.Dependency, dependencies));
			this.m_TerrainSystem.AddCPUHeightReader(base.Dependency);
			this.m_TerrainAttractivenessSystem.AddReader(base.Dependency);
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
		public AttractionSystem()
		{
		}
	}
}

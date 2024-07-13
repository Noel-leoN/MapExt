using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;
using Game.Tools;
using Game;

namespace MapExt.Systems
{
	//[CompilerGenerated]
	public partial class TelecomPreviewSystem : CellMapSystem<TelecomCoverage>
	{
		private struct TypeHandle
		{
			[ReadOnly]
			public ComponentTypeHandle<Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<Game.Buildings.TelecomFacility> __Game_Buildings_TelecomFacility_RO_ComponentTypeHandle;

			[ReadOnly]
			public BufferTypeHandle<Efficiency> __Game_Buildings_Efficiency_RO_BufferTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

			[ReadOnly]
			public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

			[ReadOnly]
			public BufferTypeHandle<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferTypeHandle;

			[ReadOnly]
			public BufferTypeHandle<Employee> __Game_Companies_Employee_RO_BufferTypeHandle;

			[ReadOnly]
			public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<Game.Buildings.TelecomFacility> __Game_Buildings_TelecomFacility_RO_ComponentLookup;

			[ReadOnly]
			public BufferLookup<Efficiency> __Game_Buildings_Efficiency_RO_BufferLookup;

			[ReadOnly]
			public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<TelecomFacilityData> __Game_Prefabs_TelecomFacilityData_RO_ComponentLookup;

			[ReadOnly]
			public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void __AssignHandles(ref SystemState state)
			{
				this.__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
				this.__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PropertyRenter>(isReadOnly: true);
				this.__Game_Buildings_TelecomFacility_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.TelecomFacility>(isReadOnly: true);
				this.__Game_Buildings_Efficiency_RO_BufferTypeHandle = state.GetBufferTypeHandle<Efficiency>(isReadOnly: true);
				this.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
				this.__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
				this.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
				this.__Game_Citizens_HouseholdCitizen_RO_BufferTypeHandle = state.GetBufferTypeHandle<HouseholdCitizen>(isReadOnly: true);
				this.__Game_Companies_Employee_RO_BufferTypeHandle = state.GetBufferTypeHandle<Employee>(isReadOnly: true);
				this.__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
				this.__Game_Buildings_TelecomFacility_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.TelecomFacility>(isReadOnly: true);
				this.__Game_Buildings_Efficiency_RO_BufferLookup = state.GetBufferLookup<Efficiency>(isReadOnly: true);
				this.__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
				this.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
				this.__Game_Prefabs_TelecomFacilityData_RO_ComponentLookup = state.GetComponentLookup<TelecomFacilityData>(isReadOnly: true);
				this.__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
			}
		}

		private TerrainSystem m_TerrainSystem;

		private CitySystem m_CitySystem;

		private EntityQuery m_DensityQuery;

		private EntityQuery m_FacilityQuery;

		private EntityQuery m_ModifiedQuery;

		private bool m_ForceUpdate;

		private NativeArray<TelecomStatus> m_Status;

		private TypeHandle __TypeHandle;

		public int2 TextureSize => new int2(128, 128);

		[Preserve]
		protected override void OnCreate()
		{
			base.OnCreate();
			this.m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
			this.m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
			this.m_DensityQuery = base.GetEntityQuery(new EntityQueryDesc
			{
				Any = new ComponentType[2]
				{
					ComponentType.ReadOnly<HouseholdCitizen>(),
					ComponentType.ReadOnly<Employee>()
				},
				None = new ComponentType[2]
				{
					ComponentType.ReadOnly<Temp>(),
					ComponentType.ReadOnly<Deleted>()
				}
			});
			this.m_FacilityQuery = base.GetEntityQuery(ComponentType.ReadOnly<Game.Buildings.TelecomFacility>(), ComponentType.ReadOnly<Transform>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Game.Buildings.ServiceUpgrade>(), ComponentType.Exclude<Hidden>(), ComponentType.Exclude<Deleted>());
			this.m_ModifiedQuery = base.GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[3]
				{
					ComponentType.ReadOnly<Game.Buildings.TelecomFacility>(),
					ComponentType.ReadOnly<Transform>(),
					ComponentType.ReadOnly<PrefabRef>()
				},
				Any = new ComponentType[2]
				{
					ComponentType.ReadOnly<Updated>(),
					ComponentType.ReadOnly<Deleted>()
				}
			});
			this.m_Status = new NativeArray<TelecomStatus>(0, Allocator.Persistent);
			base.CreateTextures(128);
		}

		[Preserve]
		protected override void OnDestroy()
		{
			this.m_Status.Dispose();
			base.OnDestroy();
		}

		[Preserve]
		protected override void OnStartRunning()
		{
			base.OnStartRunning();
			this.m_ForceUpdate = true;
		}

		[Preserve]
		protected override void OnUpdate()
		{
			if (!this.m_ModifiedQuery.IsEmptyIgnoreFilter || this.m_ForceUpdate)
			{
				this.m_ForceUpdate = false;
				JobHandle outJobHandle;
				NativeList<ArchetypeChunk> densityChunks = this.m_DensityQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
				JobHandle outJobHandle2;
				NativeList<ArchetypeChunk> facilityChunks = this.m_FacilityQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle2);
				this.__TypeHandle.__Game_City_CityModifier_RO_BufferLookup.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Prefabs_TelecomFacilityData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Buildings_Efficiency_RO_BufferLookup.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Buildings_TelecomFacility_RO_ComponentLookup.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Companies_Employee_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Buildings_Efficiency_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Buildings_TelecomFacility_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
				TelecomCoverageSystem.TelecomCoverageJob jobData = default(TelecomCoverageSystem.TelecomCoverageJob);
				jobData.m_DensityChunks = densityChunks;
				jobData.m_FacilityChunks = facilityChunks;
				jobData.m_TerrainHeightData = this.m_TerrainSystem.GetHeightData();
				jobData.m_City = this.m_CitySystem.City;
				jobData.m_Preview = true;
				jobData.m_TelecomCoverage = base.GetMap(readOnly: false, out var dependencies);
				jobData.m_TelecomStatus = this.m_Status;
				jobData.m_TransformType = this.__TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle;
				jobData.m_PropertyRenterType = this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle;
				jobData.m_TelecomFacilityType = this.__TypeHandle.__Game_Buildings_TelecomFacility_RO_ComponentTypeHandle;
				jobData.m_BuildingEfficiencyType = this.__TypeHandle.__Game_Buildings_Efficiency_RO_BufferTypeHandle;
				jobData.m_PrefabRefType = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;
				jobData.m_TempType = this.__TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle;
				jobData.m_InstalledUpgradeType = this.__TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;
				jobData.m_HouseholdCitizenType = this.__TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferTypeHandle;
				jobData.m_EmployeeType = this.__TypeHandle.__Game_Companies_Employee_RO_BufferTypeHandle;
				jobData.m_TransformData = this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup;
				jobData.m_TelecomFacilityData = this.__TypeHandle.__Game_Buildings_TelecomFacility_RO_ComponentLookup;
				jobData.m_BuildingEfficiencyData = this.__TypeHandle.__Game_Buildings_Efficiency_RO_BufferLookup;
				jobData.m_PrefabRefData = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup;
				jobData.m_ObjectGeometryData = this.__TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;
				jobData.m_PrefabTelecomFacilityData = this.__TypeHandle.__Game_Prefabs_TelecomFacilityData_RO_ComponentLookup;
				jobData.m_CityModifiers = this.__TypeHandle.__Game_City_CityModifier_RO_BufferLookup;
				JobHandle jobHandle = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(job1: JobHandle.CombineDependencies(outJobHandle, outJobHandle2, dependencies), job0: base.Dependency));
				densityChunks.Dispose(jobHandle);
				facilityChunks.Dispose(jobHandle);
				this.m_TerrainSystem.AddCPUHeightReader(jobHandle);
				base.AddWriter(jobHandle);
				base.Dependency = jobHandle;
			}
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
		public TelecomPreviewSystem()
		{
		}
	}
}

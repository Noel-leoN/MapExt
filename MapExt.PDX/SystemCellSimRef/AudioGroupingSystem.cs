using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Game.Common;
using Game.Effects;
using Game.Events;
using Game.Objects;
using Game.Prefabs;
using Game.SceneFlow;
using Game.Simulation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;
using Game;
using Game.Audio;

namespace MapExt.Systems
{
	/// <summary>
	/// bc job 未调用mapsize；可不替换
	/// </summary>
	//[CompilerGenerated]
	public partial class AudioGroupingSystem : GameSystemBase
	{
		[BurstCompile]
		private struct AudioGroupingJob : IJob
		{
			public ComponentLookup<EffectInstance> m_EffectInstances;

			[ReadOnly]
			public ComponentLookup<EffectData> m_EffectDatas;

			[ReadOnly]
			public ComponentLookup<PrefabRef> m_PrefabRefs;

			[ReadOnly]
			public ComponentLookup<Game.Objects.Transform> m_TransformData;

			[ReadOnly]
			public NativeArray<TrafficAmbienceCell> m_TrafficAmbienceMap;

			[ReadOnly]
			public NativeArray<ZoneAmbienceCell> m_AmbienceMap;

			[ReadOnly]
			public NativeArray<AudioGroupingSettingsData> m_Settings;

			public SourceUpdateData m_SourceUpdateData;

			public EffectFlagSystem.EffectFlagData m_EffectFlagData;

			public float3 m_CameraPosition;

			public NativeArray<Entity> m_AmbienceEntities;

			public NativeArray<Entity> m_NearAmbienceEntities;

			[DeallocateOnJobCompletion]
			public NativeArray<Entity> m_OnFireTrees;

			[ReadOnly]
			public TerrainHeightData m_TerrainData;

			[ReadOnly]
			public float m_ForestFireDistance;

			[ReadOnly]
			public float m_Precipitation;

			[ReadOnly]
			public bool m_IsRaining;

			public void Execute()
			{
				float3 cameraPosition = this.m_CameraPosition;
				float num = TerrainUtils.SampleHeight(ref this.m_TerrainData, this.m_CameraPosition);
				this.m_CameraPosition.y -= num;
				for (int i = 0; i < this.m_AmbienceEntities.Length; i++)
				{
					Entity entity = this.m_AmbienceEntities[i];
					Entity entity2 = this.m_NearAmbienceEntities[i];
					AudioGroupingSettingsData audioGroupingSettingsData = this.m_Settings[i];
					if (!this.m_EffectInstances.HasComponent(entity))
					{
						continue;
					}
					float num2 = 0f;
					float num3 = 0f;
					if (audioGroupingSettingsData.m_Type == GroupAmbienceType.Traffic)
					{
						num2 = TrafficAmbienceSystem.GetTrafficAmbience2(this.m_CameraPosition, this.m_TrafficAmbienceMap, 1f / audioGroupingSettingsData.m_Scale).m_Traffic;
					}
					else if (audioGroupingSettingsData.m_Type == GroupAmbienceType.Forest)
					{
						bool flag = false;
						for (int j = 0; j < this.m_OnFireTrees.Length; j++)
						{
							Entity entity3 = this.m_OnFireTrees[j];
							if (this.m_TransformData.HasComponent(entity3) && math.distancesq(this.m_TransformData[entity3].m_Position, cameraPosition) < this.m_ForestFireDistance * this.m_ForestFireDistance)
							{
								flag = true;
								break;
							}
						}
						if (!flag)
						{
							num2 = ZoneAmbienceSystem.GetZoneAmbience(GroupAmbienceType.Forest, this.m_CameraPosition, this.m_AmbienceMap, 1f / audioGroupingSettingsData.m_Scale);
							if (entity2 != Entity.Null)
							{
								num3 = ZoneAmbienceSystem.GetZoneAmbienceNear(GroupAmbienceType.Forest, this.m_CameraPosition, this.m_AmbienceMap, this.m_Settings[i].m_NearWeight, 1f / audioGroupingSettingsData.m_Scale);
							}
						}
					}
					else if (audioGroupingSettingsData.m_Type == GroupAmbienceType.Rain)
					{
						if (this.m_IsRaining)
						{
							num2 = math.min(1f / audioGroupingSettingsData.m_Scale, math.max(0f, this.m_Precipitation) * 2f);
							num3 = num2;
						}
					}
					else
					{
						num2 = ZoneAmbienceSystem.GetZoneAmbience(audioGroupingSettingsData.m_Type, this.m_CameraPosition, this.m_AmbienceMap, 1f / audioGroupingSettingsData.m_Scale);
						if (entity2 != Entity.Null)
						{
							num3 = ZoneAmbienceSystem.GetZoneAmbienceNear(audioGroupingSettingsData.m_Type, this.m_CameraPosition, this.m_AmbienceMap, this.m_Settings[i].m_NearWeight, 1f / audioGroupingSettingsData.m_Scale);
						}
					}
					bool flag2 = true;
					Entity prefab = this.m_PrefabRefs[entity].m_Prefab;
					bool flag3 = (this.m_EffectDatas[prefab].m_Flags.m_RequiredFlags & EffectConditionFlags.Cold) != 0;
					bool flag4 = (this.m_EffectDatas[prefab].m_Flags.m_ForbiddenFlags & EffectConditionFlags.Cold) != 0;
					if (flag3 || flag4)
					{
						bool isColdSeason = this.m_EffectFlagData.m_IsColdSeason;
						flag2 = (flag3 && isColdSeason) || (flag4 && !isColdSeason);
					}
					if (num2 > 0.001f && flag2)
					{
						EffectInstance value = this.m_EffectInstances[entity];
						float num4 = math.saturate(audioGroupingSettingsData.m_Scale * num2);
						num4 *= math.saturate((audioGroupingSettingsData.m_Height.y - this.m_CameraPosition.y) / (audioGroupingSettingsData.m_Height.y - audioGroupingSettingsData.m_Height.x));
						num4 = math.lerp(value.m_Intensity, num4, audioGroupingSettingsData.m_FadeSpeed);
						value.m_Position = cameraPosition;
						value.m_Rotation = quaternion.identity;
						value.m_Intensity = math.saturate(num4);
						this.m_EffectInstances[entity] = value;
						this.m_SourceUpdateData.Add(entity, new Game.Objects.Transform
						{
							m_Position = cameraPosition,
							m_Rotation = quaternion.identity
						});
					}
					else
					{
						this.m_SourceUpdateData.Remove(entity);
					}
					flag2 = true;
					if (entity2 != Entity.Null)
					{
						prefab = this.m_PrefabRefs[entity2].m_Prefab;
						flag3 = (this.m_EffectDatas[prefab].m_Flags.m_RequiredFlags & EffectConditionFlags.Cold) != 0;
						flag4 = (this.m_EffectDatas[prefab].m_Flags.m_ForbiddenFlags & EffectConditionFlags.Cold) != 0;
						if (flag3 || flag4)
						{
							bool isColdSeason2 = this.m_EffectFlagData.m_IsColdSeason;
							flag2 = (flag3 && isColdSeason2) || (flag4 && !isColdSeason2);
						}
					}
					if (num3 > 0.001f && flag2)
					{
						EffectInstance value2 = this.m_EffectInstances[entity2];
						float num5 = math.saturate(audioGroupingSettingsData.m_Scale * num3);
						num5 *= math.saturate((audioGroupingSettingsData.m_NearHeight.y - this.m_CameraPosition.y) / (audioGroupingSettingsData.m_NearHeight.y - audioGroupingSettingsData.m_NearHeight.x));
						num5 = math.lerp(value2.m_Intensity, num5, audioGroupingSettingsData.m_FadeSpeed);
						value2.m_Position = cameraPosition;
						value2.m_Rotation = quaternion.identity;
						value2.m_Intensity = math.saturate(num5);
						this.m_EffectInstances[entity2] = value2;
						this.m_SourceUpdateData.Add(entity2, new Game.Objects.Transform
						{
							m_Position = cameraPosition,
							m_Rotation = quaternion.identity
						});
					}
					else
					{
						this.m_SourceUpdateData.Remove(entity2);
					}
				}
			}
		}

		private struct TypeHandle
		{
			public ComponentLookup<EffectInstance> __Game_Effects_EffectInstance_RW_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<EffectData> __Game_Prefabs_EffectData_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void __AssignHandles(ref SystemState state)
			{
				this.__Game_Effects_EffectInstance_RW_ComponentLookup = state.GetComponentLookup<EffectInstance>();
				this.__Game_Prefabs_EffectData_RO_ComponentLookup = state.GetComponentLookup<EffectData>(isReadOnly: true);
				this.__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
				this.__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			}
		}

		private TrafficAmbienceSystem m_TrafficAmbienceSystem;

		private ZoneAmbienceSystem m_ZoneAmbienceSystem;

		private EffectFlagSystem m_EffectFlagSystem;

		private SimulationSystem m_SimulationSystem;

		private ClimateSystem m_ClimateSystem;

		private AudioManager m_AudioManager;

		private EntityQuery m_AudioGroupingConfigurationQuery;

		private EntityQuery m_AudioGroupingMiscSettingQuery;

		private NativeArray<Entity> m_AmbienceEntities;

		private NativeArray<Entity> m_NearAmbienceEntities;

		private NativeArray<AudioGroupingSettingsData> m_Settings;

		private TerrainSystem m_TerrainSystem;

		private EntityQuery m_OnFireTreeQuery;

		private TypeHandle __TypeHandle;

		[Preserve]
		protected override void OnCreate()
		{
			base.OnCreate();
			this.m_AudioManager = base.World.GetOrCreateSystemManaged<AudioManager>();
			this.m_TrafficAmbienceSystem = base.World.GetOrCreateSystemManaged<TrafficAmbienceSystem>();
			this.m_ZoneAmbienceSystem = base.World.GetOrCreateSystemManaged<ZoneAmbienceSystem>();
			this.m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
			this.m_EffectFlagSystem = base.World.GetOrCreateSystemManaged<EffectFlagSystem>();
			this.m_ClimateSystem = base.World.GetOrCreateSystemManaged<ClimateSystem>();
			this.m_AudioGroupingConfigurationQuery = base.GetEntityQuery(ComponentType.ReadOnly<AudioGroupingSettingsData>());
			this.m_AudioGroupingMiscSettingQuery = base.GetEntityQuery(ComponentType.ReadOnly<AudioGroupingMiscSetting>());
			this.m_OnFireTreeQuery = base.GetEntityQuery(ComponentType.ReadOnly<Game.Objects.Tree>(), ComponentType.ReadOnly<OnFire>(), ComponentType.Exclude<Deleted>());
			base.RequireForUpdate(this.m_AudioGroupingConfigurationQuery);
		}

		private Entity CreateEffect(Entity sfx)
		{
			Entity entity = base.EntityManager.CreateEntity();
			base.EntityManager.AddComponentData(entity, default(EffectInstance));
			base.EntityManager.AddComponentData(entity, new PrefabRef
			{
				m_Prefab = sfx
			});
			return entity;
		}

		private void Initialize()
		{
			NativeArray<Entity> nativeArray = this.m_AudioGroupingConfigurationQuery.ToEntityArray(Allocator.Temp);
			List<AudioGroupingSettingsData> list = new List<AudioGroupingSettingsData>();
			foreach (Entity item in nativeArray)
			{
				list.AddRange(base.World.EntityManager.GetBuffer<AudioGroupingSettingsData>(item, isReadOnly: true).AsNativeArray());
			}
			if (!this.m_Settings.IsCreated)
			{
				this.m_Settings = list.ToNativeArray(Allocator.Persistent);
			}
			nativeArray.Dispose();
			if (!this.m_AmbienceEntities.IsCreated)
			{
				this.m_AmbienceEntities = new NativeArray<Entity>(this.m_Settings.Length, Allocator.Persistent);
			}
			if (!this.m_NearAmbienceEntities.IsCreated)
			{
				this.m_NearAmbienceEntities = new NativeArray<Entity>(this.m_Settings.Length, Allocator.Persistent);
			}
			for (int i = 0; i < this.m_Settings.Length; i++)
			{
				this.m_AmbienceEntities[i] = this.CreateEffect(this.m_Settings[i].m_GroupSoundFar);
				this.m_NearAmbienceEntities[i] = ((this.m_Settings[i].m_GroupSoundNear != Entity.Null) ? this.CreateEffect(this.m_Settings[i].m_GroupSoundNear) : Entity.Null);
			}
		}

		[Preserve]
		protected override void OnDestroy()
		{
			if (this.m_AmbienceEntities.IsCreated)
			{
				this.m_AmbienceEntities.Dispose();
			}
			if (this.m_NearAmbienceEntities.IsCreated)
			{
				this.m_NearAmbienceEntities.Dispose();
			}
			if (this.m_Settings.IsCreated)
			{
				this.m_Settings.Dispose();
			}
			base.OnDestroy();
		}

		[Preserve]
		protected override void OnUpdate()
		{
			if (GameManager.instance.gameMode == GameMode.Game && !GameManager.instance.isGameLoading)
			{
				if (this.m_AmbienceEntities.Length == 0 || !base.EntityManager.HasComponent<EffectInstance>(this.m_AmbienceEntities[0]))
				{
					this.Initialize();
				}
				Camera main = Camera.main;
				if (!(main == null))
				{
					float3 cameraPosition = main.transform.position;
					AudioGroupingMiscSetting singleton = this.m_AudioGroupingMiscSettingQuery.GetSingleton<AudioGroupingMiscSetting>();
					this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup.Update(ref base.CheckedStateRef);
					this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref base.CheckedStateRef);
					this.__TypeHandle.__Game_Prefabs_EffectData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
					this.__TypeHandle.__Game_Effects_EffectInstance_RW_ComponentLookup.Update(ref base.CheckedStateRef);
					AudioGroupingJob audioGroupingJob = default(AudioGroupingJob);
					audioGroupingJob.m_CameraPosition = cameraPosition;
					audioGroupingJob.m_SourceUpdateData = this.m_AudioManager.GetSourceUpdateData(out var deps);
					audioGroupingJob.m_TrafficAmbienceMap = this.m_TrafficAmbienceSystem.GetMap(readOnly: true, out var dependencies);
					audioGroupingJob.m_AmbienceMap = this.m_ZoneAmbienceSystem.GetMap(readOnly: true, out var dependencies2);
					audioGroupingJob.m_Settings = this.m_Settings;
					audioGroupingJob.m_EffectFlagData = this.m_EffectFlagSystem.GetData();
					audioGroupingJob.m_AmbienceEntities = this.m_AmbienceEntities;
					audioGroupingJob.m_NearAmbienceEntities = this.m_NearAmbienceEntities;
					audioGroupingJob.m_OnFireTrees = this.m_OnFireTreeQuery.ToEntityArray(Allocator.TempJob);
					audioGroupingJob.m_EffectInstances = this.__TypeHandle.__Game_Effects_EffectInstance_RW_ComponentLookup;
					audioGroupingJob.m_EffectDatas = this.__TypeHandle.__Game_Prefabs_EffectData_RO_ComponentLookup;
					audioGroupingJob.m_PrefabRefs = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup;
					audioGroupingJob.m_TransformData = this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup;
					audioGroupingJob.m_TerrainData = this.m_TerrainSystem.GetHeightData();
					audioGroupingJob.m_ForestFireDistance = singleton.m_ForestFireDistance;
					audioGroupingJob.m_Precipitation = this.m_ClimateSystem.precipitation;
					audioGroupingJob.m_IsRaining = this.m_ClimateSystem.isRaining;
					AudioGroupingJob jobData = audioGroupingJob;
					base.Dependency = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(JobHandle.CombineDependencies(dependencies2, deps), dependencies, base.Dependency));
					this.m_TerrainSystem.AddCPUHeightReader(base.Dependency);
					this.m_AudioManager.AddSourceUpdateWriter(base.Dependency);
					this.m_TrafficAmbienceSystem.AddReader(base.Dependency);
				}
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
		public AudioGroupingSystem()
		{
		}
	}
}

using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Game.Effects;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.Simulation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;
using Game;
using Game.Audio;

///Game.Audio;
///WaterSystem.kMapSize ref to BC;

namespace MapExt.Systems
{
	//[CompilerGenerated]
	public partial class WeatherAudioSystem : GameSystemBase
	{
		[BurstCompile]
		private struct WeatherAudioJob : IJob
		{
			public ComponentLookup<EffectInstance> m_EffectInstances;

			public SourceUpdateData m_SourceUpdateData;

			[ReadOnly]
			public int2 m_WaterTextureSize;

			[ReadOnly]
			public float3 m_CameraPosition;

			[ReadOnly]
			public int m_WaterAudioNearDistance;

			[ReadOnly]
			public Entity m_WaterAudioEntity;

			[ReadOnly]
			public WeatherAudioData m_WeatherAudioData;

			[ReadOnly]
			public NativeArray<SurfaceWater> m_WaterDepths;

			[ReadOnly]
			public TerrainHeightData m_TerrainData;

			public void Execute()
			{
				if (WeatherAudioJob.NearWater(this.m_CameraPosition, this.m_WaterTextureSize, this.m_WaterAudioNearDistance, ref this.m_WaterDepths))
				{
					EffectInstance value = this.m_EffectInstances[this.m_WaterAudioEntity];
					float y = TerrainUtils.SampleHeight(ref this.m_TerrainData, this.m_CameraPosition);
					float x = math.lerp(value.m_Intensity, this.m_WeatherAudioData.m_WaterAudioIntensity, this.m_WeatherAudioData.m_WaterFadeSpeed);
					value.m_Position = new float3(this.m_CameraPosition.x, y, this.m_CameraPosition.z);
					value.m_Rotation = quaternion.identity;
					value.m_Intensity = math.saturate(x);
					this.m_EffectInstances[this.m_WaterAudioEntity] = value;
					this.m_SourceUpdateData.Add(this.m_WaterAudioEntity, new Transform
					{
						m_Position = this.m_CameraPosition,
						m_Rotation = quaternion.identity
					});
				}
				else if (this.m_EffectInstances.HasComponent(this.m_WaterAudioEntity))
				{
					EffectInstance value2 = this.m_EffectInstances[this.m_WaterAudioEntity];
					if (value2.m_Intensity <= 0.01f)
					{
						this.m_SourceUpdateData.Remove(this.m_WaterAudioEntity);
						return;
					}
					float x2 = math.lerp(value2.m_Intensity, 0f, this.m_WeatherAudioData.m_WaterFadeSpeed);
					value2.m_Intensity = math.saturate(x2);
					this.m_EffectInstances[this.m_WaterAudioEntity] = value2;
					this.m_SourceUpdateData.Add(this.m_WaterAudioEntity, new Transform
					{
						m_Position = this.m_CameraPosition,
						m_Rotation = quaternion.identity
					});
				}
			}

			private static bool NearWater(float3 position, int2 texSize, int distance, ref NativeArray<SurfaceWater> depthsCPU)
			{
				float2 @float = 57344 / (float2)texSize;
				int2 cell = WaterSystem.GetCell(position - new float3(@float.x / 2f, 0f, @float.y / 2f), 57344, texSize);
				int2 @int = default(int2);
				for (int i = -distance; i <= distance; i++)
				{
					for (int j = -distance; j <= distance; j++)
					{
						@int.x = math.clamp(cell.x + i, 0, texSize.x - 2);
						@int.y = math.clamp(cell.y + j, 0, texSize.y - 2);
						if (depthsCPU[@int.x + 1 + texSize.x * @int.y].m_Depth > 0f)
						{
							return true;
						}
					}
				}
				return false;
			}
		}

		private struct TypeHandle
		{
			public ComponentLookup<EffectInstance> __Game_Effects_EffectInstance_RW_ComponentLookup;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void __AssignHandles(ref SystemState state)
			{
				this.__Game_Effects_EffectInstance_RW_ComponentLookup = state.GetComponentLookup<EffectInstance>();
			}
		}

		private AudioManager m_AudioManager;

		private TerrainSystem m_TerrainSystem;

		private WaterSystem m_WaterSystem;

		private CameraUpdateSystem m_CameraUpdateSystem;

		private EntityQuery m_WeatherAudioEntityQuery;

		private Entity m_SmallWaterAudioEntity;

		private int m_WaterAudioEnabledZoom;

		private int m_WaterAudioNearDistance;

		private TypeHandle __TypeHandle;

		public override int GetUpdateInterval(SystemUpdatePhase phase)
		{
			return 16;
		}

		[Preserve]
		protected override void OnCreate()
		{
			base.OnCreate();
			this.m_AudioManager = base.World.GetOrCreateSystemManaged<AudioManager>();
			this.m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
			this.m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
			this.m_CameraUpdateSystem = base.World.GetOrCreateSystemManaged<CameraUpdateSystem>();
			this.m_WeatherAudioEntityQuery = base.GetEntityQuery(ComponentType.ReadOnly<WeatherAudioData>());
			base.RequireForUpdate(this.m_WeatherAudioEntityQuery);
		}

		protected override void OnGameLoaded(Context serializationContext)
		{
			this.m_SmallWaterAudioEntity = Entity.Null;
		}

		private void Initialize()
		{
			WeatherAudioData componentData = base.EntityManager.GetComponentData<WeatherAudioData>(this.m_WeatherAudioEntityQuery.GetSingletonEntity());
			Entity entity = base.EntityManager.CreateEntity();
			base.EntityManager.AddComponentData(entity, default(EffectInstance));
			base.EntityManager.AddComponentData(entity, new PrefabRef
			{
				m_Prefab = componentData.m_SmallWaterAudio
			});
			this.m_SmallWaterAudioEntity = entity;
			this.m_WaterAudioEnabledZoom = componentData.m_WaterAudioEnabledZoom;
			this.m_WaterAudioNearDistance = componentData.m_WaterAudioNearDistance;
		}

		[Preserve]
		protected override void OnUpdate()
		{
			if (this.m_WaterSystem.Loaded && this.m_CameraUpdateSystem.activeViewer != null && this.m_CameraUpdateSystem.activeCameraController != null)
			{
				if (this.m_SmallWaterAudioEntity == Entity.Null)
				{
					this.Initialize();
				}
				IGameCameraController activeCameraController = this.m_CameraUpdateSystem.activeCameraController;
				float3 position = this.m_CameraUpdateSystem.activeViewer.position;
				if (base.EntityManager.HasComponent<EffectInstance>(this.m_SmallWaterAudioEntity) && activeCameraController.zoom < (float)this.m_WaterAudioEnabledZoom)
				{
					this.__TypeHandle.__Game_Effects_EffectInstance_RW_ComponentLookup.Update(ref base.CheckedStateRef);
					WeatherAudioJob weatherAudioJob = default(WeatherAudioJob);
					weatherAudioJob.m_WaterTextureSize = this.m_WaterSystem.TextureSize;
					weatherAudioJob.m_WaterAudioNearDistance = this.m_WaterAudioNearDistance;
					weatherAudioJob.m_CameraPosition = position;
					weatherAudioJob.m_WaterAudioEntity = this.m_SmallWaterAudioEntity;
					weatherAudioJob.m_WeatherAudioData = base.EntityManager.GetComponentData<WeatherAudioData>(this.m_WeatherAudioEntityQuery.GetSingletonEntity());
					weatherAudioJob.m_SourceUpdateData = this.m_AudioManager.GetSourceUpdateData(out var deps);
					weatherAudioJob.m_TerrainData = this.m_TerrainSystem.GetHeightData();
					weatherAudioJob.m_EffectInstances = this.__TypeHandle.__Game_Effects_EffectInstance_RW_ComponentLookup;
					weatherAudioJob.m_WaterDepths = this.m_WaterSystem.GetDepths(out var deps2);
					WeatherAudioJob jobData = weatherAudioJob;
					base.Dependency = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(deps, deps2, base.Dependency));
					this.m_TerrainSystem.AddCPUHeightReader(base.Dependency);
					this.m_AudioManager.AddSourceUpdateWriter(base.Dependency);
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
		public WeatherAudioSystem()
		{
		}
	}
}

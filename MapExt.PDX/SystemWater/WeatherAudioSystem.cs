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
using MapExt.Systems;

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
                if (NearWater(m_CameraPosition, m_WaterTextureSize, m_WaterAudioNearDistance, ref m_WaterDepths))
                {
                    EffectInstance value = m_EffectInstances[m_WaterAudioEntity];
                    float y = TerrainUtils.SampleHeight(ref m_TerrainData, m_CameraPosition);
                    float x = math.lerp(value.m_Intensity, m_WeatherAudioData.m_WaterAudioIntensity, m_WeatherAudioData.m_WaterFadeSpeed);
                    value.m_Position = new float3(m_CameraPosition.x, y, m_CameraPosition.z);
                    value.m_Rotation = quaternion.identity;
                    value.m_Intensity = math.saturate(x);
                    m_EffectInstances[m_WaterAudioEntity] = value;
                    m_SourceUpdateData.Add(m_WaterAudioEntity, new Transform
                    {
                        m_Position = m_CameraPosition,
                        m_Rotation = quaternion.identity
                    });
                }
                else if (m_EffectInstances.HasComponent(m_WaterAudioEntity))
                {
                    EffectInstance value2 = m_EffectInstances[m_WaterAudioEntity];
                    if (value2.m_Intensity <= 0.01f)
                    {
                        m_SourceUpdateData.Remove(m_WaterAudioEntity);
                        return;
                    }

                    float x2 = math.lerp(value2.m_Intensity, 0f, m_WeatherAudioData.m_WaterFadeSpeed);
                    value2.m_Intensity = math.saturate(x2);
                    m_EffectInstances[m_WaterAudioEntity] = value2;
                    m_SourceUpdateData.Add(m_WaterAudioEntity, new Transform
                    {
                        m_Position = m_CameraPosition,
                        m_Rotation = quaternion.identity
                    });
                }
            }

            private static bool NearWater(float3 position, int2 texSize, int distance, ref NativeArray<SurfaceWater> depthsCPU)
            {
                float2 @float = CellMapSystemRe.kMapSize / (float2)texSize;
                int2 cell = WaterSystem.GetCell(position - new float3(@float.x / 2f, 0f, @float.y / 2f), CellMapSystemRe.kMapSize, texSize);
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
                __Game_Effects_EffectInstance_RW_ComponentLookup = state.GetComponentLookup<EffectInstance>();
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
            m_AudioManager = base.World.GetOrCreateSystemManaged<AudioManager>();
            m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
            m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
            m_CameraUpdateSystem = base.World.GetOrCreateSystemManaged<CameraUpdateSystem>();
            m_WeatherAudioEntityQuery = GetEntityQuery(ComponentType.ReadOnly<WeatherAudioData>());
            RequireForUpdate(m_WeatherAudioEntityQuery);
        }

        protected override void OnGameLoaded(Context serializationContext)
        {
            m_SmallWaterAudioEntity = Entity.Null;
        }

        private void Initialize()
        {
            WeatherAudioData componentData = base.EntityManager.GetComponentData<WeatherAudioData>(m_WeatherAudioEntityQuery.GetSingletonEntity());
            Entity entity = base.EntityManager.CreateEntity();
            base.EntityManager.AddComponentData(entity, default(EffectInstance));
            base.EntityManager.AddComponentData(entity, new PrefabRef
            {
                m_Prefab = componentData.m_SmallWaterAudio
            });
            m_SmallWaterAudioEntity = entity;
            m_WaterAudioEnabledZoom = componentData.m_WaterAudioEnabledZoom;
            m_WaterAudioNearDistance = componentData.m_WaterAudioNearDistance;
        }

        [Preserve]
        protected override void OnUpdate()
        {
            if (m_WaterSystem.Loaded && m_CameraUpdateSystem.activeViewer != null && m_CameraUpdateSystem.activeCameraController != null)
            {
                if (m_SmallWaterAudioEntity == Entity.Null)
                {
                    Initialize();
                }

                IGameCameraController activeCameraController = m_CameraUpdateSystem.activeCameraController;
                float3 position = m_CameraUpdateSystem.activeViewer.position;
                if (base.EntityManager.HasComponent<EffectInstance>(m_SmallWaterAudioEntity) && activeCameraController.zoom < (float)m_WaterAudioEnabledZoom)
                {
                    __TypeHandle.__Game_Effects_EffectInstance_RW_ComponentLookup.Update(ref base.CheckedStateRef);
                    WeatherAudioJob weatherAudioJob = default(WeatherAudioJob);
                    weatherAudioJob.m_WaterTextureSize = m_WaterSystem.TextureSize;
                    weatherAudioJob.m_WaterAudioNearDistance = m_WaterAudioNearDistance;
                    weatherAudioJob.m_CameraPosition = position;
                    weatherAudioJob.m_WaterAudioEntity = m_SmallWaterAudioEntity;
                    weatherAudioJob.m_WeatherAudioData = base.EntityManager.GetComponentData<WeatherAudioData>(m_WeatherAudioEntityQuery.GetSingletonEntity());
                    weatherAudioJob.m_SourceUpdateData = m_AudioManager.GetSourceUpdateData(out var deps);
                    weatherAudioJob.m_TerrainData = m_TerrainSystem.GetHeightData();
                    weatherAudioJob.m_EffectInstances = __TypeHandle.__Game_Effects_EffectInstance_RW_ComponentLookup;
                    weatherAudioJob.m_WaterDepths = m_WaterSystem.GetDepths(out var deps2);
                    WeatherAudioJob jobData = weatherAudioJob;
                    base.Dependency = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(deps, deps2, base.Dependency));
                    m_TerrainSystem.AddCPUHeightReader(base.Dependency);
                    m_AudioManager.AddSourceUpdateWriter(base.Dependency);
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
            __AssignQueries(ref base.CheckedStateRef);
            __TypeHandle.__AssignHandles(ref base.CheckedStateRef);
        }

        [Preserve]
        public WeatherAudioSystem()
        {
        }
    }
}

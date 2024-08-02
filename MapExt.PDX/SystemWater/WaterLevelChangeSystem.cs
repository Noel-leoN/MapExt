using System;
using System.Runtime.CompilerServices;
using Game.Common;
using Game.Events;
using Game.Prefabs;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;
using Game;
using Game.Simulation;

///Game.Simulation
namespace MapExt.Systems
{
    //[CompilerGenerated]
    public partial class WaterLevelChangeSystem : GameSystemBase
    {
        [BurstCompile]
        private struct WaterLevelChangeJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle m_EntityType;

            [ReadOnly]
            public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

            public ComponentTypeHandle<WaterLevelChange> m_WaterLevelChangeType;

            [ReadOnly]
            public ComponentTypeHandle<Duration> m_DurationType;

            [ReadOnly]
            public ComponentLookup<WaterLevelChangeData> m_PrefabWaterLevelChangeData;

            [ReadOnly]
            public uint m_SimulationFrame;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
                NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
                NativeArray<WaterLevelChange> nativeArray3 = chunk.GetNativeArray(ref m_WaterLevelChangeType);
                NativeArray<Duration> nativeArray4 = chunk.GetNativeArray(ref m_DurationType);
                for (int i = 0; i < nativeArray3.Length; i++)
                {
                    _ = nativeArray[i];
                    PrefabRef prefabRef = nativeArray2[i];
                    WaterLevelChange value = nativeArray3[i];
                    Duration duration = nativeArray4[i];
                    WaterLevelChangeData waterLevelChangeData = m_PrefabWaterLevelChangeData[prefabRef.m_Prefab];
                    float num = (float)(m_SimulationFrame - duration.m_StartFrame) / 60f - waterLevelChangeData.m_EscalationDelay;
                    if (num < 0f)
                    {
                        continue;
                    }

                    if (waterLevelChangeData.m_ChangeType == WaterLevelChangeType.Sine)
                    {
                        float num2 = (float)(duration.m_EndFrame - TsunamiEndDelay - duration.m_StartFrame) / 60f;
                        if (num < 0.05f * num2)
                        {
                            value.m_Intensity = -0.2f * value.m_MaxIntensity * math.sin(20f * num / num2 * (float)Math.PI);
                        }
                        else if (num < num2)
                        {
                            value.m_Intensity = value.m_MaxIntensity * (0.5f * math.sin(5f * (num - 0.05f * num2) / (0.95f * num2) * 2f * (float)Math.PI) + 0.5f * math.saturate((num - 0.05f * num2) / (0.2f * num2)));
                        }
                        else
                        {
                            value.m_Intensity = 0f;
                        }

                        value.m_Intensity *= 4f;
                    }
                    else
                    {
                        _ = waterLevelChangeData.m_ChangeType;
                        _ = 2;
                    }

                    nativeArray3[i] = value;
                }
            }

            void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
            }
        }

        private struct TypeHandle
        {
            [ReadOnly]
            public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

            [ReadOnly]
            public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

            public ComponentTypeHandle<WaterLevelChange> __Game_Events_WaterLevelChange_RW_ComponentTypeHandle;

            [ReadOnly]
            public ComponentTypeHandle<Duration> __Game_Events_Duration_RO_ComponentTypeHandle;

            [ReadOnly]
            public ComponentLookup<WaterLevelChangeData> __Game_Prefabs_WaterLevelChangeData_RO_ComponentLookup;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                __Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
                __Game_Events_WaterLevelChange_RW_ComponentTypeHandle = state.GetComponentTypeHandle<WaterLevelChange>();
                __Game_Events_Duration_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Duration>(isReadOnly: true);
                __Game_Prefabs_WaterLevelChangeData_RO_ComponentLookup = state.GetComponentLookup<WaterLevelChangeData>(isReadOnly: true);
            }
        }

        public static readonly int kUpdateInterval = 4;

        private SimulationSystem m_SimulationSystem;

        private EndFrameBarrier m_EndFrameBarrier;

        private EntityQuery m_WaterLevelChangeQuery;

        private TypeHandle __TypeHandle;

        public static int TsunamiEndDelay => Mathf.RoundToInt(CellMapSystemRe.kMapSize / WaterSystem.WaveSpeed);

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return kUpdateInterval;
        }

        public static uint GetMinimumDelayAt(WaterLevelChange change, float3 position)
        {
            float2 @float = CellMapSystemRe.kMapSize / 2 * new float2(math.cos(0f - change.m_Direction.x), math.sin(0f - change.m_Direction.y));
            float2 float2 = new float2(change.m_Direction.y, 0f - change.m_Direction.x);
            float2 float3 = math.dot(float2, position.xz - @float) * float2;
            return (uint)Mathf.RoundToInt(math.length(position.xz - @float - float3) / WaterSystem.WaveSpeed);
        }

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
            m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
            m_WaterLevelChangeQuery = GetEntityQuery(ComponentType.ReadWrite<WaterLevelChange>(), ComponentType.Exclude<Deleted>());
            RequireForUpdate(m_WaterLevelChangeQuery);
        }

        [Preserve]
        protected override void OnUpdate()
        {
            __TypeHandle.__Game_Prefabs_WaterLevelChangeData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Events_Duration_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Events_WaterLevelChange_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            __TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
            WaterLevelChangeJob waterLevelChangeJob = default(WaterLevelChangeJob);
            waterLevelChangeJob.m_EntityType = __TypeHandle.__Unity_Entities_Entity_TypeHandle;
            waterLevelChangeJob.m_PrefabRefType = __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;
            waterLevelChangeJob.m_WaterLevelChangeType = __TypeHandle.__Game_Events_WaterLevelChange_RW_ComponentTypeHandle;
            waterLevelChangeJob.m_DurationType = __TypeHandle.__Game_Events_Duration_RO_ComponentTypeHandle;
            waterLevelChangeJob.m_PrefabWaterLevelChangeData = __TypeHandle.__Game_Prefabs_WaterLevelChangeData_RO_ComponentLookup;
            waterLevelChangeJob.m_SimulationFrame = m_SimulationSystem.frameIndex;
            WaterLevelChangeJob jobData = waterLevelChangeJob;
            base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_WaterLevelChangeQuery, base.Dependency);
            m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
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
        public WaterLevelChangeSystem()
        {
        }
    }
}

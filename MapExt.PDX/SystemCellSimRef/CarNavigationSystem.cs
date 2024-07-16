using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Creatures;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;
using Game;
using Game.Simulation;


//better diable original system;
namespace MapExt.Systems
{
	/// <summary>
	/// bcjobµ÷ÓÃcell!!!
	/// </summary>
    //[CompilerGenerated]
    public partial class CarNavigationSystem : GameSystemBase
    {
        //[CompilerGenerated]
        public partial class Actions : GameSystemBase
		{
			private struct TypeHandle
			{
				public ComponentLookup<LaneReservation> __Game_Net_LaneReservation_RW_ComponentLookup;

				[ReadOnly]
				public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

				[ReadOnly]
				public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

				[ReadOnly]
				public ComponentLookup<LaneDeteriorationData> __Game_Prefabs_LaneDeteriorationData_RO_ComponentLookup;

				public ComponentLookup<Game.Net.Pollution> __Game_Net_Pollution_RW_ComponentLookup;

				public ComponentLookup<LaneCondition> __Game_Net_LaneCondition_RW_ComponentLookup;

				public ComponentLookup<LaneFlow> __Game_Net_LaneFlow_RW_ComponentLookup;

				public ComponentLookup<LaneSignal> __Game_Net_LaneSignal_RW_ComponentLookup;

				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				public void __AssignHandles(ref SystemState state)
				{
					this.__Game_Net_LaneReservation_RW_ComponentLookup = state.GetComponentLookup<LaneReservation>();
					this.__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
					this.__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
					this.__Game_Prefabs_LaneDeteriorationData_RO_ComponentLookup = state.GetComponentLookup<LaneDeteriorationData>(isReadOnly: true);
					this.__Game_Net_Pollution_RW_ComponentLookup = state.GetComponentLookup<Game.Net.Pollution>();
					this.__Game_Net_LaneCondition_RW_ComponentLookup = state.GetComponentLookup<LaneCondition>();
					this.__Game_Net_LaneFlow_RW_ComponentLookup = state.GetComponentLookup<LaneFlow>();
					this.__Game_Net_LaneSignal_RW_ComponentLookup = state.GetComponentLookup<LaneSignal>();
				}
			}

			private TrafficAmbienceSystem m_TrafficAmbienceSystem;

			public LaneObjectUpdater m_LaneObjectUpdater;

			public NativeQueue<CarNavigationHelpers.LaneReservation> m_LaneReservationQueue;

			public NativeQueue<CarNavigationHelpers.LaneEffects> m_LaneEffectsQueue;

			public NativeQueue<CarNavigationHelpers.LaneSignal> m_LaneSignalQueue;

			public NativeQueue<TrafficAmbienceEffect> m_TrafficAmbienceQueue;

			public JobHandle m_Dependency;

			private TypeHandle __TypeHandle;

			[Preserve]
			protected override void OnCreate()
			{
				base.OnCreate();
				this.m_TrafficAmbienceSystem = base.World.GetOrCreateSystemManaged<TrafficAmbienceSystem>();
				this.m_LaneObjectUpdater = new LaneObjectUpdater(this);
			}

			[Preserve]
			protected override void OnUpdate()
			{
				JobHandle jobHandle = JobHandle.CombineDependencies(base.Dependency, this.m_Dependency);
				this.__TypeHandle.__Game_Net_LaneReservation_RW_ComponentLookup.Update(ref base.CheckedStateRef);
				UpdateLaneReservationsJob updateLaneReservationsJob = default(UpdateLaneReservationsJob);
				updateLaneReservationsJob.m_LaneReservationQueue = this.m_LaneReservationQueue;
				updateLaneReservationsJob.m_LaneReservationData = this.__TypeHandle.__Game_Net_LaneReservation_RW_ComponentLookup;
				UpdateLaneReservationsJob jobData = updateLaneReservationsJob;
				this.__TypeHandle.__Game_Net_LaneFlow_RW_ComponentLookup.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Net_LaneCondition_RW_ComponentLookup.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Net_Pollution_RW_ComponentLookup.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Prefabs_LaneDeteriorationData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Common_Owner_RO_ComponentLookup.Update(ref base.CheckedStateRef);
				ApplyLaneEffectsJob applyLaneEffectsJob = default(ApplyLaneEffectsJob);
				applyLaneEffectsJob.m_OwnerData = this.__TypeHandle.__Game_Common_Owner_RO_ComponentLookup;
				applyLaneEffectsJob.m_PrefabRefData = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup;
				applyLaneEffectsJob.m_LaneDeteriorationData = this.__TypeHandle.__Game_Prefabs_LaneDeteriorationData_RO_ComponentLookup;
				applyLaneEffectsJob.m_PollutionData = this.__TypeHandle.__Game_Net_Pollution_RW_ComponentLookup;
				applyLaneEffectsJob.m_LaneConditionData = this.__TypeHandle.__Game_Net_LaneCondition_RW_ComponentLookup;
				applyLaneEffectsJob.m_LaneFlowData = this.__TypeHandle.__Game_Net_LaneFlow_RW_ComponentLookup;
				applyLaneEffectsJob.m_LaneEffectsQueue = this.m_LaneEffectsQueue;
				ApplyLaneEffectsJob jobData2 = applyLaneEffectsJob;
				ApplyTrafficAmbienceJob applyTrafficAmbienceJob = default(ApplyTrafficAmbienceJob);
				applyTrafficAmbienceJob.m_EffectsQueue = this.m_TrafficAmbienceQueue;
				applyTrafficAmbienceJob.m_TrafficAmbienceMap = this.m_TrafficAmbienceSystem.GetMap(readOnly: false, out var dependencies);
				ApplyTrafficAmbienceJob jobData3 = applyTrafficAmbienceJob;
				this.__TypeHandle.__Game_Net_LaneSignal_RW_ComponentLookup.Update(ref base.CheckedStateRef);
				UpdateLaneSignalsJob updateLaneSignalsJob = default(UpdateLaneSignalsJob);
				updateLaneSignalsJob.m_LaneSignalQueue = this.m_LaneSignalQueue;
				updateLaneSignalsJob.m_LaneSignalData = this.__TypeHandle.__Game_Net_LaneSignal_RW_ComponentLookup;
				UpdateLaneSignalsJob jobData4 = updateLaneSignalsJob;
				JobHandle jobHandle2 = IJobExtensions.Schedule(jobData, jobHandle);
				JobHandle jobHandle3 = IJobExtensions.Schedule(jobData2, jobHandle);
				JobHandle jobHandle4 = IJobExtensions.Schedule(jobData3, JobHandle.CombineDependencies(dependencies, jobHandle));
				JobHandle jobHandle5 = IJobExtensions.Schedule(jobData4, jobHandle);
				this.m_LaneReservationQueue.Dispose(jobHandle2);
				this.m_LaneEffectsQueue.Dispose(jobHandle3);
				this.m_LaneSignalQueue.Dispose(jobHandle5);
				this.m_TrafficAmbienceQueue.Dispose(jobHandle4);
				this.m_TrafficAmbienceSystem.AddWriter(jobHandle4);
				JobHandle job = this.m_LaneObjectUpdater.Apply(this, jobHandle);
				base.Dependency = JobUtils.CombineDependencies(job, jobHandle2, jobHandle3, jobHandle5);
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
			public Actions()
			{
			}
		}

		[BurstCompile]
		private struct UpdateNavigationJob : IJobChunk
		{
			[ReadOnly]
			public EntityTypeHandle m_EntityType;

			[ReadOnly]
			public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;

			[ReadOnly]
			public ComponentTypeHandle<Moving> m_MovingType;

			[ReadOnly]
			public ComponentTypeHandle<Target> m_TargetType;

			[ReadOnly]
			public ComponentTypeHandle<Car> m_CarType;

			[ReadOnly]
			public ComponentTypeHandle<OutOfControl> m_OutOfControlType;

			[ReadOnly]
			public ComponentTypeHandle<PseudoRandomSeed> m_PseudoRandomSeedType;

			[ReadOnly]
			public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

			[ReadOnly]
			public BufferTypeHandle<LayoutElement> m_LayoutElementType;

			public ComponentTypeHandle<CarNavigation> m_NavigationType;

			public ComponentTypeHandle<CarCurrentLane> m_CurrentLaneType;

			public ComponentTypeHandle<PathOwner> m_PathOwnerType;

			public ComponentTypeHandle<Blocker> m_BlockerType;

			public ComponentTypeHandle<Odometer> m_OdometerType;

			public BufferTypeHandle<CarNavigationLane> m_NavigationLaneType;

			public BufferTypeHandle<PathElement> m_PathElementType;

			[ReadOnly]
			public EntityStorageInfoLookup m_EntityStorageInfoLookup;

			[ReadOnly]
			public ComponentLookup<Owner> m_OwnerData;

			[ReadOnly]
			public ComponentLookup<Unspawned> m_UnspawnedData;

			[ReadOnly]
			public ComponentLookup<Lane> m_LaneData;

			[ReadOnly]
			public ComponentLookup<Game.Net.CarLane> m_CarLaneData;

			[ReadOnly]
			public ComponentLookup<Game.Net.ParkingLane> m_ParkingLaneData;

			[ReadOnly]
			public ComponentLookup<Game.Net.ConnectionLane> m_ConnectionLaneData;

			[ReadOnly]
			public ComponentLookup<MasterLane> m_MasterLaneData;

			[ReadOnly]
			public ComponentLookup<SlaveLane> m_SlaveLaneData;

			[ReadOnly]
			public ComponentLookup<AreaLane> m_AreaLaneData;

			[ReadOnly]
			public ComponentLookup<Curve> m_CurveData;

			[ReadOnly]
			public ComponentLookup<LaneReservation> m_LaneReservationData;

			[ReadOnly]
			public ComponentLookup<LaneCondition> m_LaneConditionData;

			[ReadOnly]
			public ComponentLookup<LaneSignal> m_LaneSignalData;

			[ReadOnly]
			public ComponentLookup<Road> m_RoadData;

			[ReadOnly]
			public ComponentLookup<PropertyRenter> m_PropertyRenterData;

			[ReadOnly]
			public ComponentLookup<Game.Objects.Transform> m_TransformData;

			[ReadOnly]
			public ComponentLookup<Position> m_PositionData;

			[ReadOnly]
			public ComponentLookup<Moving> m_MovingData;

			[ReadOnly]
			public ComponentLookup<Car> m_CarData;

			[ReadOnly]
			public ComponentLookup<Train> m_TrainData;

			[ReadOnly]
			public ComponentLookup<Controller> m_ControllerData;

			[ReadOnly]
			public ComponentLookup<Vehicle> m_VehicleData;

			[ReadOnly]
			public ComponentLookup<Creature> m_CreatureData;

			[ReadOnly]
			public ComponentLookup<PrefabRef> m_PrefabRefData;

			[ReadOnly]
			public ComponentLookup<CarData> m_PrefabCarData;

			[ReadOnly]
			public ComponentLookup<TrainData> m_PrefabTrainData;

			[ReadOnly]
			public ComponentLookup<BuildingData> m_PrefabBuildingData;

			[ReadOnly]
			public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

			[ReadOnly]
			public ComponentLookup<VehicleSideEffectData> m_PrefabSideEffectData;

			[ReadOnly]
			public ComponentLookup<NetLaneData> m_PrefabLaneData;

			[ReadOnly]
			public ComponentLookup<CarLaneData> m_PrefabCarLaneData;

			[ReadOnly]
			public ComponentLookup<ParkingLaneData> m_PrefabParkingLaneData;

			[ReadOnly]
			public BufferLookup<Game.Net.SubLane> m_Lanes;

			[ReadOnly]
			public BufferLookup<LaneObject> m_LaneObjects;

			[ReadOnly]
			public BufferLookup<LaneOverlap> m_LaneOverlaps;

			[ReadOnly]
			public BufferLookup<Game.Areas.Node> m_AreaNodes;

			[ReadOnly]
			public BufferLookup<Triangle> m_AreaTriangles;

			[NativeDisableParallelForRestriction]
			public ComponentLookup<CarTrailerLane> m_TrailerLaneData;

			[NativeDisableParallelForRestriction]
			public BufferLookup<BlockedLane> m_BlockedLanes;

			[ReadOnly]
			public RandomSeed m_RandomSeed;

			[ReadOnly]
			public uint m_SimulationFrame;

			[ReadOnly]
			public bool m_LeftHandTraffic;

			[ReadOnly]
			public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

			[ReadOnly]
			public NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> m_AreaSearchTree;

			[ReadOnly]
			public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_StaticObjectSearchTree;

			[ReadOnly]
			public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_MovingObjectSearchTree;

			[ReadOnly]
			public TerrainHeightData m_TerrainHeightData;

			public LaneObjectCommandBuffer m_LaneObjectBuffer;

			public NativeQueue<CarNavigationHelpers.LaneReservation>.ParallelWriter m_LaneReservations;

			public NativeQueue<CarNavigationHelpers.LaneEffects>.ParallelWriter m_LaneEffects;

			public NativeQueue<CarNavigationHelpers.LaneSignal>.ParallelWriter m_LaneSignals;

			public NativeQueue<TrafficAmbienceEffect>.ParallelWriter m_TrafficAmbienceEffects;

			public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				NativeArray<Entity> nativeArray = chunk.GetNativeArray(this.m_EntityType);
				NativeArray<Game.Objects.Transform> nativeArray2 = chunk.GetNativeArray(ref this.m_TransformType);
				NativeArray<Moving> nativeArray3 = chunk.GetNativeArray(ref this.m_MovingType);
				NativeArray<Blocker> nativeArray4 = chunk.GetNativeArray(ref this.m_BlockerType);
				NativeArray<CarCurrentLane> nativeArray5 = chunk.GetNativeArray(ref this.m_CurrentLaneType);
				NativeArray<CarNavigation> nativeArray6 = chunk.GetNativeArray(ref this.m_NavigationType);
				NativeArray<PrefabRef> nativeArray7 = chunk.GetNativeArray(ref this.m_PrefabRefType);
				NativeArray<PathOwner> nativeArray8 = chunk.GetNativeArray(ref this.m_PathOwnerType);
				BufferAccessor<CarNavigationLane> bufferAccessor = chunk.GetBufferAccessor(ref this.m_NavigationLaneType);
				BufferAccessor<PathElement> bufferAccessor2 = chunk.GetBufferAccessor(ref this.m_PathElementType);
				BufferAccessor<LayoutElement> bufferAccessor3 = chunk.GetBufferAccessor(ref this.m_LayoutElementType);
				Unity.Mathematics.Random random = this.m_RandomSeed.GetRandom(unfilteredChunkIndex);
				if (chunk.Has(ref this.m_OutOfControlType))
				{
					NativeList<BlockedLane> nativeList = new NativeList<BlockedLane>(16, Allocator.Temp);
					for (int i = 0; i < chunk.Count; i++)
					{
						Entity entity = nativeArray[i];
						Game.Objects.Transform transform = nativeArray2[i];
						CarNavigation carNavigation = nativeArray6[i];
						CarCurrentLane currentLane = nativeArray5[i];
						Blocker blocker = nativeArray4[i];
						PathOwner pathOwner = nativeArray8[i];
						PrefabRef prefabRef = nativeArray7[i];
						DynamicBuffer<CarNavigationLane> navigationLanes = bufferAccessor[i];
						DynamicBuffer<PathElement> pathElements = bufferAccessor2[i];
						DynamicBuffer<BlockedLane> blockedLanes = this.m_BlockedLanes[entity];
						ObjectGeometryData objectGeometryData = this.m_PrefabObjectGeometryData[prefabRef.m_Prefab];
						Moving moving = default(Moving);
						if (nativeArray3.Length != 0)
						{
							moving = nativeArray3[i];
						}
						CarNavigationHelpers.CurrentLaneCache currentLaneCache = new CarNavigationHelpers.CurrentLaneCache(ref currentLane, blockedLanes, this.m_EntityStorageInfoLookup, this.m_MovingObjectSearchTree);
						this.UpdateOutOfControl(entity, transform, objectGeometryData, ref carNavigation, ref currentLane, ref blocker, ref pathOwner, navigationLanes, pathElements, blockedLanes, nativeList);
						currentLaneCache.CheckChanges(entity, ref currentLane, nativeList, this.m_LaneObjectBuffer, this.m_LaneObjects, transform, moving, carNavigation, objectGeometryData);
						nativeArray6[i] = carNavigation;
						nativeArray5[i] = currentLane;
						nativeArray8[i] = pathOwner;
						nativeArray4[i] = blocker;
						nativeList.Clear();
						if (bufferAccessor3.Length != 0)
						{
							this.UpdateOutOfControlTrailers(carNavigation, bufferAccessor3[i], nativeList);
						}
					}
					nativeList.Dispose();
					return;
				}
				if (nativeArray3.Length != 0)
				{
					NativeArray<Target> nativeArray9 = chunk.GetNativeArray(ref this.m_TargetType);
					NativeArray<Car> nativeArray10 = chunk.GetNativeArray(ref this.m_CarType);
					NativeArray<Odometer> nativeArray11 = chunk.GetNativeArray(ref this.m_OdometerType);
					NativeArray<PseudoRandomSeed> nativeArray12 = chunk.GetNativeArray(ref this.m_PseudoRandomSeedType);
					NativeList<Entity> tempBuffer = default(NativeList<Entity>);
					CarLaneSelectBuffer laneSelectBuffer = default(CarLaneSelectBuffer);
					bool flag = nativeArray11.Length != 0;
					for (int j = 0; j < chunk.Count; j++)
					{
						Entity entity2 = nativeArray[j];
						Game.Objects.Transform transform2 = nativeArray2[j];
						Moving moving2 = nativeArray3[j];
						Target target = nativeArray9[j];
						Car car = nativeArray10[j];
						CarNavigation navigation = nativeArray6[j];
						CarCurrentLane currentLane2 = nativeArray5[j];
						PseudoRandomSeed pseudoRandomSeed = nativeArray12[j];
						Blocker blocker2 = nativeArray4[j];
						PathOwner pathOwner2 = nativeArray8[j];
						PrefabRef prefabRef2 = nativeArray7[j];
						DynamicBuffer<CarNavigationLane> navigationLanes2 = bufferAccessor[j];
						DynamicBuffer<PathElement> pathElements2 = bufferAccessor2[j];
						DynamicBuffer<BlockedLane> blockedLanes2 = this.m_BlockedLanes[entity2];
						CarData prefabCarData = this.m_PrefabCarData[prefabRef2.m_Prefab];
						ObjectGeometryData objectGeometryData2 = this.m_PrefabObjectGeometryData[prefabRef2.m_Prefab];
						if (bufferAccessor3.Length != 0)
						{
							this.UpdateCarLimits(ref prefabCarData, bufferAccessor3[j]);
						}
						CarNavigationHelpers.CurrentLaneCache currentLaneCache2 = new CarNavigationHelpers.CurrentLaneCache(ref currentLane2, blockedLanes2, this.m_EntityStorageInfoLookup, this.m_MovingObjectSearchTree);
						int priority = VehicleUtils.GetPriority(car);
						Odometer odometer = default(Odometer);
						if (flag)
						{
							odometer = nativeArray11[j];
						}
						this.UpdateNavigationLanes(ref random, priority, entity2, transform2, moving2, target, car, prefabCarData, ref laneSelectBuffer, ref currentLane2, ref blocker2, ref pathOwner2, navigationLanes2, pathElements2);
						this.UpdateNavigationTarget(ref random, priority, entity2, transform2, moving2, car, pseudoRandomSeed, prefabRef2, prefabCarData, objectGeometryData2, ref navigation, ref currentLane2, ref blocker2, ref odometer, ref pathOwner2, ref tempBuffer, navigationLanes2, pathElements2);
						this.ReserveNavigationLanes(ref random, priority, entity2, prefabCarData, objectGeometryData2, car, ref navigation, ref currentLane2, navigationLanes2);
						currentLaneCache2.CheckChanges(entity2, ref currentLane2, default(NativeList<BlockedLane>), this.m_LaneObjectBuffer, this.m_LaneObjects, transform2, moving2, navigation, objectGeometryData2);
						this.m_TrafficAmbienceEffects.Enqueue(new TrafficAmbienceEffect
						{
							m_Amount = this.CalculateNoise(ref currentLane2, prefabRef2, prefabCarData),
							m_Position = transform2.m_Position
						});
						nativeArray6[j] = navigation;
						nativeArray5[j] = currentLane2;
						nativeArray8[j] = pathOwner2;
						nativeArray4[j] = blocker2;
						if (flag)
						{
							nativeArray11[j] = odometer;
						}
						if (bufferAccessor3.Length != 0)
						{
							this.UpdateTrailers(navigation, currentLane2, bufferAccessor3[j]);
						}
					}
					laneSelectBuffer.Dispose();
					if (tempBuffer.IsCreated)
					{
						tempBuffer.Dispose();
					}
					return;
				}
				for (int k = 0; k < chunk.Count; k++)
				{
					Entity entity3 = nativeArray[k];
					Game.Objects.Transform transform3 = nativeArray2[k];
					CarNavigation navigation2 = nativeArray6[k];
					CarCurrentLane currentLane3 = nativeArray5[k];
					Blocker blocker3 = nativeArray4[k];
					PathOwner pathOwner3 = nativeArray8[k];
					PrefabRef prefabRef3 = nativeArray7[k];
					DynamicBuffer<CarNavigationLane> navigationLanes3 = bufferAccessor[k];
					DynamicBuffer<PathElement> pathElements3 = bufferAccessor2[k];
					DynamicBuffer<BlockedLane> blockedLanes3 = this.m_BlockedLanes[entity3];
					ObjectGeometryData objectGeometryData3 = this.m_PrefabObjectGeometryData[prefabRef3.m_Prefab];
					CarNavigationHelpers.CurrentLaneCache currentLaneCache3 = new CarNavigationHelpers.CurrentLaneCache(ref currentLane3, blockedLanes3, this.m_EntityStorageInfoLookup, this.m_MovingObjectSearchTree);
					this.UpdateStopped(transform3, ref currentLane3, ref blocker3, ref pathOwner3, navigationLanes3, pathElements3);
					currentLaneCache3.CheckChanges(entity3, ref currentLane3, default(NativeList<BlockedLane>), this.m_LaneObjectBuffer, this.m_LaneObjects, transform3, default(Moving), navigation2, objectGeometryData3);
					nativeArray5[k] = currentLane3;
					nativeArray8[k] = pathOwner3;
					nativeArray4[k] = blocker3;
					if (bufferAccessor3.Length != 0)
					{
						this.UpdateStoppedTrailers(navigation2, bufferAccessor3[k]);
					}
				}
			}

			private void UpdateCarLimits(ref CarData prefabCarData, DynamicBuffer<LayoutElement> layout)
			{
				for (int i = 1; i < layout.Length; i++)
				{
					Entity vehicle = layout[i].m_Vehicle;
					PrefabRef prefabRef = this.m_PrefabRefData[vehicle];
					CarData carData = this.m_PrefabCarData[prefabRef.m_Prefab];
					prefabCarData.m_Acceleration = math.min(prefabCarData.m_Acceleration, carData.m_Acceleration);
					prefabCarData.m_Braking = math.min(prefabCarData.m_Braking, carData.m_Braking);
					prefabCarData.m_MaxSpeed = math.min(prefabCarData.m_MaxSpeed, carData.m_MaxSpeed);
					prefabCarData.m_Turning = math.min(prefabCarData.m_Turning, carData.m_Turning);
				}
			}

			private void UpdateTrailers(CarNavigation navigation, CarCurrentLane currentLane, DynamicBuffer<LayoutElement> layout)
			{
				Entity lane = currentLane.m_Lane;
				float2 nextPosition = currentLane.m_CurvePosition.xy;
				bool forceNext = (currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.Connection) != 0;
				for (int i = 1; i < layout.Length; i++)
				{
					Entity vehicle = layout[i].m_Vehicle;
					CarTrailerLane trailerLane = this.m_TrailerLaneData[vehicle];
					Game.Objects.Transform transform = this.m_TransformData[vehicle];
					Moving moving = this.m_MovingData[vehicle];
					DynamicBuffer<BlockedLane> blockedLanes = this.m_BlockedLanes[vehicle];
					PrefabRef prefabRef = this.m_PrefabRefData[vehicle];
					ObjectGeometryData objectGeometryData = this.m_PrefabObjectGeometryData[prefabRef.m_Prefab];
					CarNavigationHelpers.TrailerLaneCache trailerLaneCache = new CarNavigationHelpers.TrailerLaneCache(ref trailerLane, blockedLanes, this.m_PrefabRefData, this.m_MovingObjectSearchTree);
					if (trailerLane.m_Lane == Entity.Null)
					{
						this.TryFindCurrentLane(ref trailerLane, transform, moving);
					}
					this.UpdateTrailer(vehicle, transform, objectGeometryData, lane, nextPosition, forceNext, ref trailerLane);
					trailerLaneCache.CheckChanges(vehicle, ref trailerLane, default(NativeList<BlockedLane>), this.m_LaneObjectBuffer, this.m_LaneObjects, transform, moving, navigation, objectGeometryData);
					this.m_TrailerLaneData[vehicle] = trailerLane;
					lane = trailerLane.m_Lane;
					nextPosition = trailerLane.m_CurvePosition;
				}
			}

			private void UpdateOutOfControlTrailers(CarNavigation navigation, DynamicBuffer<LayoutElement> layout, NativeList<BlockedLane> tempBlockedLanes)
			{
				for (int i = 1; i < layout.Length; i++)
				{
					Entity vehicle = layout[i].m_Vehicle;
					CarTrailerLane trailerLane = this.m_TrailerLaneData[vehicle];
					Game.Objects.Transform transform = this.m_TransformData[vehicle];
					Moving moving = this.m_MovingData[vehicle];
					DynamicBuffer<BlockedLane> blockedLanes = this.m_BlockedLanes[vehicle];
					PrefabRef prefabRef = this.m_PrefabRefData[vehicle];
					ObjectGeometryData objectGeometryData = this.m_PrefabObjectGeometryData[prefabRef.m_Prefab];
					CarNavigationHelpers.TrailerLaneCache trailerLaneCache = new CarNavigationHelpers.TrailerLaneCache(ref trailerLane, blockedLanes, this.m_PrefabRefData, this.m_MovingObjectSearchTree);
					this.UpdateOutOfControl(vehicle, transform, objectGeometryData, ref trailerLane, blockedLanes, tempBlockedLanes);
					trailerLaneCache.CheckChanges(vehicle, ref trailerLane, tempBlockedLanes, this.m_LaneObjectBuffer, this.m_LaneObjects, transform, moving, navigation, objectGeometryData);
					this.m_TrailerLaneData[vehicle] = trailerLane;
					tempBlockedLanes.Clear();
				}
			}

			private void UpdateStoppedTrailers(CarNavigation navigation, DynamicBuffer<LayoutElement> layout)
			{
				for (int i = 1; i < layout.Length; i++)
				{
					Entity vehicle = layout[i].m_Vehicle;
					CarTrailerLane trailerLane = this.m_TrailerLaneData[vehicle];
					Game.Objects.Transform transform = this.m_TransformData[vehicle];
					DynamicBuffer<BlockedLane> blockedLanes = this.m_BlockedLanes[vehicle];
					PrefabRef prefabRef = this.m_PrefabRefData[vehicle];
					ObjectGeometryData objectGeometryData = this.m_PrefabObjectGeometryData[prefabRef.m_Prefab];
					CarNavigationHelpers.TrailerLaneCache trailerLaneCache = new CarNavigationHelpers.TrailerLaneCache(ref trailerLane, blockedLanes, this.m_PrefabRefData, this.m_MovingObjectSearchTree);
					if (trailerLane.m_Lane == Entity.Null)
					{
						this.TryFindCurrentLane(ref trailerLane, transform, default(Moving));
					}
					trailerLaneCache.CheckChanges(vehicle, ref trailerLane, default(NativeList<BlockedLane>), this.m_LaneObjectBuffer, this.m_LaneObjects, transform, default(Moving), navigation, objectGeometryData);
					this.m_TrailerLaneData[vehicle] = trailerLane;
				}
			}

			private void UpdateStopped(Game.Objects.Transform transform, ref CarCurrentLane currentLane, ref Blocker blocker, ref PathOwner pathOwner, DynamicBuffer<CarNavigationLane> navigationLanes, DynamicBuffer<PathElement> pathElements)
			{
				if (currentLane.m_Lane == Entity.Null || (currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.Obsolete) != 0)
				{
					this.TryFindCurrentLane(ref currentLane, transform, default(Moving));
					navigationLanes.Clear();
					pathElements.Clear();
					pathOwner.m_ElementIndex = 0;
					pathOwner.m_State |= PathFlags.Obsolete;
				}
				if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.QueueReached) != 0 && (!this.m_CarData.HasComponent(blocker.m_Blocker) || (this.m_CarData[blocker.m_Blocker].m_Flags & CarFlags.Queueing) == 0))
				{
					currentLane.m_LaneFlags &= ~Game.Vehicles.CarLaneFlags.QueueReached;
					blocker = default(Blocker);
				}
			}

			private void UpdateOutOfControl(Entity entity, Game.Objects.Transform transform, ObjectGeometryData prefabObjectGeometryData, ref CarTrailerLane trailerLane, DynamicBuffer<BlockedLane> blockedLanes, NativeList<BlockedLane> tempBlockedLanes)
			{
				float3 position = transform.m_Position;
				float3 @float = math.forward(transform.m_Rotation);
				Line3.Segment line = new Line3.Segment(position - @float * math.max(0.1f, 0f - prefabObjectGeometryData.m_Bounds.min.z - prefabObjectGeometryData.m_Size.x * 0.5f), position + @float * math.max(0.1f, prefabObjectGeometryData.m_Bounds.max.z - prefabObjectGeometryData.m_Size.x * 0.5f));
				float num = prefabObjectGeometryData.m_Size.x * 0.5f;
				Bounds3 bounds = MathUtils.Expand(MathUtils.Bounds(line), num);
				CarNavigationHelpers.FindBlockedLanesIterator findBlockedLanesIterator = default(CarNavigationHelpers.FindBlockedLanesIterator);
				findBlockedLanesIterator.m_Bounds = bounds;
				findBlockedLanesIterator.m_Line = line;
				findBlockedLanesIterator.m_Radius = num;
				findBlockedLanesIterator.m_BlockedLanes = tempBlockedLanes;
				findBlockedLanesIterator.m_SubLanes = this.m_Lanes;
				findBlockedLanesIterator.m_MasterLaneData = this.m_MasterLaneData;
				findBlockedLanesIterator.m_CurveData = this.m_CurveData;
				findBlockedLanesIterator.m_PrefabRefData = this.m_PrefabRefData;
				findBlockedLanesIterator.m_PrefabLaneData = this.m_PrefabLaneData;
				CarNavigationHelpers.FindBlockedLanesIterator iterator = findBlockedLanesIterator;
				this.m_NetSearchTree.Iterate(ref iterator);
				trailerLane = default(CarTrailerLane);
			}

			private void UpdateOutOfControl(Entity entity, Game.Objects.Transform transform, ObjectGeometryData prefabObjectGeometryData, ref CarNavigation carNavigation, ref CarCurrentLane currentLane, ref Blocker blocker, ref PathOwner pathOwner, DynamicBuffer<CarNavigationLane> navigationLanes, DynamicBuffer<PathElement> pathElements, DynamicBuffer<BlockedLane> blockedLanes, NativeList<BlockedLane> tempBlockedLanes)
			{
				float3 position = transform.m_Position;
				float3 @float = math.forward(transform.m_Rotation);
				Line3.Segment line = new Line3.Segment(position - @float * math.max(0.1f, 0f - prefabObjectGeometryData.m_Bounds.min.z - prefabObjectGeometryData.m_Size.x * 0.5f), position + @float * math.max(0.1f, prefabObjectGeometryData.m_Bounds.max.z - prefabObjectGeometryData.m_Size.x * 0.5f));
				float num = prefabObjectGeometryData.m_Size.x * 0.5f;
				Bounds3 bounds = MathUtils.Expand(MathUtils.Bounds(line), num);
				CarNavigationHelpers.FindBlockedLanesIterator findBlockedLanesIterator = default(CarNavigationHelpers.FindBlockedLanesIterator);
				findBlockedLanesIterator.m_Bounds = bounds;
				findBlockedLanesIterator.m_Line = line;
				findBlockedLanesIterator.m_Radius = num;
				findBlockedLanesIterator.m_BlockedLanes = tempBlockedLanes;
				findBlockedLanesIterator.m_SubLanes = this.m_Lanes;
				findBlockedLanesIterator.m_MasterLaneData = this.m_MasterLaneData;
				findBlockedLanesIterator.m_CurveData = this.m_CurveData;
				findBlockedLanesIterator.m_PrefabRefData = this.m_PrefabRefData;
				findBlockedLanesIterator.m_PrefabLaneData = this.m_PrefabLaneData;
				CarNavigationHelpers.FindBlockedLanesIterator iterator = findBlockedLanesIterator;
				this.m_NetSearchTree.Iterate(ref iterator);
				carNavigation = new CarNavigation
				{
					m_TargetPosition = transform.m_Position
				};
				currentLane = default(CarCurrentLane);
				blocker = default(Blocker);
				pathOwner.m_ElementIndex = 0;
				navigationLanes.Clear();
				pathElements.Clear();
			}

			private void UpdateNavigationLanes(ref Unity.Mathematics.Random random, int priority, Entity entity, Game.Objects.Transform transform, Moving moving, Target target, Car car, CarData prefabCarData, ref CarLaneSelectBuffer laneSelectBuffer, ref CarCurrentLane currentLane, ref Blocker blocker, ref PathOwner pathOwner, DynamicBuffer<CarNavigationLane> navigationLanes, DynamicBuffer<PathElement> pathElements)
			{
				int invalidPath = 10000000;
				if (currentLane.m_Lane == Entity.Null || (currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.Obsolete) != 0)
				{
					invalidPath = -1;
					this.TryFindCurrentLane(ref currentLane, transform, moving);
				}
				else if ((pathOwner.m_State & (PathFlags.Pending | PathFlags.Failed | PathFlags.Obsolete | PathFlags.Updated)) != 0 && (pathOwner.m_State & PathFlags.Append) == 0)
				{
					this.ClearNavigationLanes(ref currentLane, navigationLanes, invalidPath);
				}
				else if ((pathOwner.m_State & PathFlags.Updated) == 0)
				{
					this.FillNavigationPaths(ref random, priority, entity, transform, target, car, ref laneSelectBuffer, ref currentLane, ref blocker, ref pathOwner, navigationLanes, pathElements, ref invalidPath);
				}
				if (invalidPath != 10000000)
				{
					this.ClearNavigationLanes(moving, prefabCarData, ref currentLane, navigationLanes, invalidPath);
					pathElements.Clear();
					pathOwner.m_ElementIndex = 0;
					pathOwner.m_State |= PathFlags.Obsolete;
				}
			}

			private void ClearNavigationLanes(ref CarCurrentLane currentLane, DynamicBuffer<CarNavigationLane> navigationLanes, int invalidPath)
			{
				if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.ClearedForPathfind) == 0)
				{
					currentLane.m_CurvePosition.z = currentLane.m_CurvePosition.y;
				}
				if (invalidPath > 0)
				{
					for (int i = 0; i < navigationLanes.Length; i++)
					{
						if ((navigationLanes[i].m_Flags & (Game.Vehicles.CarLaneFlags.Reserved | Game.Vehicles.CarLaneFlags.ClearedForPathfind)) == 0)
						{
							invalidPath = math.min(i, invalidPath);
							break;
						}
					}
				}
				invalidPath = math.max(invalidPath, 0);
				if (invalidPath < navigationLanes.Length)
				{
					navigationLanes.RemoveRange(invalidPath, navigationLanes.Length - invalidPath);
				}
			}

			private void ClearNavigationLanes(Moving moving, CarData prefabCarData, ref CarCurrentLane currentLane, DynamicBuffer<CarNavigationLane> navigationLanes, int invalidPath)
			{
				if (invalidPath >= 0)
				{
					VehicleUtils.ClearNavigationForPathfind(moving, prefabCarData, ref currentLane, navigationLanes, ref this.m_CarLaneData, ref this.m_CurveData);
				}
				else
				{
					currentLane.m_CurvePosition.z = currentLane.m_CurvePosition.y;
				}
				invalidPath = math.max(invalidPath, 0);
				if (invalidPath < navigationLanes.Length)
				{
					navigationLanes.RemoveRange(invalidPath, navigationLanes.Length - invalidPath);
				}
			}

			private void TryFindCurrentLane(ref CarCurrentLane currentLane, Game.Objects.Transform transform, Moving moving)
			{
				float num = 4f / 15f;
				currentLane.m_LaneFlags &= ~(Game.Vehicles.CarLaneFlags.TransformTarget | Game.Vehicles.CarLaneFlags.ParkingSpace | Game.Vehicles.CarLaneFlags.Obsolete | Game.Vehicles.CarLaneFlags.TurnLeft | Game.Vehicles.CarLaneFlags.TurnRight | Game.Vehicles.CarLaneFlags.Area);
				currentLane.m_Lane = Entity.Null;
				currentLane.m_ChangeLane = Entity.Null;
				float3 @float = transform.m_Position + moving.m_Velocity * (num * 2f);
				float num2 = 100f;
				Bounds3 bounds = new Bounds3(@float - num2, @float + num2);
				CarNavigationHelpers.FindLaneIterator findLaneIterator = default(CarNavigationHelpers.FindLaneIterator);
				findLaneIterator.m_Bounds = bounds;
				findLaneIterator.m_Position = @float;
				findLaneIterator.m_MinDistance = num2;
				findLaneIterator.m_Result = currentLane;
				findLaneIterator.m_CarType = RoadTypes.Car;
				findLaneIterator.m_SubLanes = this.m_Lanes;
				findLaneIterator.m_AreaNodes = this.m_AreaNodes;
				findLaneIterator.m_AreaTriangles = this.m_AreaTriangles;
				findLaneIterator.m_CarLaneData = this.m_CarLaneData;
				findLaneIterator.m_MasterLaneData = this.m_MasterLaneData;
				findLaneIterator.m_ConnectionLaneData = this.m_ConnectionLaneData;
				findLaneIterator.m_CurveData = this.m_CurveData;
				findLaneIterator.m_PrefabRefData = this.m_PrefabRefData;
				findLaneIterator.m_PrefabCarLaneData = this.m_PrefabCarLaneData;
				CarNavigationHelpers.FindLaneIterator iterator = findLaneIterator;
				this.m_NetSearchTree.Iterate(ref iterator);
				this.m_StaticObjectSearchTree.Iterate(ref iterator);
				this.m_AreaSearchTree.Iterate(ref iterator);
				currentLane = iterator.m_Result;
			}

			private void TryFindCurrentLane(ref CarTrailerLane trailerLane, Game.Objects.Transform transform, Moving moving)
			{
				float num = 4f / 15f;
				float3 @float = transform.m_Position + moving.m_Velocity * (num * 2f);
				float num2 = 100f;
				Bounds3 bounds = new Bounds3(@float - num2, @float + num2);
				CarNavigationHelpers.FindLaneIterator findLaneIterator = default(CarNavigationHelpers.FindLaneIterator);
				findLaneIterator.m_Bounds = bounds;
				findLaneIterator.m_Position = @float;
				findLaneIterator.m_MinDistance = num2;
				findLaneIterator.m_CarType = RoadTypes.Car;
				findLaneIterator.m_SubLanes = this.m_Lanes;
				findLaneIterator.m_AreaNodes = this.m_AreaNodes;
				findLaneIterator.m_AreaTriangles = this.m_AreaTriangles;
				findLaneIterator.m_CarLaneData = this.m_CarLaneData;
				findLaneIterator.m_MasterLaneData = this.m_MasterLaneData;
				findLaneIterator.m_ConnectionLaneData = this.m_ConnectionLaneData;
				findLaneIterator.m_CurveData = this.m_CurveData;
				findLaneIterator.m_PrefabRefData = this.m_PrefabRefData;
				findLaneIterator.m_PrefabCarLaneData = this.m_PrefabCarLaneData;
				CarNavigationHelpers.FindLaneIterator iterator = findLaneIterator;
				this.m_NetSearchTree.Iterate(ref iterator);
				this.m_StaticObjectSearchTree.Iterate(ref iterator);
				this.m_AreaSearchTree.Iterate(ref iterator);
				trailerLane.m_Lane = iterator.m_Result.m_Lane;
				trailerLane.m_CurvePosition = iterator.m_Result.m_CurvePosition.xy;
				trailerLane.m_NextLane = Entity.Null;
				trailerLane.m_NextPosition = default(float2);
			}

			private void FillNavigationPaths(ref Unity.Mathematics.Random random, int priority, Entity entity, Game.Objects.Transform transform, Target target, Car car, ref CarLaneSelectBuffer laneSelectBuffer, ref CarCurrentLane currentLane, ref Blocker blocker, ref PathOwner pathOwner, DynamicBuffer<CarNavigationLane> navigationLanes, DynamicBuffer<PathElement> pathElements, ref int invalidPath)
			{
				if ((currentLane.m_LaneFlags & (Game.Vehicles.CarLaneFlags.EndOfPath | Game.Vehicles.CarLaneFlags.ParkingSpace | Game.Vehicles.CarLaneFlags.Waypoint)) == 0)
				{
					for (int i = 0; i <= 8; i++)
					{
						if (i >= navigationLanes.Length)
						{
							if (i == 8)
							{
								if ((pathOwner.m_State & PathFlags.Pending) != 0)
								{
									break;
								}
								int num = math.min(40000, pathElements.Length - pathOwner.m_ElementIndex);
								if (num <= 0)
								{
									break;
								}
								int num2 = random.NextInt(num) * (random.NextInt(num) + 1) / num;
								PathElement pathElement = pathElements[pathOwner.m_ElementIndex + num2];
								if (this.m_EntityStorageInfoLookup.Exists(pathElement.m_Target))
								{
									break;
								}
								invalidPath = navigationLanes.Length;
								return;
							}
							i = navigationLanes.Length;
							if (pathOwner.m_ElementIndex >= pathElements.Length)
							{
								if ((pathOwner.m_State & PathFlags.Pending) != 0)
								{
									break;
								}
								CarNavigationLane navLaneData = default(CarNavigationLane);
								if (i > 0)
								{
									CarNavigationLane value = navigationLanes[i - 1];
									if ((value.m_Flags & Game.Vehicles.CarLaneFlags.TransformTarget) == 0 && (car.m_Flags & (CarFlags.StayOnRoad | CarFlags.AnyLaneTarget)) != (CarFlags.StayOnRoad | CarFlags.AnyLaneTarget) && this.GetTransformTarget(ref navLaneData.m_Lane, target))
									{
										if ((value.m_Flags & Game.Vehicles.CarLaneFlags.GroupTarget) == 0)
										{
											Entity lane = navLaneData.m_Lane;
											navLaneData.m_Lane = value.m_Lane;
											navLaneData.m_Flags = value.m_Flags & (Game.Vehicles.CarLaneFlags.Connection | Game.Vehicles.CarLaneFlags.Area);
											navLaneData.m_CurvePosition = value.m_CurvePosition.yy;
											float3 position = default(float3);
											if (VehicleUtils.CalculateTransformPosition(ref position, lane, this.m_TransformData, this.m_PositionData, this.m_PrefabRefData, this.m_PrefabBuildingData))
											{
												this.UpdateSlaveLane(ref navLaneData, position);
											}
											if ((car.m_Flags & CarFlags.StayOnRoad) != 0)
											{
												navLaneData.m_Flags |= Game.Vehicles.CarLaneFlags.EndOfPath | Game.Vehicles.CarLaneFlags.GroupTarget;
												navigationLanes.Add(navLaneData);
												currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.UpdateOptimalLane;
												break;
											}
											navLaneData.m_Flags |= Game.Vehicles.CarLaneFlags.GroupTarget;
											navigationLanes.Add(navLaneData);
											currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.UpdateOptimalLane;
											continue;
										}
										navLaneData.m_Flags |= Game.Vehicles.CarLaneFlags.EndOfPath | Game.Vehicles.CarLaneFlags.TransformTarget;
										navigationLanes.Add(navLaneData);
										currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.UpdateOptimalLane;
										break;
									}
									value.m_Flags |= Game.Vehicles.CarLaneFlags.EndOfPath;
									navigationLanes[i - 1] = value;
									currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.UpdateOptimalLane;
									break;
								}
								if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.TransformTarget) != 0 || (car.m_Flags & CarFlags.StayOnRoad) != 0 || !this.GetTransformTarget(ref navLaneData.m_Lane, target))
								{
									currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.EndOfPath;
									break;
								}
								navLaneData.m_Flags |= Game.Vehicles.CarLaneFlags.EndOfPath | Game.Vehicles.CarLaneFlags.TransformTarget;
								navigationLanes.Add(navLaneData);
								currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.UpdateOptimalLane;
								break;
							}
							PathElement pathElement2 = pathElements[pathOwner.m_ElementIndex++];
							CarNavigationLane navLaneData2 = default(CarNavigationLane);
							navLaneData2.m_Lane = pathElement2.m_Target;
							navLaneData2.m_CurvePosition = pathElement2.m_TargetDelta;
							if (!this.m_CarLaneData.HasComponent(navLaneData2.m_Lane))
							{
								if (this.m_ParkingLaneData.HasComponent(navLaneData2.m_Lane))
								{
									Game.Net.ParkingLane parkingLane = this.m_ParkingLaneData[navLaneData2.m_Lane];
									navLaneData2.m_Flags |= Game.Vehicles.CarLaneFlags.ParkingSpace | Game.Vehicles.CarLaneFlags.FixedLane;
									if ((parkingLane.m_Flags & ParkingLaneFlags.ParkingLeft) != 0)
									{
										navLaneData2.m_Flags |= Game.Vehicles.CarLaneFlags.TurnLeft;
									}
									if ((parkingLane.m_Flags & ParkingLaneFlags.ParkingRight) != 0)
									{
										navLaneData2.m_Flags |= Game.Vehicles.CarLaneFlags.TurnRight;
									}
									navigationLanes.Add(navLaneData2);
									if (i > 0)
									{
										float3 targetPosition = MathUtils.Position(this.m_CurveData[navLaneData2.m_Lane].m_Bezier, navLaneData2.m_CurvePosition.y);
										CarNavigationLane navLaneData3 = navigationLanes[i - 1];
										this.UpdateSlaveLane(ref navLaneData3, targetPosition);
										navigationLanes[i - 1] = navLaneData3;
									}
									currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.UpdateOptimalLane;
									break;
								}
								if (this.m_ConnectionLaneData.HasComponent(navLaneData2.m_Lane))
								{
									Game.Net.ConnectionLane connectionLane = this.m_ConnectionLaneData[navLaneData2.m_Lane];
									navLaneData2.m_Flags |= Game.Vehicles.CarLaneFlags.FixedLane;
									if ((connectionLane.m_Flags & ConnectionLaneFlags.Area) != 0)
									{
										navLaneData2.m_Flags |= Game.Vehicles.CarLaneFlags.Area;
									}
									else
									{
										navLaneData2.m_Flags |= Game.Vehicles.CarLaneFlags.Connection;
									}
									currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.UpdateOptimalLane;
									if ((connectionLane.m_Flags & ConnectionLaneFlags.Parking) != 0)
									{
										navLaneData2.m_Flags |= Game.Vehicles.CarLaneFlags.ParkingSpace;
										navigationLanes.Add(navLaneData2);
										break;
									}
									navigationLanes.Add(navLaneData2);
									continue;
								}
								if (this.m_LaneData.HasComponent(navLaneData2.m_Lane))
								{
									if (pathOwner.m_ElementIndex >= pathElements.Length && (pathOwner.m_State & PathFlags.Pending) != 0)
									{
										pathOwner.m_ElementIndex--;
										break;
									}
									if (i > 0)
									{
										float3 targetPosition2 = MathUtils.Position(this.m_CurveData[navLaneData2.m_Lane].m_Bezier, navLaneData2.m_CurvePosition.y);
										CarNavigationLane navLaneData4 = navigationLanes[i - 1];
										this.UpdateSlaveLane(ref navLaneData4, targetPosition2);
										navLaneData4.m_Flags |= Game.Vehicles.CarLaneFlags.Waypoint;
										if (pathOwner.m_ElementIndex >= pathElements.Length)
										{
											navLaneData4.m_Flags |= Game.Vehicles.CarLaneFlags.EndOfPath;
										}
										navigationLanes[i - 1] = navLaneData4;
									}
									else
									{
										currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.Waypoint;
										if (pathOwner.m_ElementIndex >= pathElements.Length)
										{
											currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.EndOfPath;
										}
									}
									currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.UpdateOptimalLane;
									break;
								}
								if (this.m_TransformData.HasComponent(navLaneData2.m_Lane))
								{
									if (pathOwner.m_ElementIndex >= pathElements.Length && (pathOwner.m_State & PathFlags.Pending) != 0)
									{
										pathOwner.m_ElementIndex--;
										break;
									}
									if ((car.m_Flags & CarFlags.StayOnRoad) == 0 || pathElements.Length > pathOwner.m_ElementIndex)
									{
										navLaneData2.m_Flags |= Game.Vehicles.CarLaneFlags.TransformTarget;
										navigationLanes.Add(navLaneData2);
										if (i > 0)
										{
											float3 position2 = this.m_TransformData[navLaneData2.m_Lane].m_Position;
											CarNavigationLane navLaneData5 = navigationLanes[i - 1];
											this.UpdateSlaveLane(ref navLaneData5, position2);
											navigationLanes[i - 1] = navLaneData5;
										}
										currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.UpdateOptimalLane;
									}
									continue;
								}
								invalidPath = i;
								return;
							}
							Game.Net.CarLane carLane = this.m_CarLaneData[navLaneData2.m_Lane];
							if ((carLane.m_Flags & Game.Net.CarLaneFlags.Forward) == 0)
							{
								bool flag = (carLane.m_Flags & (Game.Net.CarLaneFlags.UTurnLeft | Game.Net.CarLaneFlags.TurnLeft | Game.Net.CarLaneFlags.GentleTurnLeft)) != 0;
								bool flag2 = (carLane.m_Flags & (Game.Net.CarLaneFlags.TurnRight | Game.Net.CarLaneFlags.UTurnRight | Game.Net.CarLaneFlags.GentleTurnRight)) != 0;
								if (flag && !flag2)
								{
									navLaneData2.m_Flags |= Game.Vehicles.CarLaneFlags.TurnLeft;
								}
								if (flag2 && !flag)
								{
									navLaneData2.m_Flags |= Game.Vehicles.CarLaneFlags.TurnRight;
								}
							}
							if ((carLane.m_Flags & (Game.Net.CarLaneFlags.Approach | Game.Net.CarLaneFlags.Roundabout)) == Game.Net.CarLaneFlags.Roundabout)
							{
								navLaneData2.m_Flags |= Game.Vehicles.CarLaneFlags.Roundabout;
							}
							if ((carLane.m_Flags & Game.Net.CarLaneFlags.Twoway) != 0)
							{
								navLaneData2.m_Flags |= Game.Vehicles.CarLaneFlags.CanReverse;
							}
							if ((carLane.m_Flags & Game.Net.CarLaneFlags.Unsafe) != 0 && ((carLane.m_Flags & (Game.Net.CarLaneFlags.UTurnLeft | Game.Net.CarLaneFlags.UTurnRight)) != 0 || (this.m_OwnerData.TryGetComponent(navLaneData2.m_Lane, out var componentData) && this.m_CurveData.HasComponent(componentData.m_Owner))))
							{
								navLaneData2.m_Flags |= Game.Vehicles.CarLaneFlags.RequestSpace;
							}
							navLaneData2.m_Flags |= Game.Vehicles.CarLaneFlags.UpdateOptimalLane;
							currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.UpdateOptimalLane;
							if (i == 0)
							{
								if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.ParkingSpace) != 0 && this.m_ParkingLaneData.TryGetComponent(currentLane.m_Lane, out var componentData2))
								{
									currentLane.m_LaneFlags &= ~(Game.Vehicles.CarLaneFlags.TurnLeft | Game.Vehicles.CarLaneFlags.TurnRight);
									if ((componentData2.m_Flags & ParkingLaneFlags.ParkingRight) != 0)
									{
										currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.TurnLeft;
									}
									if ((componentData2.m_Flags & ParkingLaneFlags.ParkingLeft) != 0)
									{
										currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.TurnRight;
									}
								}
							}
							else
							{
								CarNavigationLane value2 = navigationLanes[i - 1];
								if ((value2.m_Flags & Game.Vehicles.CarLaneFlags.ParkingSpace) != 0 && this.m_ParkingLaneData.TryGetComponent(value2.m_Lane, out var componentData3))
								{
									value2.m_Flags &= ~(Game.Vehicles.CarLaneFlags.TurnLeft | Game.Vehicles.CarLaneFlags.TurnRight);
									if ((componentData3.m_Flags & ParkingLaneFlags.ParkingRight) != 0)
									{
										value2.m_Flags |= Game.Vehicles.CarLaneFlags.TurnLeft;
									}
									if ((componentData3.m_Flags & ParkingLaneFlags.ParkingLeft) != 0)
									{
										value2.m_Flags |= Game.Vehicles.CarLaneFlags.TurnRight;
									}
									navigationLanes[i - 1] = value2;
								}
							}
							if (i == 0 && (currentLane.m_LaneFlags & (Game.Vehicles.CarLaneFlags.FixedLane | Game.Vehicles.CarLaneFlags.Connection)) == Game.Vehicles.CarLaneFlags.FixedLane)
							{
								this.GetSlaveLaneFromMasterLane(ref random, ref navLaneData2, currentLane);
							}
							else
							{
								this.GetSlaveLaneFromMasterLane(ref random, ref navLaneData2);
							}
							if ((pathElement2.m_Flags & PathElementFlags.PathStart) != 0)
							{
								Entity lane2;
								float prevCurvePos;
								if (i == 0)
								{
									lane2 = currentLane.m_Lane;
									prevCurvePos = currentLane.m_CurvePosition.z;
								}
								else
								{
									lane2 = navigationLanes[i - 1].m_Lane;
									prevCurvePos = navigationLanes[i - 1].m_CurvePosition.y;
								}
								if (this.IsContinuous(lane2, prevCurvePos, pathElement2.m_Target, pathElement2.m_TargetDelta.x, out var sameLane))
								{
									if (sameLane)
									{
										if (i == 0)
										{
											currentLane.m_CurvePosition.z = pathElement2.m_TargetDelta.y;
											continue;
										}
										CarNavigationLane value3 = navigationLanes[i - 1];
										value3.m_CurvePosition.y = pathElement2.m_TargetDelta.y;
										navigationLanes[i - 1] = value3;
										continue;
									}
								}
								else
								{
									navLaneData2.m_Flags |= Game.Vehicles.CarLaneFlags.Interruption;
								}
							}
							navigationLanes.Add(navLaneData2);
						}
						else
						{
							CarNavigationLane carNavigationLane = navigationLanes[i];
							if (!this.m_PrefabRefData.HasComponent(carNavigationLane.m_Lane))
							{
								invalidPath = i;
								return;
							}
							if ((carNavigationLane.m_Flags & (Game.Vehicles.CarLaneFlags.EndOfPath | Game.Vehicles.CarLaneFlags.ParkingSpace | Game.Vehicles.CarLaneFlags.Waypoint)) != 0)
							{
								break;
							}
						}
					}
				}
				if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.UpdateOptimalLane) == 0)
				{
					return;
				}
				currentLane.m_LaneFlags &= ~Game.Vehicles.CarLaneFlags.UpdateOptimalLane;
				if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.IsBlocked) != 0)
				{
					if (this.IsBlockedLane(currentLane.m_Lane, currentLane.m_CurvePosition.xz))
					{
						invalidPath = -1;
						return;
					}
					for (int j = 0; j < navigationLanes.Length; j++)
					{
						CarNavigationLane carNavigationLane2 = navigationLanes[j];
						if (this.IsBlockedLane(carNavigationLane2.m_Lane, carNavigationLane2.m_CurvePosition))
						{
							invalidPath = j;
							return;
						}
					}
					currentLane.m_LaneFlags &= ~(Game.Vehicles.CarLaneFlags.FixedLane | Game.Vehicles.CarLaneFlags.IsBlocked);
					currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.IgnoreBlocker;
				}
				CarLaneSelectIterator carLaneSelectIterator = default(CarLaneSelectIterator);
				carLaneSelectIterator.m_OwnerData = this.m_OwnerData;
				carLaneSelectIterator.m_LaneData = this.m_LaneData;
				carLaneSelectIterator.m_CarLaneData = this.m_CarLaneData;
				carLaneSelectIterator.m_SlaveLaneData = this.m_SlaveLaneData;
				carLaneSelectIterator.m_LaneReservationData = this.m_LaneReservationData;
				carLaneSelectIterator.m_MovingData = this.m_MovingData;
				carLaneSelectIterator.m_CarData = this.m_CarData;
				carLaneSelectIterator.m_ControllerData = this.m_ControllerData;
				carLaneSelectIterator.m_Lanes = this.m_Lanes;
				carLaneSelectIterator.m_LaneObjects = this.m_LaneObjects;
				carLaneSelectIterator.m_Entity = entity;
				carLaneSelectIterator.m_Blocker = blocker.m_Blocker;
				carLaneSelectIterator.m_Priority = priority;
				carLaneSelectIterator.m_ForbidLaneFlags = VehicleUtils.GetForbiddenLaneFlags(car);
				carLaneSelectIterator.m_PreferLaneFlags = VehicleUtils.GetPreferredLaneFlags(car);
				CarLaneSelectIterator carLaneSelectIterator2 = carLaneSelectIterator;
				carLaneSelectIterator2.SetBuffer(ref laneSelectBuffer);
				if (navigationLanes.Length != 0)
				{
					CarNavigationLane carNavigationLane3 = navigationLanes[navigationLanes.Length - 1];
					carLaneSelectIterator2.CalculateLaneCosts(carNavigationLane3, navigationLanes.Length - 1);
					for (int num3 = navigationLanes.Length - 2; num3 >= 0; num3--)
					{
						CarNavigationLane carNavigationLane4 = navigationLanes[num3];
						carLaneSelectIterator2.CalculateLaneCosts(carNavigationLane4, carNavigationLane3, num3);
						carNavigationLane3 = carNavigationLane4;
					}
					carLaneSelectIterator2.UpdateOptimalLane(ref currentLane, navigationLanes[0]);
					for (int k = 0; k < navigationLanes.Length; k++)
					{
						CarNavigationLane navLaneData6 = navigationLanes[k];
						carLaneSelectIterator2.UpdateOptimalLane(ref navLaneData6);
						navLaneData6.m_Flags &= ~Game.Vehicles.CarLaneFlags.Reserved;
						navigationLanes[k] = navLaneData6;
					}
				}
				else if (currentLane.m_CurvePosition.x != currentLane.m_CurvePosition.z)
				{
					carLaneSelectIterator2.UpdateOptimalLane(ref currentLane, default(CarNavigationLane));
				}
			}

			private bool IsContinuous(Entity prevLane, float prevCurvePos, Entity pathTarget, float nextCurvePos, out bool sameLane)
			{
				sameLane = false;
				if (this.m_SlaveLaneData.HasComponent(prevLane))
				{
					SlaveLane slaveLane = this.m_SlaveLaneData[prevLane];
					Entity owner = this.m_OwnerData[prevLane].m_Owner;
					prevLane = this.m_Lanes[owner][slaveLane.m_MasterIndex].m_SubLane;
					if (!this.m_MasterLaneData.HasComponent(prevLane))
					{
						return false;
					}
				}
				if (prevLane == pathTarget && prevCurvePos == nextCurvePos)
				{
					sameLane = true;
					return true;
				}
				if (!this.m_LaneData.HasComponent(prevLane) || !this.m_LaneData.HasComponent(pathTarget))
				{
					return false;
				}
				Lane lane = this.m_LaneData[prevLane];
				Lane lane2 = this.m_LaneData[pathTarget];
				return lane.m_EndNode.Equals(lane2.m_StartNode);
			}

			private bool IsBlockedLane(Entity lane, float2 range)
			{
				if (this.m_SlaveLaneData.HasComponent(lane))
				{
					SlaveLane slaveLane = this.m_SlaveLaneData[lane];
					Entity owner = this.m_OwnerData[lane].m_Owner;
					lane = this.m_Lanes[owner][slaveLane.m_MasterIndex].m_SubLane;
					if (!this.m_MasterLaneData.HasComponent(lane))
					{
						return false;
					}
				}
				if (!this.m_CarLaneData.HasComponent(lane))
				{
					return false;
				}
				Game.Net.CarLane carLane = this.m_CarLaneData[lane];
				if (carLane.m_BlockageEnd < carLane.m_BlockageStart)
				{
					return false;
				}
				if (math.min(range.x, range.y) <= (float)(int)carLane.m_BlockageEnd * 0.003921569f)
				{
					return math.max(range.x, range.y) >= (float)(int)carLane.m_BlockageStart * 0.003921569f;
				}
				return false;
			}

			private bool GetTransformTarget(ref Entity entity, Target target)
			{
				if (this.m_PropertyRenterData.HasComponent(target.m_Target))
				{
					target.m_Target = this.m_PropertyRenterData[target.m_Target].m_Property;
				}
				if (this.m_TransformData.HasComponent(target.m_Target))
				{
					entity = target.m_Target;
					return true;
				}
				if (this.m_PositionData.HasComponent(target.m_Target))
				{
					entity = target.m_Target;
					return true;
				}
				return false;
			}

			private void UpdateSlaveLane(ref CarNavigationLane navLaneData, float3 targetPosition)
			{
				if (this.m_SlaveLaneData.HasComponent(navLaneData.m_Lane))
				{
					SlaveLane slaveLane = this.m_SlaveLaneData[navLaneData.m_Lane];
					Entity owner = this.m_OwnerData[navLaneData.m_Lane].m_Owner;
					DynamicBuffer<Game.Net.SubLane> lanes = this.m_Lanes[owner];
					int index = NetUtils.ChooseClosestLane(slaveLane.m_MinIndex, slaveLane.m_MaxIndex, targetPosition, lanes, this.m_CurveData, navLaneData.m_CurvePosition.y);
					navLaneData.m_Lane = lanes[index].m_SubLane;
				}
				navLaneData.m_Flags |= Game.Vehicles.CarLaneFlags.FixedLane;
			}

			private void GetSlaveLaneFromMasterLane(ref Unity.Mathematics.Random random, ref CarNavigationLane navLaneData, CarCurrentLane currentLaneData)
			{
				if (this.m_MasterLaneData.HasComponent(navLaneData.m_Lane))
				{
					MasterLane masterLane = this.m_MasterLaneData[navLaneData.m_Lane];
					Owner owner = this.m_OwnerData[navLaneData.m_Lane];
					DynamicBuffer<Game.Net.SubLane> lanes = this.m_Lanes[owner.m_Owner];
					if ((currentLaneData.m_LaneFlags & Game.Vehicles.CarLaneFlags.TransformTarget) != 0)
					{
						float3 position = default(float3);
						if (VehicleUtils.CalculateTransformPosition(ref position, currentLaneData.m_Lane, this.m_TransformData, this.m_PositionData, this.m_PrefabRefData, this.m_PrefabBuildingData))
						{
							int index = NetUtils.ChooseClosestLane(masterLane.m_MinIndex, masterLane.m_MaxIndex, position, lanes, this.m_CurveData, navLaneData.m_CurvePosition.y);
							navLaneData.m_Lane = lanes[index].m_SubLane;
							navLaneData.m_Flags |= Game.Vehicles.CarLaneFlags.FixedStart;
						}
						else
						{
							int index2 = random.NextInt(masterLane.m_MinIndex, masterLane.m_MaxIndex + 1);
							navLaneData.m_Lane = lanes[index2].m_SubLane;
						}
					}
					else
					{
						float3 comparePosition = MathUtils.Position(this.m_CurveData[currentLaneData.m_Lane].m_Bezier, currentLaneData.m_CurvePosition.z);
						int index3 = NetUtils.ChooseClosestLane(masterLane.m_MinIndex, masterLane.m_MaxIndex, comparePosition, lanes, this.m_CurveData, navLaneData.m_CurvePosition.x);
						navLaneData.m_Lane = lanes[index3].m_SubLane;
						navLaneData.m_Flags |= Game.Vehicles.CarLaneFlags.FixedStart;
					}
				}
				else
				{
					navLaneData.m_Flags |= Game.Vehicles.CarLaneFlags.FixedLane;
				}
			}

			private void GetSlaveLaneFromMasterLane(ref Unity.Mathematics.Random random, ref CarNavigationLane navLaneData)
			{
				if (this.m_MasterLaneData.HasComponent(navLaneData.m_Lane))
				{
					MasterLane masterLane = this.m_MasterLaneData[navLaneData.m_Lane];
					Entity owner = this.m_OwnerData[navLaneData.m_Lane].m_Owner;
					DynamicBuffer<Game.Net.SubLane> dynamicBuffer = this.m_Lanes[owner];
					int index = random.NextInt(masterLane.m_MinIndex, masterLane.m_MaxIndex + 1);
					navLaneData.m_Lane = dynamicBuffer[index].m_SubLane;
				}
				else
				{
					navLaneData.m_Flags |= Game.Vehicles.CarLaneFlags.FixedLane;
				}
			}

			private bool GetNextLane(Entity prevLane, Entity nextLane, out Entity selectedLane)
			{
				if (this.m_SlaveLaneData.TryGetComponent(nextLane, out var componentData) && this.m_LaneData.TryGetComponent(prevLane, out var componentData2))
				{
					Entity owner = this.m_OwnerData[nextLane].m_Owner;
					DynamicBuffer<Game.Net.SubLane> dynamicBuffer = this.m_Lanes[owner];
					int num = math.min(componentData.m_MaxIndex, dynamicBuffer.Length - 1);
					for (int i = componentData.m_MinIndex; i <= num; i++)
					{
						if (this.m_LaneData[dynamicBuffer[i].m_SubLane].m_StartNode.Equals(componentData2.m_EndNode))
						{
							selectedLane = dynamicBuffer[i].m_SubLane;
							return true;
						}
					}
				}
				selectedLane = Entity.Null;
				return false;
			}

			private void CheckBlocker(ref CarCurrentLane currentLane, ref Blocker blocker, ref CarLaneSpeedIterator laneIterator)
			{
				if (laneIterator.m_Blocker != blocker.m_Blocker)
				{
					currentLane.m_LaneFlags &= ~(Game.Vehicles.CarLaneFlags.IgnoreBlocker | Game.Vehicles.CarLaneFlags.QueueReached);
				}
				if (laneIterator.m_Blocker != Entity.Null)
				{
					if (!this.m_MovingData.HasComponent(laneIterator.m_Blocker))
					{
						if (this.m_CarData.HasComponent(laneIterator.m_Blocker))
						{
							if ((this.m_CarData[laneIterator.m_Blocker].m_Flags & CarFlags.Queueing) != 0 && (currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.Queue) != 0)
							{
								if (laneIterator.m_MaxSpeed <= 3f)
								{
									currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.QueueReached;
								}
							}
							else
							{
								currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.UpdateOptimalLane;
								if (laneIterator.m_MaxSpeed <= 3f)
								{
									currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.IsBlocked;
								}
							}
						}
						else
						{
							currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.UpdateOptimalLane;
							if (laneIterator.m_MaxSpeed <= 3f)
							{
								currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.IsBlocked;
							}
						}
					}
					else if (laneIterator.m_Blocker != blocker.m_Blocker)
					{
						currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.UpdateOptimalLane;
					}
				}
				blocker.m_Blocker = laneIterator.m_Blocker;
				blocker.m_Type = laneIterator.m_BlockerType;
				blocker.m_MaxSpeed = (byte)math.clamp(Mathf.RoundToInt(laneIterator.m_MaxSpeed * 2.29499984f), 0, 255);
			}

			private void UpdateTrailer(Entity entity, Game.Objects.Transform transform, ObjectGeometryData prefabObjectGeometryData, Entity nextLane, float2 nextPosition, bool forceNext, ref CarTrailerLane trailerLane)
			{
				if (forceNext)
				{
					trailerLane.m_Lane = nextLane;
					trailerLane.m_CurvePosition = nextPosition;
					trailerLane.m_NextLane = Entity.Null;
					trailerLane.m_NextPosition = default(float2);
					if (this.m_CurveData.HasComponent(nextLane))
					{
						MathUtils.Distance(this.m_CurveData[nextLane].m_Bezier, transform.m_Position, out trailerLane.m_CurvePosition.x);
					}
					return;
				}
				if (nextLane != Entity.Null)
				{
					if (trailerLane.m_Lane == nextLane)
					{
						trailerLane.m_CurvePosition.y = nextPosition.y;
						trailerLane.m_NextLane = Entity.Null;
						trailerLane.m_NextPosition = default(float2);
						nextLane = Entity.Null;
						nextPosition = default(float2);
					}
					else if (trailerLane.m_NextLane == nextLane)
					{
						trailerLane.m_NextPosition.y = nextPosition.y;
						nextLane = Entity.Null;
						nextPosition = default(float2);
					}
					else if (trailerLane.m_NextLane == Entity.Null)
					{
						trailerLane.m_NextLane = nextLane;
						trailerLane.m_NextPosition = nextPosition;
						nextLane = Entity.Null;
						nextPosition = default(float2);
					}
				}
				float3 @float = float.MaxValue;
				float3 float2 = default(float3);
				if (this.m_CurveData.HasComponent(trailerLane.m_Lane))
				{
					@float.x = MathUtils.Distance(this.m_CurveData[trailerLane.m_Lane].m_Bezier, transform.m_Position, out float2.x);
				}
				if (this.m_CurveData.HasComponent(trailerLane.m_NextLane))
				{
					@float.y = MathUtils.Distance(this.m_CurveData[trailerLane.m_NextLane].m_Bezier, transform.m_Position, out float2.y);
				}
				if (this.m_CurveData.HasComponent(nextLane))
				{
					@float.z = MathUtils.Distance(this.m_CurveData[nextLane].m_Bezier, transform.m_Position, out float2.z);
				}
				if (math.all(@float.z < @float.xy) || forceNext)
				{
					trailerLane.m_Lane = nextLane;
					trailerLane.m_CurvePosition = new float2(float2.z, nextPosition.y);
					trailerLane.m_NextLane = Entity.Null;
					trailerLane.m_NextPosition = default(float2);
				}
				else if (@float.y < @float.x)
				{
					trailerLane.m_Lane = trailerLane.m_NextLane;
					trailerLane.m_CurvePosition = new float2(float2.y, trailerLane.m_NextPosition.y);
					trailerLane.m_NextLane = nextLane;
					trailerLane.m_NextPosition = nextPosition;
				}
				else
				{
					trailerLane.m_CurvePosition.x = float2.x;
				}
			}

			private void UpdateNavigationTarget(ref Unity.Mathematics.Random random, int priority, Entity entity, Game.Objects.Transform transform, Moving moving, Car car, PseudoRandomSeed pseudoRandomSeed, PrefabRef prefabRef, CarData prefabCarData, ObjectGeometryData prefabObjectGeometryData, ref CarNavigation navigation, ref CarCurrentLane currentLane, ref Blocker blocker, ref Odometer odometer, ref PathOwner pathOwner, ref NativeList<Entity> tempBuffer, DynamicBuffer<CarNavigationLane> navigationLanes, DynamicBuffer<PathElement> pathElements)
			{
				float num = 4f / 15f;
				float num2 = math.length(moving.m_Velocity);
				float speedLimitFactor = VehicleUtils.GetSpeedLimitFactor(car);
				VehicleUtils.GetDrivingStyle(this.m_SimulationFrame, pseudoRandomSeed, out var safetyTime);
				if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.Connection) != 0)
				{
					prefabCarData.m_MaxSpeed = 277.777771f;
					prefabCarData.m_Acceleration = 277.777771f;
					prefabCarData.m_Braking = 277.777771f;
				}
				else
				{
					num2 = math.min(num2, prefabCarData.m_MaxSpeed);
					if ((pathOwner.m_State & (PathFlags.Pending | PathFlags.Obsolete)) != 0)
					{
						prefabCarData.m_Acceleration = 0f;
					}
				}
				Bounds1 speedRange = (((currentLane.m_LaneFlags & (Game.Vehicles.CarLaneFlags.Connection | Game.Vehicles.CarLaneFlags.ResetSpeed)) == 0) ? VehicleUtils.CalculateSpeedRange(prefabCarData, num2, num) : new Bounds1(0f, prefabCarData.m_MaxSpeed));
				bool flag = blocker.m_Type == BlockerType.Temporary;
				bool flag2 = math.asuint(navigation.m_MaxSpeed) >> 31 != 0;
				CarLaneSpeedIterator carLaneSpeedIterator = default(CarLaneSpeedIterator);
				carLaneSpeedIterator.m_TransformData = this.m_TransformData;
				carLaneSpeedIterator.m_MovingData = this.m_MovingData;
				carLaneSpeedIterator.m_CarData = this.m_CarData;
				carLaneSpeedIterator.m_TrainData = this.m_TrainData;
				carLaneSpeedIterator.m_ControllerData = this.m_ControllerData;
				carLaneSpeedIterator.m_LaneReservationData = this.m_LaneReservationData;
				carLaneSpeedIterator.m_LaneConditionData = this.m_LaneConditionData;
				carLaneSpeedIterator.m_LaneSignalData = this.m_LaneSignalData;
				carLaneSpeedIterator.m_CurveData = this.m_CurveData;
				carLaneSpeedIterator.m_CarLaneData = this.m_CarLaneData;
				carLaneSpeedIterator.m_ParkingLaneData = this.m_ParkingLaneData;
				carLaneSpeedIterator.m_UnspawnedData = this.m_UnspawnedData;
				carLaneSpeedIterator.m_CreatureData = this.m_CreatureData;
				carLaneSpeedIterator.m_PrefabRefData = this.m_PrefabRefData;
				carLaneSpeedIterator.m_PrefabObjectGeometryData = this.m_PrefabObjectGeometryData;
				carLaneSpeedIterator.m_PrefabCarData = this.m_PrefabCarData;
				carLaneSpeedIterator.m_PrefabTrainData = this.m_PrefabTrainData;
				carLaneSpeedIterator.m_PrefabParkingLaneData = this.m_PrefabParkingLaneData;
				carLaneSpeedIterator.m_LaneOverlapData = this.m_LaneOverlaps;
				carLaneSpeedIterator.m_LaneObjectData = this.m_LaneObjects;
				carLaneSpeedIterator.m_Entity = entity;
				carLaneSpeedIterator.m_Ignore = (((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.IgnoreBlocker) != 0) ? blocker.m_Blocker : Entity.Null);
				carLaneSpeedIterator.m_TempBuffer = tempBuffer;
				carLaneSpeedIterator.m_Priority = priority;
				carLaneSpeedIterator.m_TimeStep = num;
				carLaneSpeedIterator.m_SafeTimeStep = num + safetyTime;
				carLaneSpeedIterator.m_DistanceOffset = math.select(0f, math.max(-0.5f, -0.5f * math.lengthsq(1.5f - num2)), num2 < 1.5f);
				carLaneSpeedIterator.m_SpeedLimitFactor = speedLimitFactor;
				carLaneSpeedIterator.m_CurrentSpeed = num2;
				carLaneSpeedIterator.m_PrefabCar = prefabCarData;
				carLaneSpeedIterator.m_PrefabObjectGeometry = prefabObjectGeometryData;
				carLaneSpeedIterator.m_SpeedRange = speedRange;
				carLaneSpeedIterator.m_PushBlockers = (currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.PushBlockers) != 0;
				carLaneSpeedIterator.m_MaxSpeed = speedRange.max;
				carLaneSpeedIterator.m_CanChangeLane = 1f;
				carLaneSpeedIterator.m_CurrentPosition = transform.m_Position;
				CarLaneSpeedIterator laneIterator = carLaneSpeedIterator;
				Game.Vehicles.CarLaneFlags carLaneFlags = (Game.Vehicles.CarLaneFlags)0u;
				Game.Net.CarLaneFlags laneFlags;
				if ((currentLane.m_LaneFlags & (Game.Vehicles.CarLaneFlags.TransformTarget | Game.Vehicles.CarLaneFlags.ParkingSpace)) != 0)
				{
					laneIterator.IterateTarget(navigation.m_TargetPosition);
					navigation.m_MaxSpeed = laneIterator.m_MaxSpeed;
					blocker.m_Blocker = Entity.Null;
					blocker.m_Type = BlockerType.None;
					blocker.m_MaxSpeed = byte.MaxValue;
				}
				else
				{
					if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.Area) == 0)
					{
						if (currentLane.m_Lane == Entity.Null)
						{
							navigation.m_MaxSpeed = math.max(0f, num2 - prefabCarData.m_Braking * num);
							blocker.m_Blocker = Entity.Null;
							blocker.m_Type = BlockerType.None;
							blocker.m_MaxSpeed = byte.MaxValue;
							return;
						}
						PrefabRef prefabRef2 = this.m_PrefabRefData[currentLane.m_Lane];
						NetLaneData prefabLaneData = this.m_PrefabLaneData[prefabRef2.m_Prefab];
						float laneOffset = VehicleUtils.GetLaneOffset(prefabObjectGeometryData, prefabLaneData, currentLane.m_LanePosition);
						if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.HighBeams) != 0)
						{
							if (!this.m_CarLaneData.HasComponent(currentLane.m_Lane) || !this.AllowHighBeams(transform, blocker, ref currentLane, navigationLanes, 100f, 2f))
							{
								currentLane.m_LaneFlags &= ~Game.Vehicles.CarLaneFlags.HighBeams;
							}
						}
						else if (this.m_CarLaneData.HasComponent(currentLane.m_Lane) && (this.m_CarLaneData[currentLane.m_Lane].m_Flags & Game.Net.CarLaneFlags.Highway) != 0 && !this.IsLit(currentLane.m_Lane) && this.AllowHighBeams(transform, blocker, ref currentLane, navigationLanes, 150f, 0f))
						{
							currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.HighBeams;
						}
						Entity nextLane = Entity.Null;
						float2 nextOffset = 0f;
						if (navigationLanes.Length > 0)
						{
							CarNavigationLane carNavigationLane = navigationLanes[0];
							nextLane = carNavigationLane.m_Lane;
							nextOffset = carNavigationLane.m_CurvePosition;
						}
						if (currentLane.m_ChangeLane != Entity.Null)
						{
							PrefabRef prefabRef3 = this.m_PrefabRefData[currentLane.m_ChangeLane];
							NetLaneData prefabLaneData2 = this.m_PrefabLaneData[prefabRef3.m_Prefab];
							float laneOffset2 = VehicleUtils.GetLaneOffset(prefabObjectGeometryData, prefabLaneData2, 0f - currentLane.m_LanePosition);
							if (!laneIterator.IterateFirstLane(currentLane.m_Lane, currentLane.m_ChangeLane, currentLane.m_CurvePosition, nextLane, nextOffset, currentLane.m_ChangeProgress, laneOffset, laneOffset2, (currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.RequestSpace) != 0, out laneFlags))
							{
								goto IL_05e3;
							}
						}
						else if (!laneIterator.IterateFirstLane(currentLane.m_Lane, currentLane.m_CurvePosition, nextLane, nextOffset, laneOffset, (currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.RequestSpace) != 0, out laneFlags))
						{
							goto IL_05e3;
						}
						goto IL_07c5;
					}
					navigation.m_TargetPosition.y = TerrainUtils.SampleHeight(ref this.m_TerrainHeightData, navigation.m_TargetPosition);
					laneIterator.IterateTarget(navigation.m_TargetPosition, 11.1111116f);
					navigation.m_MaxSpeed = laneIterator.m_MaxSpeed;
					blocker.m_Blocker = Entity.Null;
					blocker.m_Type = BlockerType.None;
					blocker.m_MaxSpeed = byte.MaxValue;
				}
				goto IL_0802;
				IL_07c5:
				navigation.m_MaxSpeed = laneIterator.m_MaxSpeed;
				this.CheckBlocker(ref currentLane, ref blocker, ref laneIterator);
				if (laneIterator.m_TempBuffer.IsCreated)
				{
					tempBuffer = laneIterator.m_TempBuffer;
					tempBuffer.Clear();
				}
				goto IL_0802;
				IL_05e3:
				int num3 = 0;
				while (true)
				{
					if (num3 < navigationLanes.Length)
					{
						CarNavigationLane carNavigationLane2 = navigationLanes[num3];
						if ((carNavigationLane2.m_Flags & (Game.Vehicles.CarLaneFlags.TransformTarget | Game.Vehicles.CarLaneFlags.Area)) == 0)
						{
							if ((carNavigationLane2.m_Flags & Game.Vehicles.CarLaneFlags.Connection) != 0)
							{
								laneIterator.m_PrefabCar.m_MaxSpeed = 277.777771f;
								laneIterator.m_PrefabCar.m_Acceleration = 277.777771f;
								laneIterator.m_PrefabCar.m_Braking = 277.777771f;
								laneIterator.m_SpeedRange = new Bounds1(0f, 277.777771f);
							}
							else
							{
								if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.Connection) != 0)
								{
									goto IL_07b7;
								}
								if ((carNavigationLane2.m_Flags & Game.Vehicles.CarLaneFlags.Interruption) != 0)
								{
									laneIterator.m_PrefabCar.m_MaxSpeed = 3f;
								}
							}
							if ((num3 == 0 || (carNavigationLane2.m_Flags & (Game.Vehicles.CarLaneFlags.ParkingSpace | Game.Vehicles.CarLaneFlags.Roundabout)) == 0) && carLaneFlags == (Game.Vehicles.CarLaneFlags)0u && (carNavigationLane2.m_Flags & (Game.Vehicles.CarLaneFlags.ParkingSpace | Game.Vehicles.CarLaneFlags.Validated)) != Game.Vehicles.CarLaneFlags.ParkingSpace)
							{
								carLaneFlags |= carNavigationLane2.m_Flags & (Game.Vehicles.CarLaneFlags.TurnLeft | Game.Vehicles.CarLaneFlags.TurnRight);
							}
							bool c = (carNavigationLane2.m_Lane == currentLane.m_Lane) | (carNavigationLane2.m_Lane == currentLane.m_ChangeLane);
							float a = math.select(-1f, 2f, carNavigationLane2.m_CurvePosition.y < carNavigationLane2.m_CurvePosition.x);
							a = math.select(a, currentLane.m_CurvePosition.y, c);
							bool needSignal;
							bool num4 = laneIterator.IterateNextLane(carNavigationLane2.m_Lane, carNavigationLane2.m_CurvePosition, a, navigationLanes.AsNativeArray().GetSubArray(num3 + 1, navigationLanes.Length - 1 - num3), (carNavigationLane2.m_Flags & Game.Vehicles.CarLaneFlags.RequestSpace) != 0, ref laneFlags, out needSignal);
							if (needSignal)
							{
								this.m_LaneSignals.Enqueue(new CarNavigationHelpers.LaneSignal(entity, carNavigationLane2.m_Lane, priority));
							}
							if (num4)
							{
								break;
							}
							num3++;
							continue;
						}
					}
					goto IL_07b7;
					IL_07b7:
					laneIterator.IterateTarget(laneIterator.m_CurrentPosition);
					break;
				}
				goto IL_07c5;
				IL_0802:
				float num5 = math.select(prefabCarData.m_PivotOffset, 0f - prefabCarData.m_PivotOffset, flag2);
				float3 position = transform.m_Position;
				if (num5 < 0f)
				{
					position += math.rotate(transform.m_Rotation, new float3(0f, 0f, num5));
					num5 = 0f - num5;
				}
				float num6 = math.lerp(math.distance(position, navigation.m_TargetPosition), 0f, laneIterator.m_Oncoming);
				float num7 = math.max(1f, navigation.m_MaxSpeed * num) + num5;
				float num8 = num7;
				if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.Area) != 0)
				{
					float brakingDistance = VehicleUtils.GetBrakingDistance(prefabCarData, math.min(prefabCarData.m_MaxSpeed, 11.1111116f), num);
					num8 = math.max(num7, brakingDistance + 1f + num5);
					num6 = math.select(num6, 0f, currentLane.m_ChangeProgress != 0f);
				}
				if (currentLane.m_ChangeLane != Entity.Null)
				{
					float num9 = 0.05f;
					float num10 = 1f + prefabObjectGeometryData.m_Bounds.max.z * num9;
					float2 x = new float2(0.4f, 0.6f * math.saturate(num2 * num9));
					x *= laneIterator.m_CanChangeLane * num;
					x.x = math.min(x.x, math.max(0f, 1f - currentLane.m_ChangeProgress));
					currentLane.m_ChangeProgress = math.min(num10, currentLane.m_ChangeProgress + math.csum(x));
					if (currentLane.m_ChangeProgress == num10 || (currentLane.m_LaneFlags & (Game.Vehicles.CarLaneFlags.Connection | Game.Vehicles.CarLaneFlags.ResetSpeed)) != 0)
					{
						this.ApplySideEffects(ref currentLane, speedLimitFactor, prefabRef, prefabCarData);
						currentLane.m_LanePosition = 0f - currentLane.m_LanePosition;
						currentLane.m_Lane = currentLane.m_ChangeLane;
						currentLane.m_ChangeLane = Entity.Null;
						currentLane.m_LaneFlags &= ~(Game.Vehicles.CarLaneFlags.TurnLeft | Game.Vehicles.CarLaneFlags.TurnRight);
					}
				}
				if ((currentLane.m_LaneFlags & (Game.Vehicles.CarLaneFlags.TurnLeft | Game.Vehicles.CarLaneFlags.TurnRight)) == 0)
				{
					currentLane.m_LaneFlags |= carLaneFlags;
				}
				bool num11 = blocker.m_Type == BlockerType.Temporary;
				if (num11 != flag || currentLane.m_Duration >= 30f)
				{
					this.ApplySideEffects(ref currentLane, speedLimitFactor, prefabRef, prefabCarData);
				}
				if (num11)
				{
					if (currentLane.m_Duration >= 5f)
					{
						currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.PushBlockers;
					}
				}
				else if (currentLane.m_Duration >= 5f)
				{
					currentLane.m_LaneFlags &= ~Game.Vehicles.CarLaneFlags.PushBlockers;
				}
				currentLane.m_Duration += num;
				if (num2 > 0.01f)
				{
					float num12 = num2 * num;
					currentLane.m_Distance += num12;
					odometer.m_Distance += num12;
					carLaneFlags = currentLane.m_LaneFlags & (Game.Vehicles.CarLaneFlags.TurnLeft | Game.Vehicles.CarLaneFlags.TurnRight);
					float4 @float = math.select(new float4(-0.5f, 0.5f, 0.002f, 0.1f), new float4(0f, 0f, 0.01f, 0.1f), new bool4(carLaneFlags == Game.Vehicles.CarLaneFlags.TurnRight, carLaneFlags == Game.Vehicles.CarLaneFlags.TurnLeft, carLaneFlags != (Game.Vehicles.CarLaneFlags)0u, w: true));
					@float.zw = math.min(1f, num12 * @float.zw);
					currentLane.m_LanePosition -= (math.max(0f, currentLane.m_LanePosition - 0.5f) + math.min(0f, currentLane.m_LanePosition + 0.5f)) * @float.w;
					currentLane.m_LanePosition = math.lerp(currentLane.m_LanePosition, random.NextFloat(@float.x, @float.y), @float.z);
				}
				if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.ResetSpeed) != 0)
				{
					if (currentLane.m_Distance > 10f + num2 * 0.5f)
					{
						currentLane.m_LaneFlags &= ~Game.Vehicles.CarLaneFlags.ResetSpeed;
						currentLane.m_Distance = 0f;
						currentLane.m_Duration = 0f;
					}
					else if (currentLane.m_Duration > 60f)
					{
						blocker.m_Blocker = entity;
						blocker.m_Type = BlockerType.Spawn;
					}
				}
				if (num6 < num8)
				{
					while (true)
					{
						if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.ParkingSpace) != 0)
						{
							Curve curve = this.m_CurveData[currentLane.m_Lane];
							currentLane.m_CurvePosition.x = currentLane.m_CurvePosition.z;
							if (this.m_ParkingLaneData.HasComponent(currentLane.m_Lane))
							{
								Game.Net.ParkingLane parkingLane = this.m_ParkingLaneData[currentLane.m_Lane];
								PrefabRef prefabRef4 = this.m_PrefabRefData[currentLane.m_Lane];
								ParkingLaneData parkingLaneData = this.m_PrefabParkingLaneData[prefabRef4.m_Prefab];
								Game.Objects.Transform ownerTransform = default(Game.Objects.Transform);
								if (this.m_OwnerData.TryGetComponent(currentLane.m_Lane, out var componentData) && this.m_TransformData.HasComponent(componentData.m_Owner))
								{
									ownerTransform = this.m_TransformData[componentData.m_Owner];
								}
								Game.Objects.Transform transform2 = VehicleUtils.CalculateParkingSpaceTarget(parkingLane, parkingLaneData, prefabObjectGeometryData, curve, ownerTransform, currentLane.m_CurvePosition.x);
								navigation.m_TargetPosition = transform2.m_Position;
								navigation.m_TargetRotation = transform2.m_Rotation;
							}
							else
							{
								Game.Net.ConnectionLane connectionLane = this.m_ConnectionLaneData[currentLane.m_Lane];
								navigation.m_TargetPosition = VehicleUtils.GetConnectionParkingPosition(connectionLane, curve.m_Bezier, currentLane.m_CurvePosition.x);
								navigation.m_TargetRotation = quaternion.LookRotationSafe(MathUtils.Tangent(curve.m_Bezier, currentLane.m_CurvePosition.x), math.up());
							}
							num6 = math.distance(position, navigation.m_TargetPosition);
							if (num6 >= 1f + num5)
							{
								navigation.m_TargetRotation = default(quaternion);
							}
						}
						else if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.TransformTarget) != 0)
						{
							bool flag3 = false;
							if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.ResetSpeed) != 0)
							{
								quaternion targetRotation = this.CalculateNavigationRotation(currentLane.m_Lane, navigationLanes);
								flag3 = !targetRotation.Equals(navigation.m_TargetRotation);
								navigation.m_TargetRotation = targetRotation;
							}
							else
							{
								navigation.m_TargetRotation = default(quaternion);
							}
							if (this.MoveTarget(position, ref navigation.m_TargetPosition, num7, currentLane.m_Lane) || flag3)
							{
								break;
							}
						}
						else if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.Area) != 0)
						{
							navigation.m_TargetRotation = default(quaternion);
							float navigationSize = VehicleUtils.GetNavigationSize(prefabObjectGeometryData);
							bool num13 = this.MoveAreaTarget(ref random, transform.m_Position, pathOwner, navigationLanes, pathElements, ref navigation.m_TargetPosition, num8, currentLane.m_Lane, ref currentLane.m_CurvePosition, currentLane.m_LanePosition, navigationSize);
							navigation.m_TargetPosition.y = TerrainUtils.SampleHeight(ref this.m_TerrainHeightData, navigation.m_TargetPosition);
							currentLane.m_ChangeProgress = 0f;
							if (num13)
							{
								break;
							}
						}
						else
						{
							navigation.m_TargetRotation = default(quaternion);
							if (currentLane.m_ChangeLane != Entity.Null)
							{
								Curve curve2 = this.m_CurveData[currentLane.m_Lane];
								Curve curve3 = this.m_CurveData[currentLane.m_ChangeLane];
								PrefabRef prefabRef5 = this.m_PrefabRefData[currentLane.m_Lane];
								PrefabRef prefabRef6 = this.m_PrefabRefData[currentLane.m_ChangeLane];
								NetLaneData prefabLaneData3 = this.m_PrefabLaneData[prefabRef5.m_Prefab];
								NetLaneData prefabLaneData4 = this.m_PrefabLaneData[prefabRef6.m_Prefab];
								float laneOffset3 = VehicleUtils.GetLaneOffset(prefabObjectGeometryData, prefabLaneData3, currentLane.m_LanePosition);
								float laneOffset4 = VehicleUtils.GetLaneOffset(prefabObjectGeometryData, prefabLaneData4, 0f - currentLane.m_LanePosition);
								if (this.MoveTarget(position, ref navigation.m_TargetPosition, num7, curve2.m_Bezier, curve3.m_Bezier, currentLane.m_ChangeProgress, ref currentLane.m_CurvePosition, laneOffset3, laneOffset4))
								{
									if ((prefabLaneData3.m_Flags & LaneFlags.Twoway) == 0)
									{
										currentLane.m_LaneFlags &= ~Game.Vehicles.CarLaneFlags.CanReverse;
									}
									break;
								}
							}
							else
							{
								Curve curve4 = this.m_CurveData[currentLane.m_Lane];
								PrefabRef prefabRef7 = this.m_PrefabRefData[currentLane.m_Lane];
								NetLaneData prefabLaneData5 = this.m_PrefabLaneData[prefabRef7.m_Prefab];
								float num14 = VehicleUtils.GetLaneOffset(prefabObjectGeometryData, prefabLaneData5, currentLane.m_LanePosition);
								if (laneIterator.m_Oncoming != 0f)
								{
									float num15 = prefabObjectGeometryData.m_Bounds.max.x - prefabObjectGeometryData.m_Bounds.min.x;
									float num16 = math.lerp(num14, num15 * math.select(0.5f, -0.5f, this.m_LeftHandTraffic), math.min(1f, laneIterator.m_Oncoming));
									num14 = math.select(num14, num16, (!this.m_LeftHandTraffic && num16 > num14) | (this.m_LeftHandTraffic && num16 < num14));
									currentLane.m_LanePosition = num14 / math.max(0.1f, prefabLaneData5.m_Width - num15);
								}
								num14 = math.select(num14, 0f - num14, currentLane.m_CurvePosition.z < currentLane.m_CurvePosition.x);
								if (this.MoveTarget(position, ref navigation.m_TargetPosition, num7, curve4.m_Bezier, ref currentLane.m_CurvePosition, num14))
								{
									if ((prefabLaneData5.m_Flags & LaneFlags.Twoway) == 0)
									{
										currentLane.m_LaneFlags &= ~Game.Vehicles.CarLaneFlags.CanReverse;
									}
									break;
								}
							}
						}
						if (navigationLanes.Length == 0)
						{
							num6 = math.distance(position, navigation.m_TargetPosition);
							if (num6 < 1f + num5 && num2 < 0.1f)
							{
								currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.EndReached;
							}
							break;
						}
						CarNavigationLane carNavigationLane3 = navigationLanes[0];
						if ((carNavigationLane3.m_Flags & (Game.Vehicles.CarLaneFlags.ParkingSpace | Game.Vehicles.CarLaneFlags.Validated)) == Game.Vehicles.CarLaneFlags.ParkingSpace || !this.m_PrefabRefData.HasComponent(carNavigationLane3.m_Lane))
						{
							break;
						}
						if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.Connection) != 0)
						{
							if ((carNavigationLane3.m_Flags & Game.Vehicles.CarLaneFlags.TransformTarget) != 0)
							{
								carNavigationLane3.m_Flags |= Game.Vehicles.CarLaneFlags.ResetSpeed;
							}
							else if ((carNavigationLane3.m_Flags & Game.Vehicles.CarLaneFlags.Connection) == 0)
							{
								num6 = math.distance(position, navigation.m_TargetPosition);
								if (num6 >= 1f + num5 || num2 > 3f)
								{
									break;
								}
								carNavigationLane3.m_Flags |= Game.Vehicles.CarLaneFlags.ResetSpeed;
							}
						}
						if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.HighBeams) != 0 && this.m_CarLaneData.TryGetComponent(carNavigationLane3.m_Lane, out var componentData2) && (componentData2.m_Flags & Game.Net.CarLaneFlags.Highway) != 0 && !this.IsLit(carNavigationLane3.m_Lane))
						{
							carNavigationLane3.m_Flags |= Game.Vehicles.CarLaneFlags.HighBeams;
						}
						this.ApplySideEffects(ref currentLane, speedLimitFactor, prefabRef, prefabCarData);
						if (currentLane.m_ChangeLane != Entity.Null && this.GetNextLane(currentLane.m_Lane, carNavigationLane3.m_Lane, out var selectedLane) && selectedLane != carNavigationLane3.m_Lane)
						{
							currentLane.m_Lane = selectedLane;
							currentLane.m_ChangeLane = carNavigationLane3.m_Lane;
						}
						else
						{
							currentLane.m_Lane = carNavigationLane3.m_Lane;
							currentLane.m_ChangeLane = Entity.Null;
							currentLane.m_ChangeProgress = 0f;
						}
						currentLane.m_CurvePosition = carNavigationLane3.m_CurvePosition.xxy;
						currentLane.m_LaneFlags = carNavigationLane3.m_Flags | (currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.PushBlockers);
						navigationLanes.RemoveAt(0);
					}
				}
				if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.Area) != 0)
				{
					VehicleCollisionIterator vehicleCollisionIterator = default(VehicleCollisionIterator);
					vehicleCollisionIterator.m_OwnerData = this.m_OwnerData;
					vehicleCollisionIterator.m_TransformData = this.m_TransformData;
					vehicleCollisionIterator.m_MovingData = this.m_MovingData;
					vehicleCollisionIterator.m_ControllerData = this.m_ControllerData;
					vehicleCollisionIterator.m_CreatureData = this.m_CreatureData;
					vehicleCollisionIterator.m_CurveData = this.m_CurveData;
					vehicleCollisionIterator.m_AreaLaneData = this.m_AreaLaneData;
					vehicleCollisionIterator.m_PrefabRefData = this.m_PrefabRefData;
					vehicleCollisionIterator.m_PrefabObjectGeometryData = this.m_PrefabObjectGeometryData;
					vehicleCollisionIterator.m_PrefabLaneData = this.m_PrefabLaneData;
					vehicleCollisionIterator.m_AreaNodes = this.m_AreaNodes;
					vehicleCollisionIterator.m_StaticObjectSearchTree = this.m_StaticObjectSearchTree;
					vehicleCollisionIterator.m_MovingObjectSearchTree = this.m_MovingObjectSearchTree;
					vehicleCollisionIterator.m_TerrainHeightData = this.m_TerrainHeightData;
					vehicleCollisionIterator.m_Entity = entity;
					vehicleCollisionIterator.m_CurrentLane = currentLane.m_Lane;
					vehicleCollisionIterator.m_CurvePosition = currentLane.m_CurvePosition.z;
					vehicleCollisionIterator.m_TimeStep = num;
					vehicleCollisionIterator.m_PrefabObjectGeometry = prefabObjectGeometryData;
					vehicleCollisionIterator.m_SpeedRange = speedRange;
					vehicleCollisionIterator.m_CurrentPosition = transform.m_Position;
					vehicleCollisionIterator.m_CurrentVelocity = moving.m_Velocity;
					vehicleCollisionIterator.m_MinDistance = num8;
					vehicleCollisionIterator.m_TargetPosition = navigation.m_TargetPosition;
					vehicleCollisionIterator.m_MaxSpeed = navigation.m_MaxSpeed;
					vehicleCollisionIterator.m_LanePosition = currentLane.m_LanePosition;
					vehicleCollisionIterator.m_Blocker = blocker.m_Blocker;
					vehicleCollisionIterator.m_BlockerType = blocker.m_Type;
					VehicleCollisionIterator vehicleCollisionIterator2 = vehicleCollisionIterator;
					if (vehicleCollisionIterator2.m_MaxSpeed != 0f && !flag2)
					{
						vehicleCollisionIterator2.IterateFirstLane(currentLane.m_Lane);
						vehicleCollisionIterator2.m_MaxSpeed = math.select(vehicleCollisionIterator2.m_MaxSpeed, 0f, vehicleCollisionIterator2.m_MaxSpeed < 0.1f);
						if (!navigation.m_TargetPosition.Equals(vehicleCollisionIterator2.m_TargetPosition))
						{
							navigation.m_TargetPosition = vehicleCollisionIterator2.m_TargetPosition;
							currentLane.m_LanePosition = math.lerp(currentLane.m_LanePosition, vehicleCollisionIterator2.m_LanePosition, 0.1f);
							currentLane.m_ChangeProgress = 1f;
						}
						navigation.m_MaxSpeed = vehicleCollisionIterator2.m_MaxSpeed;
						blocker.m_Blocker = vehicleCollisionIterator2.m_Blocker;
						blocker.m_Type = vehicleCollisionIterator2.m_BlockerType;
						blocker.m_MaxSpeed = (byte)math.clamp(Mathf.RoundToInt(vehicleCollisionIterator2.m_MaxSpeed * 2.29499984f), 0, 255);
					}
					navigation.m_MaxSpeed = math.min(navigation.m_MaxSpeed, math.distance(transform.m_Position.xz, navigation.m_TargetPosition.xz) / num);
				}
				else
				{
					navigation.m_MaxSpeed = math.min(navigation.m_MaxSpeed, math.distance(transform.m_Position, navigation.m_TargetPosition) / num);
				}
				if ((currentLane.m_LaneFlags & (Game.Vehicles.CarLaneFlags.Connection | Game.Vehicles.CarLaneFlags.ResetSpeed)) != 0)
				{
					return;
				}
				float3 float2 = navigation.m_TargetPosition - position;
				if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.Area) != 0)
				{
					float2.xz = MathUtils.ClampLength(float2.xz, num7);
					float2.y = TerrainUtils.SampleHeight(ref this.m_TerrainHeightData, position + float2) - position.y;
				}
				num6 = math.length(float2);
				float3 x2 = math.forward(transform.m_Rotation);
				if (flag2)
				{
					if (num6 < 1f + num5 || math.dot(x2, math.normalizesafe(float2)) < 0.8f)
					{
						navigation.m_MaxSpeed = 0f - math.min(3f, navigation.m_MaxSpeed);
					}
					else if (num2 >= 0.1f)
					{
						navigation.m_MaxSpeed = 0f - math.max(0f, math.min(navigation.m_MaxSpeed, num2 - prefabCarData.m_Braking * num));
					}
				}
				else
				{
					if (!(num6 >= 1f + num5) || !(currentLane.m_ChangeLane == Entity.Null) || !(math.dot(x2, math.normalizesafe(float2)) < 0.7f))
					{
						return;
					}
					if (num2 >= 0.1f)
					{
						navigation.m_MaxSpeed = math.max(0f, math.min(navigation.m_MaxSpeed, num2 - prefabCarData.m_Braking * num));
						return;
					}
					navigation.m_MaxSpeed = 0f - math.min(3f, navigation.m_MaxSpeed);
					if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.Area) == 0)
					{
						currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.CanReverse;
					}
				}
			}

			private quaternion CalculateNavigationRotation(Entity sourceLocation, DynamicBuffer<CarNavigationLane> navigationLanes)
			{
				float3 @float = default(float3);
				bool flag = false;
				if (this.m_TransformData.TryGetComponent(sourceLocation, out var componentData))
				{
					@float = componentData.m_Position;
					flag = true;
				}
				for (int i = 0; i < navigationLanes.Length; i++)
				{
					CarNavigationLane carNavigationLane = navigationLanes[i];
					if (this.m_TransformData.TryGetComponent(carNavigationLane.m_Lane, out componentData))
					{
						if (flag)
						{
							float3 value = componentData.m_Position - @float;
							if (MathUtils.TryNormalize(ref value))
							{
								return quaternion.LookRotationSafe(value, math.up());
							}
						}
						else
						{
							@float = componentData.m_Position;
							flag = true;
						}
					}
					else
					{
						if (!this.m_CurveData.TryGetComponent(carNavigationLane.m_Lane, out var componentData2))
						{
							continue;
						}
						float3 float2 = MathUtils.Position(componentData2.m_Bezier, carNavigationLane.m_CurvePosition.x);
						if (flag)
						{
							float3 value2 = float2 - @float;
							if (MathUtils.TryNormalize(ref value2))
							{
								return quaternion.LookRotationSafe(value2, math.up());
							}
						}
						else
						{
							@float = float2;
							flag = true;
						}
						if (carNavigationLane.m_CurvePosition.x != carNavigationLane.m_CurvePosition.y)
						{
							float3 float3 = MathUtils.Tangent(componentData2.m_Bezier, carNavigationLane.m_CurvePosition.x);
							float3 = math.select(float3, -float3, carNavigationLane.m_CurvePosition.y < carNavigationLane.m_CurvePosition.x);
							if (MathUtils.TryNormalize(ref float3))
							{
								return quaternion.LookRotationSafe(float3, math.up());
							}
						}
					}
				}
				return default(quaternion);
			}

			private bool IsLit(Entity lane)
			{
				if (this.m_OwnerData.TryGetComponent(lane, out var componentData) && this.m_RoadData.TryGetComponent(componentData.m_Owner, out var componentData2))
				{
					return (componentData2.m_Flags & (Game.Net.RoadFlags.IsLit | Game.Net.RoadFlags.LightsOff)) == Game.Net.RoadFlags.IsLit;
				}
				return false;
			}

			private float CalculateNoise(ref CarCurrentLane currentLaneData, PrefabRef prefabRefData, CarData prefabCarData)
			{
				if (this.m_PrefabSideEffectData.HasComponent(prefabRefData.m_Prefab) && this.m_CarLaneData.HasComponent(currentLaneData.m_Lane))
				{
					VehicleSideEffectData vehicleSideEffectData = this.m_PrefabSideEffectData[prefabRefData.m_Prefab];
					Game.Net.CarLane carLaneData = this.m_CarLaneData[currentLaneData.m_Lane];
					float maxDriveSpeed = VehicleUtils.GetMaxDriveSpeed(prefabCarData, carLaneData);
					float num = math.select(currentLaneData.m_Distance / currentLaneData.m_Duration, maxDriveSpeed, currentLaneData.m_Duration == 0f) / prefabCarData.m_MaxSpeed;
					num = math.saturate(num * num);
					return math.lerp(vehicleSideEffectData.m_Min.z, vehicleSideEffectData.m_Max.z, num) * currentLaneData.m_Duration;
				}
				return 0f;
			}

			private void ApplySideEffects(ref CarCurrentLane currentLane, float speedLimitFactor, PrefabRef prefabRefData, CarData prefabCarData)
			{
				if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.ResetSpeed) != 0)
				{
					return;
				}
				if (this.m_CarLaneData.HasComponent(currentLane.m_Lane) && (currentLane.m_Duration != 0f || currentLane.m_Distance != 0f))
				{
					Game.Net.CarLane carLaneData = this.m_CarLaneData[currentLane.m_Lane];
					Curve curve = this.m_CurveData[currentLane.m_Lane];
					carLaneData.m_SpeedLimit *= speedLimitFactor;
					float maxDriveSpeed = VehicleUtils.GetMaxDriveSpeed(prefabCarData, carLaneData);
					float num = 1f / math.max(1f, curve.m_Length);
					float3 sideEffects = default(float3);
					if (this.m_PrefabSideEffectData.HasComponent(prefabRefData.m_Prefab))
					{
						VehicleSideEffectData vehicleSideEffectData = this.m_PrefabSideEffectData[prefabRefData.m_Prefab];
						float num2 = math.select(currentLane.m_Distance / currentLane.m_Duration, maxDriveSpeed, currentLane.m_Duration == 0f) / prefabCarData.m_MaxSpeed;
						num2 = math.saturate(num2 * num2);
						sideEffects = math.lerp(vehicleSideEffectData.m_Min, vehicleSideEffectData.m_Max, num2);
						float x = math.min(1f, currentLane.m_Distance * num);
						sideEffects *= new float3(x, currentLane.m_Duration, currentLane.m_Duration);
					}
					maxDriveSpeed = math.min(prefabCarData.m_MaxSpeed, carLaneData.m_SpeedLimit);
					float2 flow = new float2(currentLane.m_Duration * maxDriveSpeed, currentLane.m_Distance) * num;
					this.m_LaneEffects.Enqueue(new CarNavigationHelpers.LaneEffects(currentLane.m_Lane, sideEffects, flow));
				}
				currentLane.m_Duration = 0f;
				currentLane.m_Distance = 0f;
			}

			private bool AllowHighBeams(Game.Objects.Transform transform, Blocker blocker, ref CarCurrentLane currentLaneData, DynamicBuffer<CarNavigationLane> navigationLanes, float maxDistance, float minOffset)
			{
				if (blocker.m_Blocker != Entity.Null && this.m_TransformData.TryGetComponent(blocker.m_Blocker, out var componentData))
				{
					float3 @float = componentData.m_Position - transform.m_Position;
					if (math.lengthsq(@float) < maxDistance * maxDistance && math.dot(math.forward(transform.m_Rotation), @float) > minOffset && this.m_VehicleData.HasComponent(blocker.m_Blocker))
					{
						return false;
					}
				}
				float num = maxDistance - this.m_CurveData[currentLaneData.m_Lane].m_Length * math.abs(currentLaneData.m_CurvePosition.z - currentLaneData.m_CurvePosition.x);
				Entity entity = Entity.Null;
				if (this.m_OwnerData.TryGetComponent(currentLaneData.m_Lane, out var componentData2) && entity != componentData2.m_Owner)
				{
					if (!this.AllowHighBeams(transform, componentData2.m_Owner, maxDistance, minOffset))
					{
						return false;
					}
					entity = componentData2.m_Owner;
				}
				for (int i = 0; i < navigationLanes.Length; i++)
				{
					if (!(num > 0f))
					{
						break;
					}
					CarNavigationLane carNavigationLane = navigationLanes[i];
					if (!this.m_CarLaneData.HasComponent(carNavigationLane.m_Lane))
					{
						break;
					}
					if (this.m_OwnerData.TryGetComponent(carNavigationLane.m_Lane, out componentData2) && entity != componentData2.m_Owner)
					{
						if (!this.AllowHighBeams(transform, componentData2.m_Owner, maxDistance, minOffset))
						{
							return false;
						}
						entity = componentData2.m_Owner;
					}
					num -= this.m_CurveData[carNavigationLane.m_Lane].m_Length * math.abs(carNavigationLane.m_CurvePosition.y - carNavigationLane.m_CurvePosition.x);
				}
				return true;
			}

			private bool AllowHighBeams(Game.Objects.Transform transform, Entity owner, float maxDistance, float minOffset)
			{
				if (this.m_Lanes.TryGetBuffer(owner, out var bufferData))
				{
					float3 x = math.forward(transform.m_Rotation);
					maxDistance *= maxDistance;
					for (int i = 0; i < bufferData.Length; i++)
					{
						Game.Net.SubLane subLane = bufferData[i];
						if ((subLane.m_PathMethods & (PathMethod.Road | PathMethod.Track)) == 0 || !this.m_LaneObjects.TryGetBuffer(subLane.m_SubLane, out var bufferData2))
						{
							continue;
						}
						for (int j = 0; j < bufferData2.Length; j++)
						{
							LaneObject laneObject = bufferData2[j];
							if (this.m_TransformData.TryGetComponent(laneObject.m_LaneObject, out var componentData))
							{
								float3 @float = componentData.m_Position - transform.m_Position;
								if (math.lengthsq(@float) < maxDistance && math.dot(x, @float) > minOffset && this.m_VehicleData.HasComponent(laneObject.m_LaneObject))
								{
									return false;
								}
							}
						}
					}
				}
				return true;
			}

			private void ReserveNavigationLanes(ref Unity.Mathematics.Random random, int priority, Entity entity, CarData prefabCarData, ObjectGeometryData prefabObjectGeometryData, Car carData, ref CarNavigation navigationData, ref CarCurrentLane currentLaneData, DynamicBuffer<CarNavigationLane> navigationLanes)
			{
				float timeStep = 4f / 15f;
				if (!this.m_CarLaneData.HasComponent(currentLaneData.m_Lane))
				{
					return;
				}
				Curve curve = this.m_CurveData[currentLaneData.m_Lane];
				bool flag = currentLaneData.m_CurvePosition.z < currentLaneData.m_CurvePosition.x;
				float num = math.max(0f, VehicleUtils.GetBrakingDistance(prefabCarData, math.abs(navigationData.m_MaxSpeed), timeStep) - 0.01f);
				float num2 = num;
				float num3 = num2 / math.max(1E-06f, curve.m_Length) + 1E-06f;
				currentLaneData.m_CurvePosition.y = currentLaneData.m_CurvePosition.x + math.select(num3, 0f - num3, flag);
				num2 -= curve.m_Length * math.abs(currentLaneData.m_CurvePosition.z - currentLaneData.m_CurvePosition.x);
				int i = 0;
				if ((carData.m_Flags & CarFlags.Emergency) != 0 && num > 1f)
				{
					if (currentLaneData.m_ChangeLane != Entity.Null)
					{
						this.ReserveOtherLanesInGroup(currentLaneData.m_ChangeLane, 102);
					}
					else
					{
						this.ReserveOtherLanesInGroup(currentLaneData.m_Lane, 102);
					}
				}
				if ((currentLaneData.m_LaneFlags & Game.Vehicles.CarLaneFlags.RequestSpace) != 0 && this.m_LaneReservationData.HasComponent(currentLaneData.m_Lane))
				{
					this.m_LaneReservations.Enqueue(new CarNavigationHelpers.LaneReservation(currentLaneData.m_Lane, 0f, 96));
				}
				if (navigationLanes.Length > 0)
				{
					CarNavigationLane carNavigationLane = navigationLanes[0];
					if ((carNavigationLane.m_Flags & Game.Vehicles.CarLaneFlags.RequestSpace) != 0 && this.m_LaneReservationData.HasComponent(carNavigationLane.m_Lane))
					{
						this.m_LaneReservations.Enqueue(new CarNavigationHelpers.LaneReservation(carNavigationLane.m_Lane, 0f, 96));
					}
				}
				bool2 @bool = currentLaneData.m_CurvePosition.yz > currentLaneData.m_CurvePosition.zy;
				if (flag ? @bool.y : @bool.x)
				{
					currentLaneData.m_CurvePosition.y = currentLaneData.m_CurvePosition.z;
					while (i < navigationLanes.Length && num2 > 0f)
					{
						CarNavigationLane value = navigationLanes[i];
						if (!this.m_CarLaneData.HasComponent(value.m_Lane))
						{
							break;
						}
						curve = this.m_CurveData[value.m_Lane];
						if (this.m_LaneReservationData.HasComponent(value.m_Lane))
						{
							num3 = num2 / math.max(1E-06f, curve.m_Length);
							num3 = math.max(value.m_CurvePosition.x, math.min(value.m_CurvePosition.y, value.m_CurvePosition.x + num3));
							this.m_LaneReservations.Enqueue(new CarNavigationHelpers.LaneReservation(value.m_Lane, num3, priority));
						}
						if ((carData.m_Flags & CarFlags.Emergency) != 0)
						{
							this.ReserveOtherLanesInGroup(value.m_Lane, 102);
							if (this.m_LaneSignalData.HasComponent(value.m_Lane))
							{
								this.m_LaneSignals.Enqueue(new CarNavigationHelpers.LaneSignal(entity, value.m_Lane, priority));
							}
						}
						num2 -= curve.m_Length * math.abs(value.m_CurvePosition.y - value.m_CurvePosition.x);
						value.m_Flags |= Game.Vehicles.CarLaneFlags.Reserved;
						navigationLanes[i++] = value;
					}
				}
				if ((carData.m_Flags & CarFlags.Emergency) != 0)
				{
					num2 += num;
					if (random.NextInt(4) != 0)
					{
						num2 += prefabObjectGeometryData.m_Bounds.max.z + 1f;
					}
					for (; i < navigationLanes.Length; i++)
					{
						if (!(num2 > 0f))
						{
							break;
						}
						CarNavigationLane carNavigationLane2 = navigationLanes[i];
						if (this.m_CarLaneData.HasComponent(carNavigationLane2.m_Lane))
						{
							curve = this.m_CurveData[carNavigationLane2.m_Lane];
							if (this.m_LaneReservationData.HasComponent(carNavigationLane2.m_Lane))
							{
								this.m_LaneReservations.Enqueue(new CarNavigationHelpers.LaneReservation(carNavigationLane2.m_Lane, 0f, priority));
							}
							if (this.m_LaneSignalData.HasComponent(carNavigationLane2.m_Lane))
							{
								this.m_LaneSignals.Enqueue(new CarNavigationHelpers.LaneSignal(entity, carNavigationLane2.m_Lane, priority));
							}
							num2 -= curve.m_Length * math.abs(carNavigationLane2.m_CurvePosition.y - carNavigationLane2.m_CurvePosition.x);
							continue;
						}
						break;
					}
				}
				else
				{
					if ((currentLaneData.m_LaneFlags & Game.Vehicles.CarLaneFlags.Roundabout) == 0)
					{
						return;
					}
					num2 += num * 0.5f;
					if (random.NextInt(2) != 0)
					{
						num2 += prefabObjectGeometryData.m_Bounds.max.z + 1f;
					}
					for (; i < navigationLanes.Length; i++)
					{
						if (!(num2 > 0f))
						{
							break;
						}
						CarNavigationLane carNavigationLane3 = navigationLanes[i];
						if (this.m_CarLaneData.HasComponent(carNavigationLane3.m_Lane) && (carNavigationLane3.m_Flags & Game.Vehicles.CarLaneFlags.Roundabout) != 0)
						{
							curve = this.m_CurveData[carNavigationLane3.m_Lane];
							if (this.m_LaneReservationData.HasComponent(carNavigationLane3.m_Lane))
							{
								this.m_LaneReservations.Enqueue(new CarNavigationHelpers.LaneReservation(carNavigationLane3.m_Lane, 0f, priority));
							}
							num2 -= curve.m_Length * math.abs(carNavigationLane3.m_CurvePosition.y - carNavigationLane3.m_CurvePosition.x);
							continue;
						}
						break;
					}
				}
			}

			private void ReserveOtherLanesInGroup(Entity lane, int priority)
			{
				if (!this.m_SlaveLaneData.HasComponent(lane))
				{
					return;
				}
				SlaveLane slaveLane = this.m_SlaveLaneData[lane];
				Owner owner = this.m_OwnerData[lane];
				DynamicBuffer<Game.Net.SubLane> dynamicBuffer = this.m_Lanes[owner.m_Owner];
				int num = math.min(slaveLane.m_MaxIndex, dynamicBuffer.Length - 1);
				for (int i = slaveLane.m_MinIndex; i <= num; i++)
				{
					Entity subLane = dynamicBuffer[i].m_SubLane;
					if (subLane != lane && this.m_LaneReservationData.HasComponent(subLane))
					{
						this.m_LaneReservations.Enqueue(new CarNavigationHelpers.LaneReservation(subLane, 0f, priority));
					}
				}
			}

			private bool MoveAreaTarget(ref Unity.Mathematics.Random random, float3 comparePosition, PathOwner pathOwner, DynamicBuffer<CarNavigationLane> navigationLanes, DynamicBuffer<PathElement> pathElements, ref float3 targetPosition, float minDistance, Entity target, ref float3 curveDelta, float lanePosition, float navigationSize)
			{
				if ((pathOwner.m_State & (PathFlags.Pending | PathFlags.Obsolete | PathFlags.Updated)) != 0)
				{
					return true;
				}
				Entity owner = this.m_OwnerData[target].m_Owner;
				AreaLane areaLane = this.m_AreaLaneData[target];
				DynamicBuffer<Game.Areas.Node> nodes = this.m_AreaNodes[owner];
				int num = math.min(pathOwner.m_ElementIndex, pathElements.Length);
				NativeArray<PathElement> subArray = pathElements.AsNativeArray().GetSubArray(num, pathElements.Length - num);
				num = 0;
				bool flag = curveDelta.z < curveDelta.x;
				float lanePosition2 = math.select(lanePosition, 0f - lanePosition, flag);
				if (areaLane.m_Nodes.y == areaLane.m_Nodes.z)
				{
					float3 position = nodes[areaLane.m_Nodes.x].m_Position;
					float3 position2 = nodes[areaLane.m_Nodes.y].m_Position;
					float3 position3 = nodes[areaLane.m_Nodes.w].m_Position;
					if (VehicleUtils.SetTriangleTarget(position, position2, position3, comparePosition, num, navigationLanes, subArray, ref targetPosition, minDistance, lanePosition2, curveDelta.z, navigationSize, isSingle: true, this.m_TransformData, this.m_AreaLaneData, this.m_CurveData))
					{
						return true;
					}
					curveDelta.y = curveDelta.z;
				}
				else
				{
					bool4 @bool = new bool4(curveDelta.yz < 0.5f, curveDelta.yz > 0.5f);
					int2 @int = math.select(areaLane.m_Nodes.x, areaLane.m_Nodes.w, @bool.zw);
					float3 position4 = nodes[@int.x].m_Position;
					float3 position5 = nodes[areaLane.m_Nodes.y].m_Position;
					float3 position6 = nodes[areaLane.m_Nodes.z].m_Position;
					float3 position7 = nodes[@int.y].m_Position;
					if (math.any(@bool.xy & @bool.wz))
					{
						if (VehicleUtils.SetAreaTarget(position4, position4, position5, position6, position7, owner, nodes, comparePosition, num, navigationLanes, subArray, ref targetPosition, minDistance, lanePosition2, curveDelta.z, navigationSize, flag, this.m_TransformData, this.m_AreaLaneData, this.m_CurveData, this.m_OwnerData))
						{
							return true;
						}
						curveDelta.y = 0.5f;
						@bool.xz = false;
					}
					if (VehicleUtils.GetPathElement(num, navigationLanes, subArray, out var pathElement) && this.m_OwnerData.TryGetComponent(pathElement.m_Target, out var componentData) && componentData.m_Owner == owner)
					{
						bool4 bool2 = new bool4(pathElement.m_TargetDelta < 0.5f, pathElement.m_TargetDelta > 0.5f);
						if (math.any(!@bool.xz) & math.any(@bool.yw) & math.any(bool2.xy & bool2.wz))
						{
							AreaLane areaLane2 = this.m_AreaLaneData[pathElement.m_Target];
							@int = math.select(areaLane2.m_Nodes.x, areaLane2.m_Nodes.w, bool2.zw);
							position4 = nodes[@int.x].m_Position;
							float3 prev = math.select(position5, position6, position4.Equals(position5));
							position5 = nodes[areaLane2.m_Nodes.y].m_Position;
							position6 = nodes[areaLane2.m_Nodes.z].m_Position;
							position7 = nodes[@int.y].m_Position;
							bool flag2 = pathElement.m_TargetDelta.y < pathElement.m_TargetDelta.x;
							if (VehicleUtils.SetAreaTarget(lanePosition: math.select(lanePosition, 0f - lanePosition, flag2), prev2: prev, prev: position4, left: position5, right: position6, next: position7, areaEntity: owner, nodes: nodes, comparePosition: comparePosition, elementIndex: num + 1, navigationLanes: navigationLanes, pathElements: subArray, targetPosition: ref targetPosition, minDistance: minDistance, curveDelta: pathElement.m_TargetDelta.y, navigationSize: navigationSize, isBackward: flag2, transforms: this.m_TransformData, areaLanes: this.m_AreaLaneData, curves: this.m_CurveData, owners: this.m_OwnerData))
							{
								return true;
							}
						}
						curveDelta.y = curveDelta.z;
						return false;
					}
					if (VehicleUtils.SetTriangleTarget(position5, position6, position7, comparePosition, num, navigationLanes, subArray, ref targetPosition, minDistance, lanePosition2, curveDelta.z, navigationSize, isSingle: false, this.m_TransformData, this.m_AreaLaneData, this.m_CurveData))
					{
						return true;
					}
					curveDelta.y = curveDelta.z;
				}
				return math.distance(comparePosition.xz, targetPosition.xz) >= minDistance;
			}

			private bool MoveTarget(float3 comparePosition, ref float3 targetPosition, float minDistance, Entity target)
			{
				if (VehicleUtils.CalculateTransformPosition(ref targetPosition, target, this.m_TransformData, this.m_PositionData, this.m_PrefabRefData, this.m_PrefabBuildingData))
				{
					return math.distance(comparePosition, targetPosition) >= minDistance;
				}
				return false;
			}

			private bool MoveTarget(float3 comparePosition, ref float3 targetPosition, float minDistance, Bezier4x3 curve, ref float3 curveDelta, float laneOffset)
			{
				float3 lanePosition = VehicleUtils.GetLanePosition(curve, curveDelta.z, laneOffset);
				if (math.distance(comparePosition, lanePosition) < minDistance)
				{
					curveDelta.x = curveDelta.z;
					targetPosition = lanePosition;
					return false;
				}
				float2 xz = curveDelta.xz;
				for (int i = 0; i < 8; i++)
				{
					float num = math.lerp(xz.x, xz.y, 0.5f);
					float3 lanePosition2 = VehicleUtils.GetLanePosition(curve, num, laneOffset);
					if (math.distance(comparePosition, lanePosition2) < minDistance)
					{
						xz.x = num;
					}
					else
					{
						xz.y = num;
					}
				}
				curveDelta.x = xz.y;
				targetPosition = VehicleUtils.GetLanePosition(curve, xz.y, laneOffset);
				return true;
			}

			private bool MoveTarget(float3 comparePosition, ref float3 targetPosition, float minDistance, Bezier4x3 curve1, Bezier4x3 curve2, float curveSelect, ref float3 curveDelta, float laneOffset1, float laneOffset2)
			{
				curveSelect = math.saturate(curveSelect);
				float3 lanePosition = VehicleUtils.GetLanePosition(curve1, curveDelta.z, laneOffset1);
				float3 lanePosition2 = VehicleUtils.GetLanePosition(curve2, curveDelta.z, laneOffset2);
				if (MathUtils.Distance(new Line3.Segment(lanePosition, lanePosition2), comparePosition, out var t) < minDistance)
				{
					curveDelta.x = curveDelta.z;
					targetPosition = math.lerp(lanePosition, lanePosition2, curveSelect);
					return false;
				}
				float2 xz = curveDelta.xz;
				for (int i = 0; i < 8; i++)
				{
					float num = math.lerp(xz.x, xz.y, 0.5f);
					float3 lanePosition3 = VehicleUtils.GetLanePosition(curve1, num, laneOffset1);
					float3 lanePosition4 = VehicleUtils.GetLanePosition(curve2, num, laneOffset2);
					if (MathUtils.Distance(new Line3.Segment(lanePosition3, lanePosition4), comparePosition, out t) < minDistance)
					{
						xz.x = num;
					}
					else
					{
						xz.y = num;
					}
				}
				curveDelta.x = xz.y;
				float3 lanePosition5 = VehicleUtils.GetLanePosition(curve1, xz.y, laneOffset1);
				float3 lanePosition6 = VehicleUtils.GetLanePosition(curve2, xz.y, laneOffset2);
				targetPosition = math.lerp(lanePosition5, lanePosition6, curveSelect);
				return true;
			}

			void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				this.Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
			}
		}

		[BurstCompile]
		private struct UpdateLaneSignalsJob : IJob
		{
			public NativeQueue<CarNavigationHelpers.LaneSignal> m_LaneSignalQueue;

			public ComponentLookup<LaneSignal> m_LaneSignalData;

			public void Execute()
			{
				CarNavigationHelpers.LaneSignal item;
				while (this.m_LaneSignalQueue.TryDequeue(out item))
				{
					LaneSignal value = this.m_LaneSignalData[item.m_Lane];
					if (item.m_Priority > value.m_Priority)
					{
						value.m_Petitioner = item.m_Petitioner;
						value.m_Priority = item.m_Priority;
						this.m_LaneSignalData[item.m_Lane] = value;
					}
				}
			}
		}

		[BurstCompile]
		private struct UpdateLaneReservationsJob : IJob
		{
			public NativeQueue<CarNavigationHelpers.LaneReservation> m_LaneReservationQueue;

			public ComponentLookup<LaneReservation> m_LaneReservationData;

			public void Execute()
			{
				CarNavigationHelpers.LaneReservation item;
				while (this.m_LaneReservationQueue.TryDequeue(out item))
				{
					ref LaneReservation valueRW = ref this.m_LaneReservationData.GetRefRW(item.m_Lane).ValueRW;
					if (item.m_Offset > valueRW.m_Next.m_Offset)
					{
						valueRW.m_Next.m_Offset = item.m_Offset;
					}
					if (item.m_Priority > valueRW.m_Next.m_Priority)
					{
						if (item.m_Priority >= valueRW.m_Prev.m_Priority)
						{
							valueRW.m_Blocker = Entity.Null;
						}
						valueRW.m_Next.m_Priority = item.m_Priority;
					}
				}
			}
		}

		public struct TrafficAmbienceEffect
		{
			public float3 m_Position;

			public float m_Amount;
		}

		[BurstCompile]
		private struct ApplyTrafficAmbienceJob : IJob
		{
			public NativeQueue<TrafficAmbienceEffect> m_EffectsQueue;

			public NativeArray<TrafficAmbienceCell> m_TrafficAmbienceMap;

			public void Execute()
			{
				TrafficAmbienceEffect item;
				while (this.m_EffectsQueue.TryDequeue(out item))
				{
					int2 cell = CellMapSystemRe.GetCell(item.m_Position, CellMapSystemRe.kMapSize, TrafficAmbienceSystem.kTextureSize);
					if (cell.x >= 0 && cell.y >= 0 && cell.x < TrafficAmbienceSystem.kTextureSize && cell.y < TrafficAmbienceSystem.kTextureSize)
					{
						int index = cell.x + cell.y * TrafficAmbienceSystem.kTextureSize;
						TrafficAmbienceCell value = this.m_TrafficAmbienceMap[index];
						value.m_Accumulator += item.m_Amount;
						this.m_TrafficAmbienceMap[index] = value;
					}
				}
			}
		}

		[BurstCompile]
		private struct ApplyLaneEffectsJob : IJob
		{
			[ReadOnly]
			public ComponentLookup<Owner> m_OwnerData;

			[ReadOnly]
			public ComponentLookup<PrefabRef> m_PrefabRefData;

			[ReadOnly]
			public ComponentLookup<LaneDeteriorationData> m_LaneDeteriorationData;

			public ComponentLookup<Game.Net.Pollution> m_PollutionData;

			public ComponentLookup<LaneCondition> m_LaneConditionData;

			public ComponentLookup<LaneFlow> m_LaneFlowData;

			public NativeQueue<CarNavigationHelpers.LaneEffects> m_LaneEffectsQueue;

			public void Execute()
			{
				CarNavigationHelpers.LaneEffects item;
				while (this.m_LaneEffectsQueue.TryDequeue(out item))
				{
					Entity owner = this.m_OwnerData[item.m_Lane].m_Owner;
					if (this.m_LaneConditionData.HasComponent(item.m_Lane))
					{
						PrefabRef prefabRef = this.m_PrefabRefData[item.m_Lane];
						LaneDeteriorationData laneDeteriorationData = this.m_LaneDeteriorationData[prefabRef.m_Prefab];
						LaneCondition value = this.m_LaneConditionData[item.m_Lane];
						value.m_Wear = math.min(value.m_Wear + item.m_SideEffects.x * laneDeteriorationData.m_TrafficFactor, 10f);
						this.m_LaneConditionData[item.m_Lane] = value;
					}
					if (this.m_LaneFlowData.HasComponent(item.m_Lane))
					{
						LaneFlow value2 = this.m_LaneFlowData[item.m_Lane];
						value2.m_Next += item.m_Flow;
						this.m_LaneFlowData[item.m_Lane] = value2;
					}
					if (this.m_PollutionData.HasComponent(owner))
					{
						Game.Net.Pollution value3 = this.m_PollutionData[owner];
						value3.m_Pollution += item.m_SideEffects.yz;
						this.m_PollutionData[owner] = value3;
					}
				}
			}
		}

		private struct TypeHandle
		{
			[ReadOnly]
			public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<Moving> __Game_Objects_Moving_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<Target> __Game_Common_Target_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<Car> __Game_Vehicles_Car_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<OutOfControl> __Game_Vehicles_OutOfControl_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<PseudoRandomSeed> __Game_Common_PseudoRandomSeed_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

			[ReadOnly]
			public BufferTypeHandle<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferTypeHandle;

			public ComponentTypeHandle<CarNavigation> __Game_Vehicles_CarNavigation_RW_ComponentTypeHandle;

			public ComponentTypeHandle<CarCurrentLane> __Game_Vehicles_CarCurrentLane_RW_ComponentTypeHandle;

			public ComponentTypeHandle<PathOwner> __Game_Pathfind_PathOwner_RW_ComponentTypeHandle;

			public ComponentTypeHandle<Blocker> __Game_Vehicles_Blocker_RW_ComponentTypeHandle;

			public ComponentTypeHandle<Odometer> __Game_Vehicles_Odometer_RW_ComponentTypeHandle;

			public BufferTypeHandle<CarNavigationLane> __Game_Vehicles_CarNavigationLane_RW_BufferTypeHandle;

			public BufferTypeHandle<PathElement> __Game_Pathfind_PathElement_RW_BufferTypeHandle;

			[ReadOnly]
			public EntityStorageInfoLookup __EntityStorageInfoLookup;

			[ReadOnly]
			public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<Unspawned> __Game_Objects_Unspawned_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<Lane> __Game_Net_Lane_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<Game.Net.CarLane> __Game_Net_CarLane_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<Game.Net.ParkingLane> __Game_Net_ParkingLane_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<Game.Net.ConnectionLane> __Game_Net_ConnectionLane_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<MasterLane> __Game_Net_MasterLane_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<SlaveLane> __Game_Net_SlaveLane_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<AreaLane> __Game_Net_AreaLane_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<LaneReservation> __Game_Net_LaneReservation_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<LaneCondition> __Game_Net_LaneCondition_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<LaneSignal> __Game_Net_LaneSignal_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<Road> __Game_Net_Road_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<Position> __Game_Routes_Position_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<Moving> __Game_Objects_Moving_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<Car> __Game_Vehicles_Car_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<Train> __Game_Vehicles_Train_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<Controller> __Game_Vehicles_Controller_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<Vehicle> __Game_Vehicles_Vehicle_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<Creature> __Game_Creatures_Creature_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<CarData> __Game_Prefabs_CarData_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<TrainData> __Game_Prefabs_TrainData_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<VehicleSideEffectData> __Game_Prefabs_VehicleSideEffectData_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<NetLaneData> __Game_Prefabs_NetLaneData_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<CarLaneData> __Game_Prefabs_CarLaneData_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<ParkingLaneData> __Game_Prefabs_ParkingLaneData_RO_ComponentLookup;

			[ReadOnly]
			public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

			[ReadOnly]
			public BufferLookup<LaneObject> __Game_Net_LaneObject_RO_BufferLookup;

			[ReadOnly]
			public BufferLookup<LaneOverlap> __Game_Net_LaneOverlap_RO_BufferLookup;

			[ReadOnly]
			public BufferLookup<Game.Areas.Node> __Game_Areas_Node_RO_BufferLookup;

			[ReadOnly]
			public BufferLookup<Triangle> __Game_Areas_Triangle_RO_BufferLookup;

			public ComponentLookup<CarTrailerLane> __Game_Vehicles_CarTrailerLane_RW_ComponentLookup;

			public BufferLookup<BlockedLane> __Game_Objects_BlockedLane_RW_BufferLookup;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void __AssignHandles(ref SystemState state)
			{
				this.__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
				this.__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Transform>(isReadOnly: true);
				this.__Game_Objects_Moving_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Moving>(isReadOnly: true);
				this.__Game_Common_Target_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Target>(isReadOnly: true);
				this.__Game_Vehicles_Car_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Car>(isReadOnly: true);
				this.__Game_Vehicles_OutOfControl_RO_ComponentTypeHandle = state.GetComponentTypeHandle<OutOfControl>(isReadOnly: true);
				this.__Game_Common_PseudoRandomSeed_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PseudoRandomSeed>(isReadOnly: true);
				this.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
				this.__Game_Vehicles_LayoutElement_RO_BufferTypeHandle = state.GetBufferTypeHandle<LayoutElement>(isReadOnly: true);
				this.__Game_Vehicles_CarNavigation_RW_ComponentTypeHandle = state.GetComponentTypeHandle<CarNavigation>();
				this.__Game_Vehicles_CarCurrentLane_RW_ComponentTypeHandle = state.GetComponentTypeHandle<CarCurrentLane>();
				this.__Game_Pathfind_PathOwner_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PathOwner>();
				this.__Game_Vehicles_Blocker_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Blocker>();
				this.__Game_Vehicles_Odometer_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Odometer>();
				this.__Game_Vehicles_CarNavigationLane_RW_BufferTypeHandle = state.GetBufferTypeHandle<CarNavigationLane>();
				this.__Game_Pathfind_PathElement_RW_BufferTypeHandle = state.GetBufferTypeHandle<PathElement>();
				this.__EntityStorageInfoLookup = state.GetEntityStorageInfoLookup();
				this.__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
				this.__Game_Objects_Unspawned_RO_ComponentLookup = state.GetComponentLookup<Unspawned>(isReadOnly: true);
				this.__Game_Net_Lane_RO_ComponentLookup = state.GetComponentLookup<Lane>(isReadOnly: true);
				this.__Game_Net_CarLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.CarLane>(isReadOnly: true);
				this.__Game_Net_ParkingLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ParkingLane>(isReadOnly: true);
				this.__Game_Net_ConnectionLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ConnectionLane>(isReadOnly: true);
				this.__Game_Net_MasterLane_RO_ComponentLookup = state.GetComponentLookup<MasterLane>(isReadOnly: true);
				this.__Game_Net_SlaveLane_RO_ComponentLookup = state.GetComponentLookup<SlaveLane>(isReadOnly: true);
				this.__Game_Net_AreaLane_RO_ComponentLookup = state.GetComponentLookup<AreaLane>(isReadOnly: true);
				this.__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
				this.__Game_Net_LaneReservation_RO_ComponentLookup = state.GetComponentLookup<LaneReservation>(isReadOnly: true);
				this.__Game_Net_LaneCondition_RO_ComponentLookup = state.GetComponentLookup<LaneCondition>(isReadOnly: true);
				this.__Game_Net_LaneSignal_RO_ComponentLookup = state.GetComponentLookup<LaneSignal>(isReadOnly: true);
				this.__Game_Net_Road_RO_ComponentLookup = state.GetComponentLookup<Road>(isReadOnly: true);
				this.__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
				this.__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
				this.__Game_Routes_Position_RO_ComponentLookup = state.GetComponentLookup<Position>(isReadOnly: true);
				this.__Game_Objects_Moving_RO_ComponentLookup = state.GetComponentLookup<Moving>(isReadOnly: true);
				this.__Game_Vehicles_Car_RO_ComponentLookup = state.GetComponentLookup<Car>(isReadOnly: true);
				this.__Game_Vehicles_Train_RO_ComponentLookup = state.GetComponentLookup<Train>(isReadOnly: true);
				this.__Game_Vehicles_Controller_RO_ComponentLookup = state.GetComponentLookup<Controller>(isReadOnly: true);
				this.__Game_Vehicles_Vehicle_RO_ComponentLookup = state.GetComponentLookup<Vehicle>(isReadOnly: true);
				this.__Game_Creatures_Creature_RO_ComponentLookup = state.GetComponentLookup<Creature>(isReadOnly: true);
				this.__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
				this.__Game_Prefabs_CarData_RO_ComponentLookup = state.GetComponentLookup<CarData>(isReadOnly: true);
				this.__Game_Prefabs_TrainData_RO_ComponentLookup = state.GetComponentLookup<TrainData>(isReadOnly: true);
				this.__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
				this.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
				this.__Game_Prefabs_VehicleSideEffectData_RO_ComponentLookup = state.GetComponentLookup<VehicleSideEffectData>(isReadOnly: true);
				this.__Game_Prefabs_NetLaneData_RO_ComponentLookup = state.GetComponentLookup<NetLaneData>(isReadOnly: true);
				this.__Game_Prefabs_CarLaneData_RO_ComponentLookup = state.GetComponentLookup<CarLaneData>(isReadOnly: true);
				this.__Game_Prefabs_ParkingLaneData_RO_ComponentLookup = state.GetComponentLookup<ParkingLaneData>(isReadOnly: true);
				this.__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
				this.__Game_Net_LaneObject_RO_BufferLookup = state.GetBufferLookup<LaneObject>(isReadOnly: true);
				this.__Game_Net_LaneOverlap_RO_BufferLookup = state.GetBufferLookup<LaneOverlap>(isReadOnly: true);
				this.__Game_Areas_Node_RO_BufferLookup = state.GetBufferLookup<Game.Areas.Node>(isReadOnly: true);
				this.__Game_Areas_Triangle_RO_BufferLookup = state.GetBufferLookup<Triangle>(isReadOnly: true);
				this.__Game_Vehicles_CarTrailerLane_RW_ComponentLookup = state.GetComponentLookup<CarTrailerLane>();
				this.__Game_Objects_BlockedLane_RW_BufferLookup = state.GetBufferLookup<BlockedLane>();
			}
		}

		private SimulationSystem m_SimulationSystem;

		private TerrainSystem m_TerrainSystem;

		private Game.Net.SearchSystem m_NetSearchSystem;

		private Game.Areas.SearchSystem m_AreaSearchSystem;

		private Game.Objects.SearchSystem m_ObjectSearchSystem;

		private CityConfigurationSystem m_CityConfigurationSystem;

		private Actions m_Actions;

		private EntityQuery m_VehicleQuery;

		private TypeHandle __TypeHandle;

		[Preserve]
		protected override void OnCreate()
		{
			base.OnCreate();
			this.m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
			this.m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
			this.m_NetSearchSystem = base.World.GetOrCreateSystemManaged<Game.Net.SearchSystem>();
			this.m_AreaSearchSystem = base.World.GetOrCreateSystemManaged<Game.Areas.SearchSystem>();
			this.m_ObjectSearchSystem = base.World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
			this.m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
			this.m_Actions = base.World.GetOrCreateSystemManaged<Actions>();
			this.m_VehicleQuery = base.GetEntityQuery(ComponentType.ReadOnly<Car>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.ReadWrite<CarCurrentLane>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<TripSource>(), ComponentType.Exclude<ParkedCar>());
		}

		[Preserve]
		protected override void OnUpdate()
		{
			uint index = this.m_SimulationSystem.frameIndex % 16u;
			this.m_VehicleQuery.ResetFilter();
			this.m_VehicleQuery.SetSharedComponentFilter(new UpdateFrame(index));
			this.m_Actions.m_LaneReservationQueue = new NativeQueue<CarNavigationHelpers.LaneReservation>(Allocator.TempJob);
			this.m_Actions.m_LaneEffectsQueue = new NativeQueue<CarNavigationHelpers.LaneEffects>(Allocator.TempJob);
			this.m_Actions.m_LaneSignalQueue = new NativeQueue<CarNavigationHelpers.LaneSignal>(Allocator.TempJob);
			this.m_Actions.m_TrafficAmbienceQueue = new NativeQueue<TrafficAmbienceEffect>(Allocator.TempJob);
			this.__TypeHandle.__Game_Objects_BlockedLane_RW_BufferLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Vehicles_CarTrailerLane_RW_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Areas_Triangle_RO_BufferLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Areas_Node_RO_BufferLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_LaneOverlap_RO_BufferLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_LaneObject_RO_BufferLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_SubLane_RO_BufferLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_ParkingLaneData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_CarLaneData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_NetLaneData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_VehicleSideEffectData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_TrainData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_CarData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Creatures_Creature_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Vehicles_Vehicle_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Vehicles_Controller_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Vehicles_Train_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Vehicles_Car_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Objects_Moving_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Routes_Position_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_Road_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_LaneSignal_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_LaneCondition_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_LaneReservation_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_Curve_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_AreaLane_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_SlaveLane_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_MasterLane_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_ConnectionLane_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_ParkingLane_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_CarLane_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_Lane_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Objects_Unspawned_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Common_Owner_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__EntityStorageInfoLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Pathfind_PathElement_RW_BufferTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Vehicles_CarNavigationLane_RW_BufferTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Vehicles_Odometer_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Vehicles_Blocker_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Pathfind_PathOwner_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Vehicles_CarCurrentLane_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Vehicles_CarNavigation_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Common_PseudoRandomSeed_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Vehicles_OutOfControl_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Vehicles_Car_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Common_Target_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Objects_Moving_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
			UpdateNavigationJob jobData = default(UpdateNavigationJob);
			jobData.m_EntityType = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle;
			jobData.m_TransformType = this.__TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle;
			jobData.m_MovingType = this.__TypeHandle.__Game_Objects_Moving_RO_ComponentTypeHandle;
			jobData.m_TargetType = this.__TypeHandle.__Game_Common_Target_RO_ComponentTypeHandle;
			jobData.m_CarType = this.__TypeHandle.__Game_Vehicles_Car_RO_ComponentTypeHandle;
			jobData.m_OutOfControlType = this.__TypeHandle.__Game_Vehicles_OutOfControl_RO_ComponentTypeHandle;
			jobData.m_PseudoRandomSeedType = this.__TypeHandle.__Game_Common_PseudoRandomSeed_RO_ComponentTypeHandle;
			jobData.m_PrefabRefType = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;
			jobData.m_LayoutElementType = this.__TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferTypeHandle;
			jobData.m_NavigationType = this.__TypeHandle.__Game_Vehicles_CarNavigation_RW_ComponentTypeHandle;
			jobData.m_CurrentLaneType = this.__TypeHandle.__Game_Vehicles_CarCurrentLane_RW_ComponentTypeHandle;
			jobData.m_PathOwnerType = this.__TypeHandle.__Game_Pathfind_PathOwner_RW_ComponentTypeHandle;
			jobData.m_BlockerType = this.__TypeHandle.__Game_Vehicles_Blocker_RW_ComponentTypeHandle;
			jobData.m_OdometerType = this.__TypeHandle.__Game_Vehicles_Odometer_RW_ComponentTypeHandle;
			jobData.m_NavigationLaneType = this.__TypeHandle.__Game_Vehicles_CarNavigationLane_RW_BufferTypeHandle;
			jobData.m_PathElementType = this.__TypeHandle.__Game_Pathfind_PathElement_RW_BufferTypeHandle;
			jobData.m_EntityStorageInfoLookup = this.__TypeHandle.__EntityStorageInfoLookup;
			jobData.m_OwnerData = this.__TypeHandle.__Game_Common_Owner_RO_ComponentLookup;
			jobData.m_UnspawnedData = this.__TypeHandle.__Game_Objects_Unspawned_RO_ComponentLookup;
			jobData.m_LaneData = this.__TypeHandle.__Game_Net_Lane_RO_ComponentLookup;
			jobData.m_CarLaneData = this.__TypeHandle.__Game_Net_CarLane_RO_ComponentLookup;
			jobData.m_ParkingLaneData = this.__TypeHandle.__Game_Net_ParkingLane_RO_ComponentLookup;
			jobData.m_ConnectionLaneData = this.__TypeHandle.__Game_Net_ConnectionLane_RO_ComponentLookup;
			jobData.m_MasterLaneData = this.__TypeHandle.__Game_Net_MasterLane_RO_ComponentLookup;
			jobData.m_SlaveLaneData = this.__TypeHandle.__Game_Net_SlaveLane_RO_ComponentLookup;
			jobData.m_AreaLaneData = this.__TypeHandle.__Game_Net_AreaLane_RO_ComponentLookup;
			jobData.m_CurveData = this.__TypeHandle.__Game_Net_Curve_RO_ComponentLookup;
			jobData.m_LaneReservationData = this.__TypeHandle.__Game_Net_LaneReservation_RO_ComponentLookup;
			jobData.m_LaneConditionData = this.__TypeHandle.__Game_Net_LaneCondition_RO_ComponentLookup;
			jobData.m_LaneSignalData = this.__TypeHandle.__Game_Net_LaneSignal_RO_ComponentLookup;
			jobData.m_RoadData = this.__TypeHandle.__Game_Net_Road_RO_ComponentLookup;
			jobData.m_PropertyRenterData = this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup;
			jobData.m_TransformData = this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup;
			jobData.m_PositionData = this.__TypeHandle.__Game_Routes_Position_RO_ComponentLookup;
			jobData.m_MovingData = this.__TypeHandle.__Game_Objects_Moving_RO_ComponentLookup;
			jobData.m_CarData = this.__TypeHandle.__Game_Vehicles_Car_RO_ComponentLookup;
			jobData.m_TrainData = this.__TypeHandle.__Game_Vehicles_Train_RO_ComponentLookup;
			jobData.m_ControllerData = this.__TypeHandle.__Game_Vehicles_Controller_RO_ComponentLookup;
			jobData.m_VehicleData = this.__TypeHandle.__Game_Vehicles_Vehicle_RO_ComponentLookup;
			jobData.m_CreatureData = this.__TypeHandle.__Game_Creatures_Creature_RO_ComponentLookup;
			jobData.m_PrefabRefData = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup;
			jobData.m_PrefabCarData = this.__TypeHandle.__Game_Prefabs_CarData_RO_ComponentLookup;
			jobData.m_PrefabTrainData = this.__TypeHandle.__Game_Prefabs_TrainData_RO_ComponentLookup;
			jobData.m_PrefabBuildingData = this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup;
			jobData.m_PrefabObjectGeometryData = this.__TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;
			jobData.m_PrefabSideEffectData = this.__TypeHandle.__Game_Prefabs_VehicleSideEffectData_RO_ComponentLookup;
			jobData.m_PrefabLaneData = this.__TypeHandle.__Game_Prefabs_NetLaneData_RO_ComponentLookup;
			jobData.m_PrefabCarLaneData = this.__TypeHandle.__Game_Prefabs_CarLaneData_RO_ComponentLookup;
			jobData.m_PrefabParkingLaneData = this.__TypeHandle.__Game_Prefabs_ParkingLaneData_RO_ComponentLookup;
			jobData.m_Lanes = this.__TypeHandle.__Game_Net_SubLane_RO_BufferLookup;
			jobData.m_LaneObjects = this.__TypeHandle.__Game_Net_LaneObject_RO_BufferLookup;
			jobData.m_LaneOverlaps = this.__TypeHandle.__Game_Net_LaneOverlap_RO_BufferLookup;
			jobData.m_AreaNodes = this.__TypeHandle.__Game_Areas_Node_RO_BufferLookup;
			jobData.m_AreaTriangles = this.__TypeHandle.__Game_Areas_Triangle_RO_BufferLookup;
			jobData.m_TrailerLaneData = this.__TypeHandle.__Game_Vehicles_CarTrailerLane_RW_ComponentLookup;
			jobData.m_BlockedLanes = this.__TypeHandle.__Game_Objects_BlockedLane_RW_BufferLookup;
			jobData.m_RandomSeed = RandomSeed.Next();
			jobData.m_SimulationFrame = this.m_SimulationSystem.frameIndex;
			jobData.m_LeftHandTraffic = this.m_CityConfigurationSystem.leftHandTraffic;
			jobData.m_NetSearchTree = this.m_NetSearchSystem.GetNetSearchTree(readOnly: true, out var dependencies);
			jobData.m_AreaSearchTree = this.m_AreaSearchSystem.GetSearchTree(readOnly: true, out var dependencies2);
			jobData.m_StaticObjectSearchTree = this.m_ObjectSearchSystem.GetStaticSearchTree(readOnly: true, out var dependencies3);
			jobData.m_MovingObjectSearchTree = this.m_ObjectSearchSystem.GetMovingSearchTree(readOnly: true, out var dependencies4);
			jobData.m_TerrainHeightData = this.m_TerrainSystem.GetHeightData();
			jobData.m_LaneObjectBuffer = this.m_Actions.m_LaneObjectUpdater.Begin(Allocator.TempJob);
			jobData.m_LaneReservations = this.m_Actions.m_LaneReservationQueue.AsParallelWriter();
			jobData.m_LaneEffects = this.m_Actions.m_LaneEffectsQueue.AsParallelWriter();
			jobData.m_LaneSignals = this.m_Actions.m_LaneSignalQueue.AsParallelWriter();
			jobData.m_TrafficAmbienceEffects = this.m_Actions.m_TrafficAmbienceQueue.AsParallelWriter();
			JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData, this.m_VehicleQuery, JobUtils.CombineDependencies(base.Dependency, dependencies, dependencies2, dependencies3, dependencies4));
			this.m_NetSearchSystem.AddNetSearchTreeReader(jobHandle);
			this.m_AreaSearchSystem.AddSearchTreeReader(jobHandle);
			this.m_ObjectSearchSystem.AddStaticSearchTreeReader(jobHandle);
			this.m_ObjectSearchSystem.AddMovingSearchTreeReader(jobHandle);
			this.m_TerrainSystem.AddCPUHeightReader(jobHandle);
			this.m_Actions.m_Dependency = jobHandle;
			base.Dependency = jobHandle;
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
		public CarNavigationSystem()
		{
		}
	}
}

using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Economy;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Game.Zones;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;
using Game;
using Game.Simulation;

namespace MapExt.Systems
{
	//[CompilerGenerated]
	public partial class ZoneSpawnSystem : GameSystemBase
	{
		public struct SpawnLocation
		{
			public Entity m_Entity;

			public Entity m_Building;

			public int4 m_LotArea;

			public float m_Priority;

			public ZoneType m_ZoneType;

			public Game.Zones.AreaType m_AreaType;

			public LotFlags m_LotFlags;
		}

		[BurstCompile]
		public struct EvaluateSpawnAreas : IJobChunk
		{
			[ReadOnly]
			public NativeList<ArchetypeChunk> m_BuildingChunks;

			[ReadOnly]
			public ZonePrefabs m_ZonePrefabs;

			[ReadOnly]
			public ZonePreferenceData m_Preferences;

			[ReadOnly]
			public int m_SpawnResidential;

			[ReadOnly]
			public int m_SpawnCommercial;

			[ReadOnly]
			public int m_SpawnIndustrial;

			[ReadOnly]
			public int m_SpawnStorage;

			[ReadOnly]
			public int m_MinDemand;

			public int3 m_ResidentialDemands;

			[ReadOnly]
			public NativeArray<int> m_CommercialBuildingDemands;

			[ReadOnly]
			public NativeArray<int> m_IndustrialDemands;

			[ReadOnly]
			public NativeArray<int> m_StorageDemands;

			[ReadOnly]
			public RandomSeed m_RandomSeed;

			[ReadOnly]
			public EntityTypeHandle m_EntityType;

			[ReadOnly]
			public ComponentTypeHandle<Block> m_BlockType;

			[ReadOnly]
			public ComponentTypeHandle<Owner> m_OwnerType;

			[ReadOnly]
			public ComponentTypeHandle<CurvePosition> m_CurvePositionType;

			[ReadOnly]
			public BufferTypeHandle<VacantLot> m_VacantLotType;

			[ReadOnly]
			public ComponentTypeHandle<BuildingData> m_BuildingDataType;

			[ReadOnly]
			public ComponentTypeHandle<SpawnableBuildingData> m_SpawnableBuildingType;

			[ReadOnly]
			public ComponentTypeHandle<BuildingPropertyData> m_BuildingPropertyType;

			[ReadOnly]
			public ComponentTypeHandle<ObjectGeometryData> m_ObjectGeometryType;

			[ReadOnly]
			public SharedComponentTypeHandle<BuildingSpawnGroupData> m_BuildingSpawnGroupType;

			[ReadOnly]
			public ComponentTypeHandle<WarehouseData> m_WarehouseType;

			[ReadOnly]
			public ComponentLookup<ZoneData> m_ZoneData;

			[ReadOnly]
			public BufferLookup<ResourceAvailability> m_Availabilities;

			[ReadOnly]
			public NativeList<IndustrialProcessData> m_Processes;

			[ReadOnly]
			public BufferLookup<ProcessEstimate> m_ProcessEstimates;

			[ReadOnly]
			public ComponentLookup<LandValue> m_LandValues;

			[ReadOnly]
			public ComponentLookup<Block> m_BlockData;

			[ReadOnly]
			public ComponentLookup<ResourceData> m_ResourceDatas;

			[ReadOnly]
			public ResourcePrefabs m_ResourcePrefabs;

			[ReadOnly]
			public NativeArray<GroundPollution> m_PollutionMap;

			public NativeQueue<SpawnLocation>.ParallelWriter m_Residential;

			public NativeQueue<SpawnLocation>.ParallelWriter m_Commercial;

			public NativeQueue<SpawnLocation>.ParallelWriter m_Industrial;

			public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				Random random = this.m_RandomSeed.GetRandom(unfilteredChunkIndex);
				SpawnLocation bestLocation = default(SpawnLocation);
				SpawnLocation bestLocation2 = default(SpawnLocation);
				SpawnLocation bestLocation3 = default(SpawnLocation);
				NativeArray<Entity> nativeArray = chunk.GetNativeArray(this.m_EntityType);
				BufferAccessor<VacantLot> bufferAccessor = chunk.GetBufferAccessor(ref this.m_VacantLotType);
				if (bufferAccessor.Length != 0)
				{
					NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref this.m_OwnerType);
					NativeArray<CurvePosition> nativeArray3 = chunk.GetNativeArray(ref this.m_CurvePositionType);
					NativeArray<Block> nativeArray4 = chunk.GetNativeArray(ref this.m_BlockType);
					for (int i = 0; i < nativeArray.Length; i++)
					{
						Entity entity = nativeArray[i];
						DynamicBuffer<VacantLot> dynamicBuffer = bufferAccessor[i];
						Owner owner = nativeArray2[i];
						CurvePosition curvePosition = nativeArray3[i];
						Block block = nativeArray4[i];
						for (int j = 0; j < dynamicBuffer.Length; j++)
						{
							VacantLot lot = dynamicBuffer[j];
							ZoneData zoneData = this.m_ZoneData[this.m_ZonePrefabs[lot.m_Type]];
							DynamicBuffer<ProcessEstimate> estimates = this.m_ProcessEstimates[this.m_ZonePrefabs[lot.m_Type]];
							switch (zoneData.m_AreaType)
							{
							case Game.Zones.AreaType.Residential:
								if (this.m_SpawnResidential != 0)
								{
									float curvePos2 = this.CalculateCurvePos(curvePosition, lot, block);
									this.TryAddLot(ref bestLocation, ref random, owner.m_Owner, curvePos2, entity, lot.m_Area, lot.m_Flags, lot.m_Height, zoneData, estimates, this.m_Processes);
								}
								break;
							case Game.Zones.AreaType.Commercial:
								if (this.m_SpawnCommercial != 0)
								{
									float curvePos3 = this.CalculateCurvePos(curvePosition, lot, block);
									this.TryAddLot(ref bestLocation2, ref random, owner.m_Owner, curvePos3, entity, lot.m_Area, lot.m_Flags, lot.m_Height, zoneData, estimates, this.m_Processes);
								}
								break;
							case Game.Zones.AreaType.Industrial:
								if (this.m_SpawnIndustrial != 0 || this.m_SpawnStorage != 0)
								{
									float curvePos = this.CalculateCurvePos(curvePosition, lot, block);
									this.TryAddLot(ref bestLocation3, ref random, owner.m_Owner, curvePos, entity, lot.m_Area, lot.m_Flags, lot.m_Height, zoneData, estimates, this.m_Processes, this.m_SpawnIndustrial != 0, this.m_SpawnStorage != 0);
								}
								break;
							}
						}
					}
				}
				if (bestLocation.m_Priority != 0f)
				{
					this.m_Residential.Enqueue(bestLocation);
				}
				if (bestLocation2.m_Priority != 0f)
				{
					this.m_Commercial.Enqueue(bestLocation2);
				}
				if (bestLocation3.m_Priority != 0f)
				{
					this.m_Industrial.Enqueue(bestLocation3);
				}
			}

			private float CalculateCurvePos(CurvePosition curvePosition, VacantLot lot, Block block)
			{
				float s = math.saturate((float)(lot.m_Area.x + lot.m_Area.y) * 0.5f / (float)block.m_Size.x);
				return math.lerp(curvePosition.m_CurvePosition.x, curvePosition.m_CurvePosition.y, s);
			}

			private void TryAddLot(ref SpawnLocation bestLocation, ref Random random, Entity road, float curvePos, Entity entity, int4 area, LotFlags flags, int height, ZoneData zoneData, DynamicBuffer<ProcessEstimate> estimates, NativeList<IndustrialProcessData> processes, bool normal = true, bool storage = false)
			{
				if (!this.m_Availabilities.HasBuffer(road))
				{
					return;
				}
				if ((zoneData.m_ZoneFlags & ZoneFlags.SupportLeftCorner) == 0)
				{
					flags &= ~LotFlags.CornerLeft;
				}
				if ((zoneData.m_ZoneFlags & ZoneFlags.SupportRightCorner) == 0)
				{
					flags &= ~LotFlags.CornerRight;
				}
				SpawnLocation location = default(SpawnLocation);
				location.m_Entity = entity;
				location.m_LotArea = area;
				location.m_ZoneType = zoneData.m_ZoneType;
				location.m_AreaType = zoneData.m_AreaType;
				location.m_LotFlags = flags;
				bool office = zoneData.m_AreaType == Game.Zones.AreaType.Industrial && estimates.Length == 0;
				DynamicBuffer<ResourceAvailability> availabilities = this.m_Availabilities[road];
				if (this.m_BlockData.HasComponent(location.m_Entity))
				{
					float3 position = ZoneUtils.GetPosition(this.m_BlockData[location.m_Entity], location.m_LotArea.xz, location.m_LotArea.yw);
					bool extractor = false;
					GroundPollution pollution = GroundPollutionSystem.GetPollution(position, this.m_PollutionMap);
					float2 pollution2 = new float2(pollution.m_Pollution, pollution.m_Pollution - pollution.m_Previous);
					float landValue = this.m_LandValues[road].m_LandValue;
					float maxHeight = (float)height - position.y;
					if (this.SelectBuilding(ref location, ref random, availabilities, zoneData, curvePos, pollution2, landValue, maxHeight, estimates, processes, normal, storage, extractor, office) && location.m_Priority > bestLocation.m_Priority)
					{
						bestLocation = location;
					}
				}
			}

			private bool SelectBuilding(ref SpawnLocation location, ref Random random, DynamicBuffer<ResourceAvailability> availabilities, ZoneData zoneData, float curvePos, float2 pollution, float landValue, float maxHeight, DynamicBuffer<ProcessEstimate> estimates, NativeList<IndustrialProcessData> processes, bool normal = true, bool storage = false, bool extractor = false, bool office = false)
			{
				int2 @int = location.m_LotArea.yw - location.m_LotArea.xz;
				BuildingData buildingData = default(BuildingData);
				bool2 @bool = new bool2((location.m_LotFlags & LotFlags.CornerLeft) != 0, (location.m_LotFlags & LotFlags.CornerRight) != 0);
				bool flag = (zoneData.m_ZoneFlags & ZoneFlags.SupportNarrow) == 0;
				for (int i = 0; i < this.m_BuildingChunks.Length; i++)
				{
					ArchetypeChunk archetypeChunk = this.m_BuildingChunks[i];
					if (!archetypeChunk.GetSharedComponent(this.m_BuildingSpawnGroupType).m_ZoneType.Equals(location.m_ZoneType))
					{
						continue;
					}
					bool flag2 = archetypeChunk.Has(ref this.m_WarehouseType);
					if ((flag2 && !storage) || (!flag2 && !normal))
					{
						continue;
					}
					NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(this.m_EntityType);
					NativeArray<BuildingData> nativeArray2 = archetypeChunk.GetNativeArray(ref this.m_BuildingDataType);
					NativeArray<SpawnableBuildingData> nativeArray3 = archetypeChunk.GetNativeArray(ref this.m_SpawnableBuildingType);
					NativeArray<BuildingPropertyData> nativeArray4 = archetypeChunk.GetNativeArray(ref this.m_BuildingPropertyType);
					NativeArray<ObjectGeometryData> nativeArray5 = archetypeChunk.GetNativeArray(ref this.m_ObjectGeometryType);
					for (int j = 0; j < nativeArray3.Length; j++)
					{
						if (nativeArray3[j].m_Level != 1)
						{
							continue;
						}
						BuildingData buildingData2 = nativeArray2[j];
						int2 lotSize = buildingData2.m_LotSize;
						bool2 bool2 = new bool2((buildingData2.m_Flags & Game.Prefabs.BuildingFlags.LeftAccess) != 0, (buildingData2.m_Flags & Game.Prefabs.BuildingFlags.RightAccess) != 0);
						float y = nativeArray5[j].m_Size.y;
						if (!math.all(lotSize <= @int) || !(y <= maxHeight))
						{
							continue;
						}
						BuildingPropertyData propertyData = nativeArray4[j];
						int num = this.EvaluateDemandAndAvailability(location.m_AreaType, propertyData, lotSize.x * lotSize.y, flag2);
						if (!(num >= this.m_MinDemand || extractor))
						{
							continue;
						}
						int2 int2 = math.select(@int - lotSize, 0, lotSize == @int - 1);
						float num2 = (float)(lotSize.x * lotSize.y) * random.NextFloat(1f, 1.05f);
						num2 += (float)(int2.x * lotSize.y) * random.NextFloat(0.95f, 1f);
						num2 += (float)(@int.x * int2.y) * random.NextFloat(0.55f, 0.6f);
						num2 /= (float)(@int.x * @int.y);
						num2 *= (float)(num + 1);
						num2 *= math.csum(math.select(0.01f, 0.5f, @bool == bool2));
						if (!extractor)
						{
							float num3 = landValue;
							float num4;
							if (location.m_AreaType == Game.Zones.AreaType.Residential)
							{
								num4 = ((propertyData.m_ResidentialProperties == 1) ? 2f : ((float)propertyData.CountProperties()));
								lotSize.x = math.select(lotSize.x, @int.x, lotSize.x == @int.x - 1 && flag);
								num3 *= (float)(lotSize.x * @int.y);
							}
							else
							{
								num4 = propertyData.m_SpaceMultiplier;
							}
							float score = ZoneEvaluationUtils.GetScore(location.m_AreaType, office, availabilities, curvePos, ref this.m_Preferences, flag2, flag2 ? this.m_StorageDemands : this.m_IndustrialDemands, propertyData, pollution, num3 / num4, estimates, processes, this.m_ResourcePrefabs, ref this.m_ResourceDatas);
							score = math.select(score, math.max(0f, score) + 1f, this.m_MinDemand == 0);
							num2 *= score;
						}
						if (num2 > location.m_Priority)
						{
							location.m_Building = nativeArray[j];
							buildingData = buildingData2;
							location.m_Priority = num2;
						}
					}
				}
				if (location.m_Building != Entity.Null)
				{
					if ((buildingData.m_Flags & Game.Prefabs.BuildingFlags.LeftAccess) == 0 && ((buildingData.m_Flags & Game.Prefabs.BuildingFlags.RightAccess) != 0 || random.NextBool()))
					{
						location.m_LotArea.x = location.m_LotArea.y - buildingData.m_LotSize.x;
						location.m_LotArea.w = location.m_LotArea.z + buildingData.m_LotSize.y;
					}
					else
					{
						location.m_LotArea.yw = location.m_LotArea.xz + buildingData.m_LotSize;
					}
					return true;
				}
				return false;
			}

			private int EvaluateDemandAndAvailability(Game.Zones.AreaType areaType, BuildingPropertyData propertyData, int size, bool storage = false)
			{
				switch (areaType)
				{
				case Game.Zones.AreaType.Residential:
					if (propertyData.m_ResidentialProperties == 1)
					{
						return this.m_ResidentialDemands.x;
					}
					if ((float)propertyData.m_ResidentialProperties / (propertyData.m_SpaceMultiplier * (float)size) < 1f)
					{
						return this.m_ResidentialDemands.y;
					}
					return this.m_ResidentialDemands.z;
				case Game.Zones.AreaType.Commercial:
				{
					int num2 = 0;
					ResourceIterator iterator2 = ResourceIterator.GetIterator();
					while (iterator2.Next())
					{
						if ((propertyData.m_AllowedSold & iterator2.resource) != Resource.NoResource)
						{
							num2 += this.m_CommercialBuildingDemands[EconomyUtils.GetResourceIndex(iterator2.resource)];
						}
					}
					return num2;
				}
				case Game.Zones.AreaType.Industrial:
				{
					int num = 0;
					ResourceIterator iterator = ResourceIterator.GetIterator();
					while (iterator.Next())
					{
						if (storage)
						{
							if ((propertyData.m_AllowedStored & iterator.resource) != Resource.NoResource)
							{
								num += this.m_StorageDemands[EconomyUtils.GetResourceIndex(iterator.resource)];
							}
						}
						else if ((propertyData.m_AllowedManufactured & iterator.resource) != Resource.NoResource)
						{
							num += this.m_IndustrialDemands[EconomyUtils.GetResourceIndex(iterator.resource)];
						}
					}
					return num;
				}
				default:
					return 0;
				}
			}

			void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				this.Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
			}
		}

		[BurstCompile]
		public struct SpawnBuildingJob : IJobParallelFor
		{
			private struct Iterator : INativeQuadTreeIterator<Entity, Bounds2>, IUnsafeQuadTreeIterator<Entity, Bounds2>
			{
				public Bounds2 m_Bounds;

				public int2 m_LotSize;

				public float2 m_StartPosition;

				public float2 m_Right;

				public float2 m_Forward;

				public int m_MaxHeight;

				public ComponentLookup<Block> m_BlockData;

				public ComponentLookup<ValidArea> m_ValidAreaData;

				public BufferLookup<Cell> m_Cells;

				public bool Intersect(Bounds2 bounds)
				{
					return MathUtils.Intersect(bounds, this.m_Bounds);
				}

				public void Iterate(Bounds2 bounds, Entity blockEntity)
				{
					if (!MathUtils.Intersect(bounds, this.m_Bounds))
					{
						return;
					}
					ValidArea validArea = this.m_ValidAreaData[blockEntity];
					if (validArea.m_Area.y <= validArea.m_Area.x)
					{
						return;
					}
					Block block = this.m_BlockData[blockEntity];
					DynamicBuffer<Cell> dynamicBuffer = this.m_Cells[blockEntity];
					float2 startPosition = this.m_StartPosition;
					int2 @int = default(int2);
					@int.y = 0;
					while (@int.y < this.m_LotSize.y)
					{
						float2 position = startPosition;
						@int.x = 0;
						while (@int.x < this.m_LotSize.x)
						{
							int2 cellIndex = ZoneUtils.GetCellIndex(block, position);
							if (math.all((cellIndex >= validArea.m_Area.xz) & (cellIndex < validArea.m_Area.yw)))
							{
								int index = cellIndex.y * block.m_Size.x + cellIndex.x;
								Cell cell = dynamicBuffer[index];
								if ((cell.m_State & CellFlags.Visible) != 0)
								{
									this.m_MaxHeight = math.min(this.m_MaxHeight, cell.m_Height);
								}
							}
							position -= this.m_Right;
							@int.x++;
						}
						startPosition -= this.m_Forward;
						@int.y++;
					}
				}
			}

			[ReadOnly]
			public ComponentLookup<Block> m_BlockData;

			[ReadOnly]
			public ComponentLookup<ValidArea> m_ValidAreaData;

			[ReadOnly]
			public ComponentLookup<Transform> m_TransformData;

			[ReadOnly]
			public ComponentLookup<PrefabRef> m_PrefabRefData;

			[ReadOnly]
			public ComponentLookup<BuildingData> m_PrefabBuildingData;

			[ReadOnly]
			public ComponentLookup<PlaceableObjectData> m_PrefabPlaceableObjectData;

			[ReadOnly]
			public ComponentLookup<SpawnableObjectData> m_PrefabSpawnableObjectData;

			[ReadOnly]
			public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

			[ReadOnly]
			public ComponentLookup<AreaGeometryData> m_PrefabAreaGeometryData;

			[ReadOnly]
			public ComponentLookup<NetGeometryData> m_PrefabNetGeometryData;

			[ReadOnly]
			public BufferLookup<Cell> m_Cells;

			[ReadOnly]
			public BufferLookup<Game.Prefabs.SubArea> m_PrefabSubAreas;

			[ReadOnly]
			public BufferLookup<SubAreaNode> m_PrefabSubAreaNodes;

			[ReadOnly]
			public BufferLookup<Game.Prefabs.SubNet> m_PrefabSubNets;

			[ReadOnly]
			public BufferLookup<PlaceholderObjectElement> m_PrefabPlaceholderElements;

			[ReadOnly]
			public EntityArchetype m_DefinitionArchetype;

			[ReadOnly]
			public RandomSeed m_RandomSeed;

			[ReadOnly]
			public bool m_LefthandTraffic;

			[ReadOnly]
			public TerrainHeightData m_TerrainHeightData;

			[ReadOnly]
			public NativeQuadTree<Entity, Bounds2> m_ZoneSearchTree;

			[ReadOnly]
			public BuildingConfigurationData m_BuildingConfigurationData;

			[NativeDisableParallelForRestriction]
			public NativeQueue<SpawnLocation> m_Residential;

			[NativeDisableParallelForRestriction]
			public NativeQueue<SpawnLocation> m_Commercial;

			[NativeDisableParallelForRestriction]
			public NativeQueue<SpawnLocation> m_Industrial;

			public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

			public void Execute(int index)
			{
				SpawnLocation location;
				switch (index)
				{
				default:
					return;
				case 0:
					if (!this.SelectLocation(this.m_Residential, out location))
					{
						return;
					}
					break;
				case 1:
					if (!this.SelectLocation(this.m_Commercial, out location))
					{
						return;
					}
					break;
				case 2:
					if (!this.SelectLocation(this.m_Industrial, out location))
					{
						return;
					}
					break;
				}
				Random random = this.m_RandomSeed.GetRandom(index);
				this.Spawn(index, location, ref random);
			}

			private bool SelectLocation(NativeQueue<SpawnLocation> queue, out SpawnLocation location)
			{
				location = default(SpawnLocation);
				SpawnLocation item;
				while (queue.TryDequeue(out item))
				{
					if (item.m_Priority > location.m_Priority)
					{
						location = item;
					}
				}
				return location.m_Priority != 0f;
			}

			private void Spawn(int jobIndex, SpawnLocation location, ref Random random)
			{
				BuildingData prefabBuildingData = this.m_PrefabBuildingData[location.m_Building];
				ObjectGeometryData objectGeometryData = this.m_PrefabObjectGeometryData[location.m_Building];
				PlaceableObjectData placeableObjectData = default(PlaceableObjectData);
				if (this.m_PrefabPlaceableObjectData.HasComponent(location.m_Building))
				{
					placeableObjectData = this.m_PrefabPlaceableObjectData[location.m_Building];
				}
				CreationDefinition component = default(CreationDefinition);
				component.m_Prefab = location.m_Building;
				component.m_Flags |= CreationFlags.Permanent | CreationFlags.Construction;
				component.m_RandomSeed = random.NextInt();
				Transform transform = default(Transform);
				if (this.m_BlockData.HasComponent(location.m_Entity))
				{
					Block block = this.m_BlockData[location.m_Entity];
					transform.m_Position = ZoneUtils.GetPosition(block, location.m_LotArea.xz, location.m_LotArea.yw);
					transform.m_Rotation = ZoneUtils.GetRotation(block);
				}
				else if (this.m_TransformData.HasComponent(location.m_Entity))
				{
					component.m_Attached = location.m_Entity;
					component.m_Flags |= CreationFlags.Attach;
					Transform transform2 = this.m_TransformData[location.m_Entity];
					PrefabRef prefabRef = this.m_PrefabRefData[location.m_Entity];
					BuildingData buildingData = this.m_PrefabBuildingData[prefabRef.m_Prefab];
					transform.m_Position = transform2.m_Position;
					transform.m_Rotation = transform2.m_Rotation;
					float z = (float)(buildingData.m_LotSize.y - prefabBuildingData.m_LotSize.y) * 4f;
					transform.m_Position += math.rotate(transform.m_Rotation, new float3(0f, 0f, z));
				}
				float3 worldPosition = BuildingUtils.CalculateFrontPosition(transform, prefabBuildingData.m_LotSize.y);
				transform.m_Position.y = TerrainUtils.SampleHeight(ref this.m_TerrainHeightData, worldPosition);
				if ((placeableObjectData.m_Flags & (Game.Objects.PlacementFlags.Shoreline | Game.Objects.PlacementFlags.Floating)) == 0)
				{
					transform.m_Position.y += placeableObjectData.m_PlacementOffset.y;
				}
				float maxHeight = this.GetMaxHeight(transform, prefabBuildingData);
				transform.m_Position.y = math.min(transform.m_Position.y, maxHeight - objectGeometryData.m_Size.y - 0.1f);
				ObjectDefinition component2 = default(ObjectDefinition);
				component2.m_ParentMesh = -1;
				component2.m_Position = transform.m_Position;
				component2.m_Rotation = transform.m_Rotation;
				component2.m_LocalPosition = transform.m_Position;
				component2.m_LocalRotation = transform.m_Rotation;
				Entity e = this.m_CommandBuffer.CreateEntity(jobIndex, this.m_DefinitionArchetype);
				this.m_CommandBuffer.SetComponent(jobIndex, e, component);
				this.m_CommandBuffer.SetComponent(jobIndex, e, component2);
				OwnerDefinition ownerDefinition = default(OwnerDefinition);
				ownerDefinition.m_Prefab = location.m_Building;
				ownerDefinition.m_Position = component2.m_Position;
				ownerDefinition.m_Rotation = component2.m_Rotation;
				if (this.m_PrefabSubAreas.HasBuffer(location.m_Building))
				{
					this.Spawn(jobIndex, ownerDefinition, this.m_PrefabSubAreas[location.m_Building], this.m_PrefabSubAreaNodes[location.m_Building], prefabBuildingData, ref random);
				}
				if (this.m_PrefabSubNets.HasBuffer(location.m_Building))
				{
					this.Spawn(jobIndex, ownerDefinition, this.m_PrefabSubNets[location.m_Building], ref random);
				}
			}

			private float GetMaxHeight(Transform transform, BuildingData prefabBuildingData)
			{
				float2 xz = math.rotate(transform.m_Rotation, new float3(8f, 0f, 0f)).xz;
				float2 xz2 = math.rotate(transform.m_Rotation, new float3(0f, 0f, 8f)).xz;
				float2 @float = xz * ((float)prefabBuildingData.m_LotSize.x * 0.5f - 0.5f);
				float2 float2 = xz2 * ((float)prefabBuildingData.m_LotSize.y * 0.5f - 0.5f);
				float2 float3 = math.abs(float2) + math.abs(@float);
				Iterator iterator = default(Iterator);
				iterator.m_Bounds = new Bounds2(transform.m_Position.xz - float3, transform.m_Position.xz + float3);
				iterator.m_LotSize = prefabBuildingData.m_LotSize;
				iterator.m_StartPosition = transform.m_Position.xz + float2 + @float;
				iterator.m_Right = xz;
				iterator.m_Forward = xz2;
				iterator.m_MaxHeight = int.MaxValue;
				iterator.m_BlockData = this.m_BlockData;
				iterator.m_ValidAreaData = this.m_ValidAreaData;
				iterator.m_Cells = this.m_Cells;
				Iterator iterator2 = iterator;
				this.m_ZoneSearchTree.Iterate(ref iterator2);
				return iterator2.m_MaxHeight;
			}

			private void Spawn(int jobIndex, OwnerDefinition ownerDefinition, DynamicBuffer<Game.Prefabs.SubArea> subAreas, DynamicBuffer<SubAreaNode> subAreaNodes, BuildingData prefabBuildingData, ref Random random)
			{
				NativeParallelHashMap<Entity, int> selectedSpawnables = default(NativeParallelHashMap<Entity, int>);
				bool flag = false;
				for (int i = 0; i < subAreas.Length; i++)
				{
					Game.Prefabs.SubArea subArea = subAreas[i];
					AreaGeometryData areaGeometryData = this.m_PrefabAreaGeometryData[subArea.m_Prefab];
					if (areaGeometryData.m_Type == Game.Areas.AreaType.Surface)
					{
						if (flag)
						{
							continue;
						}
						subArea.m_Prefab = this.m_BuildingConfigurationData.m_ConstructionSurface;
						flag = true;
					}
					int seed;
					if (this.m_PrefabPlaceholderElements.TryGetBuffer(subArea.m_Prefab, out var bufferData))
					{
						if (!selectedSpawnables.IsCreated)
						{
							selectedSpawnables = new NativeParallelHashMap<Entity, int>(10, Allocator.Temp);
						}
						if (!AreaUtils.SelectAreaPrefab(bufferData, this.m_PrefabSpawnableObjectData, selectedSpawnables, ref random, out subArea.m_Prefab, out seed))
						{
							continue;
						}
					}
					else
					{
						seed = random.NextInt();
					}
					Entity e = this.m_CommandBuffer.CreateEntity(jobIndex);
					CreationDefinition component = default(CreationDefinition);
					component.m_Prefab = subArea.m_Prefab;
					component.m_RandomSeed = seed;
					component.m_Flags |= CreationFlags.Permanent;
					this.m_CommandBuffer.AddComponent(jobIndex, e, component);
					this.m_CommandBuffer.AddComponent(jobIndex, e, default(Updated));
					this.m_CommandBuffer.AddComponent(jobIndex, e, ownerDefinition);
					DynamicBuffer<Game.Areas.Node> dynamicBuffer = this.m_CommandBuffer.AddBuffer<Game.Areas.Node>(jobIndex, e);
					if (areaGeometryData.m_Type == Game.Areas.AreaType.Surface)
					{
						Quad3 quad = BuildingUtils.CalculateCorners(new Transform(ownerDefinition.m_Position, ownerDefinition.m_Rotation), prefabBuildingData.m_LotSize);
						dynamicBuffer.ResizeUninitialized(5);
						dynamicBuffer[0] = new Game.Areas.Node(quad.a, float.MinValue);
						dynamicBuffer[1] = new Game.Areas.Node(quad.b, float.MinValue);
						dynamicBuffer[2] = new Game.Areas.Node(quad.c, float.MinValue);
						dynamicBuffer[3] = new Game.Areas.Node(quad.d, float.MinValue);
						dynamicBuffer[4] = new Game.Areas.Node(quad.a, float.MinValue);
						continue;
					}
					dynamicBuffer.ResizeUninitialized(subArea.m_NodeRange.y - subArea.m_NodeRange.x + 1);
					int num = ObjectToolBaseSystem.GetFirstNodeIndex(subAreaNodes, subArea.m_NodeRange);
					int num2 = 0;
					for (int j = subArea.m_NodeRange.x; j <= subArea.m_NodeRange.y; j++)
					{
						float3 position = subAreaNodes[num].m_Position;
						float3 position2 = ObjectUtils.LocalToWorld(ownerDefinition.m_Position, ownerDefinition.m_Rotation, position);
						int parentMesh = subAreaNodes[num].m_ParentMesh;
						float elevation = math.select(float.MinValue, position.y, parentMesh >= 0);
						dynamicBuffer[num2] = new Game.Areas.Node(position2, elevation);
						num2++;
						if (++num == subArea.m_NodeRange.y)
						{
							num = subArea.m_NodeRange.x;
						}
					}
				}
				if (selectedSpawnables.IsCreated)
				{
					selectedSpawnables.Dispose();
				}
			}

			private void Spawn(int jobIndex, OwnerDefinition ownerDefinition, DynamicBuffer<Game.Prefabs.SubNet> subNets, ref Random random)
			{
				NativeList<float4> nodePositions = new NativeList<float4>(subNets.Length * 2, Allocator.Temp);
				for (int i = 0; i < subNets.Length; i++)
				{
					Game.Prefabs.SubNet subNet = subNets[i];
					if (subNet.m_NodeIndex.x >= 0)
					{
						while (nodePositions.Length <= subNet.m_NodeIndex.x)
						{
							float4 value = default(float4);
							nodePositions.Add(in value);
						}
						nodePositions[subNet.m_NodeIndex.x] += new float4(subNet.m_Curve.a, 1f);
					}
					if (subNet.m_NodeIndex.y >= 0)
					{
						while (nodePositions.Length <= subNet.m_NodeIndex.y)
						{
							float4 value = default(float4);
							nodePositions.Add(in value);
						}
						nodePositions[subNet.m_NodeIndex.y] += new float4(subNet.m_Curve.d, 1f);
					}
				}
				for (int j = 0; j < nodePositions.Length; j++)
				{
					nodePositions[j] /= math.max(1f, nodePositions[j].w);
				}
				for (int k = 0; k < subNets.Length; k++)
				{
					Game.Prefabs.SubNet subNet2 = NetUtils.GetSubNet(subNets, k, this.m_LefthandTraffic, ref this.m_PrefabNetGeometryData);
					this.CreateSubNet(jobIndex, subNet2.m_Prefab, subNet2.m_Curve, subNet2.m_NodeIndex, subNet2.m_ParentMesh, subNet2.m_Upgrades, nodePositions, ownerDefinition, ref random);
				}
				nodePositions.Dispose();
			}

			private void CreateSubNet(int jobIndex, Entity netPrefab, Bezier4x3 curve, int2 nodeIndex, int2 parentMesh, CompositionFlags upgrades, NativeList<float4> nodePositions, OwnerDefinition ownerDefinition, ref Random random)
			{
				Entity e = this.m_CommandBuffer.CreateEntity(jobIndex);
				CreationDefinition component = default(CreationDefinition);
				component.m_Prefab = netPrefab;
				component.m_RandomSeed = random.NextInt();
				component.m_Flags |= CreationFlags.Permanent;
				this.m_CommandBuffer.AddComponent(jobIndex, e, component);
				this.m_CommandBuffer.AddComponent(jobIndex, e, default(Updated));
				this.m_CommandBuffer.AddComponent(jobIndex, e, ownerDefinition);
				NetCourse component2 = default(NetCourse);
				component2.m_Curve = ObjectUtils.LocalToWorld(ownerDefinition.m_Position, ownerDefinition.m_Rotation, curve);
				component2.m_StartPosition.m_Position = component2.m_Curve.a;
				component2.m_StartPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.StartTangent(component2.m_Curve), ownerDefinition.m_Rotation);
				component2.m_StartPosition.m_CourseDelta = 0f;
				component2.m_StartPosition.m_Elevation = curve.a.y;
				component2.m_StartPosition.m_ParentMesh = parentMesh.x;
				if (nodeIndex.x >= 0)
				{
					component2.m_StartPosition.m_Position = ObjectUtils.LocalToWorld(ownerDefinition.m_Position, ownerDefinition.m_Rotation, nodePositions[nodeIndex.x].xyz);
				}
				component2.m_EndPosition.m_Position = component2.m_Curve.d;
				component2.m_EndPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.EndTangent(component2.m_Curve), ownerDefinition.m_Rotation);
				component2.m_EndPosition.m_CourseDelta = 1f;
				component2.m_EndPosition.m_Elevation = curve.d.y;
				component2.m_EndPosition.m_ParentMesh = parentMesh.y;
				if (nodeIndex.y >= 0)
				{
					component2.m_EndPosition.m_Position = ObjectUtils.LocalToWorld(ownerDefinition.m_Position, ownerDefinition.m_Rotation, nodePositions[nodeIndex.y].xyz);
				}
				component2.m_Length = MathUtils.Length(component2.m_Curve);
				component2.m_FixedIndex = -1;
				component2.m_StartPosition.m_Flags |= CoursePosFlags.IsFirst | CoursePosFlags.DisableMerge;
				component2.m_EndPosition.m_Flags |= CoursePosFlags.IsLast | CoursePosFlags.DisableMerge;
				if (component2.m_StartPosition.m_Position.Equals(component2.m_EndPosition.m_Position))
				{
					component2.m_StartPosition.m_Flags |= CoursePosFlags.IsLast;
					component2.m_EndPosition.m_Flags |= CoursePosFlags.IsFirst;
				}
				this.m_CommandBuffer.AddComponent(jobIndex, e, component2);
				if (upgrades != default(CompositionFlags))
				{
					Upgraded upgraded = default(Upgraded);
					upgraded.m_Flags = upgrades;
					Upgraded component3 = upgraded;
					this.m_CommandBuffer.AddComponent(jobIndex, e, component3);
				}
			}
		}

		private struct TypeHandle
		{
			[ReadOnly]
			public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<Block> __Game_Zones_Block_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<CurvePosition> __Game_Zones_CurvePosition_RO_ComponentTypeHandle;

			[ReadOnly]
			public BufferTypeHandle<VacantLot> __Game_Zones_VacantLot_RO_BufferTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentTypeHandle;

			public SharedComponentTypeHandle<BuildingSpawnGroupData> __Game_Prefabs_BuildingSpawnGroupData_SharedComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<WarehouseData> __Game_Prefabs_WarehouseData_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentLookup<ZoneData> __Game_Prefabs_ZoneData_RO_ComponentLookup;

			[ReadOnly]
			public BufferLookup<ResourceAvailability> __Game_Net_ResourceAvailability_RO_BufferLookup;

			[ReadOnly]
			public ComponentLookup<LandValue> __Game_Net_LandValue_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<Block> __Game_Zones_Block_RO_ComponentLookup;

			[ReadOnly]
			public BufferLookup<ProcessEstimate> __Game_Zones_ProcessEstimate_RO_BufferLookup;

			[ReadOnly]
			public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<ValidArea> __Game_Zones_ValidArea_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<PlaceableObjectData> __Game_Prefabs_PlaceableObjectData_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<SpawnableObjectData> __Game_Prefabs_SpawnableObjectData_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<AreaGeometryData> __Game_Prefabs_AreaGeometryData_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

			[ReadOnly]
			public BufferLookup<Cell> __Game_Zones_Cell_RO_BufferLookup;

			[ReadOnly]
			public BufferLookup<Game.Prefabs.SubArea> __Game_Prefabs_SubArea_RO_BufferLookup;

			[ReadOnly]
			public BufferLookup<SubAreaNode> __Game_Prefabs_SubAreaNode_RO_BufferLookup;

			[ReadOnly]
			public BufferLookup<Game.Prefabs.SubNet> __Game_Prefabs_SubNet_RO_BufferLookup;

			[ReadOnly]
			public BufferLookup<PlaceholderObjectElement> __Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void __AssignHandles(ref SystemState state)
			{
				this.__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
				this.__Game_Zones_Block_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Block>(isReadOnly: true);
				this.__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
				this.__Game_Zones_CurvePosition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurvePosition>(isReadOnly: true);
				this.__Game_Zones_VacantLot_RO_BufferTypeHandle = state.GetBufferTypeHandle<VacantLot>(isReadOnly: true);
				this.__Game_Prefabs_BuildingData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<BuildingData>(isReadOnly: true);
				this.__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SpawnableBuildingData>(isReadOnly: true);
				this.__Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<BuildingPropertyData>(isReadOnly: true);
				this.__Game_Prefabs_ObjectGeometryData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ObjectGeometryData>(isReadOnly: true);
				this.__Game_Prefabs_BuildingSpawnGroupData_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<BuildingSpawnGroupData>();
				this.__Game_Prefabs_WarehouseData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WarehouseData>(isReadOnly: true);
				this.__Game_Prefabs_ZoneData_RO_ComponentLookup = state.GetComponentLookup<ZoneData>(isReadOnly: true);
				this.__Game_Net_ResourceAvailability_RO_BufferLookup = state.GetBufferLookup<ResourceAvailability>(isReadOnly: true);
				this.__Game_Net_LandValue_RO_ComponentLookup = state.GetComponentLookup<LandValue>(isReadOnly: true);
				this.__Game_Zones_Block_RO_ComponentLookup = state.GetComponentLookup<Block>(isReadOnly: true);
				this.__Game_Zones_ProcessEstimate_RO_BufferLookup = state.GetBufferLookup<ProcessEstimate>(isReadOnly: true);
				this.__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
				this.__Game_Zones_ValidArea_RO_ComponentLookup = state.GetComponentLookup<ValidArea>(isReadOnly: true);
				this.__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
				this.__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
				this.__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
				this.__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup = state.GetComponentLookup<PlaceableObjectData>(isReadOnly: true);
				this.__Game_Prefabs_SpawnableObjectData_RO_ComponentLookup = state.GetComponentLookup<SpawnableObjectData>(isReadOnly: true);
				this.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
				this.__Game_Prefabs_AreaGeometryData_RO_ComponentLookup = state.GetComponentLookup<AreaGeometryData>(isReadOnly: true);
				this.__Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(isReadOnly: true);
				this.__Game_Zones_Cell_RO_BufferLookup = state.GetBufferLookup<Cell>(isReadOnly: true);
				this.__Game_Prefabs_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Prefabs.SubArea>(isReadOnly: true);
				this.__Game_Prefabs_SubAreaNode_RO_BufferLookup = state.GetBufferLookup<SubAreaNode>(isReadOnly: true);
				this.__Game_Prefabs_SubNet_RO_BufferLookup = state.GetBufferLookup<Game.Prefabs.SubNet>(isReadOnly: true);
				this.__Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup = state.GetBufferLookup<PlaceholderObjectElement>(isReadOnly: true);
			}
		}

		private ZoneSystem m_ZoneSystem;

		private ResidentialDemandSystem m_ResidentialDemandSystem;

		private CommercialDemandSystem m_CommercialDemandSystem;

		private IndustrialDemandSystem m_IndustrialDemandSystem;

		private GroundPollutionSystem m_PollutionSystem;

		private TerrainSystem m_TerrainSystem;

		private Game.Zones.SearchSystem m_SearchSystem;

		private ResourceSystem m_ResourceSystem;

		private CityConfigurationSystem m_CityConfigurationSystem;

		private EndFrameBarrier m_EndFrameBarrier;

		private EntityQuery m_LotQuery;

		private EntityQuery m_BuildingQuery;

		private EntityQuery m_ProcessQuery;

		private EntityQuery m_BuildingConfigurationQuery;

		private EntityArchetype m_DefinitionArchetype;

		private TypeHandle __TypeHandle;

		private EntityQuery __query_1944910156_0;

		public bool debugFastSpawn { get; set; }

		public override int GetUpdateInterval(SystemUpdatePhase phase)
		{
			return 16;
		}

		public override int GetUpdateOffset(SystemUpdatePhase phase)
		{
			return 13;
		}

		[Preserve]
		protected override void OnCreate()
		{
			base.OnCreate();
			this.m_ZoneSystem = base.World.GetOrCreateSystemManaged<ZoneSystem>();
			this.m_ResidentialDemandSystem = base.World.GetOrCreateSystemManaged<ResidentialDemandSystem>();
			this.m_CommercialDemandSystem = base.World.GetOrCreateSystemManaged<CommercialDemandSystem>();
			this.m_IndustrialDemandSystem = base.World.GetOrCreateSystemManaged<IndustrialDemandSystem>();
			this.m_PollutionSystem = base.World.GetOrCreateSystemManaged<GroundPollutionSystem>();
			this.m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
			this.m_SearchSystem = base.World.GetOrCreateSystemManaged<Game.Zones.SearchSystem>();
			this.m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
			this.m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
			this.m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
			this.m_LotQuery = base.GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[4]
				{
					ComponentType.ReadOnly<Block>(),
					ComponentType.ReadOnly<Owner>(),
					ComponentType.ReadOnly<CurvePosition>(),
					ComponentType.ReadOnly<VacantLot>()
				},
				Any = new ComponentType[0],
				None = new ComponentType[2]
				{
					ComponentType.ReadWrite<Temp>(),
					ComponentType.ReadWrite<Deleted>()
				}
			});
			this.m_BuildingQuery = base.GetEntityQuery(ComponentType.ReadOnly<BuildingData>(), ComponentType.ReadOnly<SpawnableBuildingData>(), ComponentType.ReadOnly<BuildingSpawnGroupData>(), ComponentType.ReadOnly<PrefabData>());
			this.m_DefinitionArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<CreationDefinition>(), ComponentType.ReadWrite<ObjectDefinition>(), ComponentType.ReadWrite<Updated>(), ComponentType.ReadWrite<Deleted>());
			this.m_ProcessQuery = base.GetEntityQuery(ComponentType.ReadOnly<IndustrialProcessData>());
			this.m_BuildingConfigurationQuery = base.GetEntityQuery(ComponentType.ReadOnly<BuildingConfigurationData>());
			base.RequireForUpdate(this.m_LotQuery);
			base.RequireForUpdate(this.m_BuildingQuery);
		}

		[Preserve]
		protected override void OnUpdate()
		{
			RandomSeed.Next().GetRandom(0);
			bool flag = this.debugFastSpawn || (this.m_ResidentialDemandSystem.buildingDemand.x + this.m_ResidentialDemandSystem.buildingDemand.y + this.m_ResidentialDemandSystem.buildingDemand.z) / 3 > 0;
			bool flag2 = this.debugFastSpawn || this.m_CommercialDemandSystem.buildingDemand > 0;
			bool flag3 = this.debugFastSpawn || (this.m_IndustrialDemandSystem.industrialBuildingDemand + this.m_IndustrialDemandSystem.officeBuildingDemand) / 2 > 0;
			bool flag4 = this.debugFastSpawn || this.m_IndustrialDemandSystem.storageBuildingDemand > 0;
			NativeQueue<SpawnLocation> residential = new NativeQueue<SpawnLocation>(Allocator.TempJob);
			NativeQueue<SpawnLocation> commercial = new NativeQueue<SpawnLocation>(Allocator.TempJob);
			NativeQueue<SpawnLocation> industrial = new NativeQueue<SpawnLocation>(Allocator.TempJob);
			this.__TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Zones_ProcessEstimate_RO_BufferLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Zones_Block_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_LandValue_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_ResourceAvailability_RO_BufferLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_WarehouseData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_BuildingSpawnGroupData_SharedComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Zones_VacantLot_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Zones_CurvePosition_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Zones_Block_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
			EvaluateSpawnAreas evaluateSpawnAreas = default(EvaluateSpawnAreas);
			evaluateSpawnAreas.m_BuildingChunks = this.m_BuildingQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out var outJobHandle);
			evaluateSpawnAreas.m_ZonePrefabs = this.m_ZoneSystem.GetPrefabs();
			evaluateSpawnAreas.m_Preferences = this.__query_1944910156_0.GetSingleton<ZonePreferenceData>();
			evaluateSpawnAreas.m_SpawnResidential = (flag ? 1 : 0);
			evaluateSpawnAreas.m_SpawnCommercial = (flag2 ? 1 : 0);
			evaluateSpawnAreas.m_SpawnIndustrial = (flag3 ? 1 : 0);
			evaluateSpawnAreas.m_SpawnStorage = (flag4 ? 1 : 0);
			evaluateSpawnAreas.m_MinDemand = ((!this.debugFastSpawn) ? 1 : 0);
			evaluateSpawnAreas.m_ResidentialDemands = this.m_ResidentialDemandSystem.buildingDemand;
			evaluateSpawnAreas.m_CommercialBuildingDemands = this.m_CommercialDemandSystem.GetBuildingDemands(out var deps);
			evaluateSpawnAreas.m_IndustrialDemands = this.m_IndustrialDemandSystem.GetBuildingDemands(out var deps2);
			evaluateSpawnAreas.m_StorageDemands = this.m_IndustrialDemandSystem.GetStorageBuildingDemands(out var deps3);
			evaluateSpawnAreas.m_RandomSeed = RandomSeed.Next();
			evaluateSpawnAreas.m_EntityType = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle;
			evaluateSpawnAreas.m_BlockType = this.__TypeHandle.__Game_Zones_Block_RO_ComponentTypeHandle;
			evaluateSpawnAreas.m_OwnerType = this.__TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle;
			evaluateSpawnAreas.m_CurvePositionType = this.__TypeHandle.__Game_Zones_CurvePosition_RO_ComponentTypeHandle;
			evaluateSpawnAreas.m_VacantLotType = this.__TypeHandle.__Game_Zones_VacantLot_RO_BufferTypeHandle;
			evaluateSpawnAreas.m_BuildingDataType = this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentTypeHandle;
			evaluateSpawnAreas.m_SpawnableBuildingType = this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle;
			evaluateSpawnAreas.m_BuildingPropertyType = this.__TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle;
			evaluateSpawnAreas.m_ObjectGeometryType = this.__TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentTypeHandle;
			evaluateSpawnAreas.m_BuildingSpawnGroupType = this.__TypeHandle.__Game_Prefabs_BuildingSpawnGroupData_SharedComponentTypeHandle;
			evaluateSpawnAreas.m_WarehouseType = this.__TypeHandle.__Game_Prefabs_WarehouseData_RO_ComponentTypeHandle;
			evaluateSpawnAreas.m_ZoneData = this.__TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup;
			evaluateSpawnAreas.m_Availabilities = this.__TypeHandle.__Game_Net_ResourceAvailability_RO_BufferLookup;
			evaluateSpawnAreas.m_LandValues = this.__TypeHandle.__Game_Net_LandValue_RO_ComponentLookup;
			evaluateSpawnAreas.m_BlockData = this.__TypeHandle.__Game_Zones_Block_RO_ComponentLookup;
			evaluateSpawnAreas.m_Processes = this.m_ProcessQuery.ToComponentDataListAsync<IndustrialProcessData>(base.World.UpdateAllocator.ToAllocator, out var outJobHandle2);
			evaluateSpawnAreas.m_ProcessEstimates = this.__TypeHandle.__Game_Zones_ProcessEstimate_RO_BufferLookup;
			evaluateSpawnAreas.m_ResourceDatas = this.__TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup;
			evaluateSpawnAreas.m_ResourcePrefabs = this.m_ResourceSystem.GetPrefabs();
			evaluateSpawnAreas.m_PollutionMap = this.m_PollutionSystem.GetMap(readOnly: true, out var dependencies);
			evaluateSpawnAreas.m_Residential = residential.AsParallelWriter();
			evaluateSpawnAreas.m_Commercial = commercial.AsParallelWriter();
			evaluateSpawnAreas.m_Industrial = industrial.AsParallelWriter();
			EvaluateSpawnAreas jobData = evaluateSpawnAreas;
			this.__TypeHandle.__Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_SubNet_RO_BufferLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_SubAreaNode_RO_BufferLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_SubArea_RO_BufferLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Zones_Cell_RO_BufferLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_AreaGeometryData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_SpawnableObjectData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Zones_ValidArea_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Zones_Block_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			SpawnBuildingJob spawnBuildingJob = default(SpawnBuildingJob);
			spawnBuildingJob.m_BlockData = this.__TypeHandle.__Game_Zones_Block_RO_ComponentLookup;
			spawnBuildingJob.m_ValidAreaData = this.__TypeHandle.__Game_Zones_ValidArea_RO_ComponentLookup;
			spawnBuildingJob.m_TransformData = this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup;
			spawnBuildingJob.m_PrefabRefData = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup;
			spawnBuildingJob.m_PrefabBuildingData = this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup;
			spawnBuildingJob.m_PrefabPlaceableObjectData = this.__TypeHandle.__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup;
			spawnBuildingJob.m_PrefabSpawnableObjectData = this.__TypeHandle.__Game_Prefabs_SpawnableObjectData_RO_ComponentLookup;
			spawnBuildingJob.m_PrefabObjectGeometryData = this.__TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;
			spawnBuildingJob.m_PrefabAreaGeometryData = this.__TypeHandle.__Game_Prefabs_AreaGeometryData_RO_ComponentLookup;
			spawnBuildingJob.m_PrefabNetGeometryData = this.__TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup;
			spawnBuildingJob.m_Cells = this.__TypeHandle.__Game_Zones_Cell_RO_BufferLookup;
			spawnBuildingJob.m_PrefabSubAreas = this.__TypeHandle.__Game_Prefabs_SubArea_RO_BufferLookup;
			spawnBuildingJob.m_PrefabSubAreaNodes = this.__TypeHandle.__Game_Prefabs_SubAreaNode_RO_BufferLookup;
			spawnBuildingJob.m_PrefabSubNets = this.__TypeHandle.__Game_Prefabs_SubNet_RO_BufferLookup;
			spawnBuildingJob.m_PrefabPlaceholderElements = this.__TypeHandle.__Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup;
			spawnBuildingJob.m_DefinitionArchetype = this.m_DefinitionArchetype;
			spawnBuildingJob.m_RandomSeed = RandomSeed.Next();
			spawnBuildingJob.m_LefthandTraffic = this.m_CityConfigurationSystem.leftHandTraffic;
			spawnBuildingJob.m_TerrainHeightData = this.m_TerrainSystem.GetHeightData();
			spawnBuildingJob.m_ZoneSearchTree = this.m_SearchSystem.GetSearchTree(readOnly: true, out var dependencies2);
			spawnBuildingJob.m_BuildingConfigurationData = this.m_BuildingConfigurationQuery.GetSingleton<BuildingConfigurationData>();
			spawnBuildingJob.m_Residential = residential;
			spawnBuildingJob.m_Commercial = commercial;
			spawnBuildingJob.m_Industrial = industrial;
			spawnBuildingJob.m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter();
			SpawnBuildingJob jobData2 = spawnBuildingJob;
			JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData, this.m_LotQuery, JobUtils.CombineDependencies(outJobHandle, deps, deps2, deps3, dependencies, base.Dependency, outJobHandle2));
			JobHandle jobHandle2 = IJobParallelForExtensions.Schedule(jobData2, 3, 1, JobHandle.CombineDependencies(jobHandle, dependencies2));
			this.m_ResourceSystem.AddPrefabsReader(jobHandle);
			this.m_PollutionSystem.AddReader(jobHandle);
			this.m_CommercialDemandSystem.AddReader(jobHandle);
			this.m_IndustrialDemandSystem.AddReader(jobHandle);
			residential.Dispose(jobHandle2);
			commercial.Dispose(jobHandle2);
			industrial.Dispose(jobHandle2);
			this.m_ZoneSystem.AddPrefabsReader(jobHandle);
			this.m_TerrainSystem.AddCPUHeightReader(jobHandle2);
			this.m_EndFrameBarrier.AddJobHandleForProducer(jobHandle2);
			this.m_SearchSystem.AddSearchTreeReader(jobHandle2);
			base.Dependency = jobHandle2;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void __AssignQueries(ref SystemState state)
		{
			this.__query_1944910156_0 = state.GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[1] { ComponentType.ReadOnly<ZonePreferenceData>() },
				Any = new ComponentType[0],
				None = new ComponentType[0],
				Disabled = new ComponentType[0],
				Absent = new ComponentType[0],
				Options = EntityQueryOptions.IncludeSystems
			});
		}

		protected override void OnCreateForCompiler()
		{
			base.OnCreateForCompiler();
			this.__AssignQueries(ref base.CheckedStateRef);
			this.__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
		}

		[Preserve]
		public ZoneSpawnSystem()
		{
		}
	}
}

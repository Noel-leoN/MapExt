using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Game.Zones;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;
using Game;
using Game.Rendering;

namespace MapExt.Systems
{
	//[CompilerGenerated]
	public partial class NetColorSystem : GameSystemBase
	{
		[BurstCompile]
		private struct UpdateEdgeColorsJob : IJobChunk
		{
			[ReadOnly]
			public NativeList<ArchetypeChunk> m_InfomodeChunks;

			[ReadOnly]
			public ComponentTypeHandle<InfomodeActive> m_InfomodeActiveType;

			[ReadOnly]
			public ComponentTypeHandle<InfoviewCoverageData> m_InfoviewCoverageType;

			[ReadOnly]
			public ComponentTypeHandle<InfoviewAvailabilityData> m_InfoviewAvailabilityType;

			[ReadOnly]
			public ComponentTypeHandle<InfoviewNetGeometryData> m_InfoviewNetGeometryType;

			[ReadOnly]
			public ComponentTypeHandle<InfoviewNetStatusData> m_InfoviewNetStatusType;

			[ReadOnly]
			public ComponentTypeHandle<TrainTrack> m_TrainTrackType;

			[ReadOnly]
			public ComponentTypeHandle<TramTrack> m_TramTrackType;

			[ReadOnly]
			public ComponentTypeHandle<Waterway> m_WaterwayType;

			[ReadOnly]
			public ComponentTypeHandle<SubwayTrack> m_SubwayTrackType;

			[ReadOnly]
			public ComponentTypeHandle<NetCondition> m_NetConditionType;

			[ReadOnly]
			public ComponentTypeHandle<Road> m_RoadType;

			[ReadOnly]
			public ComponentTypeHandle<Game.Net.Pollution> m_PollutionType;

			[ReadOnly]
			public ComponentTypeHandle<Curve> m_CurveType;

			[ReadOnly]
			public BufferTypeHandle<Game.Net.ServiceCoverage> m_ServiceCoverageType;

			[ReadOnly]
			public BufferTypeHandle<ResourceAvailability> m_ResourceAvailabilityType;

			[ReadOnly]
			public ComponentLookup<LandValue> m_LandValues;

			[ReadOnly]
			public ComponentLookup<Edge> m_Edges;

			[ReadOnly]
			public ComponentLookup<Node> m_Nodes;

			[ReadOnly]
			public ComponentLookup<Temp> m_Temps;

			[ReadOnly]
			public ComponentLookup<ResourceData> m_ResourceDatas;

			[ReadOnly]
			public ComponentLookup<ZonePropertiesData> m_ZonePropertiesDatas;

			[ReadOnly]
			public BufferLookup<Game.Net.ServiceCoverage> m_ServiceCoverageData;

			[ReadOnly]
			public BufferLookup<ResourceAvailability> m_ResourceAvailabilityData;

			[ReadOnly]
			public BufferLookup<ProcessEstimate> m_ProcessEstimates;

			[ReadOnly]
			public ComponentTypeHandle<Edge> m_EdgeType;

			[ReadOnly]
			public ComponentTypeHandle<Temp> m_TempType;

			public ComponentTypeHandle<EdgeColor> m_ColorType;

			[ReadOnly]
			public Entity m_ZonePrefab;

			[ReadOnly]
			public ResourcePrefabs m_ResourcePrefabs;

			[ReadOnly]
			public NativeArray<GroundPollution> m_PollutionMap;

			[ReadOnly]
			public NativeArray<int> m_IndustrialDemands;

			[ReadOnly]
			public NativeArray<int> m_StorageDemands;

			[ReadOnly]
			public NativeList<IndustrialProcessData> m_Processes;

			[ReadOnly]
			public ZonePreferenceData m_ZonePreferences;

			public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				NativeArray<EdgeColor> nativeArray = chunk.GetNativeArray(ref this.m_ColorType);
				InfoviewAvailabilityData availabilityData;
				InfomodeActive activeData2;
				InfoviewNetStatusData statusData;
				InfomodeActive activeData3;
				int index;
				if (chunk.Has(ref this.m_ServiceCoverageType) && this.GetServiceCoverageData(chunk, out var coverageData, out var activeData))
				{
					NativeArray<Temp> nativeArray2 = chunk.GetNativeArray(ref this.m_TempType);
					BufferAccessor<Game.Net.ServiceCoverage> bufferAccessor = chunk.GetBufferAccessor(ref this.m_ServiceCoverageType);
					EdgeColor value2 = default(EdgeColor);
					for (int i = 0; i < nativeArray.Length; i++)
					{
						DynamicBuffer<Game.Net.ServiceCoverage> dynamicBuffer = bufferAccessor[i];
						if (CollectionUtils.TryGet(nativeArray2, i, out var value) && this.m_ServiceCoverageData.TryGetBuffer(value.m_Original, out var bufferData))
						{
							dynamicBuffer = bufferData;
						}
						if (dynamicBuffer.Length == 0)
						{
							nativeArray[i] = default(EdgeColor);
							continue;
						}
						Game.Net.ServiceCoverage serviceCoverage = dynamicBuffer[(int)coverageData.m_Service];
						value2.m_Index = (byte)activeData.m_Index;
						value2.m_Value0 = (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(coverageData, serviceCoverage.m_Coverage.x) * 255f), 0, 255);
						value2.m_Value1 = (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(coverageData, serviceCoverage.m_Coverage.y) * 255f), 0, 255);
						nativeArray[i] = value2;
					}
				}
				else if (chunk.Has(ref this.m_ResourceAvailabilityType) && this.GetResourceAvailabilityData(chunk, out availabilityData, out activeData2))
				{
					ZonePreferenceData preferences = this.m_ZonePreferences;
					NativeArray<Edge> nativeArray3 = chunk.GetNativeArray(ref this.m_EdgeType);
					NativeArray<Temp> nativeArray4 = chunk.GetNativeArray(ref this.m_TempType);
					BufferAccessor<ResourceAvailability> bufferAccessor2 = chunk.GetBufferAccessor(ref this.m_ResourceAvailabilityType);
					EdgeColor value4 = default(EdgeColor);
					for (int j = 0; j < nativeArray.Length; j++)
					{
						Edge edge = nativeArray3[j];
						DynamicBuffer<ResourceAvailability> availabilityBuffer = bufferAccessor2[j];
						float num;
						float num2;
						if (CollectionUtils.TryGet(nativeArray4, j, out var value3))
						{
							if (!this.m_Edges.TryGetComponent(value3.m_Original, out var componentData))
							{
								num = ((!this.m_Temps.TryGetComponent(edge.m_Start, out var componentData2) || !this.m_LandValues.TryGetComponent(componentData2.m_Original, out var componentData3)) ? this.m_LandValues[edge.m_Start].m_LandValue : componentData3.m_LandValue);
								num2 = ((!this.m_Temps.TryGetComponent(edge.m_End, out var componentData4) || !this.m_LandValues.TryGetComponent(componentData4.m_Original, out var componentData5)) ? this.m_LandValues[edge.m_End].m_LandValue : componentData5.m_LandValue);
							}
							else
							{
								edge = componentData;
								num = this.m_LandValues[componentData.m_Start].m_LandValue;
								num2 = this.m_LandValues[componentData.m_End].m_LandValue;
								if (this.m_ResourceAvailabilityData.TryGetBuffer(value3.m_Original, out var bufferData2))
								{
									availabilityBuffer = bufferData2;
								}
							}
						}
						else
						{
							num = this.m_LandValues[edge.m_Start].m_LandValue;
							num2 = this.m_LandValues[edge.m_End].m_LandValue;
						}
						if (availabilityBuffer.Length == 0)
						{
							nativeArray[j] = default(EdgeColor);
							continue;
						}
						float3 position = this.m_Nodes[edge.m_Start].m_Position;
						float3 position2 = this.m_Nodes[edge.m_End].m_Position;
						GroundPollution pollution = GroundPollutionSystem.GetPollution(position, this.m_PollutionMap);
						GroundPollution pollution2 = GroundPollutionSystem.GetPollution(position2, this.m_PollutionMap);
						float2 pollution3 = new float2(pollution.m_Pollution, pollution.m_Pollution - pollution.m_Previous);
						float2 pollution4 = new float2(pollution2.m_Pollution, pollution2.m_Pollution - pollution2.m_Previous);
						this.m_ProcessEstimates.TryGetBuffer(this.m_ZonePrefab, out var bufferData3);
						if (this.m_ZonePropertiesDatas.TryGetComponent(this.m_ZonePrefab, out var componentData6))
						{
							float num3 = ((availabilityData.m_AreaType != AreaType.Residential) ? componentData6.m_SpaceMultiplier : (componentData6.m_ScaleResidentials ? componentData6.m_ResidentialProperties : (componentData6.m_ResidentialProperties / 8f)));
							num /= num3;
							num2 /= num3;
						}
						value4.m_Index = (byte)activeData2.m_Index;
						value4.m_Value0 = (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(availabilityData, availabilityBuffer, 0f, ref preferences, this.m_IndustrialDemands, this.m_StorageDemands, pollution3, num, bufferData3, this.m_Processes, this.m_ResourcePrefabs, this.m_ResourceDatas) * 255f), 0, 255);
						value4.m_Value1 = (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(availabilityData, availabilityBuffer, 1f, ref preferences, this.m_IndustrialDemands, this.m_StorageDemands, pollution4, num2, bufferData3, this.m_Processes, this.m_ResourcePrefabs, this.m_ResourceDatas) * 255f), 0, 255);
						nativeArray[j] = value4;
					}
				}
				else if (this.GetNetStatusType(chunk, out statusData, out activeData3))
				{
					this.GetNetStatusColors(nativeArray, chunk, statusData, activeData3);
				}
				else if (this.GetNetGeometryColor(chunk, out index))
				{
					for (int k = 0; k < nativeArray.Length; k++)
					{
						nativeArray[k] = new EdgeColor((byte)index, 0, 0);
					}
				}
				else
				{
					for (int l = 0; l < nativeArray.Length; l++)
					{
						nativeArray[l] = new EdgeColor(0, byte.MaxValue, byte.MaxValue);
					}
				}
			}

			private bool GetServiceCoverageData(ArchetypeChunk chunk, out InfoviewCoverageData coverageData, out InfomodeActive activeData)
			{
				coverageData = default(InfoviewCoverageData);
				activeData = default(InfomodeActive);
				int num = int.MaxValue;
				for (int i = 0; i < this.m_InfomodeChunks.Length; i++)
				{
					ArchetypeChunk archetypeChunk = this.m_InfomodeChunks[i];
					NativeArray<InfoviewCoverageData> nativeArray = archetypeChunk.GetNativeArray(ref this.m_InfoviewCoverageType);
					if (nativeArray.Length == 0)
					{
						continue;
					}
					NativeArray<InfomodeActive> nativeArray2 = archetypeChunk.GetNativeArray(ref this.m_InfomodeActiveType);
					for (int j = 0; j < nativeArray.Length; j++)
					{
						InfomodeActive infomodeActive = nativeArray2[j];
						int priority = infomodeActive.m_Priority;
						if (priority < num)
						{
							coverageData = nativeArray[j];
							coverageData.m_Service = CoverageService.Count;
							activeData = infomodeActive;
							num = priority;
						}
					}
				}
				return num != int.MaxValue;
			}

			private bool GetResourceAvailabilityData(ArchetypeChunk chunk, out InfoviewAvailabilityData availabilityData, out InfomodeActive activeData)
			{
				availabilityData = default(InfoviewAvailabilityData);
				activeData = default(InfomodeActive);
				int num = int.MaxValue;
				for (int i = 0; i < this.m_InfomodeChunks.Length; i++)
				{
					ArchetypeChunk archetypeChunk = this.m_InfomodeChunks[i];
					NativeArray<InfoviewAvailabilityData> nativeArray = archetypeChunk.GetNativeArray(ref this.m_InfoviewAvailabilityType);
					if (nativeArray.Length == 0)
					{
						continue;
					}
					NativeArray<InfomodeActive> nativeArray2 = archetypeChunk.GetNativeArray(ref this.m_InfomodeActiveType);
					for (int j = 0; j < nativeArray.Length; j++)
					{
						InfomodeActive infomodeActive = nativeArray2[j];
						int priority = infomodeActive.m_Priority;
						if (priority < num)
						{
							availabilityData = nativeArray[j];
							activeData = infomodeActive;
							num = priority;
						}
					}
				}
				return num != int.MaxValue;
			}

			private bool GetNetStatusType(ArchetypeChunk chunk, out InfoviewNetStatusData statusData, out InfomodeActive activeData)
			{
				statusData = default(InfoviewNetStatusData);
				activeData = default(InfomodeActive);
				int num = int.MaxValue;
				for (int i = 0; i < this.m_InfomodeChunks.Length; i++)
				{
					ArchetypeChunk archetypeChunk = this.m_InfomodeChunks[i];
					NativeArray<InfoviewNetStatusData> nativeArray = archetypeChunk.GetNativeArray(ref this.m_InfoviewNetStatusType);
					if (nativeArray.Length == 0)
					{
						continue;
					}
					NativeArray<InfomodeActive> nativeArray2 = archetypeChunk.GetNativeArray(ref this.m_InfomodeActiveType);
					for (int j = 0; j < nativeArray.Length; j++)
					{
						InfomodeActive infomodeActive = nativeArray2[j];
						int priority = infomodeActive.m_Priority;
						if (priority < num)
						{
							InfoviewNetStatusData infoviewNetStatusData = nativeArray[j];
							if (this.HasNetStatus(nativeArray[j], chunk))
							{
								statusData = infoviewNetStatusData;
								activeData = infomodeActive;
								num = priority;
							}
						}
					}
				}
				return num != int.MaxValue;
			}

			private bool HasNetStatus(InfoviewNetStatusData infoviewNetStatusData, ArchetypeChunk chunk)
			{
				return infoviewNetStatusData.m_Type switch
				{
					NetStatusType.Wear => chunk.Has(ref this.m_NetConditionType), 
					NetStatusType.TrafficFlow => chunk.Has(ref this.m_RoadType), 
					NetStatusType.NoisePollutionSource => chunk.Has(ref this.m_PollutionType), 
					NetStatusType.AirPollutionSource => chunk.Has(ref this.m_PollutionType), 
					NetStatusType.TrafficVolume => chunk.Has(ref this.m_RoadType), 
					_ => false, 
				};
			}

			private void GetNetStatusColors(NativeArray<EdgeColor> results, ArchetypeChunk chunk, InfoviewNetStatusData statusData, InfomodeActive activeData)
			{
				switch (statusData.m_Type)
				{
				case NetStatusType.Wear:
				{
					NativeArray<NetCondition> nativeArray2 = chunk.GetNativeArray(ref this.m_NetConditionType);
					EdgeColor value2 = default(EdgeColor);
					for (int j = 0; j < nativeArray2.Length; j++)
					{
						NetCondition netCondition = nativeArray2[j];
						value2.m_Index = (byte)activeData.m_Index;
						value2.m_Value0 = (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, netCondition.m_Wear.x / 10f) * 255f), 0, 255);
						value2.m_Value1 = (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, netCondition.m_Wear.y / 10f) * 255f), 0, 255);
						results[j] = value2;
					}
					break;
				}
				case NetStatusType.TrafficFlow:
				{
					NativeArray<Road> nativeArray5 = chunk.GetNativeArray(ref this.m_RoadType);
					EdgeColor value4 = default(EdgeColor);
					for (int l = 0; l < nativeArray5.Length; l++)
					{
						Road road2 = nativeArray5[l];
						float4 trafficFlowSpeed = NetUtils.GetTrafficFlowSpeed(road2.m_TrafficFlowDuration0, road2.m_TrafficFlowDistance0);
						float4 trafficFlowSpeed2 = NetUtils.GetTrafficFlowSpeed(road2.m_TrafficFlowDuration1, road2.m_TrafficFlowDistance1);
						value4.m_Index = (byte)activeData.m_Index;
						value4.m_Value0 = (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, math.csum(trafficFlowSpeed) * 0.125f + math.cmin(trafficFlowSpeed) * 0.5f) * 255f), 0, 255);
						value4.m_Value1 = (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, math.csum(trafficFlowSpeed2) * 0.125f + math.cmin(trafficFlowSpeed2) * 0.5f) * 255f), 0, 255);
						results[l] = value4;
					}
					break;
				}
				case NetStatusType.NoisePollutionSource:
				{
					NativeArray<Game.Net.Pollution> nativeArray6 = chunk.GetNativeArray(ref this.m_PollutionType);
					NativeArray<Curve> nativeArray7 = chunk.GetNativeArray(ref this.m_CurveType);
					EdgeColor value5 = default(EdgeColor);
					for (int m = 0; m < nativeArray6.Length; m++)
					{
						float status2 = nativeArray6[m].m_Accumulation.x * 16f / math.max(16f, nativeArray7[m].m_Length);
						value5.m_Index = (byte)activeData.m_Index;
						value5.m_Value0 = (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, status2) * 255f), 0, 255);
						value5.m_Value1 = value5.m_Value0;
						results[m] = value5;
					}
					break;
				}
				case NetStatusType.AirPollutionSource:
				{
					NativeArray<Game.Net.Pollution> nativeArray3 = chunk.GetNativeArray(ref this.m_PollutionType);
					NativeArray<Curve> nativeArray4 = chunk.GetNativeArray(ref this.m_CurveType);
					EdgeColor value3 = default(EdgeColor);
					for (int k = 0; k < nativeArray3.Length; k++)
					{
						float status = nativeArray3[k].m_Accumulation.y * 16f / math.max(16f, nativeArray4[k].m_Length);
						value3.m_Index = (byte)activeData.m_Index;
						value3.m_Value0 = (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, status) * 255f), 0, 255);
						value3.m_Value1 = value3.m_Value0;
						results[k] = value3;
					}
					break;
				}
				case NetStatusType.TrafficVolume:
				{
					NativeArray<Road> nativeArray = chunk.GetNativeArray(ref this.m_RoadType);
					EdgeColor value = default(EdgeColor);
					for (int i = 0; i < nativeArray.Length; i++)
					{
						Road road = nativeArray[i];
						float4 x = math.sqrt(road.m_TrafficFlowDistance0 * 5.3333335f);
						float4 x2 = math.sqrt(road.m_TrafficFlowDistance1 * 5.3333335f);
						value.m_Index = (byte)activeData.m_Index;
						value.m_Value0 = (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, math.csum(x) * 0.25f) * 255f), 0, 255);
						value.m_Value1 = (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, math.csum(x2) * 0.25f) * 255f), 0, 255);
						results[i] = value;
					}
					break;
				}
				}
			}

			private bool GetNetGeometryColor(ArchetypeChunk chunk, out int index)
			{
				index = 0;
				int num = int.MaxValue;
				for (int i = 0; i < this.m_InfomodeChunks.Length; i++)
				{
					ArchetypeChunk archetypeChunk = this.m_InfomodeChunks[i];
					NativeArray<InfoviewNetGeometryData> nativeArray = archetypeChunk.GetNativeArray(ref this.m_InfoviewNetGeometryType);
					if (nativeArray.Length == 0)
					{
						continue;
					}
					NativeArray<InfomodeActive> nativeArray2 = archetypeChunk.GetNativeArray(ref this.m_InfomodeActiveType);
					for (int j = 0; j < nativeArray.Length; j++)
					{
						InfomodeActive infomodeActive = nativeArray2[j];
						int priority = infomodeActive.m_Priority;
						if (priority < num && this.HasNetGeometryColor(nativeArray[j], chunk))
						{
							index = infomodeActive.m_Index;
							num = priority;
						}
					}
				}
				return num != int.MaxValue;
			}

			private bool HasNetGeometryColor(InfoviewNetGeometryData infoviewNetGeometryData, ArchetypeChunk chunk)
			{
				return infoviewNetGeometryData.m_Type switch
				{
					NetType.TrainTrack => chunk.Has(ref this.m_TrainTrackType), 
					NetType.TramTrack => chunk.Has(ref this.m_TramTrackType), 
					NetType.Waterway => chunk.Has(ref this.m_WaterwayType), 
					NetType.SubwayTrack => chunk.Has(ref this.m_SubwayTrackType), 
					NetType.Road => chunk.Has(ref this.m_RoadType), 
					_ => false, 
				};
			}

			void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				this.Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
			}
		}

		[BurstCompile]
		private struct UpdateNodeColorsJob : IJobChunk
		{
			[ReadOnly]
			public ComponentLookup<Edge> m_EdgeData;

			[ReadOnly]
			public ComponentLookup<EdgeColor> m_ColorData;

			[ReadOnly]
			public ComponentLookup<StartNodeGeometry> m_StartNodeGeometryData;

			[ReadOnly]
			public ComponentLookup<EndNodeGeometry> m_EndNodeGeometryData;

			[ReadOnly]
			public BufferLookup<ConnectedEdge> m_ConnectedEdges;

			[ReadOnly]
			public EntityTypeHandle m_EntityType;

			[ReadOnly]
			public ComponentTypeHandle<Temp> m_TempType;

			[ReadOnly]
			public BufferTypeHandle<ConnectedEdge> m_ConnectedEdgeType;

			public ComponentTypeHandle<NodeColor> m_ColorType;

			[ReadOnly]
			public NativeList<ArchetypeChunk> m_InfomodeChunks;

			[ReadOnly]
			public ComponentTypeHandle<InfomodeActive> m_InfomodeActiveType;

			[ReadOnly]
			public ComponentTypeHandle<InfoviewNetGeometryData> m_InfoviewNetGeometryType;

			[ReadOnly]
			public ComponentTypeHandle<InfoviewNetStatusData> m_InfoviewNetStatusType;

			[ReadOnly]
			public ComponentTypeHandle<TrainTrack> m_TrainTrackType;

			[ReadOnly]
			public ComponentTypeHandle<TramTrack> m_TramTrackType;

			[ReadOnly]
			public ComponentTypeHandle<Waterway> m_WaterwayType;

			[ReadOnly]
			public ComponentTypeHandle<SubwayTrack> m_SubwayTrackType;

			[ReadOnly]
			public ComponentTypeHandle<NetCondition> m_NetConditionType;

			[ReadOnly]
			public ComponentTypeHandle<Road> m_RoadType;

			[ReadOnly]
			public ComponentTypeHandle<Game.Net.Pollution> m_PollutionType;

			public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				NativeArray<Entity> nativeArray = chunk.GetNativeArray(this.m_EntityType);
				NativeArray<NodeColor> nativeArray2 = chunk.GetNativeArray(ref this.m_ColorType);
				NativeArray<Temp> nativeArray3 = chunk.GetNativeArray(ref this.m_TempType);
				BufferAccessor<ConnectedEdge> bufferAccessor = chunk.GetBufferAccessor(ref this.m_ConnectedEdgeType);
				bool flag = false;
				int index;
				if (this.GetNetStatusType(chunk, out var statusData, out var activeData))
				{
					this.GetNetStatusColors(nativeArray2, chunk, statusData, activeData);
					flag = true;
				}
				else if (this.GetNetGeometryColor(chunk, out index))
				{
					for (int i = 0; i < nativeArray2.Length; i++)
					{
						nativeArray2[i] = new NodeColor((byte)index, 0);
					}
					flag = true;
				}
				int3 int2 = default(int3);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					Entity entity = nativeArray[j];
					DynamicBuffer<ConnectedEdge> dynamicBuffer = bufferAccessor[j];
					if (CollectionUtils.TryGet(nativeArray3, j, out var value) && this.m_ConnectedEdges.TryGetBuffer(value.m_Original, out var bufferData))
					{
						entity = value.m_Original;
						dynamicBuffer = bufferData;
					}
					int3 @int = default(int3);
					bool flag2 = flag;
					for (int k = 0; k < dynamicBuffer.Length; k++)
					{
						Entity edge = dynamicBuffer[k].m_Edge;
						if (!this.m_ColorData.HasComponent(edge))
						{
							continue;
						}
						Edge edge2 = this.m_EdgeData[edge];
						bool2 x = new bool2(edge2.m_Start == entity, edge2.m_End == entity);
						if (!math.any(x))
						{
							continue;
						}
						if (flag2)
						{
							EndNodeGeometry componentData2;
							if (x.x)
							{
								if (this.m_StartNodeGeometryData.TryGetComponent(edge, out var componentData))
								{
									flag2 = math.any(componentData.m_Geometry.m_Left.m_Length > 0.05f) | math.any(componentData.m_Geometry.m_Right.m_Length > 0.05f);
								}
							}
							else if (this.m_EndNodeGeometryData.TryGetComponent(edge, out componentData2))
							{
								flag2 = math.any(componentData2.m_Geometry.m_Left.m_Length > 0.05f) | math.any(componentData2.m_Geometry.m_Right.m_Length > 0.05f);
							}
						}
						EdgeColor edgeColor = this.m_ColorData[edge];
						if (edgeColor.m_Index != 0)
						{
							int2.x = edgeColor.m_Index;
							int2.y = (x.x ? edgeColor.m_Value0 : edgeColor.m_Value1);
							int2.z = 1;
							if ((int2.x == @int.x) | (@int.z == 0))
							{
								@int.x = int2.x;
								@int.yz += int2.yz;
							}
							else
							{
								@int.z = -1;
							}
						}
					}
					if (!flag2)
					{
						if (@int.z > 0)
						{
							@int.y /= @int.z;
							nativeArray2[j] = new NodeColor((byte)@int.x, (byte)@int.y);
						}
						else
						{
							nativeArray2[j] = new NodeColor(0, byte.MaxValue);
						}
					}
				}
			}

			private bool GetNetStatusType(ArchetypeChunk chunk, out InfoviewNetStatusData statusData, out InfomodeActive activeData)
			{
				statusData = default(InfoviewNetStatusData);
				activeData = default(InfomodeActive);
				int num = int.MaxValue;
				for (int i = 0; i < this.m_InfomodeChunks.Length; i++)
				{
					ArchetypeChunk archetypeChunk = this.m_InfomodeChunks[i];
					NativeArray<InfoviewNetStatusData> nativeArray = archetypeChunk.GetNativeArray(ref this.m_InfoviewNetStatusType);
					if (nativeArray.Length == 0)
					{
						continue;
					}
					NativeArray<InfomodeActive> nativeArray2 = archetypeChunk.GetNativeArray(ref this.m_InfomodeActiveType);
					for (int j = 0; j < nativeArray.Length; j++)
					{
						InfomodeActive infomodeActive = nativeArray2[j];
						int priority = infomodeActive.m_Priority;
						if (priority < num)
						{
							InfoviewNetStatusData infoviewNetStatusData = nativeArray[j];
							if (this.HasNetStatus(nativeArray[j], chunk))
							{
								statusData = infoviewNetStatusData;
								activeData = infomodeActive;
								num = priority;
							}
						}
					}
				}
				return num != int.MaxValue;
			}

			private bool HasNetStatus(InfoviewNetStatusData infoviewNetStatusData, ArchetypeChunk chunk)
			{
				return infoviewNetStatusData.m_Type switch
				{
					NetStatusType.Wear => chunk.Has(ref this.m_NetConditionType), 
					NetStatusType.TrafficFlow => chunk.Has(ref this.m_RoadType), 
					NetStatusType.NoisePollutionSource => chunk.Has(ref this.m_PollutionType), 
					NetStatusType.AirPollutionSource => chunk.Has(ref this.m_PollutionType), 
					NetStatusType.TrafficVolume => chunk.Has(ref this.m_RoadType), 
					_ => false, 
				};
			}

			private void GetNetStatusColors(NativeArray<NodeColor> results, ArchetypeChunk chunk, InfoviewNetStatusData statusData, InfomodeActive activeData)
			{
				switch (statusData.m_Type)
				{
				case NetStatusType.Wear:
				{
					NativeArray<NetCondition> nativeArray2 = chunk.GetNativeArray(ref this.m_NetConditionType);
					NodeColor value2 = default(NodeColor);
					for (int j = 0; j < nativeArray2.Length; j++)
					{
						value2.m_Index = (byte)activeData.m_Index;
						value2.m_Value = (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, math.cmax(nativeArray2[j].m_Wear) / 10f) * 255f), 0, 255);
						results[j] = value2;
					}
					break;
				}
				case NetStatusType.TrafficFlow:
				{
					NativeArray<Road> nativeArray4 = chunk.GetNativeArray(ref this.m_RoadType);
					NodeColor value4 = default(NodeColor);
					for (int l = 0; l < nativeArray4.Length; l++)
					{
						float4 trafficFlowSpeed = NetUtils.GetTrafficFlowSpeed(nativeArray4[l]);
						value4.m_Index = (byte)activeData.m_Index;
						value4.m_Value = (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, math.csum(trafficFlowSpeed) * 0.125f + math.cmin(trafficFlowSpeed) * 0.5f) * 255f), 0, 255);
						results[l] = value4;
					}
					break;
				}
				case NetStatusType.NoisePollutionSource:
				{
					NativeArray<Game.Net.Pollution> nativeArray5 = chunk.GetNativeArray(ref this.m_PollutionType);
					NodeColor value5 = default(NodeColor);
					for (int m = 0; m < nativeArray5.Length; m++)
					{
						value5.m_Index = (byte)activeData.m_Index;
						value5.m_Value = (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, nativeArray5[m].m_Accumulation.x) * 255f), 0, 255);
						results[m] = value5;
					}
					break;
				}
				case NetStatusType.AirPollutionSource:
				{
					NativeArray<Game.Net.Pollution> nativeArray3 = chunk.GetNativeArray(ref this.m_PollutionType);
					NodeColor value3 = default(NodeColor);
					for (int k = 0; k < nativeArray3.Length; k++)
					{
						value3.m_Index = (byte)activeData.m_Index;
						value3.m_Value = (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, nativeArray3[k].m_Accumulation.y) * 255f), 0, 255);
						results[k] = value3;
					}
					break;
				}
				case NetStatusType.TrafficVolume:
				{
					NativeArray<Road> nativeArray = chunk.GetNativeArray(ref this.m_RoadType);
					NodeColor value = default(NodeColor);
					for (int i = 0; i < nativeArray.Length; i++)
					{
						Road road = nativeArray[i];
						float4 x = math.sqrt((road.m_TrafficFlowDistance0 + road.m_TrafficFlowDistance1) * 2.6666667f);
						value.m_Index = (byte)activeData.m_Index;
						value.m_Value = (byte)math.clamp(Mathf.RoundToInt(InfoviewUtils.GetColor(statusData, math.csum(x) * 0.25f) * 255f), 0, 255);
						results[i] = value;
					}
					break;
				}
				}
			}

			private bool GetNetGeometryColor(ArchetypeChunk chunk, out int index)
			{
				index = 0;
				int num = int.MaxValue;
				for (int i = 0; i < this.m_InfomodeChunks.Length; i++)
				{
					ArchetypeChunk archetypeChunk = this.m_InfomodeChunks[i];
					NativeArray<InfoviewNetGeometryData> nativeArray = archetypeChunk.GetNativeArray(ref this.m_InfoviewNetGeometryType);
					if (nativeArray.Length == 0)
					{
						continue;
					}
					NativeArray<InfomodeActive> nativeArray2 = archetypeChunk.GetNativeArray(ref this.m_InfomodeActiveType);
					for (int j = 0; j < nativeArray.Length; j++)
					{
						InfomodeActive infomodeActive = nativeArray2[j];
						int priority = infomodeActive.m_Priority;
						if (priority < num && this.HasNetGeometryColor(nativeArray[j], chunk))
						{
							index = infomodeActive.m_Index;
							num = priority;
						}
					}
				}
				return num != int.MaxValue;
			}

			private bool HasNetGeometryColor(InfoviewNetGeometryData infoviewNetGeometryData, ArchetypeChunk chunk)
			{
				return infoviewNetGeometryData.m_Type switch
				{
					NetType.TrainTrack => chunk.Has(ref this.m_TrainTrackType), 
					NetType.TramTrack => chunk.Has(ref this.m_TramTrackType), 
					NetType.Waterway => chunk.Has(ref this.m_WaterwayType), 
					NetType.SubwayTrack => chunk.Has(ref this.m_SubwayTrackType), 
					NetType.Road => chunk.Has(ref this.m_RoadType), 
					_ => false, 
				};
			}

			void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				this.Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
			}
		}

		[BurstCompile]
		private struct UpdateEdgeColors2Job : IJobChunk
		{
			[ReadOnly]
			public ComponentLookup<NodeColor> m_ColorData;

			[ReadOnly]
			public ComponentTypeHandle<Edge> m_EdgeType;

			[ReadOnly]
			public ComponentTypeHandle<StartNodeGeometry> m_StartNodeGeometryType;

			[ReadOnly]
			public ComponentTypeHandle<EndNodeGeometry> m_EndNodeGeometryType;

			public ComponentTypeHandle<EdgeColor> m_ColorType;

			public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				NativeArray<Edge> nativeArray = chunk.GetNativeArray(ref this.m_EdgeType);
				NativeArray<StartNodeGeometry> nativeArray2 = chunk.GetNativeArray(ref this.m_StartNodeGeometryType);
				NativeArray<EndNodeGeometry> nativeArray3 = chunk.GetNativeArray(ref this.m_EndNodeGeometryType);
				NativeArray<EdgeColor> nativeArray4 = chunk.GetNativeArray(ref this.m_ColorType);
				for (int i = 0; i < nativeArray.Length; i++)
				{
					Edge edge = nativeArray[i];
					EdgeColor value = nativeArray4[i];
					bool2 @bool = false;
					if (nativeArray2.Length != 0)
					{
						StartNodeGeometry startNodeGeometry = nativeArray2[i];
						if (math.any(startNodeGeometry.m_Geometry.m_Left.m_Length > 0.05f) | math.any(startNodeGeometry.m_Geometry.m_Right.m_Length > 0.05f))
						{
							@bool.x = true;
						}
					}
					if (nativeArray3.Length != 0)
					{
						EndNodeGeometry endNodeGeometry = nativeArray3[i];
						if (math.any(endNodeGeometry.m_Geometry.m_Left.m_Length > 0.05f) | math.any(endNodeGeometry.m_Geometry.m_Right.m_Length > 0.05f))
						{
							@bool.y = true;
						}
					}
					if (!@bool.x && this.m_ColorData.TryGetComponent(edge.m_Start, out var componentData) && componentData.m_Index == value.m_Index)
					{
						value.m_Value0 = componentData.m_Value;
					}
					if (!@bool.y && this.m_ColorData.TryGetComponent(edge.m_End, out var componentData2) && componentData2.m_Index == value.m_Index)
					{
						value.m_Value1 = componentData2.m_Value;
					}
					nativeArray4[i] = value;
				}
			}

			void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				this.Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
			}
		}

		[BurstCompile]
		private struct LaneColorJob : IJobChunk
		{
			private interface IFlowImplementation
			{
				Entity sinkNode { get; }

				int multiplier { get; }

				bool TryGetFlowNode(Entity entity, out Entity flowNode);

				bool TryGetFlowEdge(Entity startNode, Entity endNode, out int flow, out int capacity, out float warning);

				void GetConsumption(Entity building, out int wantedConsumption, out int fulfilledConsumption, out float warning);
			}

			private struct ElectricityFlow : IFlowImplementation
			{
				[ReadOnly]
				public ComponentLookup<ElectricityNodeConnection> m_NodeConnectionData;

				[ReadOnly]
				public ComponentLookup<ElectricityFlowEdge> m_FlowEdgeData;

				[ReadOnly]
				public ComponentLookup<ElectricityBuildingConnection> m_BuildingConnectionData;

				[ReadOnly]
				public ComponentLookup<ElectricityConsumer> m_ConsumerData;

				[ReadOnly]
				public BufferLookup<ConnectedFlowEdge> m_ConnectedFlowEdges;

				public Entity sinkNode { get; set; }

				public int multiplier => 1;

				public bool TryGetFlowNode(Entity entity, out Entity flowNode)
				{
					if (this.m_NodeConnectionData.TryGetComponent(entity, out var componentData))
					{
						flowNode = componentData.m_ElectricityNode;
						return true;
					}
					flowNode = default(Entity);
					return false;
				}

				public bool TryGetFlowEdge(Entity startNode, Entity endNode, out int flow, out int capacity, out float warning)
				{
					if (ElectricityGraphUtils.TryGetFlowEdge(startNode, endNode, ref this.m_ConnectedFlowEdges, ref this.m_FlowEdgeData, out ElectricityFlowEdge edge))
					{
						flow = edge.m_Flow;
						capacity = edge.m_Capacity;
						warning = math.select(0f, 0.75f, (edge.m_Flags & ElectricityFlowEdgeFlags.BeyondBottleneck) != 0);
						warning = math.select(warning, 1f, (edge.m_Flags & ElectricityFlowEdgeFlags.Bottleneck) != 0);
						return true;
					}
					flow = (capacity = 0);
					warning = 0f;
					return false;
				}

				public void GetConsumption(Entity building, out int wantedConsumption, out int fulfilledConsumption, out float warning)
				{
					if (this.m_ConsumerData.TryGetComponent(building, out var componentData) && !this.m_BuildingConnectionData.HasComponent(building))
					{
						wantedConsumption = componentData.m_WantedConsumption;
						fulfilledConsumption = componentData.m_FulfilledConsumption;
						warning = math.select(0f, 0.75f, (componentData.m_Flags & ElectricityConsumerFlags.BottleneckWarning) != 0);
					}
					else
					{
						wantedConsumption = (fulfilledConsumption = 0);
						warning = 0f;
					}
				}
			}

			private struct WaterFlow : IFlowImplementation
			{
				[ReadOnly]
				public ComponentLookup<WaterPipeNodeConnection> m_NodeConnectionData;

				[ReadOnly]
				public ComponentLookup<WaterPipeEdge> m_FlowEdgeData;

				[ReadOnly]
				public ComponentLookup<WaterPipeBuildingConnection> m_BuildingConnectionData;

				[ReadOnly]
				public ComponentLookup<WaterConsumer> m_ConsumerData;

				[ReadOnly]
				public BufferLookup<ConnectedFlowEdge> m_ConnectedFlowEdges;

				public float m_MaxToleratedPollution;

				public Entity sinkNode { get; set; }

				public int multiplier => 1;

				public bool TryGetFlowNode(Entity entity, out Entity flowNode)
				{
					if (this.m_NodeConnectionData.TryGetComponent(entity, out var componentData))
					{
						flowNode = componentData.m_WaterPipeNode;
						return true;
					}
					flowNode = default(Entity);
					return false;
				}

				public bool TryGetFlowEdge(Entity startNode, Entity endNode, out int flow, out int capacity, out float warning)
				{
					if (WaterPipeGraphUtils.TryGetFlowEdge(startNode, endNode, ref this.m_ConnectedFlowEdges, ref this.m_FlowEdgeData, out WaterPipeEdge edge))
					{
						flow = edge.m_FreshFlow;
						capacity = 10000;
						warning = math.saturate(edge.m_FreshPollution / this.m_MaxToleratedPollution);
						return true;
					}
					flow = (capacity = 0);
					warning = 0f;
					return false;
				}

				public void GetConsumption(Entity building, out int wantedConsumption, out int fulfilledConsumption, out float warning)
				{
					if (this.m_ConsumerData.TryGetComponent(building, out var componentData) && !this.m_BuildingConnectionData.HasComponent(building))
					{
						wantedConsumption = componentData.m_WantedConsumption;
						fulfilledConsumption = componentData.m_FulfilledFresh;
						warning = math.select(0f, 1f, componentData.m_Pollution > 0f);
					}
					else
					{
						wantedConsumption = (fulfilledConsumption = 0);
						warning = 0f;
					}
				}
			}

			private struct SewageFlow : IFlowImplementation
			{
				[ReadOnly]
				public ComponentLookup<WaterPipeNodeConnection> m_NodeConnectionData;

				[ReadOnly]
				public ComponentLookup<WaterPipeEdge> m_FlowEdgeData;

				[ReadOnly]
				public ComponentLookup<WaterPipeBuildingConnection> m_BuildingConnectionData;

				[ReadOnly]
				public ComponentLookup<WaterConsumer> m_ConsumerData;

				[ReadOnly]
				public BufferLookup<ConnectedFlowEdge> m_ConnectedFlowEdges;

				public Entity sinkNode { get; set; }

				public int multiplier => -1;

				public bool TryGetFlowNode(Entity entity, out Entity flowNode)
				{
					if (this.m_NodeConnectionData.TryGetComponent(entity, out var componentData))
					{
						flowNode = componentData.m_WaterPipeNode;
						return true;
					}
					flowNode = default(Entity);
					return false;
				}

				public bool TryGetFlowEdge(Entity startNode, Entity endNode, out int flow, out int capacity, out float warning)
				{
					if (WaterPipeGraphUtils.TryGetFlowEdge(startNode, endNode, ref this.m_ConnectedFlowEdges, ref this.m_FlowEdgeData, out WaterPipeEdge edge))
					{
						flow = edge.m_SewageFlow;
						capacity = 10000;
						warning = 0f;
						return true;
					}
					flow = (capacity = 0);
					warning = 0f;
					return false;
				}

				public void GetConsumption(Entity building, out int wantedConsumption, out int fulfilledConsumption, out float warning)
				{
					if (this.m_ConsumerData.TryGetComponent(building, out var componentData) && !this.m_BuildingConnectionData.HasComponent(building))
					{
						wantedConsumption = componentData.m_WantedConsumption;
						fulfilledConsumption = componentData.m_FulfilledSewage;
					}
					else
					{
						wantedConsumption = (fulfilledConsumption = 0);
					}
					warning = 0f;
				}
			}

			[ReadOnly]
			public ComponentTypeHandle<Owner> m_OwnerType;

			[ReadOnly]
			public ComponentTypeHandle<Curve> m_CurveType;

			[ReadOnly]
			public ComponentTypeHandle<EdgeLane> m_EdgeLaneType;

			[ReadOnly]
			public ComponentTypeHandle<NodeLane> m_NodeLaneType;

			[ReadOnly]
			public ComponentTypeHandle<Game.Net.TrackLane> m_TrackLaneType;

			[ReadOnly]
			public ComponentTypeHandle<Game.Net.UtilityLane> m_UtilityLaneType;

			[ReadOnly]
			public ComponentTypeHandle<Game.Net.SecondaryLane> m_SecondaryLaneType;

			[ReadOnly]
			public ComponentTypeHandle<EdgeMapping> m_EdgeMappingType;

			[ReadOnly]
			public ComponentTypeHandle<Temp> m_TempType;

			[ReadOnly]
			public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

			public ComponentTypeHandle<LaneColor> m_ColorType;

			public BufferTypeHandle<SubFlow> m_SubFlowType;

			[ReadOnly]
			public NativeList<ArchetypeChunk> m_InfomodeChunks;

			[ReadOnly]
			public ComponentTypeHandle<InfomodeActive> m_InfomodeActiveType;

			[ReadOnly]
			public ComponentTypeHandle<InfoviewNetGeometryData> m_InfoviewNetGeometryType;

			[ReadOnly]
			public ComponentTypeHandle<InfoviewNetStatusData> m_InfoviewNetStatusType;

			[ReadOnly]
			public ComponentLookup<Owner> m_OwnerData;

			[ReadOnly]
			public ComponentLookup<Edge> m_EdgeData;

			[ReadOnly]
			public ComponentLookup<Curve> m_CurveData;

			[ReadOnly]
			public ComponentLookup<Game.Objects.Color> m_ObjectColorData;

			[ReadOnly]
			public ComponentLookup<NodeColor> m_NodeColorData;

			[ReadOnly]
			public ComponentLookup<EdgeColor> m_EdgeColorData;

			[ReadOnly]
			public ComponentLookup<ElectricityNodeConnection> m_ElectricityNodeConnectionData;

			[ReadOnly]
			public ComponentLookup<ElectricityFlowEdge> m_ElectricityFlowEdgeData;

			[ReadOnly]
			public ComponentLookup<ElectricityBuildingConnection> m_ElectricityBuildingConnectionData;

			[ReadOnly]
			public ComponentLookup<WaterPipeNodeConnection> m_WaterPipeNodeConnectionData;

			[ReadOnly]
			public ComponentLookup<WaterPipeEdge> m_WaterPipeEdgeData;

			[ReadOnly]
			public ComponentLookup<WaterPipeBuildingConnection> m_WaterPipeBuildingConnectionData;

			[ReadOnly]
			public ComponentLookup<Building> m_BuildingData;

			[ReadOnly]
			public ComponentLookup<ElectricityConsumer> m_ElectricityConsumerData;

			[ReadOnly]
			public ComponentLookup<WaterConsumer> m_WaterConsumerData;

			[ReadOnly]
			public ComponentLookup<Temp> m_TempData;

			[ReadOnly]
			public ComponentLookup<TrackLaneData> m_PrefabTrackLaneData;

			[ReadOnly]
			public ComponentLookup<UtilityLaneData> m_PrefabUtilityLaneData;

			[ReadOnly]
			public ComponentLookup<NetLaneData> m_PrefabNetLaneData;

			[ReadOnly]
			public BufferLookup<ConnectedNode> m_ConnectedNodes;

			[ReadOnly]
			public BufferLookup<ConnectedBuilding> m_ConnectedBuildings;

			[ReadOnly]
			public BufferLookup<ConnectedFlowEdge> m_ConnectedFlowEdges;

			public Entity m_ElectricitySinkNode;

			public Entity m_WaterSinkNode;

			public WaterPipeParameterData m_WaterPipeParameters;

			public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				NativeArray<LaneColor> nativeArray = chunk.GetNativeArray(ref this.m_ColorType);
				NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref this.m_OwnerType);
				NativeArray<Curve> nativeArray3 = chunk.GetNativeArray(ref this.m_CurveType);
				NativeArray<EdgeLane> nativeArray4 = chunk.GetNativeArray(ref this.m_EdgeLaneType);
				NativeArray<NodeLane> nativeArray5 = chunk.GetNativeArray(ref this.m_NodeLaneType);
				int num = 0;
				int num2 = 0;
				int num3 = 0;
				int num4 = 0;
				int num5 = 0;
				int num6 = 0;
				float num7 = 0f;
				float num8 = 0f;
				float num9 = 0f;
				float num10 = 0f;
				bool flag = chunk.Has(ref this.m_TrackLaneType);
				bool flag2 = chunk.Has(ref this.m_UtilityLaneType);
				bool flag3 = chunk.Has(ref this.m_SecondaryLaneType);
				bool flag4 = chunk.Has(ref this.m_TempType);
				NativeArray<EdgeMapping> nativeArray6 = default(NativeArray<EdgeMapping>);
				NativeArray<PrefabRef> nativeArray7 = default(NativeArray<PrefabRef>);
				BufferAccessor<SubFlow> bufferAccessor = default(BufferAccessor<SubFlow>);
				if (flag)
				{
					nativeArray7 = chunk.GetNativeArray(ref this.m_PrefabRefType);
					int num11 = int.MaxValue;
					int num12 = int.MaxValue;
					for (int i = 0; i < this.m_InfomodeChunks.Length; i++)
					{
						ArchetypeChunk archetypeChunk = this.m_InfomodeChunks[i];
						NativeArray<InfoviewNetGeometryData> nativeArray8 = archetypeChunk.GetNativeArray(ref this.m_InfoviewNetGeometryType);
						if (nativeArray8.Length == 0)
						{
							continue;
						}
						NativeArray<InfomodeActive> nativeArray9 = archetypeChunk.GetNativeArray(ref this.m_InfomodeActiveType);
						for (int j = 0; j < nativeArray8.Length; j++)
						{
							InfoviewNetGeometryData infoviewNetGeometryData = nativeArray8[j];
							InfomodeActive infomodeActive = nativeArray9[j];
							int priority = infomodeActive.m_Priority;
							switch (infoviewNetGeometryData.m_Type)
							{
							case NetType.TrainTrack:
								if (priority < num11)
								{
									num = infomodeActive.m_Index;
									num11 = priority;
								}
								break;
							case NetType.TramTrack:
								if (priority < num12)
								{
									num2 = infomodeActive.m_Index;
									num12 = priority;
								}
								break;
							}
						}
					}
				}
				if (flag2)
				{
					nativeArray6 = chunk.GetNativeArray(ref this.m_EdgeMappingType);
					nativeArray7 = chunk.GetNativeArray(ref this.m_PrefabRefType);
					bufferAccessor = chunk.GetBufferAccessor(ref this.m_SubFlowType);
					int num13 = int.MaxValue;
					int num14 = int.MaxValue;
					int num15 = int.MaxValue;
					int num16 = int.MaxValue;
					for (int k = 0; k < this.m_InfomodeChunks.Length; k++)
					{
						ArchetypeChunk archetypeChunk2 = this.m_InfomodeChunks[k];
						NativeArray<InfoviewNetStatusData> nativeArray10 = archetypeChunk2.GetNativeArray(ref this.m_InfoviewNetStatusType);
						if (nativeArray10.Length == 0)
						{
							continue;
						}
						NativeArray<InfomodeActive> nativeArray11 = archetypeChunk2.GetNativeArray(ref this.m_InfomodeActiveType);
						for (int l = 0; l < nativeArray10.Length; l++)
						{
							InfoviewNetStatusData infoviewNetStatusData = nativeArray10[l];
							InfomodeActive infomodeActive2 = nativeArray11[l];
							int priority2 = infomodeActive2.m_Priority;
							switch (infoviewNetStatusData.m_Type)
							{
							case NetStatusType.LowVoltageFlow:
								if (priority2 < num13)
								{
									num3 = infomodeActive2.m_Index;
									num7 = infoviewNetStatusData.m_Tiling;
									num13 = priority2;
								}
								break;
							case NetStatusType.HighVoltageFlow:
								if (priority2 < num14)
								{
									num4 = infomodeActive2.m_Index;
									num8 = infoviewNetStatusData.m_Tiling;
									num14 = priority2;
								}
								break;
							case NetStatusType.PipeWaterFlow:
								if (priority2 < num15)
								{
									num5 = infomodeActive2.m_Index;
									num9 = infoviewNetStatusData.m_Tiling;
									num15 = priority2;
								}
								break;
							case NetStatusType.PipeSewageFlow:
								if (priority2 < num16)
								{
									num6 = infomodeActive2.m_Index;
									num10 = infoviewNetStatusData.m_Tiling;
									num16 = priority2;
								}
								break;
							}
						}
					}
				}
				bool flag5 = flag && (num != 0 || num2 != 0);
				bool flag6 = flag2 && bufferAccessor.Length != 0 && (num3 != 0 || num4 != 0 || num5 != 0 || num6 != 0);
				for (int m = 0; m < nativeArray.Length; m++)
				{
					if (flag5)
					{
						PrefabRef prefabRef = nativeArray7[m];
						if (this.m_PrefabTrackLaneData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
						{
							if ((componentData.m_TrackTypes & TrackTypes.Train) != 0 && num != 0)
							{
								nativeArray[m] = new LaneColor((byte)num, 0, 0);
								continue;
							}
							if ((componentData.m_TrackTypes & TrackTypes.Tram) != 0 && num2 != 0)
							{
								nativeArray[m] = new LaneColor((byte)num2, 0, 0);
								continue;
							}
						}
					}
					if (flag6)
					{
						PrefabRef prefabRef2 = nativeArray7[m];
						if (this.m_PrefabUtilityLaneData.TryGetComponent(prefabRef2.m_Prefab, out var componentData2))
						{
							int num17 = 0;
							float num18 = 0f;
							if ((componentData2.m_UtilityTypes & UtilityTypes.LowVoltageLine) != 0 && num3 != 0)
							{
								num17 = num3;
								num18 = num7;
							}
							else if ((componentData2.m_UtilityTypes & UtilityTypes.HighVoltageLine) != 0 && num4 != 0)
							{
								num17 = num4;
								num18 = num8;
							}
							else if ((componentData2.m_UtilityTypes & UtilityTypes.WaterPipe) != 0 && num5 != 0)
							{
								num17 = num5;
								num18 = num9;
							}
							else if ((componentData2.m_UtilityTypes & UtilityTypes.SewagePipe) != 0 && num6 != 0)
							{
								num17 = num6;
								num18 = num10;
							}
							if (num17 != 0)
							{
								Curve curve = nativeArray3[m];
								EdgeMapping edgeMapping = nativeArray6[m];
								DynamicBuffer<SubFlow> dynamicBuffer = bufferAccessor[m];
								Owner owner = default(Owner);
								if (nativeArray2.Length != 0)
								{
									owner = nativeArray2[m];
								}
								if (dynamicBuffer.Length != 16)
								{
									dynamicBuffer.ResizeUninitialized(16);
								}
								NativeArray<SubFlow> nativeArray12 = dynamicBuffer.AsNativeArray();
								float warning = 0f;
								if (edgeMapping.m_Parent1 != Entity.Null)
								{
									if (this.m_EdgeData.HasComponent(edgeMapping.m_Parent1))
									{
										if (flag4)
										{
											if (edgeMapping.m_Parent2 != Entity.Null)
											{
												MathUtils.Divide(curve.m_Bezier, out var output, out var output2, 0.5f);
												this.GetOriginalEdge(output, ref edgeMapping.m_Parent1, ref edgeMapping.m_CurveDelta1);
												this.GetOriginalEdge(output2, ref edgeMapping.m_Parent2, ref edgeMapping.m_CurveDelta2);
											}
											else
											{
												this.GetOriginalEdge(curve.m_Bezier, ref edgeMapping.m_Parent1, ref edgeMapping.m_CurveDelta1);
											}
										}
										if (num17 == num3 || num17 == num4)
										{
											this.FillEdgeFlow(this.GetElectricityFlow(), nativeArray12, edgeMapping, out warning);
										}
										else if (num17 == num5)
										{
											this.FillEdgeFlow(this.GetWaterFlow(), nativeArray12, edgeMapping, out warning);
										}
										else if (num17 == num6)
										{
											this.FillEdgeFlow(this.GetSewageFlow(), nativeArray12, edgeMapping, out warning);
										}
										else
										{
											nativeArray12.Fill(default(SubFlow));
										}
									}
									else
									{
										if (flag4)
										{
											this.GetOriginalNode(ref edgeMapping.m_Parent1);
											this.GetOriginalEdge(curve.m_Bezier, ref edgeMapping.m_Parent2, ref edgeMapping.m_CurveDelta2);
										}
										if (num17 == num3 || num17 == num4)
										{
											this.FillNodeFlow(this.GetElectricityFlow(), nativeArray12, edgeMapping, out warning);
										}
										else if (num17 == num5)
										{
											this.FillNodeFlow(this.GetWaterFlow(), nativeArray12, edgeMapping, out warning);
										}
										else if (num17 == num6)
										{
											this.FillNodeFlow(this.GetSewageFlow(), nativeArray12, edgeMapping, out warning);
										}
										else
										{
											nativeArray12.Fill(default(SubFlow));
										}
									}
								}
								else if (flag3)
								{
									if (num17 == num3 || num17 == num4)
									{
										this.FillBuildingFlow(this.GetElectricityFlow(), nativeArray12, owner.m_Owner, out warning);
									}
									else if (num17 == num5)
									{
										this.FillBuildingFlow(this.GetWaterFlow(), nativeArray12, owner.m_Owner, out warning);
									}
									else if (num17 == num6)
									{
										this.FillBuildingFlow(this.GetSewageFlow(), nativeArray12, owner.m_Owner, out warning);
									}
									else
									{
										nativeArray12.Fill(default(SubFlow));
									}
								}
								else
								{
									num17 = 0;
								}
								if (num17 != 0)
								{
									int2 @int = new int2(dynamicBuffer[0].m_Value, dynamicBuffer[15].m_Value);
									bool flag7 = (((@int.x ^ @int.y) & 0x80) != 0) & math.all(@int != 0);
									int num19 = math.clamp(Mathf.RoundToInt(curve.m_Length * num18), 1, 255);
									int num20 = math.clamp(Mathf.RoundToInt(warning * 255f), 0, 255);
									num19 = math.select(num19, 2, num19 == 1 && flag7);
									nativeArray[m] = new LaneColor((byte)num17, (byte)num19, (byte)num20);
									continue;
								}
							}
						}
					}
					if (nativeArray2.Length != 0)
					{
						Owner owner2 = nativeArray2[m];
						if (nativeArray4.Length != 0)
						{
							if (this.m_EdgeColorData.TryGetComponent(owner2.m_Owner, out var componentData3))
							{
								float2 @float = math.lerp((float)(int)componentData3.m_Value0, (float)(int)componentData3.m_Value1, nativeArray4[m].m_EdgeDelta);
								nativeArray[m] = new LaneColor(componentData3.m_Index, (byte)Mathf.RoundToInt(@float.x), (byte)Mathf.RoundToInt(@float.y));
								continue;
							}
						}
						else if (nativeArray5.Length != 0)
						{
							if (this.m_NodeColorData.TryGetComponent(owner2.m_Owner, out var componentData4))
							{
								nativeArray[m] = new LaneColor(componentData4.m_Index, componentData4.m_Value, componentData4.m_Value);
								continue;
							}
						}
						else
						{
							PrefabRef prefabRef3 = nativeArray7[m];
							if ((this.m_PrefabNetLaneData[prefabRef3.m_Prefab].m_Flags & Game.Prefabs.LaneFlags.Underground) == 0)
							{
								Game.Objects.Color componentData5;
								while (!this.m_ObjectColorData.TryGetComponent(owner2.m_Owner, out componentData5))
								{
									if (this.m_OwnerData.TryGetComponent(owner2.m_Owner, out var componentData6))
									{
										owner2 = componentData6;
										continue;
									}
									goto IL_0935;
								}
								if (componentData5.m_SubColor)
								{
									nativeArray[m] = new LaneColor(componentData5.m_Index, componentData5.m_Value, componentData5.m_Value);
									continue;
								}
							}
						}
					}
					goto IL_0935;
					IL_0935:
					nativeArray[m] = default(LaneColor);
				}
			}

			private void GetOriginalEdge(Bezier4x3 laneCurve, ref Entity parent, ref float2 curveMapping)
			{
				if (!this.m_TempData.TryGetComponent(parent, out var componentData))
				{
					return;
				}
				Edge componentData2;
				Temp componentData3;
				Temp componentData4;
				if (componentData.m_Original != Entity.Null)
				{
					parent = componentData.m_Original;
				}
				else if (this.m_EdgeData.TryGetComponent(parent, out componentData2) && this.m_TempData.TryGetComponent(componentData2.m_Start, out componentData3) && this.m_TempData.TryGetComponent(componentData2.m_End, out componentData4) && componentData3.m_Original != Entity.Null && componentData4.m_Original != Entity.Null)
				{
					Curve componentData6;
					if (this.m_CurveData.TryGetComponent(componentData3.m_Original, out var componentData5))
					{
						parent = componentData3.m_Original;
						MathUtils.Distance(componentData5.m_Bezier.xz, laneCurve.a.xz, out curveMapping.x);
						MathUtils.Distance(componentData5.m_Bezier.xz, laneCurve.d.xz, out curveMapping.y);
					}
					else if (this.m_CurveData.TryGetComponent(componentData4.m_Original, out componentData6))
					{
						parent = componentData4.m_Original;
						MathUtils.Distance(componentData6.m_Bezier.xz, laneCurve.a.xz, out curveMapping.x);
						MathUtils.Distance(componentData6.m_Bezier.xz, laneCurve.d.xz, out curveMapping.y);
					}
				}
			}

			private void GetOriginalNode(ref Entity parent)
			{
				if (this.m_TempData.TryGetComponent(parent, out var componentData))
				{
					parent = componentData.m_Original;
				}
			}

			private void FillEdgeFlow<T>(T impl, NativeArray<SubFlow> flowArray, EdgeMapping edgeMapping, out float warning) where T : struct, IFlowImplementation
			{
				if (edgeMapping.m_Parent2 != Entity.Null)
				{
					this.FillEdgeFlow(impl, flowArray.GetSubArray(0, 8), edgeMapping.m_Parent1, edgeMapping.m_CurveDelta1, out warning);
					this.FillEdgeFlow(impl, flowArray.GetSubArray(8, 8), edgeMapping.m_Parent2, edgeMapping.m_CurveDelta2, out warning);
				}
				else
				{
					this.FillEdgeFlow(impl, flowArray, edgeMapping.m_Parent1, edgeMapping.m_CurveDelta1, out warning);
				}
			}

			private unsafe void FillEdgeFlow<T>(T impl, NativeArray<SubFlow> flows, Entity edge, float2 curveMapping, out float warning) where T : struct, IFlowImplementation
			{
				if (this.m_EdgeData.TryGetComponent(edge, out var componentData) && impl.TryGetFlowNode(edge, out var flowNode) && impl.TryGetFlowNode(componentData.m_Start, out var flowNode2) && impl.TryGetFlowNode(componentData.m_End, out var flowNode3) && impl.TryGetFlowEdge(flowNode2, flowNode, out var flow, out var capacity, out var warning2) && impl.TryGetFlowEdge(flowNode, flowNode3, out var flow2, out var capacity2, out var warning3))
				{
					capacity = math.max(1, capacity);
					if (curveMapping.y < curveMapping.x)
					{
						capacity2 = -flow2;
						int num = -flow;
						flow = capacity2;
						flow2 = num;
					}
					int* ptr = stackalloc int[flows.Length];
					float warning4;
					if (this.m_ConnectedNodes.TryGetBuffer(edge, out var bufferData))
					{
						foreach (ConnectedNode item in bufferData)
						{
							if (impl.TryGetFlowNode(item.m_Node, out var flowNode4) && impl.TryGetFlowEdge(flowNode4, flowNode, out var flow3, out capacity2, out warning4))
							{
								LaneColorJob.AddTempFlow(flow3, item.m_CurvePosition, ptr, flows.Length, curveMapping);
							}
						}
					}
					if (impl.TryGetFlowEdge(flowNode, impl.sinkNode, out var flow4, out capacity2, out warning4) && this.m_ConnectedBuildings.TryGetBuffer(edge, out var bufferData2))
					{
						int totalDemand = 0;
						foreach (ConnectedBuilding item2 in bufferData2)
						{
							impl.GetConsumption(item2.m_Building, out var wantedConsumption, out capacity2, out warning4);
							totalDemand += wantedConsumption;
						}
						foreach (ConnectedBuilding item3 in bufferData2)
						{
							impl.GetConsumption(item3.m_Building, out var wantedConsumption2, out capacity2, out warning4);
							LaneColorJob.AddTempFlow(-FlowUtils.ConsumeFromTotal(wantedConsumption2, ref flow4, ref totalDemand), this.m_BuildingData[item3.m_Building].m_CurvePosition, ptr, flows.Length, curveMapping);
						}
					}
					int num2 = flow;
					for (int i = 0; i < flows.Length; i++)
					{
						num2 += ptr[i];
						flows[i] = LaneColorJob.GetSubFlow(impl.multiplier * num2, capacity);
					}
					if (MathUtils.Max(curveMapping) == 1f)
					{
						flows[flows.Length - 1] = LaneColorJob.GetSubFlow(impl.multiplier * flow2, capacity);
					}
					warning = math.max(warning2, warning3);
				}
				else
				{
					flows.Fill(default(SubFlow));
					warning = 0f;
				}
			}

			private unsafe static void AddTempFlow(int flow, float curvePosition, int* tempFlows, int length, float2 curveMapping)
			{
				float num = curveMapping.y - curveMapping.x;
				if (num != 0f)
				{
					float num2 = (curvePosition - curveMapping.x) / num;
					if (num2 < 0f)
					{
						*tempFlows += flow;
					}
					else if (num2 < 1f)
					{
						int num3 = math.clamp(Mathf.RoundToInt(num2 * (float)(length - 1)), 1, length - 1);
						tempFlows[num3] += flow;
					}
				}
				else if (curvePosition < curveMapping.x)
				{
					*tempFlows += flow;
				}
			}

			private static SubFlow GetSubFlow(int flow, int capacity)
			{
				int num = 127 * flow / capacity;
				SubFlow result = default(SubFlow);
				result.m_Value = (sbyte)((num != 0) ? math.clamp(num, -127, 127) : math.clamp(flow, -1, 1));
				return result;
			}

			private void FillNodeFlow<T>(T impl, NativeArray<SubFlow> flows, EdgeMapping edgeMapping, out float warning) where T : struct, IFlowImplementation
			{
				this.FillNodeFlow(impl, flows, edgeMapping.m_Parent1, edgeMapping.m_Parent2, edgeMapping.m_CurveDelta1, out warning);
			}

			private void FillNodeFlow<T>(T impl, NativeArray<SubFlow> flows, Entity node, Entity edge, float2 curveMapping, out float warning) where T : struct, IFlowImplementation
			{
				float num = 0f;
				if (impl.TryGetFlowNode(node, out var flowNode) && impl.TryGetFlowNode(edge, out var flowNode2))
				{
					if (impl.TryGetFlowEdge(flowNode, flowNode2, out var flow, out var capacity, out warning))
					{
						num = (float)flow / (float)capacity;
					}
					else if (impl.TryGetFlowEdge(flowNode2, flowNode, out flow, out capacity, out warning))
					{
						num = (float)(-flow) / (float)capacity;
					}
				}
				else
				{
					warning = 0f;
				}
				num = math.select(num, 0f - num, curveMapping.y < curveMapping.x);
				SubFlow subFlow = this.GetSubFlow((float)impl.multiplier * num);
				flows.Fill(subFlow);
			}

			private void FillBuildingFlow<T>(T impl, NativeArray<SubFlow> flows, Entity building, out float warning) where T : struct, IFlowImplementation
			{
				impl.GetConsumption(building, out var _, out var fulfilledConsumption, out warning);
				float num = (0f - (float)fulfilledConsumption) / (float)(10000 + math.abs(fulfilledConsumption));
				SubFlow subFlow = this.GetSubFlow((float)impl.multiplier * num);
				flows.Fill(subFlow);
			}

			private SubFlow GetSubFlow(float value)
			{
				int num = math.clamp(Mathf.RoundToInt(value * 127f), -127, 127);
				num = math.select(num, 1, num == 0 && value > 0f);
				num = math.select(num, -1, num == 0 && value < 0f);
				SubFlow result = default(SubFlow);
				result.m_Value = (sbyte)num;
				return result;
			}

			private ElectricityFlow GetElectricityFlow()
			{
				ElectricityFlow result = default(ElectricityFlow);
				result.sinkNode = this.m_ElectricitySinkNode;
				result.m_NodeConnectionData = this.m_ElectricityNodeConnectionData;
				result.m_FlowEdgeData = this.m_ElectricityFlowEdgeData;
				result.m_BuildingConnectionData = this.m_ElectricityBuildingConnectionData;
				result.m_ConsumerData = this.m_ElectricityConsumerData;
				result.m_ConnectedFlowEdges = this.m_ConnectedFlowEdges;
				return result;
			}

			private WaterFlow GetWaterFlow()
			{
				WaterFlow result = default(WaterFlow);
				result.sinkNode = this.m_WaterSinkNode;
				result.m_NodeConnectionData = this.m_WaterPipeNodeConnectionData;
				result.m_FlowEdgeData = this.m_WaterPipeEdgeData;
				result.m_BuildingConnectionData = this.m_WaterPipeBuildingConnectionData;
				result.m_ConsumerData = this.m_WaterConsumerData;
				result.m_ConnectedFlowEdges = this.m_ConnectedFlowEdges;
				result.m_MaxToleratedPollution = this.m_WaterPipeParameters.m_MaxToleratedPollution;
				return result;
			}

			private SewageFlow GetSewageFlow()
			{
				SewageFlow result = default(SewageFlow);
				result.sinkNode = this.m_WaterSinkNode;
				result.m_NodeConnectionData = this.m_WaterPipeNodeConnectionData;
				result.m_FlowEdgeData = this.m_WaterPipeEdgeData;
				result.m_BuildingConnectionData = this.m_WaterPipeBuildingConnectionData;
				result.m_ConsumerData = this.m_WaterConsumerData;
				result.m_ConnectedFlowEdges = this.m_ConnectedFlowEdges;
				return result;
			}

			void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				this.Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
			}
		}

		private struct TypeHandle
		{
			[ReadOnly]
			public ComponentTypeHandle<InfomodeActive> __Game_Prefabs_InfomodeActive_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<InfoviewCoverageData> __Game_Prefabs_InfoviewCoverageData_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<InfoviewAvailabilityData> __Game_Prefabs_InfoviewAvailabilityData_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<InfoviewNetGeometryData> __Game_Prefabs_InfoviewNetGeometryData_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<InfoviewNetStatusData> __Game_Prefabs_InfoviewNetStatusData_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<TrainTrack> __Game_Net_TrainTrack_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<TramTrack> __Game_Net_TramTrack_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<Waterway> __Game_Net_Waterway_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<SubwayTrack> __Game_Net_SubwayTrack_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<NetCondition> __Game_Net_NetCondition_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<Road> __Game_Net_Road_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<Game.Net.Pollution> __Game_Net_Pollution_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<Curve> __Game_Net_Curve_RO_ComponentTypeHandle;

			[ReadOnly]
			public BufferTypeHandle<Game.Net.ServiceCoverage> __Game_Net_ServiceCoverage_RO_BufferTypeHandle;

			[ReadOnly]
			public BufferTypeHandle<ResourceAvailability> __Game_Net_ResourceAvailability_RO_BufferTypeHandle;

			[ReadOnly]
			public ComponentLookup<LandValue> __Game_Net_LandValue_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<Node> __Game_Net_Node_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<ZonePropertiesData> __Game_Prefabs_ZonePropertiesData_RO_ComponentLookup;

			[ReadOnly]
			public BufferLookup<ProcessEstimate> __Game_Zones_ProcessEstimate_RO_BufferLookup;

			[ReadOnly]
			public BufferLookup<Game.Net.ServiceCoverage> __Game_Net_ServiceCoverage_RO_BufferLookup;

			[ReadOnly]
			public BufferLookup<ResourceAvailability> __Game_Net_ResourceAvailability_RO_BufferLookup;

			[ReadOnly]
			public ComponentTypeHandle<Edge> __Game_Net_Edge_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

			public ComponentTypeHandle<EdgeColor> __Game_Net_EdgeColor_RW_ComponentTypeHandle;

			[ReadOnly]
			public ComponentLookup<EdgeColor> __Game_Net_EdgeColor_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<StartNodeGeometry> __Game_Net_StartNodeGeometry_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<EndNodeGeometry> __Game_Net_EndNodeGeometry_RO_ComponentLookup;

			[ReadOnly]
			public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

			[ReadOnly]
			public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

			[ReadOnly]
			public BufferTypeHandle<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferTypeHandle;

			public ComponentTypeHandle<NodeColor> __Game_Net_NodeColor_RW_ComponentTypeHandle;

			[ReadOnly]
			public ComponentLookup<NodeColor> __Game_Net_NodeColor_RO_ComponentLookup;

			[ReadOnly]
			public ComponentTypeHandle<StartNodeGeometry> __Game_Net_StartNodeGeometry_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<EndNodeGeometry> __Game_Net_EndNodeGeometry_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<EdgeLane> __Game_Net_EdgeLane_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<NodeLane> __Game_Net_NodeLane_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<Game.Net.TrackLane> __Game_Net_TrackLane_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<Game.Net.UtilityLane> __Game_Net_UtilityLane_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<Game.Net.SecondaryLane> __Game_Net_SecondaryLane_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<EdgeMapping> __Game_Net_EdgeMapping_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

			public ComponentTypeHandle<LaneColor> __Game_Net_LaneColor_RW_ComponentTypeHandle;

			public BufferTypeHandle<SubFlow> __Game_Net_SubFlow_RW_BufferTypeHandle;

			[ReadOnly]
			public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<Game.Objects.Color> __Game_Objects_Color_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<ElectricityNodeConnection> __Game_Simulation_ElectricityNodeConnection_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<ElectricityFlowEdge> __Game_Simulation_ElectricityFlowEdge_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<ElectricityBuildingConnection> __Game_Simulation_ElectricityBuildingConnection_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<WaterPipeNodeConnection> __Game_Simulation_WaterPipeNodeConnection_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<WaterPipeEdge> __Game_Simulation_WaterPipeEdge_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<WaterPipeBuildingConnection> __Game_Simulation_WaterPipeBuildingConnection_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<ElectricityConsumer> __Game_Buildings_ElectricityConsumer_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<WaterConsumer> __Game_Buildings_WaterConsumer_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<TrackLaneData> __Game_Prefabs_TrackLaneData_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<UtilityLaneData> __Game_Prefabs_UtilityLaneData_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<NetLaneData> __Game_Prefabs_NetLaneData_RO_ComponentLookup;

			[ReadOnly]
			public BufferLookup<ConnectedNode> __Game_Net_ConnectedNode_RO_BufferLookup;

			[ReadOnly]
			public BufferLookup<ConnectedBuilding> __Game_Buildings_ConnectedBuilding_RO_BufferLookup;

			[ReadOnly]
			public BufferLookup<ConnectedFlowEdge> __Game_Simulation_ConnectedFlowEdge_RO_BufferLookup;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void __AssignHandles(ref SystemState state)
			{
				this.__Game_Prefabs_InfomodeActive_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InfomodeActive>(isReadOnly: true);
				this.__Game_Prefabs_InfoviewCoverageData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InfoviewCoverageData>(isReadOnly: true);
				this.__Game_Prefabs_InfoviewAvailabilityData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InfoviewAvailabilityData>(isReadOnly: true);
				this.__Game_Prefabs_InfoviewNetGeometryData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InfoviewNetGeometryData>(isReadOnly: true);
				this.__Game_Prefabs_InfoviewNetStatusData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InfoviewNetStatusData>(isReadOnly: true);
				this.__Game_Net_TrainTrack_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TrainTrack>(isReadOnly: true);
				this.__Game_Net_TramTrack_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TramTrack>(isReadOnly: true);
				this.__Game_Net_Waterway_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Waterway>(isReadOnly: true);
				this.__Game_Net_SubwayTrack_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SubwayTrack>(isReadOnly: true);
				this.__Game_Net_NetCondition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<NetCondition>(isReadOnly: true);
				this.__Game_Net_Road_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Road>(isReadOnly: true);
				this.__Game_Net_Pollution_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.Pollution>(isReadOnly: true);
				this.__Game_Net_Curve_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Curve>(isReadOnly: true);
				this.__Game_Net_ServiceCoverage_RO_BufferTypeHandle = state.GetBufferTypeHandle<Game.Net.ServiceCoverage>(isReadOnly: true);
				this.__Game_Net_ResourceAvailability_RO_BufferTypeHandle = state.GetBufferTypeHandle<ResourceAvailability>(isReadOnly: true);
				this.__Game_Net_LandValue_RO_ComponentLookup = state.GetComponentLookup<LandValue>(isReadOnly: true);
				this.__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
				this.__Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Node>(isReadOnly: true);
				this.__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
				this.__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
				this.__Game_Prefabs_ZonePropertiesData_RO_ComponentLookup = state.GetComponentLookup<ZonePropertiesData>(isReadOnly: true);
				this.__Game_Zones_ProcessEstimate_RO_BufferLookup = state.GetBufferLookup<ProcessEstimate>(isReadOnly: true);
				this.__Game_Net_ServiceCoverage_RO_BufferLookup = state.GetBufferLookup<Game.Net.ServiceCoverage>(isReadOnly: true);
				this.__Game_Net_ResourceAvailability_RO_BufferLookup = state.GetBufferLookup<ResourceAvailability>(isReadOnly: true);
				this.__Game_Net_Edge_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Edge>(isReadOnly: true);
				this.__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
				this.__Game_Net_EdgeColor_RW_ComponentTypeHandle = state.GetComponentTypeHandle<EdgeColor>();
				this.__Game_Net_EdgeColor_RO_ComponentLookup = state.GetComponentLookup<EdgeColor>(isReadOnly: true);
				this.__Game_Net_StartNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<StartNodeGeometry>(isReadOnly: true);
				this.__Game_Net_EndNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<EndNodeGeometry>(isReadOnly: true);
				this.__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
				this.__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
				this.__Game_Net_ConnectedEdge_RO_BufferTypeHandle = state.GetBufferTypeHandle<ConnectedEdge>(isReadOnly: true);
				this.__Game_Net_NodeColor_RW_ComponentTypeHandle = state.GetComponentTypeHandle<NodeColor>();
				this.__Game_Net_NodeColor_RO_ComponentLookup = state.GetComponentLookup<NodeColor>(isReadOnly: true);
				this.__Game_Net_StartNodeGeometry_RO_ComponentTypeHandle = state.GetComponentTypeHandle<StartNodeGeometry>(isReadOnly: true);
				this.__Game_Net_EndNodeGeometry_RO_ComponentTypeHandle = state.GetComponentTypeHandle<EndNodeGeometry>(isReadOnly: true);
				this.__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
				this.__Game_Net_EdgeLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<EdgeLane>(isReadOnly: true);
				this.__Game_Net_NodeLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<NodeLane>(isReadOnly: true);
				this.__Game_Net_TrackLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.TrackLane>(isReadOnly: true);
				this.__Game_Net_UtilityLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.UtilityLane>(isReadOnly: true);
				this.__Game_Net_SecondaryLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.SecondaryLane>(isReadOnly: true);
				this.__Game_Net_EdgeMapping_RO_ComponentTypeHandle = state.GetComponentTypeHandle<EdgeMapping>(isReadOnly: true);
				this.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
				this.__Game_Net_LaneColor_RW_ComponentTypeHandle = state.GetComponentTypeHandle<LaneColor>();
				this.__Game_Net_SubFlow_RW_BufferTypeHandle = state.GetBufferTypeHandle<SubFlow>();
				this.__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
				this.__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
				this.__Game_Objects_Color_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Color>(isReadOnly: true);
				this.__Game_Simulation_ElectricityNodeConnection_RO_ComponentLookup = state.GetComponentLookup<ElectricityNodeConnection>(isReadOnly: true);
				this.__Game_Simulation_ElectricityFlowEdge_RO_ComponentLookup = state.GetComponentLookup<ElectricityFlowEdge>(isReadOnly: true);
				this.__Game_Simulation_ElectricityBuildingConnection_RO_ComponentLookup = state.GetComponentLookup<ElectricityBuildingConnection>(isReadOnly: true);
				this.__Game_Simulation_WaterPipeNodeConnection_RO_ComponentLookup = state.GetComponentLookup<WaterPipeNodeConnection>(isReadOnly: true);
				this.__Game_Simulation_WaterPipeEdge_RO_ComponentLookup = state.GetComponentLookup<WaterPipeEdge>(isReadOnly: true);
				this.__Game_Simulation_WaterPipeBuildingConnection_RO_ComponentLookup = state.GetComponentLookup<WaterPipeBuildingConnection>(isReadOnly: true);
				this.__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
				this.__Game_Buildings_ElectricityConsumer_RO_ComponentLookup = state.GetComponentLookup<ElectricityConsumer>(isReadOnly: true);
				this.__Game_Buildings_WaterConsumer_RO_ComponentLookup = state.GetComponentLookup<WaterConsumer>(isReadOnly: true);
				this.__Game_Prefabs_TrackLaneData_RO_ComponentLookup = state.GetComponentLookup<TrackLaneData>(isReadOnly: true);
				this.__Game_Prefabs_UtilityLaneData_RO_ComponentLookup = state.GetComponentLookup<UtilityLaneData>(isReadOnly: true);
				this.__Game_Prefabs_NetLaneData_RO_ComponentLookup = state.GetComponentLookup<NetLaneData>(isReadOnly: true);
				this.__Game_Net_ConnectedNode_RO_BufferLookup = state.GetBufferLookup<ConnectedNode>(isReadOnly: true);
				this.__Game_Buildings_ConnectedBuilding_RO_BufferLookup = state.GetBufferLookup<ConnectedBuilding>(isReadOnly: true);
				this.__Game_Simulation_ConnectedFlowEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedFlowEdge>(isReadOnly: true);
			}
		}

		private EntityQuery m_ZonePreferenceParameterGroup;

		private EntityQuery m_EdgeQuery;

		private EntityQuery m_NodeQuery;

		private EntityQuery m_LaneQuery;

		private EntityQuery m_InfomodeQuery;

		private EntityQuery m_ProcessQuery;

		private ToolSystem m_ToolSystem;

		private ZoneToolSystem m_ZoneToolSystem;

		private ObjectToolSystem m_ObjectToolSystem;

		private IndustrialDemandSystem m_IndustrialDemandSystem;

		private PrefabSystem m_PrefabSystem;

		private ResourceSystem m_ResourceSystem;

		private GroundPollutionSystem m_GroundPollutionSystem;

		private ElectricityFlowSystem m_ElectricityFlowSystem;

		private WaterPipeFlowSystem m_WaterPipeFlowSystem;

		private TypeHandle __TypeHandle;

		private EntityQuery __query_1733354667_0;

		[Preserve]
		protected override void OnCreate()
		{
			base.OnCreate();
			this.m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
			this.m_ZoneToolSystem = base.World.GetOrCreateSystemManaged<ZoneToolSystem>();
			this.m_ObjectToolSystem = base.World.GetOrCreateSystemManaged<ObjectToolSystem>();
			this.m_IndustrialDemandSystem = base.World.GetOrCreateSystemManaged<IndustrialDemandSystem>();
			this.m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
			this.m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
			this.m_GroundPollutionSystem = base.World.GetOrCreateSystemManaged<GroundPollutionSystem>();
			this.m_ElectricityFlowSystem = base.World.GetOrCreateSystemManaged<ElectricityFlowSystem>();
			this.m_WaterPipeFlowSystem = base.World.GetOrCreateSystemManaged<WaterPipeFlowSystem>();
			this.m_ZonePreferenceParameterGroup = base.GetEntityQuery(ComponentType.ReadOnly<ZonePreferenceData>());
			this.m_EdgeQuery = base.GetEntityQuery(ComponentType.ReadOnly<Edge>(), ComponentType.ReadWrite<EdgeColor>(), ComponentType.Exclude<Deleted>());
			this.m_NodeQuery = base.GetEntityQuery(ComponentType.ReadOnly<Node>(), ComponentType.ReadWrite<NodeColor>(), ComponentType.Exclude<Deleted>());
			this.m_LaneQuery = base.GetEntityQuery(ComponentType.ReadOnly<Lane>(), ComponentType.ReadWrite<LaneColor>(), ComponentType.Exclude<Hidden>(), ComponentType.Exclude<Deleted>());
			this.m_InfomodeQuery = base.GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[1] { ComponentType.ReadOnly<InfomodeActive>() },
				Any = new ComponentType[4]
				{
					ComponentType.ReadOnly<InfoviewCoverageData>(),
					ComponentType.ReadOnly<InfoviewAvailabilityData>(),
					ComponentType.ReadOnly<InfoviewNetGeometryData>(),
					ComponentType.ReadOnly<InfoviewNetStatusData>()
				},
				None = new ComponentType[0]
			});
			this.m_ProcessQuery = base.GetEntityQuery(ComponentType.ReadOnly<IndustrialProcessData>());
		}

		[Preserve]
		protected override void OnUpdate()
		{
			if (this.m_ToolSystem.activeInfoview == null || (this.m_EdgeQuery.IsEmptyIgnoreFilter && this.m_NodeQuery.IsEmptyIgnoreFilter))
			{
				return;
			}
			ZonePreferenceData zonePreferences = ((this.m_ZonePreferenceParameterGroup.CalculateEntityCount() > 0) ? this.m_ZonePreferenceParameterGroup.GetSingleton<ZonePreferenceData>() : default(ZonePreferenceData));
			JobHandle outJobHandle;
			NativeList<ArchetypeChunk> infomodeChunks = this.m_InfomodeQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle);
			Entity zonePrefab = Entity.Null;
			if (this.m_ToolSystem.activeTool == this.m_ZoneToolSystem && this.m_ZoneToolSystem.prefab != null)
			{
				zonePrefab = this.m_PrefabSystem.GetEntity(this.m_ZoneToolSystem.prefab);
			}
			else if (this.m_ToolSystem.activeTool == this.m_ObjectToolSystem && this.m_ObjectToolSystem.prefab != null)
			{
				PlaceholderBuilding component2;
				if (this.m_ObjectToolSystem.prefab.TryGet<SignatureBuilding>(out var component) && component.m_ZoneType != null)
				{
					zonePrefab = this.m_PrefabSystem.GetEntity(component.m_ZoneType);
				}
				else if (this.m_ObjectToolSystem.prefab.TryGet<PlaceholderBuilding>(out component2) && component2.m_ZoneType != null)
				{
					zonePrefab = this.m_PrefabSystem.GetEntity(component2.m_ZoneType);
				}
			}
			this.__TypeHandle.__Game_Net_EdgeColor_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_Edge_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_ResourceAvailability_RO_BufferLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_ServiceCoverage_RO_BufferLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Zones_ProcessEstimate_RO_BufferLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_ZonePropertiesData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Tools_Temp_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_Node_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_Edge_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_LandValue_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_ResourceAvailability_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_ServiceCoverage_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_Curve_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_Pollution_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_Road_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_NetCondition_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_SubwayTrack_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_Waterway_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_TramTrack_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_TrainTrack_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_InfoviewNetStatusData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_InfoviewNetGeometryData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_InfoviewAvailabilityData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_InfoviewCoverageData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_InfomodeActive_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			UpdateEdgeColorsJob updateEdgeColorsJob = default(UpdateEdgeColorsJob);
			updateEdgeColorsJob.m_InfomodeChunks = infomodeChunks;
			updateEdgeColorsJob.m_InfomodeActiveType = this.__TypeHandle.__Game_Prefabs_InfomodeActive_RO_ComponentTypeHandle;
			updateEdgeColorsJob.m_InfoviewCoverageType = this.__TypeHandle.__Game_Prefabs_InfoviewCoverageData_RO_ComponentTypeHandle;
			updateEdgeColorsJob.m_InfoviewAvailabilityType = this.__TypeHandle.__Game_Prefabs_InfoviewAvailabilityData_RO_ComponentTypeHandle;
			updateEdgeColorsJob.m_InfoviewNetGeometryType = this.__TypeHandle.__Game_Prefabs_InfoviewNetGeometryData_RO_ComponentTypeHandle;
			updateEdgeColorsJob.m_InfoviewNetStatusType = this.__TypeHandle.__Game_Prefabs_InfoviewNetStatusData_RO_ComponentTypeHandle;
			updateEdgeColorsJob.m_TrainTrackType = this.__TypeHandle.__Game_Net_TrainTrack_RO_ComponentTypeHandle;
			updateEdgeColorsJob.m_TramTrackType = this.__TypeHandle.__Game_Net_TramTrack_RO_ComponentTypeHandle;
			updateEdgeColorsJob.m_WaterwayType = this.__TypeHandle.__Game_Net_Waterway_RO_ComponentTypeHandle;
			updateEdgeColorsJob.m_SubwayTrackType = this.__TypeHandle.__Game_Net_SubwayTrack_RO_ComponentTypeHandle;
			updateEdgeColorsJob.m_NetConditionType = this.__TypeHandle.__Game_Net_NetCondition_RO_ComponentTypeHandle;
			updateEdgeColorsJob.m_RoadType = this.__TypeHandle.__Game_Net_Road_RO_ComponentTypeHandle;
			updateEdgeColorsJob.m_PollutionType = this.__TypeHandle.__Game_Net_Pollution_RO_ComponentTypeHandle;
			updateEdgeColorsJob.m_CurveType = this.__TypeHandle.__Game_Net_Curve_RO_ComponentTypeHandle;
			updateEdgeColorsJob.m_ServiceCoverageType = this.__TypeHandle.__Game_Net_ServiceCoverage_RO_BufferTypeHandle;
			updateEdgeColorsJob.m_ResourceAvailabilityType = this.__TypeHandle.__Game_Net_ResourceAvailability_RO_BufferTypeHandle;
			updateEdgeColorsJob.m_LandValues = this.__TypeHandle.__Game_Net_LandValue_RO_ComponentLookup;
			updateEdgeColorsJob.m_Edges = this.__TypeHandle.__Game_Net_Edge_RO_ComponentLookup;
			updateEdgeColorsJob.m_Nodes = this.__TypeHandle.__Game_Net_Node_RO_ComponentLookup;
			updateEdgeColorsJob.m_Temps = this.__TypeHandle.__Game_Tools_Temp_RO_ComponentLookup;
			updateEdgeColorsJob.m_ResourceDatas = this.__TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup;
			updateEdgeColorsJob.m_ZonePropertiesDatas = this.__TypeHandle.__Game_Prefabs_ZonePropertiesData_RO_ComponentLookup;
			updateEdgeColorsJob.m_ProcessEstimates = this.__TypeHandle.__Game_Zones_ProcessEstimate_RO_BufferLookup;
			updateEdgeColorsJob.m_ServiceCoverageData = this.__TypeHandle.__Game_Net_ServiceCoverage_RO_BufferLookup;
			updateEdgeColorsJob.m_ResourceAvailabilityData = this.__TypeHandle.__Game_Net_ResourceAvailability_RO_BufferLookup;
			updateEdgeColorsJob.m_EdgeType = this.__TypeHandle.__Game_Net_Edge_RO_ComponentTypeHandle;
			updateEdgeColorsJob.m_TempType = this.__TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle;
			updateEdgeColorsJob.m_ColorType = this.__TypeHandle.__Game_Net_EdgeColor_RW_ComponentTypeHandle;
			updateEdgeColorsJob.m_ZonePrefab = zonePrefab;
			updateEdgeColorsJob.m_ResourcePrefabs = this.m_ResourceSystem.GetPrefabs();
			updateEdgeColorsJob.m_PollutionMap = this.m_GroundPollutionSystem.GetMap(readOnly: true, out var dependencies);
			updateEdgeColorsJob.m_IndustrialDemands = this.m_IndustrialDemandSystem.GetBuildingDemands(out var deps);
			updateEdgeColorsJob.m_StorageDemands = this.m_IndustrialDemandSystem.GetStorageBuildingDemands(out var deps2);
			updateEdgeColorsJob.m_Processes = this.m_ProcessQuery.ToComponentDataListAsync<IndustrialProcessData>(base.World.UpdateAllocator.ToAllocator, out var outJobHandle2);
			updateEdgeColorsJob.m_ZonePreferences = zonePreferences;
			UpdateEdgeColorsJob jobData = updateEdgeColorsJob;
			this.__TypeHandle.__Game_Net_NodeColor_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_ConnectedEdge_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_Pollution_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_Road_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_NetCondition_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_SubwayTrack_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_Waterway_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_TramTrack_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_TrainTrack_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_InfoviewNetStatusData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_InfoviewNetGeometryData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_InfomodeActive_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_EndNodeGeometry_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_StartNodeGeometry_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_EdgeColor_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_Edge_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			UpdateNodeColorsJob updateNodeColorsJob = default(UpdateNodeColorsJob);
			updateNodeColorsJob.m_EdgeData = this.__TypeHandle.__Game_Net_Edge_RO_ComponentLookup;
			updateNodeColorsJob.m_ColorData = this.__TypeHandle.__Game_Net_EdgeColor_RO_ComponentLookup;
			updateNodeColorsJob.m_StartNodeGeometryData = this.__TypeHandle.__Game_Net_StartNodeGeometry_RO_ComponentLookup;
			updateNodeColorsJob.m_EndNodeGeometryData = this.__TypeHandle.__Game_Net_EndNodeGeometry_RO_ComponentLookup;
			updateNodeColorsJob.m_ConnectedEdges = this.__TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup;
			updateNodeColorsJob.m_InfomodeChunks = infomodeChunks;
			updateNodeColorsJob.m_InfomodeActiveType = this.__TypeHandle.__Game_Prefabs_InfomodeActive_RO_ComponentTypeHandle;
			updateNodeColorsJob.m_InfoviewNetGeometryType = this.__TypeHandle.__Game_Prefabs_InfoviewNetGeometryData_RO_ComponentTypeHandle;
			updateNodeColorsJob.m_InfoviewNetStatusType = this.__TypeHandle.__Game_Prefabs_InfoviewNetStatusData_RO_ComponentTypeHandle;
			updateNodeColorsJob.m_TrainTrackType = this.__TypeHandle.__Game_Net_TrainTrack_RO_ComponentTypeHandle;
			updateNodeColorsJob.m_TramTrackType = this.__TypeHandle.__Game_Net_TramTrack_RO_ComponentTypeHandle;
			updateNodeColorsJob.m_WaterwayType = this.__TypeHandle.__Game_Net_Waterway_RO_ComponentTypeHandle;
			updateNodeColorsJob.m_SubwayTrackType = this.__TypeHandle.__Game_Net_SubwayTrack_RO_ComponentTypeHandle;
			updateNodeColorsJob.m_NetConditionType = this.__TypeHandle.__Game_Net_NetCondition_RO_ComponentTypeHandle;
			updateNodeColorsJob.m_RoadType = this.__TypeHandle.__Game_Net_Road_RO_ComponentTypeHandle;
			updateNodeColorsJob.m_PollutionType = this.__TypeHandle.__Game_Net_Pollution_RO_ComponentTypeHandle;
			updateNodeColorsJob.m_EntityType = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle;
			updateNodeColorsJob.m_TempType = this.__TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle;
			updateNodeColorsJob.m_ConnectedEdgeType = this.__TypeHandle.__Game_Net_ConnectedEdge_RO_BufferTypeHandle;
			updateNodeColorsJob.m_ColorType = this.__TypeHandle.__Game_Net_NodeColor_RW_ComponentTypeHandle;
			UpdateNodeColorsJob jobData2 = updateNodeColorsJob;
			this.__TypeHandle.__Game_Net_EdgeColor_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_EndNodeGeometry_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_StartNodeGeometry_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_Edge_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_NodeColor_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			UpdateEdgeColors2Job updateEdgeColors2Job = default(UpdateEdgeColors2Job);
			updateEdgeColors2Job.m_ColorData = this.__TypeHandle.__Game_Net_NodeColor_RO_ComponentLookup;
			updateEdgeColors2Job.m_EdgeType = this.__TypeHandle.__Game_Net_Edge_RO_ComponentTypeHandle;
			updateEdgeColors2Job.m_StartNodeGeometryType = this.__TypeHandle.__Game_Net_StartNodeGeometry_RO_ComponentTypeHandle;
			updateEdgeColors2Job.m_EndNodeGeometryType = this.__TypeHandle.__Game_Net_EndNodeGeometry_RO_ComponentTypeHandle;
			updateEdgeColors2Job.m_ColorType = this.__TypeHandle.__Game_Net_EdgeColor_RW_ComponentTypeHandle;
			UpdateEdgeColors2Job jobData3 = updateEdgeColors2Job;
			this.__TypeHandle.__Game_Simulation_ConnectedFlowEdge_RO_BufferLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Buildings_ConnectedBuilding_RO_BufferLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_ConnectedNode_RO_BufferLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_NetLaneData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_UtilityLaneData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_TrackLaneData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Tools_Temp_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Buildings_WaterConsumer_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Buildings_ElectricityConsumer_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Buildings_Building_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Simulation_WaterPipeBuildingConnection_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Simulation_WaterPipeEdge_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Simulation_WaterPipeNodeConnection_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Simulation_ElectricityBuildingConnection_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Simulation_ElectricityFlowEdge_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Simulation_ElectricityNodeConnection_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Objects_Color_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_EdgeColor_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_NodeColor_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_Curve_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_Edge_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Common_Owner_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_InfoviewNetStatusData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_InfoviewNetGeometryData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_InfomodeActive_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_SubFlow_RW_BufferTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_LaneColor_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_EdgeMapping_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_SecondaryLane_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_UtilityLane_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_TrackLane_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_NodeLane_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_EdgeLane_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_Curve_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			LaneColorJob laneColorJob = default(LaneColorJob);
			laneColorJob.m_OwnerType = this.__TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle;
			laneColorJob.m_CurveType = this.__TypeHandle.__Game_Net_Curve_RO_ComponentTypeHandle;
			laneColorJob.m_EdgeLaneType = this.__TypeHandle.__Game_Net_EdgeLane_RO_ComponentTypeHandle;
			laneColorJob.m_NodeLaneType = this.__TypeHandle.__Game_Net_NodeLane_RO_ComponentTypeHandle;
			laneColorJob.m_TrackLaneType = this.__TypeHandle.__Game_Net_TrackLane_RO_ComponentTypeHandle;
			laneColorJob.m_UtilityLaneType = this.__TypeHandle.__Game_Net_UtilityLane_RO_ComponentTypeHandle;
			laneColorJob.m_SecondaryLaneType = this.__TypeHandle.__Game_Net_SecondaryLane_RO_ComponentTypeHandle;
			laneColorJob.m_EdgeMappingType = this.__TypeHandle.__Game_Net_EdgeMapping_RO_ComponentTypeHandle;
			laneColorJob.m_TempType = this.__TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle;
			laneColorJob.m_PrefabRefType = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;
			laneColorJob.m_ColorType = this.__TypeHandle.__Game_Net_LaneColor_RW_ComponentTypeHandle;
			laneColorJob.m_SubFlowType = this.__TypeHandle.__Game_Net_SubFlow_RW_BufferTypeHandle;
			laneColorJob.m_InfomodeChunks = infomodeChunks;
			laneColorJob.m_InfomodeActiveType = this.__TypeHandle.__Game_Prefabs_InfomodeActive_RO_ComponentTypeHandle;
			laneColorJob.m_InfoviewNetGeometryType = this.__TypeHandle.__Game_Prefabs_InfoviewNetGeometryData_RO_ComponentTypeHandle;
			laneColorJob.m_InfoviewNetStatusType = this.__TypeHandle.__Game_Prefabs_InfoviewNetStatusData_RO_ComponentTypeHandle;
			laneColorJob.m_OwnerData = this.__TypeHandle.__Game_Common_Owner_RO_ComponentLookup;
			laneColorJob.m_EdgeData = this.__TypeHandle.__Game_Net_Edge_RO_ComponentLookup;
			laneColorJob.m_CurveData = this.__TypeHandle.__Game_Net_Curve_RO_ComponentLookup;
			laneColorJob.m_NodeColorData = this.__TypeHandle.__Game_Net_NodeColor_RO_ComponentLookup;
			laneColorJob.m_EdgeColorData = this.__TypeHandle.__Game_Net_EdgeColor_RO_ComponentLookup;
			laneColorJob.m_ObjectColorData = this.__TypeHandle.__Game_Objects_Color_RO_ComponentLookup;
			laneColorJob.m_ElectricityNodeConnectionData = this.__TypeHandle.__Game_Simulation_ElectricityNodeConnection_RO_ComponentLookup;
			laneColorJob.m_ElectricityFlowEdgeData = this.__TypeHandle.__Game_Simulation_ElectricityFlowEdge_RO_ComponentLookup;
			laneColorJob.m_ElectricityBuildingConnectionData = this.__TypeHandle.__Game_Simulation_ElectricityBuildingConnection_RO_ComponentLookup;
			laneColorJob.m_WaterPipeNodeConnectionData = this.__TypeHandle.__Game_Simulation_WaterPipeNodeConnection_RO_ComponentLookup;
			laneColorJob.m_WaterPipeEdgeData = this.__TypeHandle.__Game_Simulation_WaterPipeEdge_RO_ComponentLookup;
			laneColorJob.m_WaterPipeBuildingConnectionData = this.__TypeHandle.__Game_Simulation_WaterPipeBuildingConnection_RO_ComponentLookup;
			laneColorJob.m_BuildingData = this.__TypeHandle.__Game_Buildings_Building_RO_ComponentLookup;
			laneColorJob.m_ElectricityConsumerData = this.__TypeHandle.__Game_Buildings_ElectricityConsumer_RO_ComponentLookup;
			laneColorJob.m_WaterConsumerData = this.__TypeHandle.__Game_Buildings_WaterConsumer_RO_ComponentLookup;
			laneColorJob.m_TempData = this.__TypeHandle.__Game_Tools_Temp_RO_ComponentLookup;
			laneColorJob.m_PrefabTrackLaneData = this.__TypeHandle.__Game_Prefabs_TrackLaneData_RO_ComponentLookup;
			laneColorJob.m_PrefabUtilityLaneData = this.__TypeHandle.__Game_Prefabs_UtilityLaneData_RO_ComponentLookup;
			laneColorJob.m_PrefabNetLaneData = this.__TypeHandle.__Game_Prefabs_NetLaneData_RO_ComponentLookup;
			laneColorJob.m_ConnectedNodes = this.__TypeHandle.__Game_Net_ConnectedNode_RO_BufferLookup;
			laneColorJob.m_ConnectedBuildings = this.__TypeHandle.__Game_Buildings_ConnectedBuilding_RO_BufferLookup;
			laneColorJob.m_ConnectedFlowEdges = this.__TypeHandle.__Game_Simulation_ConnectedFlowEdge_RO_BufferLookup;
			laneColorJob.m_ElectricitySinkNode = this.m_ElectricityFlowSystem.sinkNode;
			laneColorJob.m_WaterSinkNode = this.m_WaterPipeFlowSystem.sinkNode;
			laneColorJob.m_WaterPipeParameters = this.__query_1733354667_0.GetSingleton<WaterPipeParameterData>();
			LaneColorJob jobData4 = laneColorJob;
			JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData, this.m_EdgeQuery, JobUtils.CombineDependencies(base.Dependency, outJobHandle, outJobHandle2, dependencies, deps, deps2));
			JobHandle jobHandle2 = JobChunkExtensions.ScheduleParallel(dependsOn: JobChunkExtensions.ScheduleParallel(dependsOn: JobChunkExtensions.ScheduleParallel(jobData2, this.m_NodeQuery, jobHandle), jobData: jobData3, query: this.m_EdgeQuery), jobData: jobData4, query: this.m_LaneQuery);
			infomodeChunks.Dispose(jobHandle2);
			this.m_GroundPollutionSystem.AddReader(jobHandle);
			this.m_IndustrialDemandSystem.AddReader(jobHandle);
			this.m_ResourceSystem.AddPrefabsReader(jobHandle);
			base.Dependency = jobHandle2;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void __AssignQueries(ref SystemState state)
		{
			this.__query_1733354667_0 = state.GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[1] { ComponentType.ReadOnly<WaterPipeParameterData>() },
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
		public NetColorSystem()
		{
		}
	}
}

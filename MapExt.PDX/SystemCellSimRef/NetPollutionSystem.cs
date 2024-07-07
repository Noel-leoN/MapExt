using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;
using Game;
using Game.Simulation;

namespace MapExt.Systems
{
    /// <summary>
    /// bcjob£¡£¡£¡£»
    /// </summary>
    //[CompilerGenerated]
    public partial class NetPollutionSystem : GameSystemBase
	{
		[BurstCompile]
		private struct UpdateNetPollutionJob : IJob
		{
			[ReadOnly]
			public uint m_UpdateFrameIndex;

			[ReadOnly]
			public NativeList<ArchetypeChunk> m_Chunks;

			[ReadOnly]
			public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

			[ReadOnly]
			public ComponentTypeHandle<Node> m_NodeType;

			[ReadOnly]
			public ComponentTypeHandle<Curve> m_CurveType;

			[ReadOnly]
			public ComponentTypeHandle<Upgraded> m_UpgradedType;

			[ReadOnly]
			public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

			public ComponentTypeHandle<Game.Net.Pollution> m_PollutionType;

			[ReadOnly]
			public ComponentLookup<NetPollutionData> m_NetPollutionData;

			public int m_MapSize;

			public int m_AirPollutionTextureSize;

			public int m_NoisePollutionTextureSize;

			public NativeArray<AirPollution> m_AirPollutionMap;

			public NativeArray<NoisePollution> m_NoisePollutionMap;

			[ReadOnly]
			public PollutionParameterData m_PollutionParameters;

			public void Execute()
			{
				PollutionParameterData pollutionParameters = this.m_PollutionParameters;
				float s = 4f / (float)NetPollutionSystem.kUpdatesPerDay;
				for (int i = 0; i < this.m_Chunks.Length; i++)
				{
					ArchetypeChunk archetypeChunk = this.m_Chunks[i];
					if (archetypeChunk.GetSharedComponent(this.m_UpdateFrameType).m_Index != this.m_UpdateFrameIndex)
					{
						continue;
					}
					NativeArray<PrefabRef> nativeArray = archetypeChunk.GetNativeArray(ref this.m_PrefabRefType);
					NativeArray<Node> nativeArray2 = archetypeChunk.GetNativeArray(ref this.m_NodeType);
					NativeArray<Curve> nativeArray3 = archetypeChunk.GetNativeArray(ref this.m_CurveType);
					NativeArray<Game.Net.Pollution> nativeArray4 = archetypeChunk.GetNativeArray(ref this.m_PollutionType);
					NativeArray<Upgraded> nativeArray5 = archetypeChunk.GetNativeArray(ref this.m_UpgradedType);
					for (int j = 0; j < nativeArray2.Length; j++)
					{
						PrefabRef prefabRef = nativeArray[j];
						ref Game.Net.Pollution reference = ref nativeArray4.ElementAt(j);
						reference.m_Accumulation = math.lerp(reference.m_Accumulation, reference.m_Pollution, s);
						reference.m_Pollution = default(float2);
						if (this.m_NetPollutionData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
						{
							Node node = nativeArray2[j];
							float2 @float = reference.m_Accumulation * componentData.m_Factors;
							this.ApplyPollution(node.m_Position, @float.x, @float.y, ref pollutionParameters);
						}
					}
					for (int k = 0; k < nativeArray3.Length; k++)
					{
						PrefabRef prefabRef2 = nativeArray[k];
						ref Game.Net.Pollution reference2 = ref nativeArray4.ElementAt(k);
						reference2.m_Accumulation = math.lerp(reference2.m_Accumulation, reference2.m_Pollution, s);
						reference2.m_Pollution = default(float2);
						if (!this.m_NetPollutionData.TryGetComponent(prefabRef2.m_Prefab, out var componentData2))
						{
							continue;
						}
						Curve curve = nativeArray3[k];
						float2 float2 = reference2.m_Accumulation * componentData2.m_Factors;
						float3 noisePollution = float2.x;
						noisePollution.y *= 2f;
						if (nativeArray5.Length != 0)
						{
							Upgraded upgraded = nativeArray5[k];
							if ((upgraded.m_Flags.m_Left & upgraded.m_Flags.m_Right & CompositionFlags.Side.SoundBarrier) != 0)
							{
								noisePollution *= new float3(0f, 0.5f, 0f);
							}
							else if ((upgraded.m_Flags.m_Left & CompositionFlags.Side.SoundBarrier) != 0)
							{
								noisePollution *= new float3(0f, 0.5f, 1.5f);
							}
							else if ((upgraded.m_Flags.m_Right & CompositionFlags.Side.SoundBarrier) != 0)
							{
								noisePollution *= new float3(1.5f, 0.5f, 0f);
							}
							if ((upgraded.m_Flags.m_Left & upgraded.m_Flags.m_Right & CompositionFlags.Side.PrimaryBeautification) != 0)
							{
								noisePollution *= new float3(0.5f, 0.5f, 0.5f);
							}
							else if ((upgraded.m_Flags.m_Left & CompositionFlags.Side.PrimaryBeautification) != 0)
							{
								noisePollution *= new float3(0.5f, 0.75f, 1f);
							}
							else if ((upgraded.m_Flags.m_Right & CompositionFlags.Side.PrimaryBeautification) != 0)
							{
								noisePollution *= new float3(1f, 0.75f, 0.5f);
							}
							if ((upgraded.m_Flags.m_Left & upgraded.m_Flags.m_Right & CompositionFlags.Side.SecondaryBeautification) != 0)
							{
								noisePollution *= new float3(0.5f, 0.5f, 0.5f);
							}
							else if ((upgraded.m_Flags.m_Left & CompositionFlags.Side.SecondaryBeautification) != 0)
							{
								noisePollution *= new float3(0.5f, 0.75f, 1f);
							}
							else if ((upgraded.m_Flags.m_Right & CompositionFlags.Side.SecondaryBeautification) != 0)
							{
								noisePollution *= new float3(1f, 0.75f, 0.5f);
							}
							if ((upgraded.m_Flags.m_General & CompositionFlags.General.PrimaryMiddleBeautification) != 0)
							{
								noisePollution *= new float3(0.875f, 0.5f, 0.875f);
							}
							if ((upgraded.m_Flags.m_General & CompositionFlags.General.SecondaryMiddleBeautification) != 0)
							{
								noisePollution *= new float3(0.875f, 0.5f, 0.875f);
							}
						}
						this.ApplyPollution(curve, noisePollution, float2.y, ref pollutionParameters);
					}
				}
			}

			private void ApplyPollution(float3 position, float noisePollution, float airPollution, ref PollutionParameterData pollutionParameters)
			{
				if (airPollution != 0f)
				{
					short amount = (short)(pollutionParameters.m_NetAirMultiplier * airPollution);
					this.AddAirPollution(position, amount);
				}
				if (noisePollution != 0f)
				{
					short num = (short)(pollutionParameters.m_NetNoiseMultiplier * noisePollution / 8f);
					this.AddNoise(position, (short)(4 * num));
					this.AddNoise(position + new float3(0f - pollutionParameters.m_NetNoiseRadius, 0f, 0f), num);
					this.AddNoise(position + new float3(pollutionParameters.m_NetNoiseRadius, 0f, 0f), num);
					this.AddNoise(position + new float3(0f, 0f, pollutionParameters.m_NetNoiseRadius), num);
					this.AddNoise(position + new float3(0f, 0f, 0f - pollutionParameters.m_NetNoiseRadius), num);
				}
			}

			private void ApplyPollution(Curve curve, float3 noisePollution, float airPollution, ref PollutionParameterData pollutionParameters)
			{
				if (airPollution != 0f)
				{
					float num = (float)this.m_MapSize / (float)this.m_AirPollutionTextureSize;
					int num2 = Mathf.CeilToInt(2f * curve.m_Length / num);
					short amount = (short)(pollutionParameters.m_NetAirMultiplier * airPollution / (float)num2);
					for (int i = 1; i <= num2; i++)
					{
						float3 position = MathUtils.Position(curve.m_Bezier, (float)i / ((float)num2 + 1f));
						this.AddAirPollution(position, amount);
					}
				}
				if (!math.any(noisePollution != 0f))
				{
					return;
				}
				float num3 = (float)this.m_MapSize / (float)this.m_NoisePollutionTextureSize;
				int num4 = Mathf.CeilToInt(2f * curve.m_Length / num3);
				int3 @int = (int3)(pollutionParameters.m_NetNoiseMultiplier * noisePollution / (4f * (float)num4));
				for (int j = 1; j <= num4; j++)
				{
					float t = (float)j / ((float)num4 + 1f);
					float3 @float = MathUtils.Position(curve.m_Bezier, t);
					float3 float2 = MathUtils.Tangent(curve.m_Bezier, t);
					float2 = math.normalize(new float3(0f - float2.z, 0f, float2.x));
					this.AddNoise(@float, (short)@int.y);
					if (@int.x != 0)
					{
						this.AddNoise(@float + pollutionParameters.m_NetNoiseRadius * float2, (short)@int.x);
					}
					if (@int.z != 0)
					{
						this.AddNoise(@float - pollutionParameters.m_NetNoiseRadius * float2, (short)@int.z);
					}
				}
			}

			private void AddAirPollution(float3 position, short amount)
			{
				int2 cell = CellMapSystemRe.GetCell(position, this.m_MapSize, this.m_AirPollutionTextureSize);
				if (math.all((cell >= 0) & (cell < this.m_AirPollutionTextureSize)))
				{
					int index = cell.x + cell.y * this.m_AirPollutionTextureSize;
					AirPollution value = this.m_AirPollutionMap[index];
					value.Add(amount);
					this.m_AirPollutionMap[index] = value;
				}
			}

			private void AddNoise(float3 position, short amount)
			{
				float2 cellCoords = CellMapSystemRe.GetCellCoords(position, this.m_MapSize, this.m_NoisePollutionTextureSize);
				float2 @float = math.frac(cellCoords);
				float2 float2 = ((@float.x < 0.5f) ? new float2(0f, 1f) : new float2(1f, 0f));
				float2 float3 = ((@float.y < 0.5f) ? new float2(0f, 1f) : new float2(1f, 0f));
				int2 cell = new int2(Mathf.FloorToInt(cellCoords.x - float2.y), Mathf.FloorToInt(cellCoords.y - float3.y));
				this.AddNoiseSingle(cell, (short)((0.5 + (double)float2.x - (double)@float.x) * (0.5 + (double)float3.x - (double)@float.y) * (double)amount));
				cell.x++;
				this.AddNoiseSingle(cell, (short)((-0.5 + (double)float2.y + (double)@float.x) * (0.5 + (double)float3.x - (double)@float.y) * (double)amount));
				cell.y++;
				this.AddNoiseSingle(cell, (short)((-0.5 + (double)float2.y + (double)@float.x) * (-0.5 + (double)float3.y + (double)@float.y) * (double)amount));
				cell.x--;
				this.AddNoiseSingle(cell, (short)((0.5 + (double)float2.x - (double)@float.x) * (-0.5 + (double)float3.y + (double)@float.y) * (double)amount));
			}

			private void AddNoiseSingle(int2 cell, short amount)
			{
				if (math.all((cell >= 0) & (cell < this.m_NoisePollutionTextureSize)))
				{
					int index = cell.x + cell.y * this.m_NoisePollutionTextureSize;
					NoisePollution value = this.m_NoisePollutionMap[index];
					value.Add(amount);
					this.m_NoisePollutionMap[index] = value;
				}
			}
		}

		private struct TypeHandle
		{
			public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<Node> __Game_Net_Node_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<Curve> __Game_Net_Curve_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<Upgraded> __Game_Net_Upgraded_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

			public ComponentTypeHandle<Game.Net.Pollution> __Game_Net_Pollution_RW_ComponentTypeHandle;

			[ReadOnly]
			public ComponentLookup<NetPollutionData> __Game_Prefabs_NetPollutionData_RO_ComponentLookup;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void __AssignHandles(ref SystemState state)
			{
				this.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
				this.__Game_Net_Node_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Node>(isReadOnly: true);
				this.__Game_Net_Curve_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Curve>(isReadOnly: true);
				this.__Game_Net_Upgraded_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Upgraded>(isReadOnly: true);
				this.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
				this.__Game_Net_Pollution_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.Pollution>();
				this.__Game_Prefabs_NetPollutionData_RO_ComponentLookup = state.GetComponentLookup<NetPollutionData>(isReadOnly: true);
			}
		}

		public static readonly int kUpdatesPerDay = 128;

		private SimulationSystem m_SimulationSystem;

		private EntityQuery m_PollutionQuery;

		private AirPollutionSystem m_AirPollutionSystem;

		private NoisePollutionSystem m_NoisePollutionSystem;

		private EntityQuery m_PollutionParameterQuery;

		private TypeHandle __TypeHandle;

		public override int GetUpdateInterval(SystemUpdatePhase phase)
		{
			return 262144 / (NetPollutionSystem.kUpdatesPerDay * 16);
		}

		[Preserve]
		protected override void OnCreate()
		{
			base.OnCreate();
			this.m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
			this.m_AirPollutionSystem = base.World.GetOrCreateSystemManaged<AirPollutionSystem>();
			this.m_NoisePollutionSystem = base.World.GetOrCreateSystemManaged<NoisePollutionSystem>();
			this.m_PollutionQuery = base.GetEntityQuery(ComponentType.ReadWrite<Game.Net.Pollution>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
			this.m_PollutionParameterQuery = base.GetEntityQuery(ComponentType.ReadOnly<PollutionParameterData>());
			base.RequireForUpdate(this.m_PollutionQuery);
			base.RequireForUpdate(this.m_PollutionParameterQuery);
		}

		[Preserve]
		protected override void OnUpdate()
		{
			uint updateFrame = SimulationUtils.GetUpdateFrame(this.m_SimulationSystem.frameIndex, NetPollutionSystem.kUpdatesPerDay, 16);
			JobHandle outJobHandle;
			NativeList<ArchetypeChunk> chunks = this.m_PollutionQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
			this.__TypeHandle.__Game_Prefabs_NetPollutionData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_Pollution_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_Upgraded_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_Curve_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Net_Node_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
			this.__TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle.Update(ref base.CheckedStateRef);
			UpdateNetPollutionJob jobData = default(UpdateNetPollutionJob);
			jobData.m_UpdateFrameIndex = updateFrame;
			jobData.m_Chunks = chunks;
			jobData.m_UpdateFrameType = this.__TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle;
			jobData.m_NodeType = this.__TypeHandle.__Game_Net_Node_RO_ComponentTypeHandle;
			jobData.m_CurveType = this.__TypeHandle.__Game_Net_Curve_RO_ComponentTypeHandle;
			jobData.m_UpgradedType = this.__TypeHandle.__Game_Net_Upgraded_RO_ComponentTypeHandle;
			jobData.m_PrefabRefType = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;
			jobData.m_PollutionType = this.__TypeHandle.__Game_Net_Pollution_RW_ComponentTypeHandle;
			jobData.m_NetPollutionData = this.__TypeHandle.__Game_Prefabs_NetPollutionData_RO_ComponentLookup;
			jobData.m_AirPollutionMap = this.m_AirPollutionSystem.GetMap(readOnly: false, out var dependencies);
			jobData.m_NoisePollutionMap = this.m_NoisePollutionSystem.GetMap(readOnly: false, out var dependencies2);
			jobData.m_AirPollutionTextureSize = AirPollutionSystem.kTextureSize;
			jobData.m_NoisePollutionTextureSize = NoisePollutionSystem.kTextureSize;
			jobData.m_MapSize = CellMapSystemRe.kMapSize;
			jobData.m_PollutionParameters = this.m_PollutionParameterQuery.GetSingleton<PollutionParameterData>();
			JobHandle jobHandle = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(dependencies, dependencies2, JobHandle.CombineDependencies(base.Dependency, outJobHandle)));
			chunks.Dispose(jobHandle);
			this.m_AirPollutionSystem.AddWriter(jobHandle);
			this.m_NoisePollutionSystem.AddWriter(jobHandle);
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
		public NetPollutionSystem()
		{
		}
	}
}

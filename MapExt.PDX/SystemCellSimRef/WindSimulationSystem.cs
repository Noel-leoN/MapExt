using System.IO;
using Colossal.Serialization.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;
using Game;
using Game.Simulation;

namespace MapExt.Systems
{
    /// <summary>
    /// bcjob！！！；间接调用；
    /// </summary>
    public partial class WindSimulationSystem : GameSystemBase, IDefaultSerializable, ISerializable
	{
		public struct WindCell : ISerializable
		{
			public float m_Pressure;

			public float3 m_Velocities;

			public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
			{
				writer.Write(this.m_Pressure);
				writer.Write(this.m_Velocities);
			}

			public void Deserialize<TReader>(TReader reader) where TReader : IReader
			{
				reader.Read(out this.m_Pressure);
				reader.Read(out this.m_Velocities);
			}
		}

		[BurstCompile]
		private struct UpdateWindVelocityJob : IJobFor
		{
			public NativeArray<WindCell> m_Cells;

			[ReadOnly]
			public TerrainHeightData m_TerrainHeightData;

			[ReadOnly]
			public WaterSurfaceData m_WaterSurfaceData;

			public float2 m_TerrainRange;

			public void Execute(int index)
			{
				int3 @int = new int3(index % WindSimulationSystem.kResolution.x, index / WindSimulationSystem.kResolution.x % WindSimulationSystem.kResolution.y, index / (WindSimulationSystem.kResolution.x * WindSimulationSystem.kResolution.y));
				bool3 @bool = new bool3(@int.x >= WindSimulationSystem.kResolution.x - 1, @int.y >= WindSimulationSystem.kResolution.y - 1, @int.z >= WindSimulationSystem.kResolution.z - 1);
				if (!@bool.x && !@bool.y && !@bool.z)
				{
					int3 position = new int3(@int.x, @int.y + 1, @int.z);
					int3 position2 = new int3(@int.x + 1, @int.y, @int.z);
					float3 cellCenter = WindSimulationSystem.GetCellCenter(index);
					cellCenter.y = math.lerp(this.m_TerrainRange.x, this.m_TerrainRange.y, ((float)@int.z + 0.5f) / (float)WindSimulationSystem.kResolution.z);
					float num = WaterUtils.SampleHeight(ref this.m_WaterSurfaceData, ref this.m_TerrainHeightData, cellCenter);
					float num2 = WaterUtils.SampleHeight(ref this.m_WaterSurfaceData, ref this.m_TerrainHeightData, cellCenter);
					float num3 = WaterUtils.SampleHeight(ref this.m_WaterSurfaceData, ref this.m_TerrainHeightData, cellCenter);
					float num4 = 65535f / (this.m_TerrainHeightData.scale.y * (float)WindSimulationSystem.kResolution.z);
					float num5 = math.saturate((0.5f * (num4 + num + num2) - cellCenter.y) / num4);
					float num6 = math.saturate((0.5f * (num4 + num + num3) - cellCenter.y) / num4);
					WindCell value = this.m_Cells[index];
					WindCell cell = WindSimulationSystem.GetCell(new int3(@int.x, @int.y, @int.z + 1), this.m_Cells);
					WindCell cell2 = WindSimulationSystem.GetCell(position, this.m_Cells);
					WindCell cell3 = WindSimulationSystem.GetCell(position2, this.m_Cells);
					value.m_Velocities.x *= math.lerp(WindSimulationSystem.kAirSlowdown, WindSimulationSystem.kTerrainSlowdown, num6);
					value.m_Velocities.y *= math.lerp(WindSimulationSystem.kAirSlowdown, WindSimulationSystem.kTerrainSlowdown, num5);
					value.m_Velocities.z *= WindSimulationSystem.kVerticalSlowdown;
					value.m_Velocities.x += WindSimulationSystem.kChangeFactor * (1f - num6) * (value.m_Pressure - cell3.m_Pressure);
					value.m_Velocities.y += WindSimulationSystem.kChangeFactor * (1f - num5) * (value.m_Pressure - cell2.m_Pressure);
					value.m_Velocities.z += WindSimulationSystem.kChangeFactor * (value.m_Pressure - cell.m_Pressure);
					this.m_Cells[index] = value;
				}
			}
		}

		[BurstCompile]
		private struct UpdatePressureJob : IJobFor
		{
			public NativeArray<WindCell> m_Cells;

			public float2 m_Wind;

			public void Execute(int index)
			{
				int3 @int = new int3(index % WindSimulationSystem.kResolution.x, index / WindSimulationSystem.kResolution.x % WindSimulationSystem.kResolution.y, index / (WindSimulationSystem.kResolution.x * WindSimulationSystem.kResolution.y));
				bool3 @bool = new bool3(@int.x == 0, @int.y == 0, @int.z == 0);
				bool3 bool2 = new bool3(@int.x >= WindSimulationSystem.kResolution.x - 1, @int.y >= WindSimulationSystem.kResolution.y - 1, @int.z >= WindSimulationSystem.kResolution.z - 1);
				if (!bool2.x && !bool2.y && !bool2.z)
				{
					WindCell value = this.m_Cells[index];
					value.m_Pressure -= value.m_Velocities.x + value.m_Velocities.y + value.m_Velocities.z;
					if (!@bool.x)
					{
						WindCell cell = WindSimulationSystem.GetCell(new int3(@int.x - 1, @int.y, @int.z), this.m_Cells);
						value.m_Pressure += cell.m_Velocities.x;
					}
					if (!@bool.y)
					{
						WindCell cell2 = WindSimulationSystem.GetCell(new int3(@int.x, @int.y - 1, @int.z), this.m_Cells);
						value.m_Pressure += cell2.m_Velocities.y;
					}
					if (!@bool.z)
					{
						WindCell cell3 = WindSimulationSystem.GetCell(new int3(@int.x, @int.y, @int.z - 1), this.m_Cells);
						value.m_Pressure += cell3.m_Velocities.z;
					}
					this.m_Cells[index] = value;
				}
				if (@bool.x || @bool.y || bool2.x || bool2.y)
				{
					WindCell value2 = this.m_Cells[index];
					float num = math.dot(math.normalize(new float2(@int.x - WindSimulationSystem.kResolution.x / 2, @int.y - WindSimulationSystem.kResolution.y / 2)), math.normalize(this.m_Wind));
					float num2 = math.pow((1f + (float)@int.z) / (1f + (float)WindSimulationSystem.kResolution.z), 1f / 7f);
					float num3 = 0.1f * (2f - num);
					float num4 = (40f - 20f * (1f + num)) * math.length(this.m_Wind) * num2;
					value2.m_Pressure = ((num4 > value2.m_Pressure) ? math.min(num4, value2.m_Pressure + num3) : math.max(num4, value2.m_Pressure - num3));
					this.m_Cells[index] = value2;
				}
			}
		}

		public static readonly int kUpdateInterval = 512;

		public static readonly int3 kResolution = new int3(WindSystem.kTextureSize, WindSystem.kTextureSize, 16);

		public static readonly float kChangeFactor = 0.02f;

		public static readonly float kTerrainSlowdown = 0.99f;

		public static readonly float kAirSlowdown = 0.995f;

		public static readonly float kVerticalSlowdown = 0.9f;

		private SimulationSystem m_SimulationSystem;

		private TerrainSystem m_TerrainSystem;

		private WaterSystem m_WaterSystem;

		private ClimateSystem m_ClimateSystem;

		private bool m_Odd;

		private JobHandle m_Deps;

		private NativeArray<WindCell> m_Cells;

		public float2 constantWind { get; set; }

		private float m_ConstantPressure { get; set; }

		public override int GetUpdateInterval(SystemUpdatePhase phase)
		{
			if (phase != SystemUpdatePhase.GameSimulation)
			{
				return 1;
			}
			return WindSimulationSystem.kUpdateInterval;
		}

		public unsafe byte[] CreateByteArray<T>(NativeArray<T> src) where T : struct
		{
			int num = UnsafeUtility.SizeOf<T>() * src.Length;
			byte* unsafeReadOnlyPtr = (byte*)src.GetUnsafeReadOnlyPtr();
			byte[] array = new byte[num];
			fixed (byte* ptr = array)
			{
				UnsafeUtility.MemCpy(ptr, unsafeReadOnlyPtr, num);
			}
			return array;
		}

		public void DebugSave()
		{
			this.m_Deps.Complete();
			using System.IO.BinaryWriter binaryWriter = new System.IO.BinaryWriter(File.OpenWrite(Application.streamingAssetsPath + "/wind_temp.dat"));
			binaryWriter.Write(WindSimulationSystem.kResolution.x);
			binaryWriter.Write(WindSimulationSystem.kResolution.y);
			binaryWriter.Write(WindSimulationSystem.kResolution.z);
			binaryWriter.Write(this.CreateByteArray(this.m_Cells));
		}

		public unsafe void DebugLoad()
		{
			this.m_Deps.Complete();
			using System.IO.BinaryReader binaryReader = new System.IO.BinaryReader(File.OpenRead(Application.streamingAssetsPath + "/wind_temp.dat"));
			int num = binaryReader.ReadInt32();
			int num2 = binaryReader.ReadInt32();
			int num3 = binaryReader.ReadInt32();
			int num4 = num * num2 * num3 * UnsafeUtility.SizeOf<WindCell>();
			byte[] array = new byte[num4];
			binaryReader.Read(array, 0, num * num2 * num3 * sizeof(WindCell));
			byte* unsafePtr = (byte*)this.m_Cells.GetUnsafePtr();
			for (int i = 0; i < num4; i++)
			{
				unsafePtr[i] = array[i];
			}
		}

		public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
		{
			writer.Write(this.m_Cells.Length);
			writer.Write(this.m_Cells);
			writer.Write(this.constantWind);
		}

		public void Deserialize<TReader>(TReader reader) where TReader : IReader
		{
			if (!(reader.context.version > Version.stormWater))
			{
				return;
			}
			if (reader.context.version > Version.cellMapLengths)
			{
				reader.Read(out int value);
				if (this.m_Cells.Length == value)
				{
					reader.Read(this.m_Cells);
				}
				if (reader.context.version > Version.windDirection)
				{
					reader.Read(out float2 value2);
					this.constantWind = value2;
				}
				else
				{
					this.constantWind = new float2(0.275f, 0.275f);
				}
			}
			else
			{
				reader.Read(this.m_Cells);
			}
		}

		public void SetDefaults(Context context)
		{
			this.m_Deps.Complete();
			for (int i = 0; i < this.m_Cells.Length; i++)
			{
				this.m_Cells[i] = new WindCell
				{
					m_Pressure = this.m_ConstantPressure,
					m_Velocities = new float3(this.constantWind, 0f)
				};
			}
		}

		public void SetWind(float2 direction, float pressure)
		{
			this.m_Deps.Complete();
			this.constantWind = direction;
			this.m_ConstantPressure = pressure;
			this.SetDefaults(default(Context));
		}

		public static float3 GetCenterVelocity(int3 cell, NativeArray<WindCell> cells)
		{
			float3 velocities = WindSimulationSystem.GetCell(cell, cells).m_Velocities;
			float3 @float = ((cell.x > 0) ? WindSimulationSystem.GetCell(cell + new int3(-1, 0, 0), cells).m_Velocities : velocities);
			float3 float2 = ((cell.y > 0) ? WindSimulationSystem.GetCell(cell + new int3(0, -1, 0), cells).m_Velocities : velocities);
			float3 float3 = ((cell.z > 0) ? WindSimulationSystem.GetCell(cell + new int3(0, 0, -1), cells).m_Velocities : velocities);
			return 0.5f * new float3(velocities.x + @float.x, velocities.y + float2.y, velocities.z + float3.z);
		}

		public static float3 GetCellCenter(int index)
		{
			int3 @int = new int3(index % WindSimulationSystem.kResolution.x, index / WindSimulationSystem.kResolution.x % WindSimulationSystem.kResolution.y, index / (WindSimulationSystem.kResolution.x * WindSimulationSystem.kResolution.y));
			float3 result = CellMapSystemRe.kMapSize * new float3(((float)@int.x + 0.5f) / (float)WindSimulationSystem.kResolution.x, 0f, ((float)@int.y + 0.5f) / (float)WindSimulationSystem.kResolution.y) - CellMapSystemRe.kMapSize / 2;
			result.y = 100f + 1024f * ((float)@int.z + 0.5f) / (float)WindSimulationSystem.kResolution.z;
			return result;
		}

		public NativeArray<WindCell> GetCells(out JobHandle deps)
		{
			deps = this.m_Deps;
			return this.m_Cells;
		}

		public void AddReader(JobHandle reader)
		{
			this.m_Deps = JobHandle.CombineDependencies(this.m_Deps, reader);
		}

		[Preserve]
		protected override void OnCreate()
		{
			this.m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
			this.m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
			this.m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
			this.m_ClimateSystem = base.World.GetOrCreateSystemManaged<ClimateSystem>();
			this.constantWind = new float2(0.275f, 0.275f);
			this.m_ConstantPressure = 40f;
			this.m_Cells = new NativeArray<WindCell>(WindSimulationSystem.kResolution.x * WindSimulationSystem.kResolution.y * WindSimulationSystem.kResolution.z, Allocator.Persistent);
		}

		[Preserve]
		protected override void OnDestroy()
		{
			this.m_Cells.Dispose();
		}

		private WindCell GetCell(int3 position)
		{
			return this.m_Cells[position.x + position.y * WindSimulationSystem.kResolution.x + position.z * WindSimulationSystem.kResolution.x * WindSimulationSystem.kResolution.y];
		}

		public static WindCell GetCell(int3 position, NativeArray<WindCell> cells)
		{
			int num = position.x + position.y * WindSimulationSystem.kResolution.x + position.z * WindSimulationSystem.kResolution.x * WindSimulationSystem.kResolution.y;
			if (num < 0 || num >= cells.Length)
			{
				return default(WindCell);
			}
			return cells[num];
		}

		[Preserve]
		protected override void OnUpdate()
		{
			if (this.m_TerrainSystem.heightmap != null)
			{
				this.m_Odd = !this.m_Odd;
				if (!this.m_Odd)
				{
					TerrainHeightData data = this.m_TerrainSystem.GetHeightData();
					float x = TerrainUtils.ToWorldSpace(ref data, 0f);
					float y = TerrainUtils.ToWorldSpace(ref data, 65535f);
					float2 terrainRange = new float2(x, y);
					UpdateWindVelocityJob updateWindVelocityJob = default(UpdateWindVelocityJob);
					updateWindVelocityJob.m_Cells = this.m_Cells;
					updateWindVelocityJob.m_TerrainHeightData = data;
					updateWindVelocityJob.m_WaterSurfaceData = this.m_WaterSystem.GetSurfaceData(out var deps);
					updateWindVelocityJob.m_TerrainRange = terrainRange;
					UpdateWindVelocityJob jobData = updateWindVelocityJob;
					this.m_Deps = jobData.Schedule(WindSimulationSystem.kResolution.x * WindSimulationSystem.kResolution.y * WindSimulationSystem.kResolution.z, JobHandle.CombineDependencies(this.m_Deps, deps, base.Dependency));
					this.m_WaterSystem.AddSurfaceReader(this.m_Deps);
					this.m_TerrainSystem.AddCPUHeightReader(this.m_Deps);
				}
				else
				{
					UpdatePressureJob updatePressureJob = default(UpdatePressureJob);
					updatePressureJob.m_Cells = this.m_Cells;
					updatePressureJob.m_Wind = this.constantWind / 10f;
					UpdatePressureJob jobData2 = updatePressureJob;
					this.m_Deps = jobData2.Schedule(WindSimulationSystem.kResolution.x * WindSimulationSystem.kResolution.y * WindSimulationSystem.kResolution.z, JobHandle.CombineDependencies(this.m_Deps, base.Dependency));
				}
				base.Dependency = this.m_Deps;
			}
		}

		[Preserve]
		public WindSimulationSystem()
		{
		}
	}
}

using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Prefabs;
using Game.Serialization;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;
using Game;
using Game.Areas;

namespace MapExt.Systems
{
	
	public partial class MapTileSystem : GameSystemBase, IDefaultSerializable, ISerializable, IPostDeserialize
	{
		[BurstCompile]
		private struct GenerateMapTilesJob : IJobParallelFor
		{
			[DeallocateOnJobCompletion]
			[ReadOnly]
			public NativeArray<Entity> m_Entities;

			[ReadOnly]
			public Entity m_Prefab;

			[NativeDisableParallelForRestriction]
			public ComponentLookup<PrefabRef> m_PrefabRefData;

			[NativeDisableParallelForRestriction]
			public ComponentLookup<Area> m_AreaData;

			[NativeDisableParallelForRestriction]
			public BufferLookup<Node> m_NodeData;

			public void Execute(int index)
			{
				Entity entity = this.m_Entities[index];
				this.m_PrefabRefData[entity] = new PrefabRef(this.m_Prefab);
				this.m_AreaData[entity] = new Area(AreaFlags.Complete);
				DynamicBuffer<Node> dynamicBuffer = this.m_NodeData[entity];
				///vanilla;4倍数量
				//int2 @int = new int2(index % 92, index / 92);
				//float2 @float = new float2(92f, 92f) * 311.652161f;//v1.1.5f=311.652161f;
				//Bounds2 bounds = default(Bounds2);
				//bounds.min = (float2)@int * 623.3043f - @float;
				//bounds.max = (float2)(@int + 1) * 623.3043f - @float;
				///mod;4倍块大小
                int2 @int = new int2(index % 23, index / 23);
                float2 @float = new float2(23f, 23f) * 1246.608644f;
                Bounds2 bounds = default(Bounds2);
                bounds.min = ((float2)@int * 2493.217288f - @float);
                bounds.max = ((float2)(@int + 1) * 2493.217288f - @float);

                dynamicBuffer.ResizeUninitialized(4);
				dynamicBuffer[0] = new Node(new float3(bounds.min.x, 0f, bounds.min.y), float.MinValue);
				dynamicBuffer[1] = new Node(new float3(bounds.min.x, 0f, bounds.max.y), float.MinValue);
				dynamicBuffer[2] = new Node(new float3(bounds.max.x, 0f, bounds.max.y), float.MinValue);
				dynamicBuffer[3] = new Node(new float3(bounds.max.x, 0f, bounds.min.y), float.MinValue);
			}
		}

		private struct TypeHandle
		{
			public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RW_ComponentLookup;

			public ComponentLookup<Area> __Game_Areas_Area_RW_ComponentLookup;

			public BufferLookup<Node> __Game_Areas_Node_RW_BufferLookup;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void __AssignHandles(ref SystemState state)
			{
				this.__Game_Prefabs_PrefabRef_RW_ComponentLookup = state.GetComponentLookup<PrefabRef>();
				this.__Game_Areas_Area_RW_ComponentLookup = state.GetComponentLookup<Area>();
				this.__Game_Areas_Node_RW_BufferLookup = state.GetBufferLookup<Node>();
			}
		}

		private const int LEGACY_GRID_WIDTH = 23;//vanilla:23

		private const int LEGACY_GRID_LENGTH = 23;//vanilla:23

		private const float LEGACY_CELL_SIZE = 2493.217288f;//not used indeed~

		private EntityQuery m_PrefabQuery;

		private EntityQuery m_MapTileQuery;

		private NativeList<Entity> m_StartTiles;

		private TypeHandle __TypeHandle;

		[Preserve]
		protected override void OnCreate()
		{
			base.OnCreate();
			this.m_PrefabQuery = base.GetEntityQuery(ComponentType.ReadOnly<MapTileData>(), ComponentType.ReadOnly<AreaData>(), ComponentType.ReadOnly<PrefabData>(), ComponentType.Exclude<Locked>());
			this.m_MapTileQuery = base.GetEntityQuery(ComponentType.ReadOnly<MapTile>());
			this.m_StartTiles = new NativeList<Entity>(Allocator.Persistent);
		}

		[Preserve]
		protected override void OnDestroy()
		{
			this.m_StartTiles.Dispose();
			base.OnDestroy();
		}

		[Preserve]
		protected override void OnUpdate()
		{
		}

		public void PostDeserialize(Context context)
		{
			if (context.purpose == Purpose.NewGame)
			{
				if (context.version >= Version.editorMapTiles)
				{
					for (int i = 0; i < this.m_StartTiles.Length; i++)
					{
						if (this.m_StartTiles[i] == Entity.Null)
						{
							this.m_StartTiles.RemoveAtSwapBack(i);
						}
					}
					if (this.m_StartTiles.Length != 0)
					{
						base.EntityManager.RemoveComponent<Native>(this.m_StartTiles.AsArray());
					}
				}
				else
				{
					this.LegacyGenerateMapTiles(editorMode: false);
				}
			}
			else if (context.purpose == Purpose.NewMap)
			{
				this.LegacyGenerateMapTiles(editorMode: true);
			}
		}

		public NativeList<Entity> GetStartTiles()
		{
			return this.m_StartTiles;
		}

		public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
		{
			writer.Write(this.m_StartTiles.Length);
			for (int i = 0; i < this.m_StartTiles.Length; i++)
			{
				writer.Write(this.m_StartTiles[i]);
			}
		}

		public void Deserialize<TReader>(TReader reader) where TReader : IReader
		{
			reader.Read(out int value);
			this.m_StartTiles.ResizeUninitialized(value);
			for (int i = 0; i < value; i++)
			{
				reader.Read(out Entity value2);
				this.m_StartTiles[i] = value2;
			}
		}

		public void SetDefaults(Context context)
		{
			this.m_StartTiles.Clear();
		}

		private void LegacyGenerateMapTiles(bool editorMode)
		{
			if (!this.m_MapTileQuery.IsEmptyIgnoreFilter)
			{
				base.EntityManager.DestroyEntity(this.m_MapTileQuery);
			}
			this.m_StartTiles.Clear();
			NativeArray<Entity> nativeArray = this.m_PrefabQuery.ToEntityArray(Allocator.TempJob);
			try
			{
				Entity entity = nativeArray[0];
				AreaData componentData = base.EntityManager.GetComponentData<AreaData>(entity);
				///mod;
				//int entityCount = 529;//mod=8464;
				int entityCount = 529;
				///
				NativeArray<Entity> entities = base.EntityManager.CreateEntity(componentData.m_Archetype, entityCount, Allocator.TempJob);
				if (!editorMode)
				{
					base.EntityManager.AddComponent<Native>(entities);
				}
				this.AddOwner(new int2(10, 10), entities);
				this.AddOwner(new int2(11, 10), entities);
				this.AddOwner(new int2(12, 10), entities);
				this.AddOwner(new int2(10, 11), entities);
				this.AddOwner(new int2(11, 11), entities);
				this.AddOwner(new int2(12, 11), entities);
				this.AddOwner(new int2(10, 12), entities);
				this.AddOwner(new int2(11, 12), entities);
				this.AddOwner(new int2(12, 12), entities);
				this.__TypeHandle.__Game_Areas_Node_RW_BufferLookup.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Areas_Area_RW_ComponentLookup.Update(ref base.CheckedStateRef);
				this.__TypeHandle.__Game_Prefabs_PrefabRef_RW_ComponentLookup.Update(ref base.CheckedStateRef);
				GenerateMapTilesJob jobData = default(GenerateMapTilesJob);
				jobData.m_Entities = entities;
				jobData.m_Prefab = entity;
				jobData.m_PrefabRefData = this.__TypeHandle.__Game_Prefabs_PrefabRef_RW_ComponentLookup;
				jobData.m_AreaData = this.__TypeHandle.__Game_Areas_Area_RW_ComponentLookup;
				jobData.m_NodeData = this.__TypeHandle.__Game_Areas_Node_RW_BufferLookup;
				IJobParallelForExtensions.Schedule(jobData, entities.Length, 4).Complete();
			}
			finally
			{
				nativeArray.Dispose();
			}
		}

		private void AddOwner(int2 tile, NativeArray<Entity> entities)
		{
			int index = tile.y * 23 + tile.x;//vanilla=23;mod=92;
			base.EntityManager.RemoveComponent<Native>(entities[index]);
			ref NativeList<Entity> startTiles = ref this.m_StartTiles;
			Entity value = entities[index];
			startTiles.Add(in value);
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
		public MapTileSystem()
		{
		}
	}
}

#define UNITY_ASSERTIONS
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Serialization.Entities;
using Game.Prefabs;
using Unity.Assertions;
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
    /// bcjob未使用mapsize；可不替换
    /// </summary>
    public partial class GroundWaterSystem : CellMapSystem<GroundWater>, IJobSerializable
    {
        [BurstCompile]
        private struct GroundWaterTickJob : IJob
        {
            public NativeArray<GroundWater> m_GroundWaterMap;

            public WaterPipeParameterData m_Parameters;

            private void HandlePollution(int index, int otherIndex, NativeArray<int2> tmp)
            {
                GroundWater groundWater = this.m_GroundWaterMap[index];
                GroundWater groundWater2 = this.m_GroundWaterMap[otherIndex];
                ref int2 reference = ref tmp.ElementAt(index);
                ref int2 reference2 = ref tmp.ElementAt(otherIndex);
                int num = groundWater.m_Polluted + groundWater2.m_Polluted;
                int num2 = groundWater.m_Amount + groundWater2.m_Amount;
                int num3 = math.clamp((((num2 > 0) ? (groundWater.m_Amount * num / num2) : 0) - groundWater.m_Polluted) / 4, -(groundWater2.m_Amount - groundWater2.m_Polluted) / 4, (groundWater.m_Amount - groundWater.m_Polluted) / 4);
                reference.y += num3;
                reference2.y -= num3;
                Assert.IsTrue(0 <= groundWater.m_Polluted + reference.y);
                Assert.IsTrue(groundWater.m_Polluted + reference.y <= groundWater.m_Amount);
                Assert.IsTrue(0 <= groundWater2.m_Polluted + reference2.y);
                Assert.IsTrue(groundWater2.m_Polluted + reference2.y <= groundWater2.m_Amount);
            }

            private void HandleFlow(int index, int otherIndex, NativeArray<int2> tmp)
            {
                GroundWater groundWater = this.m_GroundWaterMap[index];
                GroundWater groundWater2 = this.m_GroundWaterMap[otherIndex];
                ref int2 reference = ref tmp.ElementAt(index);
                ref int2 reference2 = ref tmp.ElementAt(otherIndex);
                Assert.IsTrue(groundWater2.m_Polluted + reference2.y <= groundWater2.m_Amount + reference2.x);
                Assert.IsTrue(groundWater.m_Polluted + reference.y <= groundWater.m_Amount + reference.x);
                float num = ((groundWater.m_Amount + reference.x != 0) ? (1f * (float)(groundWater.m_Polluted + reference.y) / (float)(groundWater.m_Amount + reference.x)) : 0f);
                float num2 = ((groundWater2.m_Amount + reference2.x != 0) ? (1f * (float)(groundWater2.m_Polluted + reference2.y) / (float)(groundWater2.m_Amount + reference2.x)) : 0f);
                int num3 = groundWater.m_Amount - groundWater.m_Max;
                int num4 = math.clamp((groundWater2.m_Amount - groundWater2.m_Max - num3) / 4, -groundWater.m_Amount / 4, groundWater2.m_Amount / 4);
                reference.x += num4;
                reference2.x -= num4;
                int num5 = 0;
                if (num4 > 0)
                {
                    num5 = (int)((float)num4 * num2);
                }
                else if (num4 < 0)
                {
                    num5 = (int)((float)num4 * num);
                }
                reference.y += num5;
                reference2.y -= num5;
                Assert.IsTrue(0 <= groundWater.m_Amount + reference.x);
                Assert.IsTrue(groundWater.m_Amount + reference.x <= groundWater.m_Max);
                Assert.IsTrue(0 <= groundWater2.m_Amount + reference2.x);
                Assert.IsTrue(groundWater2.m_Amount + reference2.x <= groundWater2.m_Max);
                Assert.IsTrue(0 <= groundWater.m_Polluted + reference.y);
                Assert.IsTrue(groundWater.m_Polluted + reference.y <= groundWater.m_Amount + reference.x);
                Assert.IsTrue(0 <= groundWater2.m_Polluted + reference2.y);
                Assert.IsTrue(groundWater2.m_Polluted + reference2.y <= groundWater2.m_Amount + reference2.x);
            }

            public void Execute()
            {
                NativeArray<int2> tmp = new NativeArray<int2>(this.m_GroundWaterMap.Length, Allocator.Temp);
                for (int i = 0; i < this.m_GroundWaterMap.Length; i++)
                {
                    int num = i % GroundWaterSystem.kTextureSize;
                    int num2 = i / GroundWaterSystem.kTextureSize;
                    if (num < GroundWaterSystem.kTextureSize - 1)
                    {
                        this.HandlePollution(i, i + 1, tmp);
                    }
                    if (num2 < GroundWaterSystem.kTextureSize - 1)
                    {
                        this.HandlePollution(i, i + GroundWaterSystem.kTextureSize, tmp);
                    }
                }
                for (int j = 0; j < this.m_GroundWaterMap.Length; j++)
                {
                    int num3 = j % GroundWaterSystem.kTextureSize;
                    int num4 = j / GroundWaterSystem.kTextureSize;
                    if (num3 < GroundWaterSystem.kTextureSize - 1)
                    {
                        this.HandleFlow(j, j + 1, tmp);
                    }
                    if (num4 < GroundWaterSystem.kTextureSize - 1)
                    {
                        this.HandleFlow(j, j + GroundWaterSystem.kTextureSize, tmp);
                    }
                }
                for (int k = 0; k < this.m_GroundWaterMap.Length; k++)
                {
                    GroundWater value = this.m_GroundWaterMap[k];
                    value.m_Amount = (short)math.min(value.m_Amount + tmp[k].x + Mathf.CeilToInt(this.m_Parameters.m_GroundwaterReplenish * (float)value.m_Max), value.m_Max);
                    value.m_Polluted = (short)math.clamp(value.m_Polluted + tmp[k].y - this.m_Parameters.m_GroundwaterPurification, 0, value.m_Amount);
                    this.m_GroundWaterMap[k] = value;
                }
                tmp.Dispose();
            }
        }

        public const int kMaxGroundWater = 10000;

        public const int kMinGroundWaterThreshold = 500;

        public static readonly int kTextureSize = 256;

        private EntityQuery m_ParameterQuery;

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 128;
        }

        public override int GetUpdateOffset(SystemUpdatePhase phase)
        {
            return 64;
        }

        public static float3 GetCellCenter(int index)
        {
            return CellMapSystemRe.GetCellCenter(index, GroundWaterSystem.kTextureSize);
        }

        public static bool TryGetCell(float3 position, out int2 cell)
        {
            cell = CellMapSystemRe.GetCell(position, CellMapSystemRe.kMapSize, GroundWaterSystem.kTextureSize);
            return GroundWaterSystem.IsValidCell(cell);
        }

        public static bool IsValidCell(int2 cell)
        {
            if (cell.x >= 0 && cell.y >= 0 && cell.x < GroundWaterSystem.kTextureSize)
            {
                return cell.y < GroundWaterSystem.kTextureSize;
            }
            return false;
        }

        public static GroundWater GetGroundWater(float3 position, NativeArray<GroundWater> groundWaterMap)
        {
            float2 @float = CellMapSystemRe.GetCellCoords(position, CellMapSystemRe.kMapSize, GroundWaterSystem.kTextureSize) - new float2(0.5f, 0.5f);
            int2 cell = new int2(Mathf.FloorToInt(@float.x), Mathf.FloorToInt(@float.y));
            int2 cell2 = new int2(cell.x + 1, cell.y);
            int2 cell3 = new int2(cell.x, cell.y + 1);
            int2 cell4 = new int2(cell.x + 1, cell.y + 1);
            GroundWater groundWater = GroundWaterSystem.GetGroundWater(groundWaterMap, cell);
            GroundWater groundWater2 = GroundWaterSystem.GetGroundWater(groundWaterMap, cell2);
            GroundWater groundWater3 = GroundWaterSystem.GetGroundWater(groundWaterMap, cell3);
            GroundWater groundWater4 = GroundWaterSystem.GetGroundWater(groundWaterMap, cell4);
            float sx = @float.x - (float)cell.x;
            float sy = @float.y - (float)cell.y;
            GroundWater result = default(GroundWater);
            result.m_Amount = (short)math.round(GroundWaterSystem.Bilinear(groundWater.m_Amount, groundWater2.m_Amount, groundWater3.m_Amount, groundWater4.m_Amount, sx, sy));
            result.m_Polluted = (short)math.round(GroundWaterSystem.Bilinear(groundWater.m_Polluted, groundWater2.m_Polluted, groundWater3.m_Polluted, groundWater4.m_Polluted, sx, sy));
            result.m_Max = (short)math.round(GroundWaterSystem.Bilinear(groundWater.m_Max, groundWater2.m_Max, groundWater3.m_Max, groundWater4.m_Max, sx, sy));
            return result;
        }

        public static void ConsumeGroundWater(float3 position, NativeArray<GroundWater> groundWaterMap, int amount)
        {
            Assert.IsTrue(amount >= 0);
            float2 @float = CellMapSystemRe.GetCellCoords(position, CellMapSystemRe.kMapSize, GroundWaterSystem.kTextureSize) - new float2(0.5f, 0.5f);
            int2 cell = new int2(Mathf.FloorToInt(@float.x), Mathf.FloorToInt(@float.y));
            int2 cell2 = new int2(cell.x + 1, cell.y);
            int2 cell3 = new int2(cell.x, cell.y + 1);
            int2 cell4 = new int2(cell.x + 1, cell.y + 1);
            GroundWater gw2 = GroundWaterSystem.GetGroundWater(groundWaterMap, cell);
            GroundWater gw3 = GroundWaterSystem.GetGroundWater(groundWaterMap, cell2);
            GroundWater gw4 = GroundWaterSystem.GetGroundWater(groundWaterMap, cell3);
            GroundWater gw5 = GroundWaterSystem.GetGroundWater(groundWaterMap, cell4);
            float sx = @float.x - (float)cell.x;
            float sy = @float.y - (float)cell.y;
            float num = math.ceil(GroundWaterSystem.Bilinear(gw2.m_Amount, 0, 0, 0, sx, sy));
            float num2 = math.ceil(GroundWaterSystem.Bilinear(0, gw3.m_Amount, 0, 0, sx, sy));
            float num3 = math.ceil(GroundWaterSystem.Bilinear(0, 0, gw4.m_Amount, 0, sx, sy));
            float num4 = math.ceil(GroundWaterSystem.Bilinear(0, 0, 0, gw5.m_Amount, sx, sy));
            float totalAvailable = num + num2 + num3 + num4;
            float totalConsumed = math.min(amount, totalAvailable);
            if (totalAvailable < (float)amount)
            {
                UnityEngine.Debug.LogWarning($"Trying to consume more groundwater than available! amount: {amount}, available: {totalAvailable}");
            }
            ConsumeFraction(ref gw2, num);
            ConsumeFraction(ref gw3, num2);
            ConsumeFraction(ref gw4, num3);
            ConsumeFraction(ref gw5, num4);
            Assert.IsTrue(Mathf.Approximately(totalAvailable, 0f));
            Assert.IsTrue(Mathf.Approximately(totalConsumed, 0f));
            GroundWaterSystem.SetGroundWater(groundWaterMap, cell, gw2);
            GroundWaterSystem.SetGroundWater(groundWaterMap, cell2, gw3);
            GroundWaterSystem.SetGroundWater(groundWaterMap, cell3, gw4);
            GroundWaterSystem.SetGroundWater(groundWaterMap, cell4, gw5);
            void ConsumeFraction(ref GroundWater gw, float cellAvailable)
            {
                if (!(totalAvailable < 0.5f))
                {
                    float num5 = cellAvailable / totalAvailable;
                    totalAvailable -= cellAvailable;
                    float num6 = math.max(y: math.max(0f, totalConsumed - totalAvailable), x: math.round(num5 * totalConsumed));
                    Assert.IsTrue(num6 <= (float)gw.m_Amount);
                    gw.Consume((int)num6);
                    totalConsumed -= num6;
                }
            }
        }

        private static GroundWater GetGroundWater(NativeArray<GroundWater> groundWaterMap, int2 cell)
        {
            if (!GroundWaterSystem.IsValidCell(cell))
            {
                return default(GroundWater);
            }
            return groundWaterMap[cell.x + GroundWaterSystem.kTextureSize * cell.y];
        }

        private static void SetGroundWater(NativeArray<GroundWater> groundWaterMap, int2 cell, GroundWater gw)
        {
            if (GroundWaterSystem.IsValidCell(cell))
            {
                groundWaterMap[cell.x + GroundWaterSystem.kTextureSize * cell.y] = gw;
            }
        }

        private static float Bilinear(short v00, short v10, short v01, short v11, float sx, float sy)
        {
            return math.lerp(math.lerp(v00, v10, sx), math.lerp(v01, v11, sx), sy);
        }

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            base.CreateTextures(GroundWaterSystem.kTextureSize);
            this.m_ParameterQuery = base.GetEntityQuery(ComponentType.ReadOnly<WaterPipeParameterData>());
        }

        [Preserve]
        protected override void OnUpdate()
        {
            GroundWaterTickJob groundWaterTickJob = default(GroundWaterTickJob);
            groundWaterTickJob.m_GroundWaterMap = base.m_Map;
            groundWaterTickJob.m_Parameters = this.m_ParameterQuery.GetSingleton<WaterPipeParameterData>();
            GroundWaterTickJob jobData = groundWaterTickJob;
            base.Dependency = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(base.m_WriteDependencies, base.m_ReadDependencies, base.Dependency));
            base.AddWriter(base.Dependency);
            base.Dependency = JobHandle.CombineDependencies(base.m_ReadDependencies, base.m_WriteDependencies, base.Dependency);
        }

        public override JobHandle SetDefaults(Context context)
        {
            if (context.purpose == Purpose.NewGame && context.version < Version.timoSerializationFlow)
            {
                for (int i = 0; i < base.m_Map.Length; i++)
                {
                    float num = (float)(i % GroundWaterSystem.kTextureSize) / (float)GroundWaterSystem.kTextureSize;
                    float num2 = (float)(i / GroundWaterSystem.kTextureSize) / (float)GroundWaterSystem.kTextureSize;
                    short num3 = (short)Mathf.RoundToInt(10000f * math.saturate((Mathf.PerlinNoise(32f * num, 32f * num2) - 0.6f) / 0.4f));
                    GroundWater groundWater = default(GroundWater);
                    groundWater.m_Amount = num3;
                    groundWater.m_Max = num3;
                    GroundWater value = groundWater;
                    base.m_Map[i] = value;
                }
                return default(JobHandle);
            }
            return base.SetDefaults(context);
        }

        [Preserve]
        public GroundWaterSystem()
        {
        }

        //泛型非静态方法重定向，配合harmony patch;
        public new NativeArray<GroundWater> GetMap(bool readOnly, out JobHandle dependencies)
        {
            dependencies = (readOnly ? this.m_WriteDependencies : JobHandle.CombineDependencies(this.m_ReadDependencies, this.m_WriteDependencies));
            return this.m_Map;
        }

        public new CellMapData<GroundWater> GetData(bool readOnly, out JobHandle dependencies)
        {
            dependencies = (readOnly ? this.m_WriteDependencies : JobHandle.CombineDependencies(this.m_ReadDependencies, this.m_WriteDependencies));
            CellMapData<GroundWater> result = default(CellMapData<GroundWater>);
            result.m_Buffer = this.m_Map;
            result.m_CellSize = 57344 / (float2)this.m_TextureSize;
            result.m_TextureSize = this.m_TextureSize;
            return result;
        }

        public new void AddReader(JobHandle jobHandle)
        {
            this.m_ReadDependencies = JobHandle.CombineDependencies(this.m_ReadDependencies, jobHandle);
        }

        public new void AddWriter(JobHandle jobHandle)
        {
            this.m_WriteDependencies = jobHandle;
        }

    }
}

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Areas;
using Game.Audio;
using Game.Buildings;
using Game.Common;
using Game.Input;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Zones;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;
using Game;
using Game.Tools;


namespace MapExt.Systems
{
    //[CompilerGenerated]
    public partial class AreaToolSystem : ToolBaseSystem
    {
        public enum Mode
        {
            Edit = 0,
            Generate = 1
        }

        public enum State
        {
            Default = 0,
            Create = 1,
            Modify = 2,
            Remove = 3
        }

        public enum Tooltip
        {
            None = 0,
            CreateArea = 1,
            ModifyNode = 2,
            ModifyEdge = 3,
            CreateAreaOrModifyNode = 4,
            CreateAreaOrModifyEdge = 5,
            AddNode = 6,
            InsertNode = 7,
            MoveNode = 8,
            MergeNodes = 9,
            CompleteArea = 10,
            DeleteArea = 11,
            RemoveNode = 12,
            GenerateAreas = 13
        }

        [BurstCompile]
        private struct SnapJob : IJob
        {
            private struct ParentObjectIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
            {
                public Line3.Segment m_Line;

                public float m_BoundsOffset;

                public float m_MaxDistance;

                public Entity m_Parent;

                public ComponentLookup<Transform> m_TransformData;

                public ComponentLookup<PrefabRef> m_PrefabRefData;

                public ComponentLookup<BuildingData> m_BuildingData;

                public ComponentLookup<ObjectGeometryData> m_ObjectGeometryData;

                public bool Intersect(QuadTreeBoundsXZ bounds)
                {
                    float2 t;
                    return MathUtils.Intersect(MathUtils.Expand(bounds.m_Bounds, this.m_BoundsOffset), this.m_Line, out t);
                }

                public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
                {
                    if (!MathUtils.Intersect(MathUtils.Expand(bounds.m_Bounds, this.m_BoundsOffset), this.m_Line, out var t) || !this.m_TransformData.HasComponent(entity))
                    {
                        return;
                    }
                    PrefabRef prefabRef = this.m_PrefabRefData[entity];
                    Transform transform = this.m_TransformData[entity];
                    if (!this.m_ObjectGeometryData.HasComponent(prefabRef.m_Prefab))
                    {
                        return;
                    }
                    ObjectGeometryData objectGeometryData = this.m_ObjectGeometryData[prefabRef.m_Prefab];
                    if (this.m_BuildingData.HasComponent(prefabRef.m_Prefab))
                    {
                        float2 @float = this.m_BuildingData[prefabRef.m_Prefab].m_LotSize;
                        objectGeometryData.m_Bounds.min.xz = @float * -4f - this.m_MaxDistance;
                        objectGeometryData.m_Bounds.max.xz = @float * 4f + this.m_MaxDistance;
                    }
                    if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Circular) != 0)
                    {
                        float num = math.max(math.cmax(objectGeometryData.m_Bounds.max.xz), 0f - math.cmin(objectGeometryData.m_Bounds.max.xz));
                        if (MathUtils.Distance(this.m_Line.xz, transform.m_Position.xz, out var _) < num + this.m_MaxDistance)
                        {
                            this.m_Parent = entity;
                        }
                    }
                    else if (MathUtils.Intersect(ObjectUtils.CalculateBaseCorners(transform.m_Position, transform.m_Rotation, objectGeometryData.m_Bounds).xz, this.m_Line.xz, out t))
                    {
                        this.m_Parent = entity;
                    }
                }
            }

            private struct AreaIterator2 : INativeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>
            {
                public bool m_EditorMode;

                public Game.Areas.AreaType m_AreaType;

                public Bounds3 m_Bounds;

                public float m_MaxDistance1;

                public float m_MaxDistance2;

                public ControlPoint m_ControlPoint1;

                public ControlPoint m_ControlPoint2;

                public NativeParallelHashSet<Entity> m_IgnoreAreas;

                public NativeList<ControlPoint> m_ControlPoints;

                public ComponentLookup<PrefabRef> m_PrefabRefData;

                public ComponentLookup<AreaGeometryData> m_PrefabAreaData;

                public ComponentLookup<Game.Areas.Lot> m_LotData;

                public ComponentLookup<Owner> m_OwnerData;

                public BufferLookup<Game.Areas.Node> m_Nodes;

                public BufferLookup<Triangle> m_Triangles;

                public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

                public bool Intersect(QuadTreeBoundsXZ bounds)
                {
                    return MathUtils.Intersect(bounds.m_Bounds, this.m_Bounds);
                }

                public void Iterate(QuadTreeBoundsXZ bounds, AreaSearchItem areaItem)
                {
                    if (!MathUtils.Intersect(bounds.m_Bounds, this.m_Bounds) || (this.m_IgnoreAreas.IsCreated && this.m_IgnoreAreas.Contains(areaItem.m_Area)) || (this.m_OwnerData.TryGetComponent(areaItem.m_Area, out var componentData) && (this.m_Nodes.HasBuffer(componentData.m_Owner) || (this.m_EditorMode && this.m_InstalledUpgrades.TryGetBuffer(componentData.m_Owner, out var bufferData) && bufferData.Length != 0))))
                    {
                        return;
                    }
                    PrefabRef prefabRef = this.m_PrefabRefData[areaItem.m_Area];
                    AreaGeometryData areaGeometryData = this.m_PrefabAreaData[prefabRef.m_Prefab];
                    if (areaGeometryData.m_Type != this.m_AreaType)
                    {
                        return;
                    }
                    DynamicBuffer<Game.Areas.Node> nodes = this.m_Nodes[areaItem.m_Area];
                    Triangle triangle = this.m_Triangles[areaItem.m_Area][areaItem.m_Triangle];
                    Triangle3 triangle2 = AreaUtils.GetTriangle3(nodes, triangle);
                    int3 @int = math.abs(triangle.m_Indices - triangle.m_Indices.yzx);
                    bool3 x = (@int == 1) | (@int == nodes.Length - 1);
                    if (math.any(x))
                    {
                        bool lockFirstEdge = !this.m_EditorMode && this.m_LotData.HasComponent(areaItem.m_Area);
                        if (x.x)
                        {
                            this.CheckLine(triangle2.ab, areaGeometryData.m_SnapDistance, areaItem.m_Area, triangle.m_Indices.xy, lockFirstEdge);
                        }
                        if (x.y)
                        {
                            this.CheckLine(triangle2.bc, areaGeometryData.m_SnapDistance, areaItem.m_Area, triangle.m_Indices.yz, lockFirstEdge);
                        }
                        if (x.z)
                        {
                            this.CheckLine(triangle2.ca, areaGeometryData.m_SnapDistance, areaItem.m_Area, triangle.m_Indices.zx, lockFirstEdge);
                        }
                    }
                }

                public void CheckLine(Line3.Segment line, float snapDistance, Entity area, int2 nodeIndex, bool lockFirstEdge)
                {
                    if (lockFirstEdge && math.cmin(nodeIndex) == 0 && math.cmax(nodeIndex) == 1)
                    {
                        return;
                    }
                    float t;
                    float num = MathUtils.Distance(line.xz, this.m_ControlPoint1.m_Position.xz, out t);
                    float t2;
                    float num2 = MathUtils.Distance(line.xz, this.m_ControlPoint2.m_HitPosition.xz, out t2);
                    if (!(num < this.m_MaxDistance1) || !(num2 < this.m_MaxDistance2))
                    {
                        return;
                    }
                    float num3 = math.distance(line.a.xz, this.m_ControlPoint2.m_HitPosition.xz);
                    float num4 = math.distance(line.b.xz, this.m_ControlPoint2.m_HitPosition.xz);
                    ControlPoint value = this.m_ControlPoint1;
                    value.m_OriginalEntity = area;
                    if (num3 <= snapDistance && num3 <= num4 && (!lockFirstEdge || nodeIndex.x >= 2))
                    {
                        value.m_ElementIndex = new int2(nodeIndex.x, -1);
                    }
                    else if (num4 <= snapDistance && (!lockFirstEdge || nodeIndex.y >= 2))
                    {
                        value.m_ElementIndex = new int2(nodeIndex.y, -1);
                    }
                    else
                    {
                        value.m_ElementIndex = new int2(-1, math.select(math.cmax(nodeIndex), math.cmin(nodeIndex), math.abs(nodeIndex.y - nodeIndex.x) == 1));
                    }
                    for (int i = 0; i < this.m_ControlPoints.Length; i++)
                    {
                        if (this.m_ControlPoints[i].m_OriginalEntity == area)
                        {
                            return;
                        }
                    }
                    this.m_ControlPoints.Add(in value);
                }
            }

            private struct AreaIterator : INativeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>
            {
                public bool m_EditorMode;

                public bool m_IgnoreStartPositions;

                public Snap m_Snap;

                public Game.Areas.AreaType m_AreaType;

                public Bounds3 m_Bounds;

                public float m_MaxDistance;

                public NativeParallelHashSet<Entity> m_IgnoreAreas;

                public Entity m_PreferArea;

                public ControlPoint m_ControlPoint;

                public ControlPoint m_BestSnapPosition;

                public NativeList<SnapLine> m_SnapLines;

                public NativeList<ControlPoint> m_MoveStartPositions;

                public ComponentLookup<PrefabRef> m_PrefabRefData;

                public ComponentLookup<AreaGeometryData> m_PrefabAreaData;

                public ComponentLookup<Game.Areas.Lot> m_LotData;

                public ComponentLookup<Owner> m_OwnerData;

                public BufferLookup<Game.Areas.Node> m_Nodes;

                public BufferLookup<Triangle> m_Triangles;

                public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

                public bool Intersect(QuadTreeBoundsXZ bounds)
                {
                    return MathUtils.Intersect(bounds.m_Bounds, this.m_Bounds);
                }

                public void Iterate(QuadTreeBoundsXZ bounds, AreaSearchItem areaItem)
                {
                    if (!MathUtils.Intersect(bounds.m_Bounds, this.m_Bounds) || (this.m_IgnoreAreas.IsCreated && this.m_IgnoreAreas.Contains(areaItem.m_Area)))
                    {
                        return;
                    }
                    Entity area = areaItem.m_Area;
                    if (areaItem.m_Area != this.m_PreferArea)
                    {
                        if ((this.m_Snap & Snap.ExistingGeometry) == 0)
                        {
                            bool flag = false;
                            if (this.m_IgnoreStartPositions)
                            {
                                for (int i = 0; i < this.m_MoveStartPositions.Length; i++)
                                {
                                    flag |= this.m_MoveStartPositions[i].m_OriginalEntity == areaItem.m_Area;
                                }
                            }
                            if (!flag)
                            {
                                return;
                            }
                        }
                        if (this.m_OwnerData.TryGetComponent(areaItem.m_Area, out var componentData))
                        {
                            if (this.m_Nodes.HasBuffer(componentData.m_Owner))
                            {
                                return;
                            }
                            if (this.m_EditorMode && this.m_InstalledUpgrades.TryGetBuffer(componentData.m_Owner, out var bufferData) && bufferData.Length != 0)
                            {
                                area = Entity.Null;
                            }
                        }
                    }
                    PrefabRef prefabRef = this.m_PrefabRefData[areaItem.m_Area];
                    AreaGeometryData areaGeometryData = this.m_PrefabAreaData[prefabRef.m_Prefab];
                    if (areaGeometryData.m_Type != this.m_AreaType)
                    {
                        return;
                    }
                    DynamicBuffer<Game.Areas.Node> nodes = this.m_Nodes[areaItem.m_Area];
                    Triangle triangle = this.m_Triangles[areaItem.m_Area][areaItem.m_Triangle];
                    Triangle3 triangle2 = AreaUtils.GetTriangle3(nodes, triangle);
                    int3 @int = math.abs(triangle.m_Indices - triangle.m_Indices.yzx);
                    bool3 @bool = @int == nodes.Length - 1;
                    bool3 x = (@int == 1) | @bool;
                    if (!math.any(x))
                    {
                        return;
                    }
                    if (this.m_IgnoreStartPositions)
                    {
                        bool3 c = triangle.m_Indices.yzx < triangle.m_Indices != @bool;
                        int3 int2 = math.select(triangle.m_Indices, triangle.m_Indices.yzx, c);
                        int3 int3 = math.select(triangle.m_Indices.yzx, triangle.m_Indices, c);
                        for (int j = 0; j < this.m_MoveStartPositions.Length; j++)
                        {
                            ControlPoint controlPoint = this.m_MoveStartPositions[j];
                            if (!(controlPoint.m_OriginalEntity != areaItem.m_Area))
                            {
                                x &= controlPoint.m_ElementIndex.x != int2;
                                x &= controlPoint.m_ElementIndex.x != int3;
                                x &= controlPoint.m_ElementIndex.y != int2;
                            }
                        }
                    }
                    bool lockFirstEdge = !this.m_EditorMode && this.m_LotData.HasComponent(areaItem.m_Area);
                    float snapDistance = math.select(areaGeometryData.m_SnapDistance, areaGeometryData.m_SnapDistance * 0.5f, (this.m_Snap & Snap.ExistingGeometry) == 0);
                    if (x.x)
                    {
                        this.CheckLine(triangle2.ab, snapDistance, area, triangle.m_Indices.xy, lockFirstEdge);
                    }
                    if (x.y)
                    {
                        this.CheckLine(triangle2.bc, snapDistance, area, triangle.m_Indices.yz, lockFirstEdge);
                    }
                    if (x.z)
                    {
                        this.CheckLine(triangle2.ca, snapDistance, area, triangle.m_Indices.zx, lockFirstEdge);
                    }
                }

                public void CheckLine(Line3.Segment line, float snapDistance, Entity area, int2 nodeIndex, bool lockFirstEdge)
                {
                    if ((!lockFirstEdge || math.cmin(nodeIndex) != 0 || math.cmax(nodeIndex) != 1) && MathUtils.Distance(line.xz, this.m_ControlPoint.m_HitPosition.xz, out var t) < this.m_MaxDistance)
                    {
                        float level = math.select(2f, 3f, area == this.m_PreferArea);
                        float num = math.distance(line.a.xz, this.m_ControlPoint.m_HitPosition.xz);
                        float num2 = math.distance(line.b.xz, this.m_ControlPoint.m_HitPosition.xz);
                        ControlPoint controlPoint = this.m_ControlPoint;
                        controlPoint.m_OriginalEntity = area;
                        controlPoint.m_Direction = line.b.xz - line.a.xz;
                        MathUtils.TryNormalize(ref controlPoint.m_Direction);
                        if (num <= snapDistance && num <= num2 && (!lockFirstEdge || nodeIndex.x >= 2))
                        {
                            controlPoint.m_Position = line.a;
                            controlPoint.m_ElementIndex = new int2(nodeIndex.x, -1);
                            controlPoint.m_SnapPriority = ToolUtils.CalculateSnapPriority(level, 1f, this.m_ControlPoint.m_HitPosition.xz, controlPoint.m_Position.xz, controlPoint.m_Direction);
                            ToolUtils.AddSnapPosition(ref this.m_BestSnapPosition, controlPoint);
                            ToolUtils.AddSnapLine(ref this.m_BestSnapPosition, this.m_SnapLines, new SnapLine(controlPoint, NetUtils.StraightCurve(line.a, line.b), (SnapLineFlags)0));
                        }
                        else if (num2 <= snapDistance && (!lockFirstEdge || nodeIndex.y >= 2))
                        {
                            controlPoint.m_Position = line.b;
                            controlPoint.m_ElementIndex = new int2(nodeIndex.y, -1);
                            controlPoint.m_SnapPriority = ToolUtils.CalculateSnapPriority(level, 1f, this.m_ControlPoint.m_HitPosition.xz, controlPoint.m_Position.xz, controlPoint.m_Direction);
                            ToolUtils.AddSnapPosition(ref this.m_BestSnapPosition, controlPoint);
                            ToolUtils.AddSnapLine(ref this.m_BestSnapPosition, this.m_SnapLines, new SnapLine(controlPoint, NetUtils.StraightCurve(line.a, line.b), (SnapLineFlags)0));
                        }
                        else
                        {
                            controlPoint.m_Position = MathUtils.Position(line, t);
                            controlPoint.m_ElementIndex = new int2(-1, math.select(math.cmax(nodeIndex), math.cmin(nodeIndex), math.abs(nodeIndex.y - nodeIndex.x) == 1));
                            controlPoint.m_SnapPriority = ToolUtils.CalculateSnapPriority(level, 1f, this.m_ControlPoint.m_HitPosition.xz, controlPoint.m_Position.xz, controlPoint.m_Direction);
                            ToolUtils.AddSnapPosition(ref this.m_BestSnapPosition, controlPoint);
                            ToolUtils.AddSnapLine(ref this.m_BestSnapPosition, this.m_SnapLines, new SnapLine(controlPoint, NetUtils.StraightCurve(line.a, line.b), (SnapLineFlags)0));
                        }
                    }
                }
            }

            private struct NetIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
            {
                public Snap m_Snap;

                public Bounds3 m_Bounds;

                public float m_MaxDistance;

                public ControlPoint m_ControlPoint;

                public ControlPoint m_BestSnapPosition;

                public NativeList<SnapLine> m_SnapLines;

                public ComponentLookup<Curve> m_CurveData;

                public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

                public ComponentLookup<StartNodeGeometry> m_StartGeometryData;

                public ComponentLookup<EndNodeGeometry> m_EndGeometryData;

                public ComponentLookup<Composition> m_CompositionData;

                public ComponentLookup<NetCompositionData> m_PrefabCompositionData;

                public bool Intersect(QuadTreeBoundsXZ bounds)
                {
                    return MathUtils.Intersect(bounds.m_Bounds, this.m_Bounds);
                }

                public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
                {
                    if (!MathUtils.Intersect(bounds.m_Bounds, this.m_Bounds))
                    {
                        return;
                    }
                    Composition composition = default(Composition);
                    if (this.m_CompositionData.HasComponent(entity))
                    {
                        composition = this.m_CompositionData[entity];
                    }
                    if ((this.m_Snap & Snap.NetSide) != 0)
                    {
                        if (this.m_EdgeGeometryData.HasComponent(entity) && this.CheckComposition(composition.m_Edge))
                        {
                            EdgeGeometry edgeGeometry = this.m_EdgeGeometryData[entity];
                            this.SnapEdgeCurve(edgeGeometry.m_Start.m_Left);
                            this.SnapEdgeCurve(edgeGeometry.m_Start.m_Right);
                            this.SnapEdgeCurve(edgeGeometry.m_End.m_Left);
                            this.SnapEdgeCurve(edgeGeometry.m_End.m_Right);
                        }
                        if (this.m_StartGeometryData.HasComponent(entity) && this.CheckComposition(composition.m_StartNode))
                        {
                            StartNodeGeometry startNodeGeometry = this.m_StartGeometryData[entity];
                            if (startNodeGeometry.m_Geometry.m_MiddleRadius > 0f)
                            {
                                this.SnapNodeCurve(startNodeGeometry.m_Geometry.m_Left.m_Left);
                                this.SnapNodeCurve(startNodeGeometry.m_Geometry.m_Left.m_Right);
                                this.SnapNodeCurve(startNodeGeometry.m_Geometry.m_Right.m_Left);
                                this.SnapNodeCurve(startNodeGeometry.m_Geometry.m_Right.m_Right);
                            }
                            else
                            {
                                this.SnapNodeCurve(startNodeGeometry.m_Geometry.m_Left.m_Left);
                                this.SnapNodeCurve(startNodeGeometry.m_Geometry.m_Right.m_Right);
                            }
                        }
                        if (this.m_EndGeometryData.HasComponent(entity) && this.CheckComposition(composition.m_EndNode))
                        {
                            EndNodeGeometry endNodeGeometry = this.m_EndGeometryData[entity];
                            if (endNodeGeometry.m_Geometry.m_MiddleRadius > 0f)
                            {
                                this.SnapNodeCurve(endNodeGeometry.m_Geometry.m_Left.m_Left);
                                this.SnapNodeCurve(endNodeGeometry.m_Geometry.m_Left.m_Right);
                                this.SnapNodeCurve(endNodeGeometry.m_Geometry.m_Right.m_Left);
                                this.SnapNodeCurve(endNodeGeometry.m_Geometry.m_Right.m_Right);
                            }
                            else
                            {
                                this.SnapNodeCurve(endNodeGeometry.m_Geometry.m_Left.m_Left);
                                this.SnapNodeCurve(endNodeGeometry.m_Geometry.m_Right.m_Right);
                            }
                        }
                    }
                    if ((this.m_Snap & Snap.NetMiddle) != 0 && this.m_CurveData.HasComponent(entity) && this.CheckComposition(composition.m_Edge))
                    {
                        this.SnapEdgeCurve(this.m_CurveData[entity].m_Bezier);
                    }
                }

                private bool CheckComposition(Entity composition)
                {
                    if (this.m_PrefabCompositionData.TryGetComponent(composition, out var componentData) && (componentData.m_Flags.m_General & CompositionFlags.General.Tunnel) != 0)
                    {
                        return false;
                    }
                    return true;
                }

                private void SnapEdgeCurve(Bezier4x3 curve)
                {
                    if (MathUtils.Intersect(this.m_Bounds, MathUtils.Bounds(curve)) && MathUtils.Distance(curve.xz, this.m_ControlPoint.m_HitPosition.xz, out var t) < this.m_MaxDistance)
                    {
                        ControlPoint controlPoint = this.m_ControlPoint;
                        controlPoint.m_OriginalEntity = Entity.Null;
                        controlPoint.m_Position = MathUtils.Position(curve, t);
                        controlPoint.m_Direction = MathUtils.Tangent(curve, t).xz;
                        MathUtils.TryNormalize(ref controlPoint.m_Direction);
                        controlPoint.m_SnapPriority = ToolUtils.CalculateSnapPriority(1f, 1f, this.m_ControlPoint.m_HitPosition.xz, controlPoint.m_Position.xz, controlPoint.m_Direction);
                        ToolUtils.AddSnapPosition(ref this.m_BestSnapPosition, controlPoint);
                        ToolUtils.AddSnapLine(ref this.m_BestSnapPosition, this.m_SnapLines, new SnapLine(controlPoint, curve, (SnapLineFlags)0));
                    }
                }

                private void SnapNodeCurve(Bezier4x3 curve)
                {
                    float3 value = MathUtils.StartTangent(curve);
                    value = MathUtils.Normalize(value, value.xz);
                    value.y = math.clamp(value.y, -1f, 1f);
                    Line3.Segment line = new Line3.Segment(curve.a, curve.a + value * math.dot(curve.d - curve.a, value));
                    if (MathUtils.Intersect(this.m_Bounds, MathUtils.Bounds(line)) && MathUtils.Distance(line.xz, this.m_ControlPoint.m_HitPosition.xz, out var t) < this.m_MaxDistance)
                    {
                        ControlPoint controlPoint = this.m_ControlPoint;
                        controlPoint.m_OriginalEntity = Entity.Null;
                        controlPoint.m_Direction = value.xz;
                        controlPoint.m_Position = MathUtils.Position(line, t);
                        controlPoint.m_SnapPriority = ToolUtils.CalculateSnapPriority(1f, 1f, this.m_ControlPoint.m_HitPosition.xz, controlPoint.m_Position.xz, controlPoint.m_Direction);
                        ToolUtils.AddSnapPosition(ref this.m_BestSnapPosition, controlPoint);
                        ToolUtils.AddSnapLine(ref this.m_BestSnapPosition, this.m_SnapLines, new SnapLine(controlPoint, NetUtils.StraightCurve(line.a, line.b), (SnapLineFlags)0));
                    }
                }
            }

            private struct ObjectIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
            {
                public Bounds3 m_Bounds;

                public float m_MaxDistance;

                public Snap m_Snap;

                public ControlPoint m_ControlPoint;

                public ControlPoint m_BestSnapPosition;

                public NativeList<SnapLine> m_SnapLines;

                public ComponentLookup<Transform> m_TransformData;

                public ComponentLookup<PrefabRef> m_PrefabRefData;

                public ComponentLookup<BuildingData> m_BuildingData;

                public ComponentLookup<BuildingExtensionData> m_BuildingExtensionData;

                public ComponentLookup<AssetStampData> m_AssetStampData;

                public ComponentLookup<ObjectGeometryData> m_ObjectGeometryData;

                public bool Intersect(QuadTreeBoundsXZ bounds)
                {
                    return MathUtils.Intersect(bounds.m_Bounds, this.m_Bounds);
                }

                public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
                {
                    if (!MathUtils.Intersect(bounds.m_Bounds, this.m_Bounds) || !this.m_TransformData.HasComponent(entity))
                    {
                        return;
                    }
                    PrefabRef prefabRef = this.m_PrefabRefData[entity];
                    Transform transform = this.m_TransformData[entity];
                    if (!this.m_ObjectGeometryData.HasComponent(prefabRef.m_Prefab))
                    {
                        return;
                    }
                    ObjectGeometryData objectGeometryData = this.m_ObjectGeometryData[prefabRef.m_Prefab];
                    if ((this.m_Snap & Snap.LotGrid) != 0 && (this.m_BuildingData.HasComponent(prefabRef.m_Prefab) || this.m_BuildingExtensionData.HasComponent(prefabRef.m_Prefab) || this.m_AssetStampData.HasComponent(prefabRef.m_Prefab)))
                    {
                        float2 @float = math.normalizesafe(math.forward(transform.m_Rotation).xz, new float2(0f, 1f));
                        float2 float2 = MathUtils.Right(@float);
                        float2 x = this.m_ControlPoint.m_HitPosition.xz - transform.m_Position.xz;
                        int2 @int = default(int2);
                        @int.x = ZoneUtils.GetCellWidth(objectGeometryData.m_Size.x);
                        @int.y = ZoneUtils.GetCellWidth(objectGeometryData.m_Size.z);
                        float2 float3 = (float2)@int * 8f;
                        float2 offset = math.select(0f, 4f, (@int & 1) != 0);
                        float2 float4 = new float2(math.dot(x, float2), math.dot(x, @float));
                        float2 float5 = MathUtils.Snap(float4, 8f, offset);
                        bool2 @bool = math.abs(float4 - float5) < this.m_MaxDistance;
                        if (!math.any(@bool))
                        {
                            return;
                        }
                        float5 = math.select(float4, float5, @bool);
                        float2 float6 = transform.m_Position.xz + float2 * float5.x + @float * float5.y;
                        if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Circular) != 0)
                        {
                            if (math.distance(float6, transform.m_Position.xz) > float3.x * 0.5f + 4f)
                            {
                                return;
                            }
                        }
                        else if (math.any(math.abs(float5) > float3 * 0.5f + 4f))
                        {
                            return;
                        }
                        ControlPoint controlPoint = this.m_ControlPoint;
                        controlPoint.m_OriginalEntity = Entity.Null;
                        controlPoint.m_Direction = float2;
                        controlPoint.m_Position.xz = float6;
                        controlPoint.m_SnapPriority = ToolUtils.CalculateSnapPriority(0f, 1f, this.m_ControlPoint.m_HitPosition.xz, controlPoint.m_Position.xz, controlPoint.m_Direction);
                        Line3 line = new Line3(controlPoint.m_Position, controlPoint.m_Position);
                        Line3 line2 = new Line3(controlPoint.m_Position, controlPoint.m_Position);
                        line.a.xz -= controlPoint.m_Direction * 8f;
                        line.b.xz += controlPoint.m_Direction * 8f;
                        line2.a.xz -= MathUtils.Right(controlPoint.m_Direction) * 8f;
                        line2.b.xz += MathUtils.Right(controlPoint.m_Direction) * 8f;
                        ToolUtils.AddSnapPosition(ref this.m_BestSnapPosition, controlPoint);
                        if (@bool.y)
                        {
                            ToolUtils.AddSnapLine(ref this.m_BestSnapPosition, this.m_SnapLines, new SnapLine(controlPoint, NetUtils.StraightCurve(line.a, line.b), SnapLineFlags.Hidden));
                        }
                        controlPoint.m_Direction = MathUtils.Right(controlPoint.m_Direction);
                        if (@bool.x)
                        {
                            ToolUtils.AddSnapLine(ref this.m_BestSnapPosition, this.m_SnapLines, new SnapLine(controlPoint, NetUtils.StraightCurve(line2.a, line2.b), SnapLineFlags.Hidden));
                        }
                    }
                    else if ((this.m_Snap & Snap.ObjectSide) != 0 && (objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Circular) == 0)
                    {
                        if (this.m_BuildingData.HasComponent(prefabRef.m_Prefab))
                        {
                            float2 float7 = this.m_BuildingData[prefabRef.m_Prefab].m_LotSize;
                            objectGeometryData.m_Bounds.min.xz = float7 * -4f;
                            objectGeometryData.m_Bounds.max.xz = float7 * 4f;
                        }
                        Quad3 quad = ObjectUtils.CalculateBaseCorners(transform.m_Position, transform.m_Rotation, objectGeometryData.m_Bounds);
                        this.CheckLine(quad.ab);
                        this.CheckLine(quad.bc);
                        this.CheckLine(quad.cd);
                        this.CheckLine(quad.da);
                    }
                }

                private void CheckLine(Line3 line)
                {
                    if (MathUtils.Distance(line.xz, this.m_ControlPoint.m_HitPosition.xz, out var t) < this.m_MaxDistance)
                    {
                        ControlPoint controlPoint = this.m_ControlPoint;
                        controlPoint.m_OriginalEntity = Entity.Null;
                        controlPoint.m_Direction = math.normalizesafe(MathUtils.Tangent(line.xz));
                        controlPoint.m_Position = MathUtils.Position(line, t);
                        controlPoint.m_SnapPriority = ToolUtils.CalculateSnapPriority(1f, 1f, this.m_ControlPoint.m_HitPosition.xz, controlPoint.m_Position.xz, controlPoint.m_Direction);
                        ToolUtils.AddSnapPosition(ref this.m_BestSnapPosition, controlPoint);
                        ToolUtils.AddSnapLine(ref this.m_BestSnapPosition, this.m_SnapLines, new SnapLine(controlPoint, NetUtils.StraightCurve(line.a, line.b), (SnapLineFlags)0));
                    }
                }
            }

            [ReadOnly]
            public bool m_AllowCreateArea;

            [ReadOnly]
            public bool m_ControlPointsMoved;

            [ReadOnly]
            public bool m_EditorMode;

            [ReadOnly]
            public Snap m_Snap;

            [ReadOnly]
            public State m_State;

            [ReadOnly]
            public Entity m_Prefab;

            [NativeDisableContainerSafetyRestriction]
            [ReadOnly]
            public NativeArray<Entity> m_ApplyTempAreas;

            [ReadOnly]
            public NativeList<ControlPoint> m_MoveStartPositions;

            [ReadOnly]
            public ComponentLookup<PrefabRef> m_PrefabRefData;

            [ReadOnly]
            public ComponentLookup<AreaGeometryData> m_PrefabAreaData;

            [ReadOnly]
            public ComponentLookup<Temp> m_TempData;

            [ReadOnly]
            public ComponentLookup<Owner> m_OwnerData;

            [ReadOnly]
            public ComponentLookup<Building> m_BuildingData;

            [ReadOnly]
            public ComponentLookup<Curve> m_CurveData;

            [ReadOnly]
            public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

            [ReadOnly]
            public ComponentLookup<StartNodeGeometry> m_StartGeometryData;

            [ReadOnly]
            public ComponentLookup<EndNodeGeometry> m_EndGeometryData;

            [ReadOnly]
            public ComponentLookup<Composition> m_CompositionData;

            [ReadOnly]
            public ComponentLookup<Transform> m_TransformData;

            [ReadOnly]
            public ComponentLookup<BuildingData> m_PrefabBuildingData;

            [ReadOnly]
            public ComponentLookup<BuildingExtensionData> m_BuildingExtensionData;

            [ReadOnly]
            public ComponentLookup<AssetStampData> m_AssetStampData;

            [ReadOnly]
            public ComponentLookup<ObjectGeometryData> m_ObjectGeometryData;

            [ReadOnly]
            public ComponentLookup<NetCompositionData> m_PrefabCompositionData;

            [ReadOnly]
            public ComponentLookup<Game.Areas.Lot> m_LotData;

            [ReadOnly]
            public BufferLookup<Game.Areas.Node> m_Nodes;

            [ReadOnly]
            public BufferLookup<Triangle> m_Triangles;

            [ReadOnly]
            public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

            [ReadOnly]
            public NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> m_AreaSearchTree;

            [ReadOnly]
            public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

            [ReadOnly]
            public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_ObjectSearchTree;

            public NativeList<ControlPoint> m_ControlPoints;

            public void Execute()
            {
                AreaGeometryData areaGeometryData = this.m_PrefabAreaData[this.m_Prefab];
                int index = math.select(0, this.m_ControlPoints.Length - 1, this.m_State == State.Create);
                ControlPoint controlPoint = this.m_ControlPoints[index];
                controlPoint.m_Position = controlPoint.m_HitPosition;
                ControlPoint bestSnapPosition = controlPoint;
                switch (this.m_State)
                {
                    case State.Default:
                        if (this.FindControlPoint(ref bestSnapPosition, controlPoint, areaGeometryData.m_Type, areaGeometryData.m_SnapDistance, controlPoint.m_OriginalEntity, ignoreStartPositions: false, 0))
                        {
                            this.FixControlPointPosition(ref bestSnapPosition);
                        }
                        else if (!this.m_AllowCreateArea)
                        {
                            bestSnapPosition = default(ControlPoint);
                        }
                        else if (this.m_EditorMode)
                        {
                            this.FindParent(ref bestSnapPosition, controlPoint, areaGeometryData.m_Type, areaGeometryData.m_SnapDistance);
                        }
                        else
                        {
                            bestSnapPosition.m_ElementIndex = -1;
                        }
                        break;
                    case State.Create:
                        this.FindControlPoint(ref bestSnapPosition, controlPoint, areaGeometryData.m_Type, areaGeometryData.m_SnapDistance, Entity.Null, ignoreStartPositions: false, this.m_ControlPoints.Length - 3);
                        if (this.m_ControlPoints.Length >= 4)
                        {
                            ControlPoint controlPoint3 = this.m_ControlPoints[0];
                            if (math.distance(controlPoint3.m_Position, bestSnapPosition.m_Position) < areaGeometryData.m_SnapDistance * 0.5f)
                            {
                                bestSnapPosition.m_Position = controlPoint3.m_Position;
                            }
                        }
                        if (this.m_EditorMode)
                        {
                            this.FindParent(ref bestSnapPosition, controlPoint, areaGeometryData.m_Type, areaGeometryData.m_SnapDistance);
                        }
                        else
                        {
                            bestSnapPosition.m_ElementIndex = -1;
                        }
                        break;
                    case State.Modify:
                        if (this.m_ControlPointsMoved)
                        {
                            this.FindControlPoint(ref bestSnapPosition, controlPoint, areaGeometryData.m_Type, areaGeometryData.m_SnapDistance, Entity.Null, ignoreStartPositions: true, 0);
                            float num = areaGeometryData.m_SnapDistance * 0.5f;
                            for (int i = 0; i < this.m_MoveStartPositions.Length; i++)
                            {
                                ControlPoint controlPoint2 = this.m_MoveStartPositions[i];
                                if (this.m_Nodes.HasBuffer(controlPoint2.m_OriginalEntity) && controlPoint2.m_ElementIndex.x >= 0)
                                {
                                    DynamicBuffer<Game.Areas.Node> dynamicBuffer = this.m_Nodes[controlPoint2.m_OriginalEntity];
                                    int index2 = math.select(controlPoint2.m_ElementIndex.x - 1, dynamicBuffer.Length - 1, controlPoint2.m_ElementIndex.x == 0);
                                    int index3 = math.select(controlPoint2.m_ElementIndex.x + 1, 0, controlPoint2.m_ElementIndex.x == dynamicBuffer.Length - 1);
                                    float3 position = dynamicBuffer[index2].m_Position;
                                    float3 position2 = dynamicBuffer[index3].m_Position;
                                    float num2 = math.distance(bestSnapPosition.m_Position, position);
                                    float num3 = math.distance(bestSnapPosition.m_Position, position2);
                                    if (num2 < num)
                                    {
                                        bestSnapPosition.m_Position = position;
                                        num = num2;
                                    }
                                    if (num3 < num)
                                    {
                                        bestSnapPosition.m_Position = position2;
                                        num = num3;
                                    }
                                }
                            }
                            if (!this.m_EditorMode || !this.m_TransformData.HasComponent(controlPoint.m_OriginalEntity))
                            {
                                bestSnapPosition.m_ElementIndex = -1;
                            }
                        }
                        else
                        {
                            this.FindControlPoint(ref bestSnapPosition, controlPoint, areaGeometryData.m_Type, areaGeometryData.m_SnapDistance, controlPoint.m_OriginalEntity, ignoreStartPositions: false, 0);
                            this.FixControlPointPosition(ref bestSnapPosition);
                        }
                        break;
                    case State.Remove:
                        bestSnapPosition = this.m_MoveStartPositions[0];
                        break;
                }
                if (this.m_State == State.Default)
                {
                    this.m_ControlPoints.Clear();
                    this.m_ControlPoints.Add(in bestSnapPosition);
                    if (this.m_Nodes.HasBuffer(bestSnapPosition.m_OriginalEntity) && math.any(bestSnapPosition.m_ElementIndex >= 0))
                    {
                        this.AddControlPoints(bestSnapPosition, controlPoint, areaGeometryData.m_Type, areaGeometryData.m_SnapDistance * 0.5f);
                    }
                }
                else
                {
                    this.m_ControlPoints[index] = bestSnapPosition;
                }
            }

            private void FindParent(ref ControlPoint bestSnapPosition, ControlPoint controlPoint, Game.Areas.AreaType type, float snapDistance)
            {
                if ((this.m_Snap & Snap.AutoParent) != 0)
                {
                    ParentObjectIterator parentObjectIterator = default(ParentObjectIterator);
                    parentObjectIterator.m_BoundsOffset = snapDistance * 0.125f + 0.4f;
                    parentObjectIterator.m_MaxDistance = snapDistance * 0.125f;
                    parentObjectIterator.m_TransformData = this.m_TransformData;
                    parentObjectIterator.m_PrefabRefData = this.m_PrefabRefData;
                    parentObjectIterator.m_BuildingData = this.m_PrefabBuildingData;
                    parentObjectIterator.m_ObjectGeometryData = this.m_ObjectGeometryData;
                    ParentObjectIterator iterator = parentObjectIterator;
                    Entity entity = controlPoint.m_OriginalEntity;
                    if (this.m_EditorMode)
                    {
                        Owner componentData;
                        while (this.m_OwnerData.TryGetComponent(entity, out componentData) && !this.m_BuildingData.HasComponent(entity))
                        {
                            entity = componentData.m_Owner;
                        }
                        if (this.m_InstalledUpgrades.TryGetBuffer(entity, out var bufferData) && bufferData.Length != 0)
                        {
                            entity = bufferData[0].m_Upgrade;
                        }
                    }
                    int num = math.max(1, this.m_ControlPoints.Length - 1);
                    for (int i = 0; i < num; i++)
                    {
                        if (i == this.m_ControlPoints.Length - 1)
                        {
                            iterator.m_Line.a = bestSnapPosition.m_Position;
                        }
                        else
                        {
                            iterator.m_Line.a = this.m_ControlPoints[i].m_Position;
                        }
                        if (i + 1 >= this.m_ControlPoints.Length - 1)
                        {
                            iterator.m_Line.b = bestSnapPosition.m_Position;
                        }
                        else
                        {
                            iterator.m_Line.b = this.m_ControlPoints[i + 1].m_Position;
                        }
                        this.m_ObjectSearchTree.Iterate(ref iterator);
                        if (!(iterator.m_Parent != Entity.Null))
                        {
                            continue;
                        }
                        Entity entity2 = iterator.m_Parent;
                        if (this.m_EditorMode)
                        {
                            Owner componentData2;
                            while (this.m_OwnerData.TryGetComponent(entity2, out componentData2) && !this.m_BuildingData.HasComponent(entity2))
                            {
                                entity2 = componentData2.m_Owner;
                            }
                            if (this.m_InstalledUpgrades.TryGetBuffer(entity2, out var bufferData2) && bufferData2.Length != 0)
                            {
                                entity2 = bufferData2[0].m_Upgrade;
                            }
                        }
                        if (entity2 != entity)
                        {
                            bestSnapPosition.m_ElementIndex = -1;
                        }
                        bestSnapPosition.m_OriginalEntity = iterator.m_Parent;
                        return;
                    }
                }
                bestSnapPosition.m_OriginalEntity = Entity.Null;
                bestSnapPosition.m_ElementIndex = -1;
            }

            private void FixControlPointPosition(ref ControlPoint bestSnapPosition)
            {
                if (!this.m_Nodes.HasBuffer(bestSnapPosition.m_OriginalEntity) || bestSnapPosition.m_ElementIndex.x < 0)
                {
                    return;
                }
                Entity entity = bestSnapPosition.m_OriginalEntity;
                if (this.m_ApplyTempAreas.IsCreated)
                {
                    for (int i = 0; i < this.m_ApplyTempAreas.Length; i++)
                    {
                        Entity entity2 = this.m_ApplyTempAreas[i];
                        if (this.m_TempData[entity2].m_Original == entity)
                        {
                            entity = entity2;
                            break;
                        }
                    }
                }
                bestSnapPosition.m_Position = this.m_Nodes[entity][bestSnapPosition.m_ElementIndex.x].m_Position;
            }

            private void AddControlPoints(ControlPoint bestSnapPosition, ControlPoint controlPoint, Game.Areas.AreaType type, float snapDistance)
            {
                AreaIterator2 areaIterator = default(AreaIterator2);
                areaIterator.m_EditorMode = this.m_EditorMode;
                areaIterator.m_AreaType = type;
                areaIterator.m_Bounds = new Bounds3(controlPoint.m_HitPosition - snapDistance, controlPoint.m_HitPosition + snapDistance);
                areaIterator.m_MaxDistance1 = snapDistance * 0.1f;
                areaIterator.m_MaxDistance2 = snapDistance;
                areaIterator.m_ControlPoint1 = bestSnapPosition;
                areaIterator.m_ControlPoint2 = controlPoint;
                areaIterator.m_ControlPoints = this.m_ControlPoints;
                areaIterator.m_PrefabRefData = this.m_PrefabRefData;
                areaIterator.m_PrefabAreaData = this.m_PrefabAreaData;
                areaIterator.m_LotData = this.m_LotData;
                areaIterator.m_OwnerData = this.m_OwnerData;
                areaIterator.m_Nodes = this.m_Nodes;
                areaIterator.m_Triangles = this.m_Triangles;
                areaIterator.m_InstalledUpgrades = this.m_InstalledUpgrades;
                AreaIterator2 iterator = areaIterator;
                if (this.m_ApplyTempAreas.IsCreated && this.m_ApplyTempAreas.Length != 0)
                {
                    iterator.m_IgnoreAreas = new NativeParallelHashSet<Entity>(this.m_ApplyTempAreas.Length, Allocator.Temp);
                    for (int i = 0; i < this.m_ApplyTempAreas.Length; i++)
                    {
                        Entity entity = this.m_ApplyTempAreas[i];
                        Temp temp = this.m_TempData[entity];
                        iterator.m_IgnoreAreas.Add(temp.m_Original);
                        if ((!this.m_OwnerData.TryGetComponent(entity, out var componentData) || !this.m_Nodes.HasBuffer(componentData.m_Owner)) && (temp.m_Flags & TempFlags.Delete) == 0)
                        {
                            Entity area = (((temp.m_Flags & TempFlags.Create) != 0) ? entity : temp.m_Original);
                            DynamicBuffer<Game.Areas.Node> dynamicBuffer = this.m_Nodes[entity];
                            for (int j = 0; j < dynamicBuffer.Length; j++)
                            {
                                int2 nodeIndex = new int2(j, math.select(j + 1, 0, j == dynamicBuffer.Length - 1));
                                Line3.Segment line = new Line3.Segment(dynamicBuffer[nodeIndex.x].m_Position, dynamicBuffer[nodeIndex.y].m_Position);
                                iterator.CheckLine(line, snapDistance, area, nodeIndex, this.m_LotData.HasComponent(entity));
                            }
                        }
                    }
                }
                this.m_AreaSearchTree.Iterate(ref iterator);
                if (iterator.m_IgnoreAreas.IsCreated)
                {
                    iterator.m_IgnoreAreas.Dispose();
                }
            }

            private bool FindControlPoint(ref ControlPoint bestSnapPosition, ControlPoint controlPoint, Game.Areas.AreaType type, float snapDistance, Entity preferredArea, bool ignoreStartPositions, int selfSnap)
            {
                bestSnapPosition.m_OriginalEntity = Entity.Null;
                NativeList<SnapLine> snapLines = new NativeList<SnapLine>(10, Allocator.Temp);
                if ((this.m_Snap & Snap.StraightDirection) != 0)
                {
                    if (this.m_State == State.Create)
                    {
                        ControlPoint controlPoint2 = controlPoint;
                        controlPoint2.m_OriginalEntity = Entity.Null;
                        controlPoint2.m_Position = controlPoint.m_HitPosition;
                        float3 resultDir = default(float3);
                        float bestDirectionDistance = float.MaxValue;
                        if (this.m_ControlPoints.Length >= 2)
                        {
                            ControlPoint controlPoint3 = this.m_ControlPoints[this.m_ControlPoints.Length - 2];
                            if (!controlPoint3.m_Direction.Equals(default(float2)))
                            {
                                ToolUtils.DirectionSnap(ref bestDirectionDistance, ref controlPoint2.m_Position, ref resultDir, controlPoint.m_HitPosition, controlPoint3.m_Position, new float3(controlPoint3.m_Direction.x, 0f, controlPoint3.m_Direction.y), snapDistance);
                            }
                        }
                        if (this.m_ControlPoints.Length >= 3)
                        {
                            ControlPoint controlPoint4 = this.m_ControlPoints[this.m_ControlPoints.Length - 3];
                            ControlPoint controlPoint5 = this.m_ControlPoints[this.m_ControlPoints.Length - 2];
                            float2 @float = math.normalizesafe(controlPoint4.m_Position.xz - controlPoint5.m_Position.xz);
                            if (!@float.Equals(default(float2)))
                            {
                                ToolUtils.DirectionSnap(ref bestDirectionDistance, ref controlPoint2.m_Position, ref resultDir, controlPoint.m_HitPosition, controlPoint5.m_Position, new float3(@float.x, 0f, @float.y), snapDistance);
                            }
                        }
                        if (!resultDir.Equals(default(float3)))
                        {
                            controlPoint2.m_Direction = resultDir.xz;
                            controlPoint2.m_SnapPriority = ToolUtils.CalculateSnapPriority(0f, 1f, controlPoint.m_HitPosition.xz, controlPoint2.m_Position.xz, controlPoint2.m_Direction);
                            ToolUtils.AddSnapPosition(ref bestSnapPosition, controlPoint2);
                            float3 position = controlPoint2.m_Position;
                            float3 endPos = position;
                            endPos.xz += controlPoint2.m_Direction;
                            ToolUtils.AddSnapLine(ref bestSnapPosition, snapLines, new SnapLine(controlPoint2, NetUtils.StraightCurve(position, endPos), SnapLineFlags.Hidden));
                        }
                    }
                    else if (this.m_State == State.Modify)
                    {
                        for (int i = 0; i < this.m_MoveStartPositions.Length; i++)
                        {
                            ControlPoint controlPoint6 = this.m_MoveStartPositions[i];
                            if (!this.m_Nodes.HasBuffer(controlPoint6.m_OriginalEntity) || !math.any(controlPoint6.m_ElementIndex >= 0))
                            {
                                continue;
                            }
                            DynamicBuffer<Game.Areas.Node> dynamicBuffer = this.m_Nodes[controlPoint6.m_OriginalEntity];
                            if (dynamicBuffer.Length < 3)
                            {
                                continue;
                            }
                            int4 @int = math.select(controlPoint6.m_ElementIndex.x + new int4(-2, -1, 1, 2), controlPoint6.m_ElementIndex.y + new int4(-1, 0, 1, 2), controlPoint6.m_ElementIndex.y >= 0);
                            @int = math.select(@int, @int + new int2(dynamicBuffer.Length, -dynamicBuffer.Length).xxyy, new bool4(@int.xy < 0, @int.zw >= dynamicBuffer.Length));
                            float3 position2 = dynamicBuffer[@int.x].m_Position;
                            float3 position3 = dynamicBuffer[@int.y].m_Position;
                            float3 position4 = dynamicBuffer[@int.z].m_Position;
                            float3 position5 = dynamicBuffer[@int.w].m_Position;
                            float2 float2 = math.normalizesafe(position2.xz - position3.xz);
                            float2 float3 = math.normalizesafe(position5.xz - position4.xz);
                            if (!float2.Equals(default(float2)))
                            {
                                ControlPoint controlPoint7 = controlPoint;
                                controlPoint7.m_OriginalEntity = Entity.Null;
                                controlPoint7.m_Position = controlPoint.m_HitPosition;
                                float3 resultDir2 = default(float3);
                                float bestDirectionDistance2 = float.MaxValue;
                                ToolUtils.DirectionSnap(ref bestDirectionDistance2, ref controlPoint7.m_Position, ref resultDir2, controlPoint.m_HitPosition, position3, new float3(float2.x, 0f, float2.y), snapDistance);
                                if (!resultDir2.Equals(default(float3)))
                                {
                                    controlPoint7.m_Direction = resultDir2.xz;
                                    controlPoint7.m_SnapPriority = ToolUtils.CalculateSnapPriority(0f, 1f, controlPoint.m_HitPosition.xz, controlPoint7.m_Position.xz, controlPoint7.m_Direction);
                                    ToolUtils.AddSnapPosition(ref bestSnapPosition, controlPoint7);
                                    float3 position6 = controlPoint7.m_Position;
                                    float3 endPos2 = position6;
                                    endPos2.xz += controlPoint7.m_Direction;
                                    ToolUtils.AddSnapLine(ref bestSnapPosition, snapLines, new SnapLine(controlPoint7, NetUtils.StraightCurve(position6, endPos2), SnapLineFlags.Hidden));
                                }
                            }
                            if (!float3.Equals(default(float2)))
                            {
                                ControlPoint controlPoint8 = controlPoint;
                                controlPoint8.m_OriginalEntity = Entity.Null;
                                controlPoint8.m_Position = controlPoint.m_HitPosition;
                                float3 resultDir3 = default(float3);
                                float bestDirectionDistance3 = float.MaxValue;
                                ToolUtils.DirectionSnap(ref bestDirectionDistance3, ref controlPoint8.m_Position, ref resultDir3, controlPoint.m_HitPosition, position4, new float3(float3.x, 0f, float3.y), snapDistance);
                                if (!resultDir3.Equals(default(float3)))
                                {
                                    controlPoint8.m_Direction = resultDir3.xz;
                                    controlPoint8.m_SnapPriority = ToolUtils.CalculateSnapPriority(0f, 1f, controlPoint.m_HitPosition.xz, controlPoint8.m_Position.xz, controlPoint8.m_Direction);
                                    ToolUtils.AddSnapPosition(ref bestSnapPosition, controlPoint8);
                                    float3 position7 = controlPoint8.m_Position;
                                    float3 endPos3 = position7;
                                    endPos3.xz += controlPoint8.m_Direction;
                                    ToolUtils.AddSnapLine(ref bestSnapPosition, snapLines, new SnapLine(controlPoint8, NetUtils.StraightCurve(position7, endPos3), SnapLineFlags.Hidden));
                                }
                            }
                        }
                    }
                }
                if ((this.m_Snap & Snap.ExistingGeometry) != 0 || preferredArea != Entity.Null || ignoreStartPositions || selfSnap >= 1)
                {
                    float num = math.select(snapDistance, snapDistance * 0.5f, (this.m_Snap & Snap.ExistingGeometry) == 0);
                    AreaIterator areaIterator = default(AreaIterator);
                    areaIterator.m_EditorMode = this.m_EditorMode;
                    areaIterator.m_IgnoreStartPositions = ignoreStartPositions;
                    areaIterator.m_Snap = this.m_Snap;
                    areaIterator.m_AreaType = type;
                    areaIterator.m_Bounds = new Bounds3(controlPoint.m_HitPosition - num, controlPoint.m_HitPosition + num);
                    areaIterator.m_MaxDistance = num;
                    areaIterator.m_PreferArea = preferredArea;
                    areaIterator.m_ControlPoint = controlPoint;
                    areaIterator.m_BestSnapPosition = bestSnapPosition;
                    areaIterator.m_SnapLines = snapLines;
                    areaIterator.m_MoveStartPositions = this.m_MoveStartPositions;
                    areaIterator.m_PrefabRefData = this.m_PrefabRefData;
                    areaIterator.m_PrefabAreaData = this.m_PrefabAreaData;
                    areaIterator.m_LotData = this.m_LotData;
                    areaIterator.m_OwnerData = this.m_OwnerData;
                    areaIterator.m_Nodes = this.m_Nodes;
                    areaIterator.m_Triangles = this.m_Triangles;
                    areaIterator.m_InstalledUpgrades = this.m_InstalledUpgrades;
                    AreaIterator iterator = areaIterator;
                    if (this.m_ApplyTempAreas.IsCreated && this.m_ApplyTempAreas.Length != 0)
                    {
                        iterator.m_IgnoreAreas = new NativeParallelHashSet<Entity>(this.m_ApplyTempAreas.Length, Allocator.Temp);
                        for (int j = 0; j < this.m_ApplyTempAreas.Length; j++)
                        {
                            Entity entity = this.m_ApplyTempAreas[j];
                            Temp temp = this.m_TempData[entity];
                            iterator.m_IgnoreAreas.Add(temp.m_Original);
                            if ((this.m_OwnerData.TryGetComponent(entity, out var componentData) && this.m_Nodes.HasBuffer(componentData.m_Owner)) || (temp.m_Flags & TempFlags.Delete) != 0)
                            {
                                continue;
                            }
                            Entity entity2 = (((temp.m_Flags & TempFlags.Create) != 0) ? entity : temp.m_Original);
                            if ((this.m_Snap & Snap.ExistingGeometry) != 0 || entity2 == preferredArea)
                            {
                                DynamicBuffer<Game.Areas.Node> dynamicBuffer2 = this.m_Nodes[entity];
                                for (int k = 0; k < dynamicBuffer2.Length; k++)
                                {
                                    int2 nodeIndex = new int2(k, math.select(k + 1, 0, k == dynamicBuffer2.Length - 1));
                                    Line3.Segment line = new Line3.Segment(dynamicBuffer2[nodeIndex.x].m_Position, dynamicBuffer2[nodeIndex.y].m_Position);
                                    iterator.CheckLine(line, num, entity2, nodeIndex, !this.m_EditorMode && this.m_LotData.HasComponent(entity));
                                }
                            }
                        }
                    }
                    if ((this.m_Snap & Snap.ExistingGeometry) != 0 || preferredArea != Entity.Null || ignoreStartPositions)
                    {
                        this.m_AreaSearchTree.Iterate(ref iterator);
                    }
                    for (int l = 0; l < selfSnap; l++)
                    {
                        Line3.Segment line2 = new Line3.Segment(this.m_ControlPoints[l].m_Position, this.m_ControlPoints[l + 1].m_Position);
                        iterator.CheckLine(line2, num, Entity.Null, new int2(l, l + 1), lockFirstEdge: false);
                    }
                    bestSnapPosition = iterator.m_BestSnapPosition;
                    if (iterator.m_IgnoreAreas.IsCreated)
                    {
                        iterator.m_IgnoreAreas.Dispose();
                    }
                }
                if ((this.m_Snap & (Snap.NetSide | Snap.NetMiddle)) != 0 && (this.m_State != 0 || this.m_AllowCreateArea))
                {
                    NetIterator netIterator = default(NetIterator);
                    netIterator.m_Snap = this.m_Snap;
                    netIterator.m_Bounds = new Bounds3(controlPoint.m_HitPosition - snapDistance, controlPoint.m_HitPosition + snapDistance);
                    netIterator.m_MaxDistance = snapDistance;
                    netIterator.m_ControlPoint = controlPoint;
                    netIterator.m_BestSnapPosition = bestSnapPosition;
                    netIterator.m_SnapLines = snapLines;
                    netIterator.m_CurveData = this.m_CurveData;
                    netIterator.m_EdgeGeometryData = this.m_EdgeGeometryData;
                    netIterator.m_StartGeometryData = this.m_StartGeometryData;
                    netIterator.m_EndGeometryData = this.m_EndGeometryData;
                    netIterator.m_CompositionData = this.m_CompositionData;
                    netIterator.m_PrefabCompositionData = this.m_PrefabCompositionData;
                    NetIterator iterator2 = netIterator;
                    this.m_NetSearchTree.Iterate(ref iterator2);
                    bestSnapPosition = iterator2.m_BestSnapPosition;
                }
                if ((this.m_Snap & (Snap.ObjectSide | Snap.LotGrid)) != 0 && (this.m_State != 0 || this.m_AllowCreateArea))
                {
                    ObjectIterator objectIterator = default(ObjectIterator);
                    objectIterator.m_Bounds = new Bounds3(controlPoint.m_HitPosition - snapDistance, controlPoint.m_HitPosition + snapDistance);
                    objectIterator.m_MaxDistance = snapDistance;
                    objectIterator.m_Snap = this.m_Snap;
                    objectIterator.m_ControlPoint = controlPoint;
                    objectIterator.m_BestSnapPosition = bestSnapPosition;
                    objectIterator.m_SnapLines = snapLines;
                    objectIterator.m_TransformData = this.m_TransformData;
                    objectIterator.m_PrefabRefData = this.m_PrefabRefData;
                    objectIterator.m_BuildingData = this.m_PrefabBuildingData;
                    objectIterator.m_BuildingExtensionData = this.m_BuildingExtensionData;
                    objectIterator.m_AssetStampData = this.m_AssetStampData;
                    objectIterator.m_ObjectGeometryData = this.m_ObjectGeometryData;
                    ObjectIterator iterator3 = objectIterator;
                    this.m_ObjectSearchTree.Iterate(ref iterator3);
                    bestSnapPosition = iterator3.m_BestSnapPosition;
                }
                snapLines.Dispose();
                return this.m_Nodes.HasBuffer(bestSnapPosition.m_OriginalEntity);
            }
        }

        [BurstCompile]
        private struct RemoveMapTilesJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle m_EntityType;

            [ReadOnly]
            public BufferTypeHandle<Game.Areas.Node> m_NodeType;

            [ReadOnly]
            public BufferTypeHandle<LocalNodeCache> m_CacheType;

            [ReadOnly]
            public NativeList<ControlPoint> m_ControlPoints;

            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                if (this.m_ControlPoints.Length == 1 && this.m_ControlPoints[0].Equals(default(ControlPoint)))
                {
                    return;
                }
                NativeArray<Entity> nativeArray = chunk.GetNativeArray(this.m_EntityType);
                BufferAccessor<Game.Areas.Node> bufferAccessor = chunk.GetBufferAccessor(ref this.m_NodeType);
                BufferAccessor<LocalNodeCache> bufferAccessor2 = chunk.GetBufferAccessor(ref this.m_CacheType);
                for (int i = 0; i < nativeArray.Length; i++)
                {
                    Entity original = nativeArray[i];
                    DynamicBuffer<Game.Areas.Node> dynamicBuffer = bufferAccessor[i];
                    Entity e = this.m_CommandBuffer.CreateEntity(unfilteredChunkIndex);
                    CreationDefinition component = default(CreationDefinition);
                    component.m_Original = original;
                    component.m_Flags |= CreationFlags.Delete;
                    this.m_CommandBuffer.AddComponent(unfilteredChunkIndex, e, component);
                    this.m_CommandBuffer.AddComponent(unfilteredChunkIndex, e, default(Updated));
                    this.m_CommandBuffer.AddBuffer<Game.Areas.Node>(unfilteredChunkIndex, e).CopyFrom(dynamicBuffer.AsNativeArray());
                    if (bufferAccessor2.Length != 0)
                    {
                        DynamicBuffer<LocalNodeCache> dynamicBuffer2 = bufferAccessor2[i];
                        this.m_CommandBuffer.AddBuffer<LocalNodeCache>(unfilteredChunkIndex, e).CopyFrom(dynamicBuffer2.AsNativeArray());
                    }
                }
            }

            void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                this.Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
            }
        }

        [BurstCompile]
        private struct CreateDefinitionsJob : IJob
        {
            [ReadOnly]
            public bool m_AllowCreateArea;

            [ReadOnly]
            public bool m_EditorMode;

            [ReadOnly]
            public Mode m_Mode;

            [ReadOnly]
            public State m_State;

            [ReadOnly]
            public Entity m_Prefab;

            [ReadOnly]
            public Entity m_Recreate;

            [NativeDisableContainerSafetyRestriction]
            [ReadOnly]
            public NativeArray<Entity> m_ApplyTempAreas;

            [NativeDisableContainerSafetyRestriction]
            [ReadOnly]
            public NativeArray<Entity> m_ApplyTempBuildings;

            [ReadOnly]
            public NativeList<ControlPoint> m_MoveStartPositions;

            [ReadOnly]
            public ComponentLookup<Temp> m_TempData;

            [ReadOnly]
            public ComponentLookup<Owner> m_OwnerData;

            [ReadOnly]
            public ComponentLookup<Clear> m_ClearData;

            [ReadOnly]
            public ComponentLookup<Space> m_SpaceData;

            [ReadOnly]
            public ComponentLookup<Area> m_AreaData;

            [ReadOnly]
            public ComponentLookup<Game.Net.Node> m_NodeData;

            [ReadOnly]
            public ComponentLookup<Edge> m_EdgeData;

            [ReadOnly]
            public ComponentLookup<Curve> m_CurveData;

            [ReadOnly]
            public ComponentLookup<Game.Net.Elevation> m_NetElevationData;

            [ReadOnly]
            public ComponentLookup<Game.Tools.EditorContainer> m_EditorContainerData;

            [ReadOnly]
            public ComponentLookup<LocalTransformCache> m_LocalTransformCacheData;

            [ReadOnly]
            public ComponentLookup<Transform> m_TransformData;

            [ReadOnly]
            public ComponentLookup<Building> m_BuildingData;

            [ReadOnly]
            public ComponentLookup<PrefabRef> m_PrefabRefData;

            [ReadOnly]
            public ComponentLookup<AreaGeometryData> m_PrefabAreaData;

            [ReadOnly]
            public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

            [ReadOnly]
            public BufferLookup<Game.Areas.Node> m_Nodes;

            [ReadOnly]
            public BufferLookup<Triangle> m_Triangles;

            [ReadOnly]
            public BufferLookup<Game.Areas.SubArea> m_SubAreas;

            [ReadOnly]
            public BufferLookup<LocalNodeCache> m_CachedNodes;

            [ReadOnly]
            public BufferLookup<Game.Net.SubNet> m_SubNets;

            [ReadOnly]
            public BufferLookup<ConnectedEdge> m_ConnectedEdges;

            [ReadOnly]
            public BufferLookup<Game.Objects.SubObject> m_SubObjects;

            [ReadOnly]
            public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

            [ReadOnly]
            public NativeList<ControlPoint> m_ControlPoints;

            public NativeValue<Tooltip> m_Tooltip;

            public EntityCommandBuffer m_CommandBuffer;

            public void Execute()
            {
                if (this.m_ControlPoints.Length != 1 || !this.m_ControlPoints[0].Equals(default(ControlPoint)))
                {
                    switch (this.m_Mode)
                    {
                        case Mode.Edit:
                            this.Edit();
                            break;
                        case Mode.Generate:
                            this.Generate();
                            break;
                    }
                }
            }

            private void Generate()
            {
                int2 @int = default(int2);
                @int.y = 0;
                Bounds2 bounds = default(Bounds2);
                while (@int.y < 23)//vanilla=23;mod=92;
                {
                    @int.x = 0;
                    while (@int.x < 23)//vanilla=23;mod=92;
                    {
                        Entity e = this.m_CommandBuffer.CreateEntity();
                        CreationDefinition component = default(CreationDefinition);
                        component.m_Prefab = this.m_Prefab;
                        //float2 @float = new float2(92f, 92f) * 311.652161f;//mod;
                        //bounds.min = (float2)@int * 623.3043f - @float;
                        //bounds.max = (float2)(@int + 1) * 623.3043f - @float;
                        float2 @float = new float2(23f, 23f) * 1246.608644f;
                        bounds.min = (float2)@int * 2493.217288f - @float;
                        bounds.max = (float2)(@int + 1) * 2493.217288f - @float;
                        DynamicBuffer<Game.Areas.Node> dynamicBuffer = this.m_CommandBuffer.AddBuffer<Game.Areas.Node>(e);
                        dynamicBuffer.ResizeUninitialized(5);
                        dynamicBuffer[0] = new Game.Areas.Node(new float3(bounds.min.x, 0f, bounds.min.y), float.MinValue);
                        dynamicBuffer[1] = new Game.Areas.Node(new float3(bounds.min.x, 0f, bounds.max.y), float.MinValue);
                        dynamicBuffer[2] = new Game.Areas.Node(new float3(bounds.max.x, 0f, bounds.max.y), float.MinValue);
                        dynamicBuffer[3] = new Game.Areas.Node(new float3(bounds.max.x, 0f, bounds.min.y), float.MinValue);
                        dynamicBuffer[4] = dynamicBuffer[0];
                        this.m_CommandBuffer.AddComponent(e, component);
                        this.m_CommandBuffer.AddComponent(e, default(Updated));
                        @int.x++;
                    }
                    @int.y++;
                }
                this.m_Tooltip.value = Tooltip.GenerateAreas;
            }

            private void GetControlPoints(int index, out ControlPoint firstPoint, out ControlPoint lastPoint)
            {
                switch (this.m_State)
                {
                    case State.Default:
                        firstPoint = this.m_ControlPoints[index];
                        lastPoint = this.m_ControlPoints[index];
                        break;
                    case State.Create:
                        firstPoint = default(ControlPoint);
                        lastPoint = this.m_ControlPoints[this.m_ControlPoints.Length - 1];
                        break;
                    case State.Modify:
                        firstPoint = this.m_MoveStartPositions[index];
                        lastPoint = this.m_ControlPoints[0];
                        break;
                    case State.Remove:
                        firstPoint = this.m_MoveStartPositions[index];
                        lastPoint = this.m_ControlPoints[0];
                        break;
                    default:
                        firstPoint = default(ControlPoint);
                        lastPoint = default(ControlPoint);
                        break;
                }
            }

            private void Edit()
            {
                AreaGeometryData areaData = this.m_PrefabAreaData[this.m_Prefab];
                int num = this.m_State switch
                {
                    State.Default => this.m_ControlPoints.Length,
                    State.Create => 1,
                    State.Modify => this.m_MoveStartPositions.Length,
                    State.Remove => this.m_MoveStartPositions.Length,
                    _ => 0,
                };
                this.m_Tooltip.value = Tooltip.None;
                bool flag = false;
                NativeParallelHashSet<Entity> createdEntities = new NativeParallelHashSet<Entity>(num * 2, Allocator.Temp);
                for (int i = 0; i < num; i++)
                {
                    this.GetControlPoints(i, out var firstPoint, out var _);
                    if (this.m_Nodes.HasBuffer(firstPoint.m_OriginalEntity) && math.any(firstPoint.m_ElementIndex >= 0))
                    {
                        createdEntities.Add(firstPoint.m_OriginalEntity);
                    }
                }
                NativeList<ClearAreaData> clearAreas = default(NativeList<ClearAreaData>);
                for (int j = 0; j < num; j++)
                {
                    this.GetControlPoints(j, out var firstPoint2, out var lastPoint2);
                    if (j == 0 && this.m_State == State.Modify)
                    {
                        flag = !firstPoint2.Equals(lastPoint2);
                    }
                    Entity e = this.m_CommandBuffer.CreateEntity();
                    CreationDefinition component = default(CreationDefinition);
                    component.m_Prefab = this.m_Prefab;
                    if (this.m_Nodes.HasBuffer(firstPoint2.m_OriginalEntity) && math.any(firstPoint2.m_ElementIndex >= 0))
                    {
                        component.m_Original = firstPoint2.m_OriginalEntity;
                    }
                    else if (this.m_Recreate != Entity.Null)
                    {
                        component.m_Original = this.m_Recreate;
                    }
                    float minNodeDistance = AreaUtils.GetMinNodeDistance(areaData);
                    int2 @int = default(int2);
                    DynamicBuffer<Game.Areas.Node> nodes = this.m_CommandBuffer.AddBuffer<Game.Areas.Node>(e);
                    DynamicBuffer<LocalNodeCache> dynamicBuffer = default(DynamicBuffer<LocalNodeCache>);
                    bool isComplete = false;
                    if (this.m_Nodes.HasBuffer(firstPoint2.m_OriginalEntity) && math.any(firstPoint2.m_ElementIndex >= 0))
                    {
                        component.m_Flags |= CreationFlags.Relocate;
                        isComplete = true;
                        Entity sourceArea = this.GetSourceArea(firstPoint2.m_OriginalEntity);
                        DynamicBuffer<Game.Areas.Node> dynamicBuffer2 = this.m_Nodes[sourceArea];
                        DynamicBuffer<LocalNodeCache> dynamicBuffer3 = default(DynamicBuffer<LocalNodeCache>);
                        if (this.m_CachedNodes.HasBuffer(sourceArea))
                        {
                            dynamicBuffer3 = this.m_CachedNodes[sourceArea];
                        }
                        float elevation = float.MinValue;
                        int parentMesh = -1;
                        if (lastPoint2.m_ElementIndex.x >= 0)
                        {
                            parentMesh = lastPoint2.m_ElementIndex.x;
                            if (this.m_OwnerData.TryGetComponent(firstPoint2.m_OriginalEntity, out var componentData))
                            {
                                Entity owner = componentData.m_Owner;
                                while (this.m_OwnerData.HasComponent(owner) && !this.m_BuildingData.HasComponent(owner))
                                {
                                    if (this.m_LocalTransformCacheData.HasComponent(owner))
                                    {
                                        parentMesh = this.m_LocalTransformCacheData[owner].m_ParentMesh;
                                    }
                                    owner = this.m_OwnerData[owner].m_Owner;
                                }
                                if (this.m_TransformData.TryGetComponent(owner, out var componentData2))
                                {
                                    elevation = lastPoint2.m_Position.y - componentData2.m_Position.y;
                                }
                            }
                        }
                        if (firstPoint2.m_ElementIndex.y >= 0)
                        {
                            int y = firstPoint2.m_ElementIndex.y;
                            int index = math.select(firstPoint2.m_ElementIndex.y + 1, 0, firstPoint2.m_ElementIndex.y == dynamicBuffer2.Length - 1);
                            float2 @float = new float2(math.distance(lastPoint2.m_Position, dynamicBuffer2[y].m_Position), math.distance(lastPoint2.m_Position, dynamicBuffer2[index].m_Position));
                            bool flag2 = flag && math.any(@float < minNodeDistance);
                            int num2 = math.select(1, 0, flag2 || !flag);
                            int length = dynamicBuffer2.Length + num2;
                            nodes.ResizeUninitialized(length);
                            int num3 = 0;
                            if (dynamicBuffer3.IsCreated)
                            {
                                dynamicBuffer = this.m_CommandBuffer.AddBuffer<LocalNodeCache>(e);
                                dynamicBuffer.ResizeUninitialized(length);
                                for (int k = 0; k <= firstPoint2.m_ElementIndex.y; k++)
                                {
                                    nodes[num3] = dynamicBuffer2[k];
                                    dynamicBuffer[num3] = dynamicBuffer3[k];
                                    num3++;
                                }
                                @int.x = num3;
                                for (int l = 0; l < num2; l++)
                                {
                                    nodes[num3] = new Game.Areas.Node(lastPoint2.m_Position, elevation);
                                    dynamicBuffer[num3] = new LocalNodeCache
                                    {
                                        m_Position = lastPoint2.m_Position,
                                        m_ParentMesh = parentMesh
                                    };
                                    num3++;
                                }
                                @int.y = num3;
                                for (int m = firstPoint2.m_ElementIndex.y + 1; m < dynamicBuffer2.Length; m++)
                                {
                                    nodes[num3] = dynamicBuffer2[m];
                                    dynamicBuffer[num3] = dynamicBuffer3[m];
                                    num3++;
                                }
                            }
                            else
                            {
                                for (int n = 0; n <= firstPoint2.m_ElementIndex.y; n++)
                                {
                                    nodes[num3++] = dynamicBuffer2[n];
                                }
                                for (int num4 = 0; num4 < num2; num4++)
                                {
                                    nodes[num3++] = new Game.Areas.Node(lastPoint2.m_Position, lastPoint2.m_Elevation);
                                }
                                for (int num5 = firstPoint2.m_ElementIndex.y + 1; num5 < dynamicBuffer2.Length; num5++)
                                {
                                    nodes[num3++] = dynamicBuffer2[num5];
                                }
                            }
                            switch (this.m_State)
                            {
                                case State.Default:
                                    if (this.m_AllowCreateArea)
                                    {
                                        this.m_Tooltip.value = Tooltip.CreateAreaOrModifyEdge;
                                    }
                                    else
                                    {
                                        this.m_Tooltip.value = Tooltip.ModifyEdge;
                                    }
                                    break;
                                case State.Modify:
                                    if (!flag2 && flag)
                                    {
                                        this.m_Tooltip.value = Tooltip.InsertNode;
                                    }
                                    break;
                            }
                        }
                        else
                        {
                            bool flag3 = false;
                            if (!this.m_OwnerData.HasComponent(component.m_Original) || dynamicBuffer2.Length >= 4)
                            {
                                if (this.m_State == State.Remove)
                                {
                                    flag3 = true;
                                }
                                else
                                {
                                    int index2 = math.select(firstPoint2.m_ElementIndex.x - 1, dynamicBuffer2.Length - 1, firstPoint2.m_ElementIndex.x == 0);
                                    int index3 = math.select(firstPoint2.m_ElementIndex.x + 1, 0, firstPoint2.m_ElementIndex.x == dynamicBuffer2.Length - 1);
                                    float2 float2 = new float2(math.distance(lastPoint2.m_Position, dynamicBuffer2[index2].m_Position), math.distance(lastPoint2.m_Position, dynamicBuffer2[index3].m_Position));
                                    flag3 = flag && math.any(float2 < minNodeDistance);
                                }
                            }
                            int num6 = math.select(0, 1, flag || flag3);
                            int num7 = math.select(1, 0, flag3 || !flag);
                            int num8 = dynamicBuffer2.Length + num7 - num6;
                            nodes.ResizeUninitialized(num8);
                            int num9 = 0;
                            if (dynamicBuffer3.IsCreated)
                            {
                                dynamicBuffer = this.m_CommandBuffer.AddBuffer<LocalNodeCache>(e);
                                dynamicBuffer.ResizeUninitialized(num8);
                                for (int num10 = 0; num10 <= firstPoint2.m_ElementIndex.x - num6; num10++)
                                {
                                    nodes[num9] = dynamicBuffer2[num10];
                                    dynamicBuffer[num9] = dynamicBuffer3[num10];
                                    num9++;
                                }
                                @int.x = num9;
                                for (int num11 = 0; num11 < num7; num11++)
                                {
                                    nodes[num9] = new Game.Areas.Node(lastPoint2.m_Position, elevation);
                                    dynamicBuffer[num9] = new LocalNodeCache
                                    {
                                        m_Position = lastPoint2.m_Position,
                                        m_ParentMesh = parentMesh
                                    };
                                    num9++;
                                }
                                @int.y = num9;
                                for (int num12 = firstPoint2.m_ElementIndex.x + 1; num12 < dynamicBuffer2.Length; num12++)
                                {
                                    nodes[num9] = dynamicBuffer2[num12];
                                    dynamicBuffer[num9] = dynamicBuffer3[num12];
                                    num9++;
                                }
                            }
                            else
                            {
                                for (int num13 = 0; num13 <= firstPoint2.m_ElementIndex.x - num6; num13++)
                                {
                                    nodes[num9++] = dynamicBuffer2[num13];
                                }
                                for (int num14 = 0; num14 < num7; num14++)
                                {
                                    nodes[num9++] = new Game.Areas.Node(lastPoint2.m_Position, lastPoint2.m_Elevation);
                                }
                                for (int num15 = firstPoint2.m_ElementIndex.x + 1; num15 < dynamicBuffer2.Length; num15++)
                                {
                                    nodes[num9++] = dynamicBuffer2[num15];
                                }
                            }
                            if (num8 < 3)
                            {
                                component.m_Flags |= CreationFlags.Delete;
                            }
                            switch (this.m_State)
                            {
                                case State.Default:
                                    if (this.m_AllowCreateArea)
                                    {
                                        this.m_Tooltip.value = Tooltip.CreateAreaOrModifyNode;
                                    }
                                    else
                                    {
                                        this.m_Tooltip.value = Tooltip.ModifyNode;
                                    }
                                    break;
                                case State.Modify:
                                    if (num8 < 3)
                                    {
                                        this.m_Tooltip.value = Tooltip.DeleteArea;
                                    }
                                    else if (flag3)
                                    {
                                        this.m_Tooltip.value = Tooltip.MergeNodes;
                                    }
                                    else if (flag)
                                    {
                                        this.m_Tooltip.value = Tooltip.MoveNode;
                                    }
                                    break;
                                case State.Remove:
                                    if (num8 < 3)
                                    {
                                        this.m_Tooltip.value = Tooltip.DeleteArea;
                                    }
                                    else if (flag3)
                                    {
                                        this.m_Tooltip.value = Tooltip.RemoveNode;
                                    }
                                    break;
                            }
                        }
                    }
                    else
                    {
                        if (this.m_Recreate != Entity.Null)
                        {
                            component.m_Flags |= CreationFlags.Recreate;
                        }
                        bool flag4 = false;
                        if (this.m_ControlPoints.Length >= 2)
                        {
                            flag4 = math.distance(this.m_ControlPoints[this.m_ControlPoints.Length - 2].m_Position, this.m_ControlPoints[this.m_ControlPoints.Length - 1].m_Position) < minNodeDistance;
                        }
                        int num16 = math.select(this.m_ControlPoints.Length, this.m_ControlPoints.Length - 1, flag4);
                        nodes.ResizeUninitialized(num16);
                        if (this.m_EditorMode)
                        {
                            dynamicBuffer = this.m_CommandBuffer.AddBuffer<LocalNodeCache>(e);
                            dynamicBuffer.ResizeUninitialized(num16);
                            @int = new int2(0, num16);
                            float num17 = float.MinValue;
                            int num18 = lastPoint2.m_ElementIndex.x;
                            if (this.m_TransformData.HasComponent(lastPoint2.m_OriginalEntity))
                            {
                                Entity entity = lastPoint2.m_OriginalEntity;
                                while (this.m_OwnerData.HasComponent(entity) && !this.m_BuildingData.HasComponent(entity))
                                {
                                    if (this.m_LocalTransformCacheData.HasComponent(entity))
                                    {
                                        num18 = this.m_LocalTransformCacheData[entity].m_ParentMesh;
                                    }
                                    entity = this.m_OwnerData[entity].m_Owner;
                                }
                                if (this.m_TransformData.TryGetComponent(entity, out var componentData3))
                                {
                                    num17 = componentData3.m_Position.y;
                                }
                            }
                            for (int num19 = 0; num19 < num16; num19++)
                            {
                                int parentMesh2 = -1;
                                float num20 = float.MinValue;
                                if (this.m_ControlPoints[num19].m_ElementIndex.x >= 0)
                                {
                                    parentMesh2 = math.select(this.m_ControlPoints[num19].m_ElementIndex.x, num18, num18 != -1);
                                    num20 = math.select(num20, this.m_ControlPoints[num19].m_Position.y - num17, num17 != float.MinValue);
                                }
                                nodes[num19] = new Game.Areas.Node(this.m_ControlPoints[num19].m_Position, num20);
                                dynamicBuffer[num19] = new LocalNodeCache
                                {
                                    m_Position = this.m_ControlPoints[num19].m_Position,
                                    m_ParentMesh = parentMesh2
                                };
                            }
                        }
                        else
                        {
                            for (int num21 = 0; num21 < num16; num21++)
                            {
                                nodes[num21] = new Game.Areas.Node(this.m_ControlPoints[num21].m_Position, float.MinValue);
                            }
                        }
                        switch (this.m_State)
                        {
                            case State.Default:
                                if (this.m_ControlPoints.Length == 1 && this.m_AllowCreateArea)
                                {
                                    this.m_Tooltip.value = Tooltip.CreateArea;
                                }
                                break;
                            case State.Create:
                                if (!flag4)
                                {
                                    if (this.m_ControlPoints.Length >= 4 && this.m_ControlPoints[0].m_Position.Equals(this.m_ControlPoints[this.m_ControlPoints.Length - 1].m_Position))
                                    {
                                        this.m_Tooltip.value = Tooltip.CompleteArea;
                                    }
                                    else
                                    {
                                        this.m_Tooltip.value = Tooltip.AddNode;
                                    }
                                }
                                break;
                        }
                    }
                    bool flag5 = false;
                    Transform inverseParentTransform = default(Transform);
                    if (this.m_TransformData.HasComponent(lastPoint2.m_OriginalEntity))
                    {
                        if ((areaData.m_Flags & Game.Areas.GeometryFlags.ClearArea) != 0)
                        {
                            ClearAreaHelpers.FillClearAreas(this.m_PrefabRefData[lastPoint2.m_OriginalEntity].m_Prefab, this.m_TransformData[lastPoint2.m_OriginalEntity], nodes, isComplete, this.m_PrefabObjectGeometryData, ref clearAreas);
                        }
                        OwnerDefinition ownerDefinition = this.GetOwnerDefinition(lastPoint2.m_OriginalEntity, component.m_Original, createdEntities, upgrade: true, (areaData.m_Flags & Game.Areas.GeometryFlags.ClearArea) != 0, clearAreas);
                        if (ownerDefinition.m_Prefab != Entity.Null)
                        {
                            inverseParentTransform.m_Position = -ownerDefinition.m_Position;
                            inverseParentTransform.m_Rotation = math.inverse(ownerDefinition.m_Rotation);
                            flag5 = true;
                            this.m_CommandBuffer.AddComponent(e, ownerDefinition);
                        }
                    }
                    else if (this.m_OwnerData.HasComponent(component.m_Original))
                    {
                        Entity owner2 = this.m_OwnerData[component.m_Original].m_Owner;
                        if (this.m_TransformData.HasComponent(owner2))
                        {
                            if ((areaData.m_Flags & Game.Areas.GeometryFlags.ClearArea) != 0)
                            {
                                ClearAreaHelpers.FillClearAreas(this.m_PrefabRefData[owner2].m_Prefab, this.m_TransformData[owner2], nodes, isComplete, this.m_PrefabObjectGeometryData, ref clearAreas);
                            }
                            OwnerDefinition ownerDefinition2 = this.GetOwnerDefinition(owner2, component.m_Original, createdEntities, upgrade: true, (areaData.m_Flags & Game.Areas.GeometryFlags.ClearArea) != 0, clearAreas);
                            if (ownerDefinition2.m_Prefab != Entity.Null)
                            {
                                inverseParentTransform.m_Position = -ownerDefinition2.m_Position;
                                inverseParentTransform.m_Rotation = math.inverse(ownerDefinition2.m_Rotation);
                                flag5 = true;
                                this.m_CommandBuffer.AddComponent(e, ownerDefinition2);
                            }
                            else
                            {
                                Transform transform = this.m_TransformData[owner2];
                                inverseParentTransform.m_Position = -transform.m_Position;
                                inverseParentTransform.m_Rotation = math.inverse(transform.m_Rotation);
                                flag5 = true;
                                component.m_Owner = owner2;
                            }
                        }
                        else
                        {
                            component.m_Owner = owner2;
                        }
                    }
                    if (flag5)
                    {
                        for (int num22 = @int.x; num22 < @int.y; num22++)
                        {
                            LocalNodeCache localNodeCache = dynamicBuffer[num22];
                            localNodeCache.m_Position = ObjectUtils.WorldToLocal(inverseParentTransform, localNodeCache.m_Position);
                        }
                    }
                    this.m_CommandBuffer.AddComponent(e, component);
                    this.m_CommandBuffer.AddComponent(e, default(Updated));
                    if (this.m_AreaData.TryGetComponent(component.m_Original, out var componentData4) && this.m_SubObjects.TryGetBuffer(component.m_Original, out var bufferData) && (componentData4.m_Flags & AreaFlags.Complete) != 0)
                    {
                        this.CheckSubObjects(bufferData, nodes, createdEntities, minNodeDistance, (componentData4.m_Flags & AreaFlags.CounterClockwise) != 0);
                    }
                    if (clearAreas.IsCreated)
                    {
                        clearAreas.Clear();
                    }
                }
                if (clearAreas.IsCreated)
                {
                    clearAreas.Dispose();
                }
                createdEntities.Dispose();
            }

            private Entity GetSourceArea(Entity originalArea)
            {
                if (this.m_ApplyTempAreas.IsCreated)
                {
                    for (int i = 0; i < this.m_ApplyTempAreas.Length; i++)
                    {
                        Entity entity = this.m_ApplyTempAreas[i];
                        if (originalArea == this.m_TempData[entity].m_Original)
                        {
                            return entity;
                        }
                    }
                }
                return originalArea;
            }

            private void CheckSubObjects(DynamicBuffer<Game.Objects.SubObject> subObjects, DynamicBuffer<Game.Areas.Node> nodes, NativeParallelHashSet<Entity> createdEntities, float minNodeDistance, bool isCounterClockwise)
            {
                Line2.Segment line = default(Line2.Segment);
                for (int i = 0; i < subObjects.Length; i++)
                {
                    Game.Objects.SubObject subObject = subObjects[i];
                    if (!this.m_BuildingData.HasComponent(subObject.m_SubObject))
                    {
                        continue;
                    }
                    if (this.m_ApplyTempBuildings.IsCreated)
                    {
                        bool flag = false;
                        for (int j = 0; j < this.m_ApplyTempBuildings.Length; j++)
                        {
                            if (this.m_ApplyTempBuildings[j] == subObject.m_SubObject)
                            {
                                flag = true;
                                break;
                            }
                        }
                        if (flag)
                        {
                            continue;
                        }
                    }
                    Transform transform = this.m_TransformData[subObject.m_SubObject];
                    PrefabRef prefabRef = this.m_PrefabRefData[subObject.m_SubObject];
                    if (!this.m_PrefabObjectGeometryData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
                    {
                        continue;
                    }
                    float num;
                    if ((componentData.m_Flags & Game.Objects.GeometryFlags.Circular) != 0)
                    {
                        num = componentData.m_Size.x * 0.5f;
                    }
                    else
                    {
                        num = math.length(MathUtils.Size(componentData.m_Bounds.xz)) * 0.5f;
                        transform.m_Position.xz -= math.rotate(transform.m_Rotation, MathUtils.Center(componentData.m_Bounds)).xz;
                    }
                    float num2 = 0f;
                    int num3 = -1;
                    bool flag2 = nodes.Length <= 2;
                    if (!flag2)
                    {
                        float num4 = float.MaxValue;
                        float num5 = num + minNodeDistance;
                        num5 *= num5;
                        line.a = nodes[nodes.Length - 1].m_Position.xz;
                        for (int k = 0; k < nodes.Length; k++)
                        {
                            line.b = nodes[k].m_Position.xz;
                            float t;
                            float num6 = MathUtils.DistanceSquared(line, transform.m_Position.xz, out t);
                            if (num6 < num5)
                            {
                                flag2 = true;
                                break;
                            }
                            if (num6 < num4)
                            {
                                num4 = num6;
                                num2 = t;
                                num3 = k;
                            }
                            line.a = line.b;
                        }
                    }
                    if (!flag2 && num3 >= 0)
                    {
                        int2 @int = math.select(new int2(num3 - 1, num3), new int2(num3 - 2, num3 + 1), new bool2(num2 == 0f, num2 == 1f));
                        @int = math.select(@int, @int + new int2(nodes.Length, -nodes.Length), new bool2(@int.x < 0, @int.y >= nodes.Length));
                        @int = math.select(@int, @int.yx, isCounterClockwise);
                        float2 xz = nodes[@int.x].m_Position.xz;
                        float2 xz2 = nodes[@int.y].m_Position.xz;
                        flag2 = math.dot(transform.m_Position.xz - xz, MathUtils.Right(xz2 - xz)) <= 0f;
                    }
                    if (flag2)
                    {
                        Entity e = this.m_CommandBuffer.CreateEntity();
                        CreationDefinition component = default(CreationDefinition);
                        component.m_Original = subObject.m_SubObject;
                        component.m_Flags |= CreationFlags.Delete;
                        ObjectDefinition component2 = default(ObjectDefinition);
                        component2.m_ParentMesh = -1;
                        component2.m_Position = transform.m_Position;
                        component2.m_Rotation = transform.m_Rotation;
                        component2.m_LocalPosition = transform.m_Position;
                        component2.m_LocalRotation = transform.m_Rotation;
                        this.m_CommandBuffer.AddComponent(e, component);
                        this.m_CommandBuffer.AddComponent(e, component2);
                        this.m_CommandBuffer.AddComponent(e, default(Updated));
                        this.UpdateSubNets(transform, prefabRef.m_Prefab, subObject.m_SubObject, default(NativeList<ClearAreaData>), removeAll: true);
                        this.UpdateSubAreas(transform, prefabRef.m_Prefab, subObject.m_SubObject, createdEntities, default(NativeList<ClearAreaData>), removeAll: true);
                    }
                }
            }

            private OwnerDefinition GetOwnerDefinition(Entity parent, Entity area, NativeParallelHashSet<Entity> createdEntities, bool upgrade, bool fullUpdate, NativeList<ClearAreaData> clearAreas)
            {
                OwnerDefinition result = default(OwnerDefinition);
                if (!this.m_EditorMode)
                {
                    return result;
                }
                Entity entity = parent;
                while (this.m_OwnerData.HasComponent(entity) && !this.m_BuildingData.HasComponent(entity))
                {
                    entity = this.m_OwnerData[entity].m_Owner;
                }
                OwnerDefinition ownerDefinition = default(OwnerDefinition);
                if (this.m_InstalledUpgrades.TryGetBuffer(entity, out var bufferData) && bufferData.Length != 0)
                {
                    if (fullUpdate && this.m_TransformData.HasComponent(entity))
                    {
                        Transform transform = this.m_TransformData[entity];
                        ClearAreaHelpers.FillClearAreas(bufferData, area, this.m_TransformData, this.m_ClearData, this.m_PrefabRefData, this.m_PrefabObjectGeometryData, this.m_SubAreas, this.m_Nodes, this.m_Triangles, ref clearAreas);
                        ClearAreaHelpers.InitClearAreas(clearAreas, transform);
                        if (createdEntities.Add(entity))
                        {
                            Entity owner = Entity.Null;
                            if (this.m_OwnerData.HasComponent(entity))
                            {
                                owner = this.m_OwnerData[entity].m_Owner;
                            }
                            this.UpdateOwnerObject(owner, entity, createdEntities, transform, default(OwnerDefinition), upgrade: false, clearAreas);
                        }
                        ownerDefinition.m_Prefab = this.m_PrefabRefData[entity].m_Prefab;
                        ownerDefinition.m_Position = transform.m_Position;
                        ownerDefinition.m_Rotation = transform.m_Rotation;
                    }
                    entity = bufferData[0].m_Upgrade;
                }
                if (this.m_TransformData.HasComponent(entity))
                {
                    Transform transform2 = this.m_TransformData[entity];
                    if (createdEntities.Add(entity))
                    {
                        Entity owner2 = Entity.Null;
                        if (ownerDefinition.m_Prefab == Entity.Null && this.m_OwnerData.HasComponent(entity))
                        {
                            owner2 = this.m_OwnerData[entity].m_Owner;
                        }
                        this.UpdateOwnerObject(owner2, entity, createdEntities, transform2, ownerDefinition, upgrade, default(NativeList<ClearAreaData>));
                    }
                    result.m_Prefab = this.m_PrefabRefData[entity].m_Prefab;
                    result.m_Position = transform2.m_Position;
                    result.m_Rotation = transform2.m_Rotation;
                }
                return result;
            }

            private void UpdateOwnerObject(Entity owner, Entity original, NativeParallelHashSet<Entity> createdEntities, Transform transform, OwnerDefinition ownerDefinition, bool upgrade, NativeList<ClearAreaData> clearAreas)
            {
                Entity e = this.m_CommandBuffer.CreateEntity();
                Entity prefab = this.m_PrefabRefData[original].m_Prefab;
                CreationDefinition component = default(CreationDefinition);
                component.m_Owner = owner;
                component.m_Original = original;
                if (upgrade)
                {
                    component.m_Flags |= CreationFlags.Upgrade | CreationFlags.Parent;
                }
                ObjectDefinition component2 = default(ObjectDefinition);
                component2.m_ParentMesh = -1;
                component2.m_Position = transform.m_Position;
                component2.m_Rotation = transform.m_Rotation;
                if (this.m_TransformData.HasComponent(owner))
                {
                    Transform transform2 = ObjectUtils.WorldToLocal(ObjectUtils.InverseTransform(this.m_TransformData[owner]), transform);
                    component2.m_LocalPosition = transform2.m_Position;
                    component2.m_LocalRotation = transform2.m_Rotation;
                }
                else
                {
                    component2.m_LocalPosition = transform.m_Position;
                    component2.m_LocalRotation = transform.m_Rotation;
                }
                this.m_CommandBuffer.AddComponent(e, component);
                this.m_CommandBuffer.AddComponent(e, component2);
                this.m_CommandBuffer.AddComponent(e, default(Updated));
                if (ownerDefinition.m_Prefab != Entity.Null)
                {
                    this.m_CommandBuffer.AddComponent(e, ownerDefinition);
                }
                this.UpdateSubNets(transform, prefab, original, clearAreas, removeAll: false);
                this.UpdateSubAreas(transform, prefab, original, createdEntities, clearAreas, removeAll: false);
            }

            private void UpdateSubNets(Transform transform, Entity prefab, Entity original, NativeList<ClearAreaData> clearAreas, bool removeAll)
            {
                if (!this.m_SubNets.HasBuffer(original))
                {
                    return;
                }
                DynamicBuffer<Game.Net.SubNet> dynamicBuffer = this.m_SubNets[original];
                for (int i = 0; i < dynamicBuffer.Length; i++)
                {
                    Entity subNet = dynamicBuffer[i].m_SubNet;
                    if (this.m_NodeData.HasComponent(subNet))
                    {
                        if (!this.HasEdgeStartOrEnd(subNet, original))
                        {
                            Game.Net.Node node = this.m_NodeData[subNet];
                            Entity e = this.m_CommandBuffer.CreateEntity();
                            CreationDefinition component = default(CreationDefinition);
                            component.m_Original = subNet;
                            if (this.m_EditorContainerData.HasComponent(subNet))
                            {
                                component.m_SubPrefab = this.m_EditorContainerData[subNet].m_Prefab;
                            }
                            Game.Net.Elevation componentData;
                            bool onGround = !this.m_NetElevationData.TryGetComponent(subNet, out componentData) || math.cmin(math.abs(componentData.m_Elevation)) < 2f;
                            if (removeAll)
                            {
                                component.m_Flags |= CreationFlags.Delete;
                            }
                            else if (ClearAreaHelpers.ShouldClear(clearAreas, node.m_Position, onGround))
                            {
                                component.m_Flags |= CreationFlags.Delete | CreationFlags.Hidden;
                            }
                            OwnerDefinition component2 = default(OwnerDefinition);
                            component2.m_Prefab = prefab;
                            component2.m_Position = transform.m_Position;
                            component2.m_Rotation = transform.m_Rotation;
                            this.m_CommandBuffer.AddComponent(e, component2);
                            this.m_CommandBuffer.AddComponent(e, component);
                            this.m_CommandBuffer.AddComponent(e, default(Updated));
                            NetCourse component3 = default(NetCourse);
                            component3.m_Curve = new Bezier4x3(node.m_Position, node.m_Position, node.m_Position, node.m_Position);
                            component3.m_Length = 0f;
                            component3.m_FixedIndex = -1;
                            component3.m_StartPosition.m_Entity = subNet;
                            component3.m_StartPosition.m_Position = node.m_Position;
                            component3.m_StartPosition.m_Rotation = node.m_Rotation;
                            component3.m_StartPosition.m_CourseDelta = 0f;
                            component3.m_EndPosition.m_Entity = subNet;
                            component3.m_EndPosition.m_Position = node.m_Position;
                            component3.m_EndPosition.m_Rotation = node.m_Rotation;
                            component3.m_EndPosition.m_CourseDelta = 1f;
                            this.m_CommandBuffer.AddComponent(e, component3);
                        }
                    }
                    else if (this.m_EdgeData.HasComponent(subNet))
                    {
                        Edge edge = this.m_EdgeData[subNet];
                        Entity e2 = this.m_CommandBuffer.CreateEntity();
                        CreationDefinition component4 = default(CreationDefinition);
                        component4.m_Original = subNet;
                        if (this.m_EditorContainerData.HasComponent(subNet))
                        {
                            component4.m_SubPrefab = this.m_EditorContainerData[subNet].m_Prefab;
                        }
                        Curve curve = this.m_CurveData[subNet];
                        Game.Net.Elevation componentData2;
                        bool onGround2 = !this.m_NetElevationData.TryGetComponent(subNet, out componentData2) || math.cmin(math.abs(componentData2.m_Elevation)) < 2f;
                        if (removeAll)
                        {
                            component4.m_Flags |= CreationFlags.Delete;
                        }
                        else if (ClearAreaHelpers.ShouldClear(clearAreas, curve.m_Bezier, onGround2))
                        {
                            component4.m_Flags |= CreationFlags.Delete | CreationFlags.Hidden;
                        }
                        OwnerDefinition component5 = default(OwnerDefinition);
                        component5.m_Prefab = prefab;
                        component5.m_Position = transform.m_Position;
                        component5.m_Rotation = transform.m_Rotation;
                        this.m_CommandBuffer.AddComponent(e2, component5);
                        this.m_CommandBuffer.AddComponent(e2, component4);
                        this.m_CommandBuffer.AddComponent(e2, default(Updated));
                        NetCourse component6 = default(NetCourse);
                        component6.m_Curve = curve.m_Bezier;
                        component6.m_Length = MathUtils.Length(component6.m_Curve);
                        component6.m_FixedIndex = -1;
                        component6.m_StartPosition.m_Entity = edge.m_Start;
                        component6.m_StartPosition.m_Position = component6.m_Curve.a;
                        component6.m_StartPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.StartTangent(component6.m_Curve));
                        component6.m_StartPosition.m_CourseDelta = 0f;
                        component6.m_EndPosition.m_Entity = edge.m_End;
                        component6.m_EndPosition.m_Position = component6.m_Curve.d;
                        component6.m_EndPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.EndTangent(component6.m_Curve));
                        component6.m_EndPosition.m_CourseDelta = 1f;
                        this.m_CommandBuffer.AddComponent(e2, component6);
                    }
                }
            }

            private bool HasEdgeStartOrEnd(Entity node, Entity owner)
            {
                DynamicBuffer<ConnectedEdge> dynamicBuffer = this.m_ConnectedEdges[node];
                for (int i = 0; i < dynamicBuffer.Length; i++)
                {
                    Entity edge = dynamicBuffer[i].m_Edge;
                    Edge edge2 = this.m_EdgeData[edge];
                    if ((edge2.m_Start == node || edge2.m_End == node) && this.m_OwnerData.HasComponent(edge) && this.m_OwnerData[edge].m_Owner == owner)
                    {
                        return true;
                    }
                }
                return false;
            }

            private void UpdateSubAreas(Transform transform, Entity prefab, Entity original, NativeParallelHashSet<Entity> createdEntities, NativeList<ClearAreaData> clearAreas, bool removeAll)
            {
                if (!this.m_SubAreas.HasBuffer(original))
                {
                    return;
                }
                DynamicBuffer<Game.Areas.SubArea> dynamicBuffer = this.m_SubAreas[original];
                for (int i = 0; i < dynamicBuffer.Length; i++)
                {
                    Entity area = dynamicBuffer[i].m_Area;
                    if (!createdEntities.Add(area))
                    {
                        continue;
                    }
                    Entity e = this.m_CommandBuffer.CreateEntity();
                    CreationDefinition component = default(CreationDefinition);
                    component.m_Original = area;
                    OwnerDefinition component2 = default(OwnerDefinition);
                    component2.m_Prefab = prefab;
                    component2.m_Position = transform.m_Position;
                    component2.m_Rotation = transform.m_Rotation;
                    this.m_CommandBuffer.AddComponent(e, component2);
                    DynamicBuffer<Game.Areas.Node> nodes = this.m_Nodes[area];
                    if (removeAll)
                    {
                        component.m_Flags |= CreationFlags.Delete;
                    }
                    else if (this.m_SpaceData.HasComponent(area))
                    {
                        DynamicBuffer<Triangle> triangles = this.m_Triangles[area];
                        if (ClearAreaHelpers.ShouldClear(clearAreas, nodes, triangles, transform))
                        {
                            component.m_Flags |= CreationFlags.Delete | CreationFlags.Hidden;
                        }
                    }
                    this.m_CommandBuffer.AddComponent(e, component);
                    this.m_CommandBuffer.AddComponent(e, default(Updated));
                    this.m_CommandBuffer.AddBuffer<Game.Areas.Node>(e).CopyFrom(nodes.AsNativeArray());
                    if (this.m_CachedNodes.HasBuffer(area))
                    {
                        DynamicBuffer<LocalNodeCache> dynamicBuffer2 = this.m_CachedNodes[area];
                        this.m_CommandBuffer.AddBuffer<LocalNodeCache>(e).CopyFrom(dynamicBuffer2.AsNativeArray());
                    }
                }
            }
        }

        private struct TypeHandle
        {
            [ReadOnly]
            public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<AreaGeometryData> __Game_Prefabs_AreaGeometryData_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<EdgeGeometry> __Game_Net_EdgeGeometry_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<StartNodeGeometry> __Game_Net_StartNodeGeometry_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<EndNodeGeometry> __Game_Net_EndNodeGeometry_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<Composition> __Game_Net_Composition_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<BuildingExtensionData> __Game_Prefabs_BuildingExtensionData_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<AssetStampData> __Game_Prefabs_AssetStampData_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<NetCompositionData> __Game_Prefabs_NetCompositionData_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<Game.Areas.Lot> __Game_Areas_Lot_RO_ComponentLookup;

            [ReadOnly]
            public BufferLookup<Game.Areas.Node> __Game_Areas_Node_RO_BufferLookup;

            [ReadOnly]
            public BufferLookup<Triangle> __Game_Areas_Triangle_RO_BufferLookup;

            [ReadOnly]
            public BufferLookup<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferLookup;

            [ReadOnly]
            public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

            [ReadOnly]
            public BufferTypeHandle<Game.Areas.Node> __Game_Areas_Node_RO_BufferTypeHandle;

            [ReadOnly]
            public BufferTypeHandle<LocalNodeCache> __Game_Tools_LocalNodeCache_RO_BufferTypeHandle;

            [ReadOnly]
            public ComponentLookup<Clear> __Game_Areas_Clear_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<Space> __Game_Areas_Space_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<Area> __Game_Areas_Area_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<Game.Net.Node> __Game_Net_Node_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<Game.Net.Elevation> __Game_Net_Elevation_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<Game.Tools.EditorContainer> __Game_Tools_EditorContainer_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<LocalTransformCache> __Game_Tools_LocalTransformCache_RO_ComponentLookup;

            [ReadOnly]
            public BufferLookup<Game.Areas.SubArea> __Game_Areas_SubArea_RO_BufferLookup;

            [ReadOnly]
            public BufferLookup<LocalNodeCache> __Game_Tools_LocalNodeCache_RO_BufferLookup;

            [ReadOnly]
            public BufferLookup<Game.Net.SubNet> __Game_Net_SubNet_RO_BufferLookup;

            [ReadOnly]
            public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

            [ReadOnly]
            public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                this.__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
                this.__Game_Prefabs_AreaGeometryData_RO_ComponentLookup = state.GetComponentLookup<AreaGeometryData>(isReadOnly: true);
                this.__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
                this.__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
                this.__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
                this.__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
                this.__Game_Net_EdgeGeometry_RO_ComponentLookup = state.GetComponentLookup<EdgeGeometry>(isReadOnly: true);
                this.__Game_Net_StartNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<StartNodeGeometry>(isReadOnly: true);
                this.__Game_Net_EndNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<EndNodeGeometry>(isReadOnly: true);
                this.__Game_Net_Composition_RO_ComponentLookup = state.GetComponentLookup<Composition>(isReadOnly: true);
                this.__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
                this.__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
                this.__Game_Prefabs_BuildingExtensionData_RO_ComponentLookup = state.GetComponentLookup<BuildingExtensionData>(isReadOnly: true);
                this.__Game_Prefabs_AssetStampData_RO_ComponentLookup = state.GetComponentLookup<AssetStampData>(isReadOnly: true);
                this.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
                this.__Game_Prefabs_NetCompositionData_RO_ComponentLookup = state.GetComponentLookup<NetCompositionData>(isReadOnly: true);
                this.__Game_Areas_Lot_RO_ComponentLookup = state.GetComponentLookup<Game.Areas.Lot>(isReadOnly: true);
                this.__Game_Areas_Node_RO_BufferLookup = state.GetBufferLookup<Game.Areas.Node>(isReadOnly: true);
                this.__Game_Areas_Triangle_RO_BufferLookup = state.GetBufferLookup<Triangle>(isReadOnly: true);
                this.__Game_Buildings_InstalledUpgrade_RO_BufferLookup = state.GetBufferLookup<InstalledUpgrade>(isReadOnly: true);
                this.__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                this.__Game_Areas_Node_RO_BufferTypeHandle = state.GetBufferTypeHandle<Game.Areas.Node>(isReadOnly: true);
                this.__Game_Tools_LocalNodeCache_RO_BufferTypeHandle = state.GetBufferTypeHandle<LocalNodeCache>(isReadOnly: true);
                this.__Game_Areas_Clear_RO_ComponentLookup = state.GetComponentLookup<Clear>(isReadOnly: true);
                this.__Game_Areas_Space_RO_ComponentLookup = state.GetComponentLookup<Space>(isReadOnly: true);
                this.__Game_Areas_Area_RO_ComponentLookup = state.GetComponentLookup<Area>(isReadOnly: true);
                this.__Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Node>(isReadOnly: true);
                this.__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
                this.__Game_Net_Elevation_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Elevation>(isReadOnly: true);
                this.__Game_Tools_EditorContainer_RO_ComponentLookup = state.GetComponentLookup<Game.Tools.EditorContainer>(isReadOnly: true);
                this.__Game_Tools_LocalTransformCache_RO_ComponentLookup = state.GetComponentLookup<LocalTransformCache>(isReadOnly: true);
                this.__Game_Areas_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Areas.SubArea>(isReadOnly: true);
                this.__Game_Tools_LocalNodeCache_RO_BufferLookup = state.GetBufferLookup<LocalNodeCache>(isReadOnly: true);
                this.__Game_Net_SubNet_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubNet>(isReadOnly: true);
                this.__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
                this.__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
            }
        }

        public const string kToolID = "Area Tool";

        private ObjectToolSystem m_ObjectToolSystem;

        private Game.Areas.SearchSystem m_AreaSearchSystem;

        private Game.Net.SearchSystem m_NetSearchSystem;

        private Game.Objects.SearchSystem m_ObjectSearchSystem;

        private ToolOutputBarrier m_ToolOutputBarrier;

        private AudioManager m_AudioManager;

        private ProxyAction m_ApplyAction;

        private ProxyAction m_SecondaryApplyAction;

        private DisplayNameOverride m_ApplyDisplayOverride;

        private DisplayNameOverride m_SecondaryApplyDisplayOverride;

        private EntityQuery m_DefinitionQuery;

        private EntityQuery m_TempAreaQuery;

        private EntityQuery m_TempBuildingQuery;

        private EntityQuery m_MapTileQuery;

        private EntityQuery m_SoundQuery;

        private ControlPoint m_LastRaycastPoint;

        private NativeList<ControlPoint> m_ControlPoints;

        private NativeList<ControlPoint> m_MoveStartPositions;

        private NativeValue<Tooltip> m_Tooltip;

        private Mode m_LastMode;

        private State m_State;

        private AreaPrefab m_Prefab;

        private bool m_ControlPointsMoved;

        private bool m_AllowCreateArea;

        private bool m_ForceCancel;

        private TypeHandle __TypeHandle;

        public override string toolID => "Area Tool";

        public override int uiModeIndex => (int)this.actualMode;

        public Mode mode { get; set; }

        public Mode actualMode
        {
            get
            {
                if (!this.allowGenerate)
                {
                    return Mode.Edit;
                }
                return this.mode;
            }
        }

        public Entity recreate { get; set; }

        public bool underground { get; set; }

        public bool allowGenerate { get; private set; }

        public State state => this.m_State;

        public Tooltip tooltip => this.m_Tooltip.value;

        public AreaPrefab prefab
        {
            get
            {
                return this.m_Prefab;
            }
            set
            {
                if (value != this.m_Prefab)
                {
                    this.m_Prefab = value;
                    this.allowGenerate = base.m_ToolSystem.actionMode.IsEditor() && value is MapTilePrefab;
                    base.m_ToolSystem.EventPrefabChanged?.Invoke(value);
                }
            }
        }

        public override void GetUIModes(List<ToolMode> modes)
        {
            modes.Add(new ToolMode(Mode.Edit.ToString(), 0));
            if (this.allowGenerate)
            {
                modes.Add(new ToolMode(Mode.Generate.ToString(), 1));
            }
        }

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            this.m_ObjectToolSystem = base.World.GetOrCreateSystemManaged<ObjectToolSystem>();
            this.m_AreaSearchSystem = base.World.GetOrCreateSystemManaged<Game.Areas.SearchSystem>();
            this.m_NetSearchSystem = base.World.GetOrCreateSystemManaged<Game.Net.SearchSystem>();
            this.m_ObjectSearchSystem = base.World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
            this.m_ToolOutputBarrier = base.World.GetOrCreateSystemManaged<ToolOutputBarrier>();
            this.m_AudioManager = base.World.GetOrCreateSystemManaged<AudioManager>();
            this.m_DefinitionQuery = base.GetDefinitionQuery();
            this.m_TempAreaQuery = base.GetEntityQuery(ComponentType.ReadOnly<Area>(), ComponentType.ReadOnly<Temp>());
            this.m_TempBuildingQuery = base.GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<Temp>());
            this.m_MapTileQuery = base.GetEntityQuery(ComponentType.ReadOnly<MapTile>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
            this.m_SoundQuery = base.GetEntityQuery(ComponentType.ReadOnly<ToolUXSoundSettingsData>());
            this.m_ApplyAction = InputManager.instance.FindAction("Tool", "Apply");
            this.m_SecondaryApplyAction = InputManager.instance.FindAction("Tool", "Secondary Apply");
            this.m_ApplyDisplayOverride = new DisplayNameOverride("AreaToolSystem", this.m_ApplyAction, "Add/Modify Area Node", 20);
            this.m_SecondaryApplyDisplayOverride = new DisplayNameOverride("AreaToolSystem", this.m_SecondaryApplyAction, null, 25);
            this.selectedSnap &= ~Snap.AutoParent;
            this.m_ControlPoints = new NativeList<ControlPoint>(20, Allocator.Persistent);
            this.m_MoveStartPositions = new NativeList<ControlPoint>(10, Allocator.Persistent);
            this.m_Tooltip = new NativeValue<Tooltip>(Allocator.Persistent);
        }

        [Preserve]
        protected override void OnDestroy()
        {
            this.m_ControlPoints.Dispose();
            this.m_MoveStartPositions.Dispose();
            this.m_Tooltip.Dispose();
            base.OnDestroy();
        }

        [Preserve]
        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            this.m_ControlPoints.Clear();
            this.m_MoveStartPositions.Clear();
            this.m_LastRaycastPoint = default(ControlPoint);
            this.m_LastMode = this.actualMode;
            this.m_State = State.Default;
            this.m_Tooltip.value = Tooltip.None;
            this.m_AllowCreateArea = false;
            this.m_ForceCancel = false;
            this.UpdateActions();
        }

        [Preserve]
        protected override void OnStopRunning()
        {
            this.recreate = Entity.Null;
            this.m_ApplyAction.shouldBeEnabled = false;//
            this.m_SecondaryApplyAction.shouldBeEnabled = false;//
            this.m_ApplyDisplayOverride.state = DisplayNameOverride.State.Off;
            this.m_SecondaryApplyDisplayOverride.state = DisplayNameOverride.State.Off;
            base.OnStopRunning();
        }

        public NativeList<ControlPoint> GetControlPoints(out NativeList<ControlPoint> moveStartPositions, out JobHandle dependencies)
        {
            moveStartPositions = this.m_MoveStartPositions;
            dependencies = base.Dependency;
            return this.m_ControlPoints;
        }

        public override PrefabBase GetPrefab()
        {
            return this.prefab;
        }

        public override bool TrySetPrefab(PrefabBase prefab)
        {
            if (prefab is AreaPrefab areaPrefab)
            {
                this.prefab = areaPrefab;
                return true;
            }
            return false;
        }

        public override void SetUnderground(bool underground)
        {
            this.underground = underground;
        }

        public override void ElevationUp()
        {
            this.underground = false;
        }

        public override void ElevationDown()
        {
            this.underground = true;
        }

        public override void ElevationScroll()
        {
            this.underground = !this.underground;
        }

        public override void InitializeRaycast()
        {
            base.InitializeRaycast();
            if (this.prefab != null)
            {
                AreaGeometryData componentData = base.m_PrefabSystem.GetComponentData<AreaGeometryData>(this.prefab);
                this.GetAvailableSnapMask(out var onMask, out var offMask);
                Snap actualSnap = ToolBaseSystem.GetActualSnap(this.selectedSnap, onMask, offMask);
                base.m_ToolRaycastSystem.raycastFlags |= RaycastFlags.SubElements;
                base.m_ToolRaycastSystem.typeMask = TypeMask.Terrain | TypeMask.Areas;
                base.m_ToolRaycastSystem.areaTypeMask = AreaUtils.GetTypeMask(componentData.m_Type);
                if ((componentData.m_Flags & Game.Areas.GeometryFlags.OnWaterSurface) != 0)
                {
                    base.m_ToolRaycastSystem.typeMask |= TypeMask.Water;
                }
                if ((actualSnap & Snap.ObjectSurface) != 0)
                {
                    base.m_ToolRaycastSystem.typeMask |= TypeMask.StaticObjects;
                    if (base.m_ToolSystem.actionMode.IsEditor())
                    {
                        base.m_ToolRaycastSystem.raycastFlags |= RaycastFlags.Placeholders;
                    }
                    if (this.underground)
                    {
                        base.m_ToolRaycastSystem.collisionMask = CollisionMask.Underground;
                        base.m_ToolRaycastSystem.typeMask &= ~(TypeMask.Terrain | TypeMask.Water);
                        base.m_ToolRaycastSystem.raycastFlags |= RaycastFlags.PartialSurface;
                    }
                }
                if ((actualSnap & Snap.ExistingGeometry) == 0 && this.m_State != 0)
                {
                    base.m_ToolRaycastSystem.typeMask &= ~TypeMask.Areas;
                }
            }
            else
            {
                base.m_ToolRaycastSystem.typeMask = TypeMask.Terrain | TypeMask.Areas;
                base.m_ToolRaycastSystem.areaTypeMask = AreaTypeMask.None;
            }
            if (base.m_ToolSystem.actionMode.IsEditor())
            {
                base.m_ToolRaycastSystem.raycastFlags |= RaycastFlags.UpgradeIsMain;
            }
        }

        [Preserve]
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (base.m_FocusChanged)
            {
                return inputDeps;
            }
            if (this.actualMode != this.m_LastMode)
            {
                this.m_ControlPoints.Clear();
                this.m_MoveStartPositions.Clear();
                this.m_LastRaycastPoint = default(ControlPoint);
                this.m_LastMode = this.actualMode;
                this.m_State = State.Default;
                this.m_Tooltip.value = Tooltip.None;
                this.m_AllowCreateArea = false;
            }
            bool forceCancel = this.m_ForceCancel;
            this.m_ForceCancel = false;
            if (base.EntityManager.TryGetBuffer(this.recreate, isReadOnly: true, out DynamicBuffer<Game.Areas.Node> buffer))
            {
                this.m_State = State.Create;
                if (this.m_ControlPoints.Length < 3 && buffer.Length >= 2)
                {
                    ref NativeList<ControlPoint> controlPoints = ref this.m_ControlPoints;
                    ControlPoint value = new ControlPoint
                    {
                        m_OriginalEntity = this.recreate,
                        m_ElementIndex = new int2(0, -1),
                        m_Position = buffer[0].m_Position,
                        m_HitPosition = buffer[0].m_Position
                    };
                    controlPoints.Add(in value);
                    ref NativeList<ControlPoint> controlPoints2 = ref this.m_ControlPoints;
                    value = new ControlPoint
                    {
                        m_OriginalEntity = this.recreate,
                        m_ElementIndex = new int2(1, -1),
                        m_Position = buffer[1].m_Position,
                        m_HitPosition = buffer[1].m_Position
                    };
                    controlPoints2.Add(in value);
                    ref NativeList<ControlPoint> controlPoints3 = ref this.m_ControlPoints;
                    value = new ControlPoint
                    {
                        m_ElementIndex = new int2(-1, -1),
                        m_Position = math.lerp(buffer[0].m_Position, buffer[1].m_Position, 0.5f),
                        m_HitPosition = math.lerp(buffer[0].m_Position, buffer[1].m_Position, 0.5f)
                    };
                    controlPoints3.Add(in value);
                }
            }
            this.UpdateActions();
            if (this.prefab != null)
            {
                AreaGeometryData componentData = base.m_PrefabSystem.GetComponentData<AreaGeometryData>(this.prefab);
                base.requireAreas = AreaUtils.GetTypeMask(componentData.m_Type);
                base.requireZones = componentData.m_Type == Game.Areas.AreaType.Lot;
                this.m_AllowCreateArea = (base.m_ToolSystem.actionMode.IsEditor() || componentData.m_Type != 0) && (componentData.m_Type != Game.Areas.AreaType.Surface || (componentData.m_Flags & Game.Areas.GeometryFlags.ClipTerrain) != 0 || base.m_PrefabSystem.HasComponent<RenderedAreaData>(this.prefab));
                base.UpdateInfoview(base.m_ToolSystem.actionMode.IsEditor() ? Entity.Null : base.m_PrefabSystem.GetEntity(this.prefab));
                AreaToolSystem.GetAvailableSnapMask(componentData, base.m_ToolSystem.actionMode.IsEditor(), out base.m_SnapOnMask, out base.m_SnapOffMask);
                this.allowUnderground = (ToolBaseSystem.GetActualSnap(this.selectedSnap, base.m_SnapOnMask, base.m_SnapOffMask) & Snap.ObjectSurface) != 0;
                base.requireUnderground = this.allowUnderground && this.underground;
                if (this.m_State != 0 && !this.m_ApplyAction.enabled)
                {
                    this.m_State = State.Default;
                    return this.Clear(inputDeps);
                }
                if ((base.m_ToolRaycastSystem.raycastFlags & (RaycastFlags.DebugDisable | RaycastFlags.UIDisable)) == 0)
                {
                    if (this.m_State != State.Remove && this.m_SecondaryApplyAction.WasPressedThisFrame())
                    {
                        return this.Cancel(inputDeps, this.m_SecondaryApplyAction.WasReleasedThisFrame());
                    }
                    if (this.m_State == State.Remove && (forceCancel || this.m_SecondaryApplyAction.WasReleasedThisFrame()))
                    {
                        return this.Cancel(inputDeps);
                    }
                    if (this.m_State != State.Modify && this.m_ApplyAction.WasPressedThisFrame())
                    {
                        return this.Apply(inputDeps, this.m_ApplyAction.WasReleasedThisFrame());
                    }
                    if (this.m_State == State.Modify && this.m_ApplyAction.WasReleasedThisFrame())
                    {
                        return this.Apply(inputDeps);
                    }
                    return this.Update(inputDeps);
                }
            }
            else
            {
                base.requireAreas = AreaTypeMask.None;
                base.requireZones = false;
                base.requireUnderground = false;
                this.m_AllowCreateArea = false;
                this.allowUnderground = false;
                base.UpdateInfoview(Entity.Null);
            }
            if (this.m_State == State.Modify && this.m_ApplyAction.WasReleasedThisFrame())
            {
                if ((base.m_ToolRaycastSystem.raycastFlags & (RaycastFlags.DebugDisable | RaycastFlags.UIDisable)) == 0)
                {
                    return this.Cancel(inputDeps);
                }
                this.m_ControlPoints.Clear();
                this.m_State = State.Default;
            }
            else if (this.m_State == State.Remove && this.m_SecondaryApplyAction.WasReleasedThisFrame())
            {
                if ((base.m_ToolRaycastSystem.raycastFlags & (RaycastFlags.DebugDisable | RaycastFlags.UIDisable)) == 0)
                {
                    return this.Apply(inputDeps);
                }
                this.m_ControlPoints.Clear();
                this.m_State = State.Default;
            }
            return this.Clear(inputDeps);
        }

        private void UpdateActions()
        {
            this.m_ApplyAction.shouldBeEnabled = true;
            this.m_SecondaryApplyAction.shouldBeEnabled = true;
            this.m_ApplyDisplayOverride.state = DisplayNameOverride.State.GlobalHint;
            this.m_SecondaryApplyDisplayOverride.state = DisplayNameOverride.State.GlobalHint;
            this.m_SecondaryApplyDisplayOverride.displayName = ((this.m_State == State.Create && this.m_ControlPoints.Length > 1) ? "Undo Area Node" : "Remove Area Node");
        }

        public override void GetAvailableSnapMask(out Snap onMask, out Snap offMask)
        {
            if (this.prefab != null)
            {
                AreaToolSystem.GetAvailableSnapMask(base.m_PrefabSystem.GetComponentData<AreaGeometryData>(this.prefab), base.m_ToolSystem.actionMode.IsEditor(), out onMask, out offMask);
            }
            else
            {
                base.GetAvailableSnapMask(out onMask, out offMask);
            }
        }

        private static void GetAvailableSnapMask(AreaGeometryData prefabAreaData, bool editorMode, out Snap onMask, out Snap offMask)
        {
            onMask = Snap.ExistingGeometry | Snap.StraightDirection;
            offMask = onMask;
            switch (prefabAreaData.m_Type)
            {
                case Game.Areas.AreaType.Lot:
                    onMask |= Snap.NetSide | Snap.ObjectSide;
                    offMask |= Snap.NetSide | Snap.ObjectSide;
                    if (editorMode)
                    {
                        onMask |= Snap.LotGrid | Snap.AutoParent;
                        offMask |= Snap.LotGrid | Snap.AutoParent;
                    }
                    break;
                case Game.Areas.AreaType.District:
                    onMask |= Snap.NetMiddle;
                    offMask |= Snap.NetMiddle;
                    break;
                case Game.Areas.AreaType.Space:
                    onMask |= Snap.NetSide | Snap.ObjectSide | Snap.ObjectSurface;
                    offMask |= Snap.NetSide | Snap.ObjectSide | Snap.ObjectSurface;
                    if (editorMode)
                    {
                        onMask |= Snap.LotGrid | Snap.AutoParent;
                        offMask |= Snap.LotGrid | Snap.AutoParent;
                    }
                    break;
                case Game.Areas.AreaType.Surface:
                    onMask |= Snap.NetSide | Snap.ObjectSide;
                    offMask |= Snap.NetSide | Snap.ObjectSide;
                    if (editorMode)
                    {
                        onMask |= Snap.LotGrid | Snap.AutoParent;
                        offMask |= Snap.LotGrid | Snap.AutoParent;
                    }
                    break;
                case Game.Areas.AreaType.MapTile:
                    break;
            }
        }

        private JobHandle Clear(JobHandle inputDeps)
        {
            base.applyMode = ApplyMode.Clear;
            return inputDeps;
        }

        private JobHandle Cancel(JobHandle inputDeps, bool singleFrameOnly = false)
        {
            switch (this.m_State)
            {
                case State.Default:
                    if (this.actualMode == Mode.Generate)
                    {
                        return this.Update(inputDeps);
                    }
                    if (this.GetAllowApply() && this.m_ControlPoints.Length > 0)
                    {
                        base.applyMode = ApplyMode.Clear;
                        ControlPoint value = this.m_ControlPoints[0];
                        if (base.EntityManager.HasComponent<Area>(value.m_OriginalEntity) && value.m_ElementIndex.x >= 0)
                        {
                            if (base.EntityManager.TryGetBuffer(value.m_OriginalEntity, isReadOnly: true, out DynamicBuffer<Game.Areas.Node> buffer) && buffer.Length <= 3)
                            {
                                this.m_AudioManager.PlayUISound(this.m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PolygonToolDeleteAreaSound);
                            }
                            else
                            {
                                this.m_AudioManager.PlayUISound(this.m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PolygonToolRemovePointSound);
                            }
                            this.m_State = State.Remove;
                            this.m_ControlPointsMoved = false;
                            this.m_ForceCancel = singleFrameOnly;
                            this.m_MoveStartPositions.Clear();
                            this.m_MoveStartPositions.AddRange(this.m_ControlPoints.AsArray());
                            this.m_ControlPoints.Clear();
                            if (this.GetRaycastResult(out var controlPoint2))
                            {
                                this.m_LastRaycastPoint = controlPoint2;
                                this.m_ControlPoints.Add(in controlPoint2);
                                inputDeps = this.SnapControlPoints(inputDeps, default(NativeArray<Entity>));
                                inputDeps = this.UpdateDefinitions(inputDeps, default(NativeArray<Entity>), default(NativeArray<Entity>));
                            }
                            else
                            {
                                this.m_ControlPoints.Add(in value);
                            }
                            return inputDeps;
                        }
                        return this.Update(inputDeps);
                    }
                    return this.Update(inputDeps);
                case State.Create:
                    {
                        this.m_ControlPoints.RemoveAtSwapBack(this.m_ControlPoints.Length - 1);
                        base.applyMode = ApplyMode.Clear;
                        if (this.m_ControlPoints.Length <= 1)
                        {
                            this.m_State = State.Default;
                        }
                        if (this.recreate != Entity.Null && this.m_ControlPoints.Length <= 2)
                        {
                            base.m_ToolSystem.activeTool = this.m_ObjectToolSystem;
                            return inputDeps;
                        }
                        this.m_AudioManager.PlayUISound(this.m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PolygonToolRemovePointSound);
                        if (this.GetRaycastResult(out var controlPoint4))
                        {
                            this.m_LastRaycastPoint = controlPoint4;
                            this.m_ControlPoints[this.m_ControlPoints.Length - 1] = controlPoint4;
                            inputDeps = this.SnapControlPoints(inputDeps, default(NativeArray<Entity>));
                            inputDeps = this.UpdateDefinitions(inputDeps, default(NativeArray<Entity>), default(NativeArray<Entity>));
                        }
                        else if (this.m_ControlPoints.Length >= 2)
                        {
                            this.m_ControlPoints[this.m_ControlPoints.Length - 1] = this.m_ControlPoints[this.m_ControlPoints.Length - 2];
                            inputDeps = this.UpdateDefinitions(inputDeps, default(NativeArray<Entity>), default(NativeArray<Entity>));
                        }
                        return inputDeps;
                    }
                case State.Modify:
                    {
                        this.m_ControlPoints.Clear();
                        base.applyMode = ApplyMode.Clear;
                        this.m_State = State.Default;
                        if (this.GetRaycastResult(out var controlPoint3))
                        {
                            this.m_LastRaycastPoint = controlPoint3;
                            this.m_ControlPoints.Add(in controlPoint3);
                            inputDeps = this.SnapControlPoints(inputDeps, default(NativeArray<Entity>));
                            inputDeps = this.UpdateDefinitions(inputDeps, default(NativeArray<Entity>), default(NativeArray<Entity>));
                        }
                        return inputDeps;
                    }
                case State.Remove:
                    {
                        NativeArray<Entity> applyTempAreas = default(NativeArray<Entity>);
                        NativeArray<Entity> applyTempBuildings = default(NativeArray<Entity>);
                        if (this.GetAllowApply() && !this.m_TempAreaQuery.IsEmptyIgnoreFilter)
                        {
                            base.applyMode = ApplyMode.Apply;
                            applyTempAreas = this.m_TempAreaQuery.ToEntityArray(Allocator.TempJob);
                            applyTempBuildings = this.m_TempBuildingQuery.ToEntityArray(Allocator.TempJob);
                        }
                        else
                        {
                            base.applyMode = ApplyMode.Clear;
                        }
                        this.m_State = State.Default;
                        this.m_ControlPoints.Clear();
                        if (this.GetRaycastResult(out var controlPoint))
                        {
                            this.m_LastRaycastPoint = controlPoint;
                            this.m_ControlPoints.Add(in controlPoint);
                            inputDeps = this.SnapControlPoints(inputDeps, applyTempAreas);
                            inputDeps = this.UpdateDefinitions(inputDeps, applyTempAreas, applyTempBuildings);
                        }
                        if (applyTempAreas.IsCreated)
                        {
                            applyTempAreas.Dispose(inputDeps);
                        }
                        if (applyTempBuildings.IsCreated)
                        {
                            applyTempBuildings.Dispose(inputDeps);
                        }
                        return inputDeps;
                    }
                default:
                    return this.Update(inputDeps);
            }
        }

        private JobHandle Apply(JobHandle inputDeps, bool singleFrameOnly = false)
        {
            switch (this.m_State)
            {
                case State.Default:
                    if (this.actualMode == Mode.Generate)
                    {
                        if (this.GetAllowApply() && !this.m_TempAreaQuery.IsEmptyIgnoreFilter)
                        {
                            NativeArray<Entity> applyTempAreas = this.m_TempAreaQuery.ToEntityArray(Allocator.TempJob);
                            base.applyMode = ApplyMode.Apply;
                            this.m_ControlPoints.Clear();
                            if (this.GetRaycastResult(out var controlPoint2))
                            {
                                this.m_LastRaycastPoint = controlPoint2;
                                this.m_ControlPoints.Add(in controlPoint2);
                                this.m_AudioManager.PlayUISound(this.m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PolygonToolDropPointSound);
                                inputDeps = this.SnapControlPoints(inputDeps, applyTempAreas);
                                inputDeps = this.UpdateDefinitions(inputDeps, applyTempAreas, default(NativeArray<Entity>));
                            }
                            if (applyTempAreas.IsCreated)
                            {
                                applyTempAreas.Dispose(inputDeps);
                            }
                            return inputDeps;
                        }
                        return this.Update(inputDeps);
                    }
                    if (this.m_ControlPoints.Length > 0)
                    {
                        base.applyMode = ApplyMode.Clear;
                        ControlPoint value = this.m_ControlPoints[0];
                        if (base.EntityManager.HasComponent<Area>(value.m_OriginalEntity) && math.any(value.m_ElementIndex >= 0) && !singleFrameOnly)
                        {
                            this.m_State = State.Modify;
                            this.m_ControlPointsMoved = false;
                            this.m_MoveStartPositions.Clear();
                            this.m_MoveStartPositions.AddRange(this.m_ControlPoints.AsArray());
                            this.m_ControlPoints.Clear();
                            if (this.GetRaycastResult(out var controlPoint3))
                            {
                                this.m_LastRaycastPoint = controlPoint3;
                                this.m_ControlPoints.Add(in controlPoint3);
                                this.m_AudioManager.PlayUISound(this.m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PolygonToolSelectPointSound);
                                inputDeps = this.SnapControlPoints(inputDeps, default(NativeArray<Entity>));
                                JobHandle.ScheduleBatchedJobs();
                                inputDeps.Complete();
                                ControlPoint other = this.m_ControlPoints[0];
                                if (!this.m_MoveStartPositions[0].Equals(other))
                                {
                                    float minNodeDistance = AreaUtils.GetMinNodeDistance(base.m_PrefabSystem.GetComponentData<AreaGeometryData>(this.prefab));
                                    if (math.distance(this.m_MoveStartPositions[0].m_Position, other.m_Position) < minNodeDistance * 0.5f)
                                    {
                                        this.m_ControlPoints[0] = this.m_MoveStartPositions[0];
                                    }
                                    else
                                    {
                                        this.m_ControlPointsMoved = true;
                                    }
                                }
                                inputDeps = this.UpdateDefinitions(inputDeps, default(NativeArray<Entity>), default(NativeArray<Entity>));
                            }
                            else
                            {
                                this.m_ControlPoints.Add(in value);
                            }
                            return inputDeps;
                        }
                        if (this.GetAllowApply() && !value.Equals(default(ControlPoint)) && this.m_AllowCreateArea)
                        {
                            this.m_State = State.Create;
                            this.m_MoveStartPositions.Clear();
                            this.m_ControlPoints.Clear();
                            this.m_ControlPoints.Add(in value);
                            this.m_AudioManager.PlayUISound(this.m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PolygonToolDropPointSound);
                            if (this.GetRaycastResult(out var controlPoint4))
                            {
                                this.m_LastRaycastPoint = controlPoint4;
                                this.m_ControlPoints.Add(in controlPoint4);
                                inputDeps = this.SnapControlPoints(inputDeps, default(NativeArray<Entity>));
                                inputDeps = this.UpdateDefinitions(inputDeps, default(NativeArray<Entity>), default(NativeArray<Entity>));
                            }
                            else
                            {
                                this.m_ControlPoints.Add(in value);
                            }
                            return inputDeps;
                        }
                        return this.Update(inputDeps);
                    }
                    return this.Update(inputDeps);
                case State.Create:
                    if (this.GetAllowApply() && !this.m_TempAreaQuery.IsEmptyIgnoreFilter)
                    {
                        AreaGeometryData componentData = base.m_PrefabSystem.GetComponentData<AreaGeometryData>(this.prefab);
                        float num = math.distance(this.m_ControlPoints[this.m_ControlPoints.Length - 2].m_Position, this.m_ControlPoints[this.m_ControlPoints.Length - 1].m_Position);
                        float minNodeDistance2 = AreaUtils.GetMinNodeDistance(componentData);
                        if (num >= minNodeDistance2)
                        {
                            bool flag = true;
                            NativeArray<Area> nativeArray = this.m_TempAreaQuery.ToComponentDataArray<Area>(Allocator.TempJob);
                            for (int i = 0; i < nativeArray.Length; i++)
                            {
                                flag &= (nativeArray[i].m_Flags & AreaFlags.Complete) != 0;
                            }
                            nativeArray.Dispose();
                            NativeArray<Entity> applyTempAreas2 = default(NativeArray<Entity>);
                            if (flag)
                            {
                                this.m_AudioManager.PlayUISound(this.m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PolygonToolFinishAreaSound);
                                base.applyMode = ApplyMode.Apply;
                                this.m_State = State.Default;
                                this.m_ControlPoints.Clear();
                                if (this.recreate != Entity.Null)
                                {
                                    if (this.m_ObjectToolSystem.mode == ObjectToolSystem.Mode.Move)
                                    {
                                        base.m_ToolSystem.activeTool = base.m_DefaultToolSystem;
                                    }
                                    else
                                    {
                                        base.m_ToolSystem.activeTool = this.m_ObjectToolSystem;
                                    }
                                    return inputDeps;
                                }
                                applyTempAreas2 = this.m_TempAreaQuery.ToEntityArray(Allocator.TempJob);
                            }
                            else
                            {
                                this.m_AudioManager.PlayUISound(this.m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PolygonToolDropPointSound);
                                base.applyMode = ApplyMode.Clear;
                            }
                            if (this.GetRaycastResult(out var controlPoint5))
                            {
                                this.m_LastRaycastPoint = controlPoint5;
                                this.m_ControlPoints.Add(in controlPoint5);
                                inputDeps = this.SnapControlPoints(inputDeps, applyTempAreas2);
                                inputDeps = this.UpdateDefinitions(inputDeps, applyTempAreas2, default(NativeArray<Entity>));
                            }
                            if (applyTempAreas2.IsCreated)
                            {
                                applyTempAreas2.Dispose(inputDeps);
                            }
                            return inputDeps;
                        }
                    }
                    return this.Update(inputDeps);
                case State.Modify:
                    {
                        if (!this.m_ControlPointsMoved && this.GetAllowApply() && this.m_ControlPoints.Length > 0)
                        {
                            if (this.m_AllowCreateArea)
                            {
                                ControlPoint value2 = this.m_ControlPoints[0];
                                base.applyMode = ApplyMode.Clear;
                                this.m_State = State.Create;
                                this.m_MoveStartPositions.Clear();
                                this.m_ControlPoints.Clear();
                                this.m_ControlPoints.Add(in value2);
                                this.m_AudioManager.PlayUISound(this.m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PolygonToolDropPointSound);
                                if (this.GetRaycastResult(out var controlPoint6))
                                {
                                    this.m_LastRaycastPoint = controlPoint6;
                                    this.m_ControlPoints.Add(in controlPoint6);
                                    inputDeps = this.SnapControlPoints(inputDeps, default(NativeArray<Entity>));
                                    inputDeps = this.UpdateDefinitions(inputDeps, default(NativeArray<Entity>), default(NativeArray<Entity>));
                                }
                                else
                                {
                                    this.m_ControlPoints.Add(in value2);
                                }
                                return inputDeps;
                            }
                            base.applyMode = ApplyMode.Clear;
                            this.m_State = State.Default;
                            this.m_ControlPoints.Clear();
                            if (this.GetRaycastResult(out var controlPoint7))
                            {
                                this.m_LastRaycastPoint = controlPoint7;
                                this.m_ControlPoints.Add(in controlPoint7);
                                this.m_AudioManager.PlayUISound(this.m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PolygonToolDropPointSound);
                                inputDeps = this.SnapControlPoints(inputDeps, default(NativeArray<Entity>));
                                inputDeps = this.UpdateDefinitions(inputDeps, default(NativeArray<Entity>), default(NativeArray<Entity>));
                            }
                            return inputDeps;
                        }
                        NativeArray<Entity> applyTempAreas3 = default(NativeArray<Entity>);
                        NativeArray<Entity> applyTempBuildings = default(NativeArray<Entity>);
                        if (this.GetAllowApply() && !this.m_TempAreaQuery.IsEmptyIgnoreFilter)
                        {
                            base.applyMode = ApplyMode.Apply;
                            applyTempAreas3 = this.m_TempAreaQuery.ToEntityArray(Allocator.TempJob);
                            applyTempBuildings = this.m_TempBuildingQuery.ToEntityArray(Allocator.TempJob);
                        }
                        else
                        {
                            base.applyMode = ApplyMode.Clear;
                        }
                        this.m_State = State.Default;
                        this.m_ControlPoints.Clear();
                        if (this.GetRaycastResult(out var controlPoint8))
                        {
                            this.m_LastRaycastPoint = controlPoint8;
                            this.m_ControlPoints.Add(in controlPoint8);
                            this.m_AudioManager.PlayUISound(this.m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PolygonToolDropPointSound);
                            inputDeps = this.SnapControlPoints(inputDeps, applyTempAreas3);
                            inputDeps = this.UpdateDefinitions(inputDeps, applyTempAreas3, applyTempBuildings);
                        }
                        if (applyTempAreas3.IsCreated)
                        {
                            applyTempAreas3.Dispose(inputDeps);
                        }
                        if (applyTempBuildings.IsCreated)
                        {
                            applyTempBuildings.Dispose(inputDeps);
                        }
                        return inputDeps;
                    }
                case State.Remove:
                    {
                        this.m_ControlPoints.Clear();
                        base.applyMode = ApplyMode.Clear;
                        this.m_State = State.Default;
                        if (this.GetRaycastResult(out var controlPoint))
                        {
                            this.m_LastRaycastPoint = controlPoint;
                            this.m_ControlPoints.Add(in controlPoint);
                            this.m_AudioManager.PlayUISound(this.m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_PolygonToolRemovePointSound);
                            inputDeps = this.SnapControlPoints(inputDeps, default(NativeArray<Entity>));
                            inputDeps = this.UpdateDefinitions(inputDeps, default(NativeArray<Entity>), default(NativeArray<Entity>));
                        }
                        return inputDeps;
                    }
                default:
                    return this.Update(inputDeps);
            }
        }

        private JobHandle Update(JobHandle inputDeps)
        {
            if (this.GetRaycastResult(out ControlPoint controlPoint, out bool forceUpdate))
            {
                if (this.m_ControlPoints.Length == 0)
                {
                    this.m_LastRaycastPoint = controlPoint;
                    this.m_ControlPoints.Add(in controlPoint);
                    inputDeps = this.SnapControlPoints(inputDeps, default(NativeArray<Entity>));
                    base.applyMode = ApplyMode.Clear;
                    return this.UpdateDefinitions(inputDeps, default(NativeArray<Entity>), default(NativeArray<Entity>));
                }
                if (this.m_LastRaycastPoint.Equals(controlPoint) && !forceUpdate)
                {
                    base.applyMode = ApplyMode.None;
                    return inputDeps;
                }
                this.m_LastRaycastPoint = controlPoint;
                int index = math.select(0, this.m_ControlPoints.Length - 1, this.m_State == State.Create);
                ControlPoint value = this.m_ControlPoints[index];
                this.m_ControlPoints[index] = controlPoint;
                inputDeps = this.SnapControlPoints(inputDeps, default(NativeArray<Entity>));
                JobHandle.ScheduleBatchedJobs();
                inputDeps.Complete();
                ControlPoint other = this.m_ControlPoints[index];
                if (value.EqualsIgnoreHit(other))
                {
                    base.applyMode = ApplyMode.None;
                }
                else
                {
                    float minNodeDistance = AreaUtils.GetMinNodeDistance(base.m_PrefabSystem.GetComponentData<AreaGeometryData>(this.prefab));
                    if (this.m_State == State.Modify && !this.m_ControlPointsMoved && math.distance(value.m_Position, other.m_Position) < minNodeDistance * 0.5f)
                    {
                        this.m_ControlPoints[index] = value;
                        base.applyMode = ApplyMode.None;
                    }
                    else
                    {
                        this.m_ControlPointsMoved = true;
                        base.applyMode = ApplyMode.Clear;
                        inputDeps = this.UpdateDefinitions(inputDeps, default(NativeArray<Entity>), default(NativeArray<Entity>));
                    }
                }
                return inputDeps;
            }
            if (this.m_LastRaycastPoint.Equals(controlPoint))
            {
                if (forceUpdate)
                {
                    base.applyMode = ApplyMode.Clear;
                    return this.UpdateDefinitions(inputDeps, default(NativeArray<Entity>), default(NativeArray<Entity>));
                }
                base.applyMode = ApplyMode.None;
                return inputDeps;
            }
            this.m_LastRaycastPoint = controlPoint;
            if (this.m_State == State.Default && this.m_ControlPoints.Length >= 1)
            {
                base.applyMode = ApplyMode.Clear;
                this.m_ControlPoints.Clear();
                ref NativeList<ControlPoint> controlPoints = ref this.m_ControlPoints;
                ControlPoint value2 = default(ControlPoint);
                controlPoints.Add(in value2);
                return this.UpdateDefinitions(inputDeps, default(NativeArray<Entity>), default(NativeArray<Entity>));
            }
            if (this.m_State == State.Modify && this.m_ControlPoints.Length >= 1)
            {
                this.m_ControlPointsMoved = true;
                base.applyMode = ApplyMode.Clear;
                this.m_ControlPoints[0] = this.m_MoveStartPositions[0];
                return this.UpdateDefinitions(inputDeps, default(NativeArray<Entity>), default(NativeArray<Entity>));
            }
            if (this.m_State == State.Remove && this.m_ControlPoints.Length >= 1)
            {
                this.m_ControlPointsMoved = true;
                base.applyMode = ApplyMode.Clear;
                this.m_ControlPoints[0] = this.m_MoveStartPositions[0];
                return this.UpdateDefinitions(inputDeps, default(NativeArray<Entity>), default(NativeArray<Entity>));
            }
            if (this.m_ControlPoints.Length >= 2)
            {
                this.m_ControlPointsMoved = true;
                base.applyMode = ApplyMode.Clear;
                this.m_ControlPoints[this.m_ControlPoints.Length - 1] = this.m_ControlPoints[this.m_ControlPoints.Length - 2];
                return this.UpdateDefinitions(inputDeps, default(NativeArray<Entity>), default(NativeArray<Entity>));
            }
            return inputDeps;
        }

        private JobHandle SnapControlPoints(JobHandle inputDeps, NativeArray<Entity> applyTempAreas)
        {
            this.__TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Areas_Triangle_RO_BufferLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Areas_Node_RO_BufferLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Areas_Lot_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_AssetStampData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_BuildingExtensionData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Net_Composition_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Net_EndNodeGeometry_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Net_StartNodeGeometry_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Net_Curve_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_Building_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Common_Owner_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Tools_Temp_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_AreaGeometryData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            SnapJob snapJob = default(SnapJob);
            snapJob.m_AllowCreateArea = this.m_AllowCreateArea;
            snapJob.m_ControlPointsMoved = this.m_ControlPointsMoved;
            snapJob.m_EditorMode = base.m_ToolSystem.actionMode.IsEditor();
            snapJob.m_Snap = base.GetActualSnap();
            snapJob.m_State = this.m_State;
            snapJob.m_Prefab = base.m_PrefabSystem.GetEntity(this.prefab);
            snapJob.m_ApplyTempAreas = applyTempAreas;
            snapJob.m_MoveStartPositions = this.m_MoveStartPositions;
            snapJob.m_PrefabRefData = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup;
            snapJob.m_PrefabAreaData = this.__TypeHandle.__Game_Prefabs_AreaGeometryData_RO_ComponentLookup;
            snapJob.m_TempData = this.__TypeHandle.__Game_Tools_Temp_RO_ComponentLookup;
            snapJob.m_OwnerData = this.__TypeHandle.__Game_Common_Owner_RO_ComponentLookup;
            snapJob.m_BuildingData = this.__TypeHandle.__Game_Buildings_Building_RO_ComponentLookup;
            snapJob.m_CurveData = this.__TypeHandle.__Game_Net_Curve_RO_ComponentLookup;
            snapJob.m_EdgeGeometryData = this.__TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup;
            snapJob.m_StartGeometryData = this.__TypeHandle.__Game_Net_StartNodeGeometry_RO_ComponentLookup;
            snapJob.m_EndGeometryData = this.__TypeHandle.__Game_Net_EndNodeGeometry_RO_ComponentLookup;
            snapJob.m_CompositionData = this.__TypeHandle.__Game_Net_Composition_RO_ComponentLookup;
            snapJob.m_TransformData = this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup;
            snapJob.m_PrefabBuildingData = this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup;
            snapJob.m_BuildingExtensionData = this.__TypeHandle.__Game_Prefabs_BuildingExtensionData_RO_ComponentLookup;
            snapJob.m_AssetStampData = this.__TypeHandle.__Game_Prefabs_AssetStampData_RO_ComponentLookup;
            snapJob.m_ObjectGeometryData = this.__TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;
            snapJob.m_PrefabCompositionData = this.__TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup;
            snapJob.m_LotData = this.__TypeHandle.__Game_Areas_Lot_RO_ComponentLookup;
            snapJob.m_Nodes = this.__TypeHandle.__Game_Areas_Node_RO_BufferLookup;
            snapJob.m_Triangles = this.__TypeHandle.__Game_Areas_Triangle_RO_BufferLookup;
            snapJob.m_InstalledUpgrades = this.__TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup;
            snapJob.m_AreaSearchTree = this.m_AreaSearchSystem.GetSearchTree(readOnly: true, out var dependencies);
            snapJob.m_NetSearchTree = this.m_NetSearchSystem.GetNetSearchTree(readOnly: true, out var dependencies2);
            snapJob.m_ObjectSearchTree = this.m_ObjectSearchSystem.GetStaticSearchTree(readOnly: true, out var dependencies3);
            snapJob.m_ControlPoints = this.m_ControlPoints;
            SnapJob jobData = snapJob;
            inputDeps = JobHandle.CombineDependencies(inputDeps, JobHandle.CombineDependencies(dependencies, dependencies2, dependencies3));
            JobHandle jobHandle = IJobExtensions.Schedule(jobData, inputDeps);
            this.m_AreaSearchSystem.AddSearchTreeReader(jobHandle);
            this.m_NetSearchSystem.AddNetSearchTreeReader(jobHandle);
            this.m_ObjectSearchSystem.AddStaticSearchTreeReader(jobHandle);
            return jobHandle;
        }

        private JobHandle UpdateDefinitions(JobHandle inputDeps, NativeArray<Entity> applyTempAreas, NativeArray<Entity> applyTempBuildings)
        {
            JobHandle jobHandle = base.DestroyDefinitions(this.m_DefinitionQuery, this.m_ToolOutputBarrier, inputDeps);
            if (this.prefab != null)
            {
                if (this.mode == Mode.Generate)
                {
                    this.__TypeHandle.__Game_Tools_LocalNodeCache_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
                    this.__TypeHandle.__Game_Areas_Node_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
                    this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
                    RemoveMapTilesJob jobData = default(RemoveMapTilesJob);
                    jobData.m_EntityType = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle;
                    jobData.m_NodeType = this.__TypeHandle.__Game_Areas_Node_RO_BufferTypeHandle;
                    jobData.m_CacheType = this.__TypeHandle.__Game_Tools_LocalNodeCache_RO_BufferTypeHandle;
                    jobData.m_ControlPoints = this.m_ControlPoints;
                    jobData.m_CommandBuffer = this.m_ToolOutputBarrier.CreateCommandBuffer().AsParallelWriter();
                    JobHandle jobHandle2 = JobChunkExtensions.ScheduleParallel(jobData, this.m_MapTileQuery, inputDeps);
                    this.m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle2);
                    jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle2);
                }
                this.__TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Objects_SubObject_RO_BufferLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Net_SubNet_RO_BufferLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Tools_LocalNodeCache_RO_BufferLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Areas_SubArea_RO_BufferLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Areas_Triangle_RO_BufferLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Areas_Node_RO_BufferLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Prefabs_AreaGeometryData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Buildings_Building_RO_ComponentLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Tools_LocalTransformCache_RO_ComponentLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Tools_EditorContainer_RO_ComponentLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Net_Elevation_RO_ComponentLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Net_Curve_RO_ComponentLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Net_Edge_RO_ComponentLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Net_Node_RO_ComponentLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Areas_Area_RO_ComponentLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Areas_Space_RO_ComponentLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Areas_Clear_RO_ComponentLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Common_Owner_RO_ComponentLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Tools_Temp_RO_ComponentLookup.Update(ref base.CheckedStateRef);
                CreateDefinitionsJob jobData2 = default(CreateDefinitionsJob);
                jobData2.m_AllowCreateArea = this.m_AllowCreateArea;
                jobData2.m_EditorMode = base.m_ToolSystem.actionMode.IsEditor();
                jobData2.m_Mode = this.actualMode;
                jobData2.m_State = this.m_State;
                jobData2.m_Prefab = base.m_PrefabSystem.GetEntity(this.prefab);
                jobData2.m_Recreate = this.recreate;
                jobData2.m_ApplyTempAreas = applyTempAreas;
                jobData2.m_ApplyTempBuildings = applyTempBuildings;
                jobData2.m_MoveStartPositions = this.m_MoveStartPositions;
                jobData2.m_TempData = this.__TypeHandle.__Game_Tools_Temp_RO_ComponentLookup;
                jobData2.m_OwnerData = this.__TypeHandle.__Game_Common_Owner_RO_ComponentLookup;
                jobData2.m_ClearData = this.__TypeHandle.__Game_Areas_Clear_RO_ComponentLookup;
                jobData2.m_SpaceData = this.__TypeHandle.__Game_Areas_Space_RO_ComponentLookup;
                jobData2.m_AreaData = this.__TypeHandle.__Game_Areas_Area_RO_ComponentLookup;
                jobData2.m_NodeData = this.__TypeHandle.__Game_Net_Node_RO_ComponentLookup;
                jobData2.m_EdgeData = this.__TypeHandle.__Game_Net_Edge_RO_ComponentLookup;
                jobData2.m_CurveData = this.__TypeHandle.__Game_Net_Curve_RO_ComponentLookup;
                jobData2.m_NetElevationData = this.__TypeHandle.__Game_Net_Elevation_RO_ComponentLookup;
                jobData2.m_EditorContainerData = this.__TypeHandle.__Game_Tools_EditorContainer_RO_ComponentLookup;
                jobData2.m_LocalTransformCacheData = this.__TypeHandle.__Game_Tools_LocalTransformCache_RO_ComponentLookup;
                jobData2.m_TransformData = this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup;
                jobData2.m_BuildingData = this.__TypeHandle.__Game_Buildings_Building_RO_ComponentLookup;
                jobData2.m_PrefabRefData = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup;
                jobData2.m_PrefabAreaData = this.__TypeHandle.__Game_Prefabs_AreaGeometryData_RO_ComponentLookup;
                jobData2.m_PrefabObjectGeometryData = this.__TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;
                jobData2.m_Nodes = this.__TypeHandle.__Game_Areas_Node_RO_BufferLookup;
                jobData2.m_Triangles = this.__TypeHandle.__Game_Areas_Triangle_RO_BufferLookup;
                jobData2.m_SubAreas = this.__TypeHandle.__Game_Areas_SubArea_RO_BufferLookup;
                jobData2.m_CachedNodes = this.__TypeHandle.__Game_Tools_LocalNodeCache_RO_BufferLookup;
                jobData2.m_SubNets = this.__TypeHandle.__Game_Net_SubNet_RO_BufferLookup;
                jobData2.m_ConnectedEdges = this.__TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup;
                jobData2.m_SubObjects = this.__TypeHandle.__Game_Objects_SubObject_RO_BufferLookup;
                jobData2.m_InstalledUpgrades = this.__TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup;
                jobData2.m_ControlPoints = this.m_ControlPoints;
                jobData2.m_Tooltip = this.m_Tooltip;
                jobData2.m_CommandBuffer = this.m_ToolOutputBarrier.CreateCommandBuffer();
                JobHandle jobHandle3 = IJobExtensions.Schedule(jobData2, inputDeps);
                this.m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle3);
                jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle3);
            }
            return jobHandle;
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
        public AreaToolSystem()
        {
        }
    }

}

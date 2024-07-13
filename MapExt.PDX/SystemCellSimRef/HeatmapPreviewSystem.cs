using Game.Prefabs;
using Game.Rendering;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;
using Game.Tools;
using Game;

namespace MapExt.Systems
{
	public partial class HeatmapPreviewSystem : GameSystemBase
	{
		private TelecomPreviewSystem m_TelecomPreviewSystem;

		private EntityQuery m_InfomodeQuery;

		private ComponentSystemBase m_LastPreviewSystem;

		[Preserve]
		protected override void OnCreate()
		{
			base.OnCreate();
			this.m_TelecomPreviewSystem = base.World.GetOrCreateSystemManaged<TelecomPreviewSystem>();
			this.m_InfomodeQuery = base.GetEntityQuery(ComponentType.ReadOnly<InfomodeActive>(), ComponentType.ReadOnly<InfoviewHeatmapData>());
			base.RequireForUpdate(this.m_InfomodeQuery);
		}

		[Preserve]
		protected override void OnStopRunning()
		{
			if (this.m_LastPreviewSystem != null)
			{
				this.m_LastPreviewSystem.Enabled = false;
				this.m_LastPreviewSystem.Update();
				this.m_LastPreviewSystem = null;
			}
			base.OnStopRunning();
		}

		private ComponentSystemBase GetPreviewSystem()
		{
			if (this.m_InfomodeQuery.IsEmptyIgnoreFilter)
			{
				return null;
			}
			NativeArray<InfoviewHeatmapData> nativeArray = this.m_InfomodeQuery.ToComponentDataArray<InfoviewHeatmapData>(Allocator.TempJob);
			try
			{
				for (int i = 0; i < nativeArray.Length; i++)
				{
					if (nativeArray[i].m_Type == HeatmapData.TelecomCoverage)
					{
						return this.m_TelecomPreviewSystem;
					}
				}
			}
			finally
			{
				nativeArray.Dispose();
			}
			return null;
		}

		[Preserve]
		protected override void OnUpdate()
		{
			ComponentSystemBase previewSystem = this.GetPreviewSystem();
			if (previewSystem != this.m_LastPreviewSystem)
			{
				if (this.m_LastPreviewSystem != null)
				{
					this.m_LastPreviewSystem.Enabled = false;
					this.m_LastPreviewSystem.Update();
				}
				this.m_LastPreviewSystem = previewSystem;
				if (this.m_LastPreviewSystem != null)
				{
					this.m_LastPreviewSystem.Enabled = true;
				}
			}
			previewSystem?.Update();
		}

		[Preserve]
		public HeatmapPreviewSystem()
		{
		}
	}
}

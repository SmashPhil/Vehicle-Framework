using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class GraphicDataLayered : GraphicData
	{
		public const int SubLayerCount = 10;

		protected int layer = 0;

		public Vector3? OriginalDrawOffset { get; private set; }

		public Vector3? OriginalDrawOffsetNorth { get; private set; }
		public Vector3? OriginalDrawOffsetEast { get; private set; }
		public Vector3? OriginalDrawOffsetSouth { get; private set; }
		public Vector3? OriginalDrawOffsetWest { get; private set; }

		public bool AboveBody => layer >= 0;

		public GraphicDataLayered() : base()
		{
		}

		public virtual void CopyFrom(GraphicDataLayered graphicData)
		{
			base.CopyFrom(graphicData);
			layer = graphicData.layer;
			ResetDrawOffsetCache();
		}

		public virtual void Init(IMaterialCacheTarget target)
		{
			RecacheLayerOffsets();
		}

		private void ResetDrawOffsetCache()
		{
			OriginalDrawOffset = null;
			OriginalDrawOffsetNorth = null;
			OriginalDrawOffsetEast = null;
			OriginalDrawOffsetSouth = null;
			OriginalDrawOffsetWest = null;

			RecacheLayerOffsets();
		}

		public void RecacheLayerOffsets()
		{
			OriginalDrawOffset ??= drawOffset;
			OriginalDrawOffsetNorth ??= drawOffsetNorth;
			OriginalDrawOffsetEast ??= drawOffsetEast;
			OriginalDrawOffsetSouth ??= drawOffsetSouth;
			OriginalDrawOffsetWest ??= drawOffsetWest;

			float layerOffset = layer * (Altitudes.AltInc / SubLayerCount);

			drawOffset = OriginalDrawOffset.Value;
			drawOffset.y += layerOffset;

			if (drawOffsetNorth != null)
			{
				drawOffsetNorth = OriginalDrawOffsetNorth.Value;
				drawOffsetNorth = new Vector3(drawOffsetNorth.Value.x, drawOffsetNorth.Value.y + layerOffset, drawOffsetNorth.Value.z);
			}

			if (drawOffsetEast != null)
			{
				drawOffsetEast = OriginalDrawOffsetEast.Value;
				drawOffsetEast = new Vector3(drawOffsetEast.Value.x, drawOffsetEast.Value.y + layerOffset, drawOffsetEast.Value.z);
			}

			if (drawOffsetSouth != null)
			{
				drawOffsetSouth = OriginalDrawOffsetSouth.Value;
				drawOffsetSouth = new Vector3(drawOffsetSouth.Value.x, drawOffsetSouth.Value.y + layerOffset, drawOffsetSouth.Value.z);
			}

			if (drawOffsetWest != null)
			{
				drawOffsetWest = OriginalDrawOffsetWest.Value;
				drawOffsetWest = new Vector3(drawOffsetWest.Value.x, drawOffsetWest.Value.y + layerOffset, drawOffsetWest.Value.z);
			}
		}
	}
}

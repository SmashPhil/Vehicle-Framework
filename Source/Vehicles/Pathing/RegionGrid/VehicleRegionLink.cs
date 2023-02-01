using System.Linq;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Verse;
using UnityEngine;
using SmashTools;

namespace Vehicles
{
	/// <summary>
	/// Link between regions for reachability determination
	/// </summary>
	public class VehicleRegionLink
	{
		private const float WeightColorCeiling = 400;

		public VehicleRegion[] regions = new VehicleRegion[2];

		private EdgeSpan span;

		public IntVec3 anchor;
		//public IntVec3[] anchors = new IntVec3[3];

		private static readonly LinearPool<SimpleColor> colorWeights = new LinearPool<SimpleColor>
		{
			range = new FloatRange(0, WeightColorCeiling),
			items = new List<SimpleColor>()
			{
				SimpleColor.Green,
				SimpleColor.Yellow,
				SimpleColor.Orange,
				SimpleColor.Red,
				SimpleColor.Magenta
			}
		};

		/// <summary>
		/// Region A of link
		/// </summary>
		public VehicleRegion RegionA
		{
			get
			{
				return regions[0];
			}
			set
			{
				regions[0] = value;
			}
		}

		/// <summary>
		/// Region B of link
		/// </summary>
		public VehicleRegion RegionB
		{
			get
			{
				return regions[1];
			}
			set
			{
				regions[1] = value;
			}
		}

		public EdgeSpan Span
		{
			get
			{
				return span;
			}
			set
			{
				span = value;
				ResetAnchors();
			}
		}

		/// <summary>
		/// Register region for region linking
		/// </summary>
		/// <param name="reg"></param>
		public void Register(VehicleRegion reg)
		{
			if (regions[0] == reg || regions[1] == reg)
			{
				Log.Error($"Tried to double-register vehicle region {reg} in {this}");
				return;
			}
			if (RegionA is null || !RegionA.valid)
			{
				RegionA = reg;	
			}
			else if (RegionB is null || !RegionB.valid)
			{
				RegionB = reg;
			}
		}

		/// <summary>
		/// Deregister and recache region for region linking
		/// </summary>
		/// <param name="reg"></param>
		public void Deregister(VehicleRegion region, VehicleDef vehicleDef)
		{
			if (RegionA == region)
			{
				RegionA = null;
				if (RegionB is null)
				{
					region.Map.GetCachedMapComponent<VehicleMapping>()[vehicleDef].VehicleRegionLinkDatabase.Notify_LinkHasNoRegions(this);
				}
			}
			else if (RegionB == region)
			{
				RegionB = null;
				if (RegionA is null)
				{
					region.Map.GetCachedMapComponent<VehicleMapping>()[vehicleDef].VehicleRegionLinkDatabase.Notify_LinkHasNoRegions(this);
				}
			}
		}

		public void ResetAnchors()
		{
			//anchors[0] = Span.root;
			anchor = VehicleRegionCostCalculator.RegionLinkCenter(this);
			//anchors[2] = CellInSpan(Span, Span.length - 1);
		}

		public void DrawWeight(VehicleRegionLink regionLink, float weight)
		{
			Vector3 from = anchor.ToVector3();
			from.y += AltitudeLayer.MapDataOverlay.AltitudeFor();
			Vector3 to = regionLink.anchor.ToVector3();
			to.y += AltitudeLayer.MapDataOverlay.AltitudeFor();
			GenDraw.DrawLineBetween(from, to, WeightColor(weight));
		}

		private static IntVec3 CellInSpan(EdgeSpan span, int length)
		{
			if (span.dir == SpanDirection.North)
			{
				return new IntVec3(span.root.x, 0, span.root.z + length);
			}
			return new IntVec3(span.root.x + length, 0, span.root.z);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static SimpleColor WeightColor(float weight)
		{
			return colorWeights.Evaluate(weight);
		}

		/// <summary>
		/// Get opposite region linking to <paramref name="region"/>
		/// </summary>
		/// <param name="reg"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public VehicleRegion GetOtherRegion(VehicleRegion region)
		{
			return (region != RegionA) ? RegionA : RegionB;
		}

		public VehicleRegion GetInFacingRegion(VehicleRegionLink regionLink)
		{
			if (RegionA == regionLink.RegionA || RegionA == regionLink.RegionB) return RegionA;
			if (RegionB == regionLink.RegionB || RegionB == regionLink.RegionA) return RegionB;
			Log.Warning($"Attempting to fetch region between links {anchor} and {regionLink.anchor}, but they do not share a region.\n--- Regions ---\n{RegionA}\n{RegionB}\n{regionLink.RegionA}\n{regionLink.RegionB}\n");
			return null;
		}

		/// <summary>
		/// Hashcode for cache data
		/// </summary>
		public ulong UniqueHashCode()
		{
			return span.UniqueHashCode();
		}

		/// <summary>
		/// String output with data
		/// </summary>
		public override string ToString()
		{
			return $"({regions.Where(region => region != null).Select(region => region.id.ToString()).ToCommaList(false)}, regions=[spawn={span}, hash={UniqueHashCode()}])";
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;

namespace Vehicles
{
	public class StashedVehicle : DynamicDrawnWorldObject, IThingHolder, ILoadReferenceable
	{
		public ThingOwner<Thing> stash = new ThingOwner<Thing>();

		private static readonly Dictionary<VehicleDef, int> vehicleCounts = new Dictionary<VehicleDef, int>();

		private Material cachedMaterial;
		private float transitionSize;

		public List<VehiclePawn> Vehicles { get; private set; }

		public override Material Material
		{
			get
			{
				if (cachedMaterial is null)
				{
					Color color;
					if (Faction != null)
					{
						color = Faction.Color;
					}
					else
					{
						color = Color.white;
					}
					VehiclePawn largestVehicle = Vehicles.MaxBy(vehicle => vehicle.VehicleDef.Size.Magnitude);
					string texPath = VehicleTex.CachedTextureIconPaths.TryGetValue(largestVehicle.VehicleDef, VehicleTex.DefaultVehicleIconTexPath);
					cachedMaterial = MaterialPool.MatFrom(texPath, ShaderDatabase.WorldOverlayTransparentLit, color, WorldMaterials.WorldObjectRenderQueue);
				}
				return cachedMaterial;
			}
		}

		public override void Draw()
		{
			if (!this.HiddenBehindTerrainNow())
			{
				float averageTileSize = Find.WorldGrid.averageTileSize;
				float transitionPct = ExpandableWorldObjectsUtility.TransitionPct;

				float drawPct = (1 + (transitionPct * Find.WorldCameraDriver.AltitudePercent * ExpandingResize)) * transitionSize;
				
				if (VehicleMod.settings.main.dynamicWorldDrawing && transitionPct <= 0)
				{
					//TODO - Rework when dynamic drawing is fixed, currently permanently disabled so this code will never be reached
					Vector3 normalized = DrawPos.normalized;
					Vector3 direction = Vector3.Cross(normalized, Vector3.down);
					Quaternion quat = Quaternion.LookRotation(direction, normalized) * Quaternion.Euler(0f, 90f, 0f);
					Vector3 size = new Vector3(averageTileSize * 0.7f * drawPct, 1, averageTileSize * 0.7f * drawPct);

					Matrix4x4 matrix = default;
					matrix.SetTRS(DrawPos + normalized, quat, size);
					//Graphics.DrawMesh(MeshPool.plane10, matrix, VehicleMat, WorldCameraManager.WorldLayer);
					//RenderGraphicOverlays(normalized, direction, size);
				}
				else
				{
					WorldHelper.DrawQuadTangentialToPlanet(DrawPos, 0.7f * averageTileSize, 0.015f, Material);
				}
			}
		}

		public void Notify_CaravanArrived(Caravan caravan)
		{
			if (caravan is VehicleCaravan vehicleCaravan)
			{
				if (vehicleCaravan.AerialVehicle || Vehicles.Any(vehicle => !vehicle.VehicleDef.canCaravan))
				{
					Messages.Message($"Unable to retrieve vehicle, aerial vehicles can't merge with other vehicle caravans.", MessageTypeDefOf.RejectInput, historical: false);
					return;
				}
			}
			
			List<Pawn> pawns = caravan.pawns.InnerListForReading.ToList(); //Copy list separately
			Log.Message($"Adding Pawns: {string.Join(", ", pawns.Select(pawn => pawn))}");
			caravan.RemoveAllPawns();

			List<Thing> stash = this.stash.InnerListForReading.ToList();
			this.stash.Clear();
			for (int i = stash.Count - 1; i >= 0; i--)
			{
				if (stash[i] is Pawn pawn)
				{
					stash.RemoveAt(i);
					pawns.Add(pawn);
					Log.Message($"Pawns in vehicle: {pawn}");
					foreach (Pawn p2 in ((VehiclePawn)pawn).AllPawnsAboard)
					{
						Log.Message($"Pawn: {p2}");
					}
				}
			}
			VehicleCaravan mergedCaravan = CaravanHelper.MakeVehicleCaravan(pawns, caravan.Faction, caravan.Tile, true);

			for (int i = stash.Count - 1; i >= 0; i--)
			{
				mergedCaravan.AddPawnOrItem(stash[i], true);
				Log.Message($"Adding: {stash[i]}");
			}

			Destroy();
			caravan.Destroy();
		}

		public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
		{
			foreach (FloatMenuOption floatMenuOption in base.GetFloatMenuOptions(caravan))
			{
				yield return floatMenuOption;
			}
			foreach (FloatMenuOption floatMenuOption in CaravanArrivalAction_StashedVehicle.GetFloatMenuOptions(caravan, this))
			{
				yield return floatMenuOption;
			}
		}

		public override string GetInspectString()
		{
			StringBuilder stringBuilder = new StringBuilder();

			vehicleCounts.Clear();
			{
				foreach (VehiclePawn vehicle in Vehicles)
				{
					if (vehicleCounts.ContainsKey(vehicle.VehicleDef))
					{
						vehicleCounts[vehicle.VehicleDef]++;
					}
					else
					{
						vehicleCounts[vehicle.VehicleDef] = 1;
					}
				}

				foreach ((VehicleDef vehicleDef, int count) in vehicleCounts)
				{
					stringBuilder.AppendLine($"{count} {vehicleDef.LabelCap}");
				}
			}
			vehicleCounts.Clear();

			stringBuilder.Append(base.GetInspectString());
			return stringBuilder.ToString();
		}

		private void RecacheVehicles()
		{
			Vehicles = stash.InnerListForReading.Where(thing => thing is VehiclePawn).Cast<VehiclePawn>().ToList();
		}

		public override void SpawnSetup()
		{
			base.SpawnSetup();
			RecacheVehicles();
		}

		public override void ExposeData()
		{
			base.ExposeData();

			Scribe_Deep.Look(ref stash, nameof(stash), new object[] { this });
		}

		public void GetChildHolders(List<IThingHolder> outChildren)
		{
			ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
		}

		public ThingOwner GetDirectlyHeldThings()
		{
			return stash;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using SmashTools;
using static SmashTools.Debug;

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

		public VehicleCaravan Notify_CaravanArrived(Caravan caravan)
		{
			if (caravan is VehicleCaravan vehicleCaravan)
			{
				if (vehicleCaravan.AerialVehicle || Vehicles.Any(vehicle => !vehicle.VehicleDef.canCaravan))
				{
					Messages.Message($"Unable to retrieve vehicle, aerial vehicles can't merge with other vehicle caravans.", MessageTypeDefOf.RejectInput, historical: false);
					return null;
				}
			}

			// Use separate list, caravan must relinquish ownership of pawns in order to add them to a new caravan
			List<Pawn> pawns = caravan.pawns.InnerListForReading.ToList();
			caravan.RemoveAllPawns();

			List<VehiclePawn> vehicles = new List<VehiclePawn>();
			foreach (Thing thing in stash.InnerListForReading.ToList())
			{
				if (thing is VehiclePawn vehicle)
				{
					stash.Remove(thing);
					vehicles.Add(vehicle);
				}
			}
			RoleHelper.Distribute(vehicles, pawns);
			// Pawns that were distributed between vehicles will not be part of the formation, but are
			// instead nested within the vehicle. Any remaining dismounted pawns + vehicles must be joined
			pawns.AddRange(vehicles);

			VehicleCaravan mergedCaravan = CaravanHelper.MakeVehicleCaravan(pawns, caravan.Faction, caravan.Tile, true);

			for (int i = stash.Count - 1; i >= 0; i--)
			{
				mergedCaravan.AddPawnOrItem(stash[i], true);
			}

			Destroy();

			caravan.Destroy();

			return mergedCaravan;
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

		public static StashedVehicle Create(VehicleCaravan vehicleCaravan, out Caravan caravan, List<TransferableOneWay> transferables = null)
		{
			caravan = null;
			if (vehicleCaravan.VehiclesListForReading.NullOrEmpty())
			{
				Log.Error("No vehicles in vehicle caravan for stashed vehicle.");
				return null;
			}

			StashedVehicle stashedVehicle = (StashedVehicle)WorldObjectMaker.MakeWorldObject(WorldObjectDefOfVehicles.StashedVehicle);
			stashedVehicle.Tile = vehicleCaravan.Tile;

			//Calculate days before removal from map
			VehiclePawn largestVehicle = vehicleCaravan.VehiclesListForReading.MaxBy(vehicle => vehicle.VehicleDef.Size.Magnitude);
			float t = Ext_Math.ReverseInterpolate(largestVehicle.VehicleDef.Size.Magnitude, 1, 10);
			float timeoutDays = 25 * Mathf.Lerp(1.2f, 0.8f, t); //20 to 30 days depending on size of vehicle
			stashedVehicle.GetComponent<TimeoutComp>().StartTimeout(Mathf.CeilToInt(timeoutDays * 60000));

			List<Pawn> pawns = vehicleCaravan.PawnsListForReading.Where(pawn => pawn is not VehiclePawn).ToList();
			List<Pawn> vehicles = new List<Pawn>();
			List<Pawn> inventoryPawns = new List<Pawn>();

			foreach (VehiclePawn vehicle in vehicleCaravan.VehiclesListForReading)
			{
				vehicles.Add(vehicle);
				foreach (VehicleHandler handler in vehicle.handlers)
				{
					pawns.AddRange(handler.handlers);
				}
				foreach (Thing thing in vehicle.inventory.innerContainer)
				{
					if (thing is Pawn pawn)
					{
						inventoryPawns.Add(pawn);
					}
				}
			}

			caravan = CaravanMaker.MakeCaravan([], vehicleCaravan.Faction, vehicleCaravan.Tile, true);
			caravan.pawns.TryAddRangeOrTransfer(pawns, canMergeWithExistingStacks: false);
			caravan.pawns.TryAddRangeOrTransfer(inventoryPawns, canMergeWithExistingStacks: false);

			if (!transferables.NullOrEmpty())
			{
				//Transfer all contents
				foreach (TransferableOneWay transferable in transferables)
				{
					TransferableUtility.TransferNoSplit(transferable.things, transferable.CountToTransfer, delegate (Thing thing, int numToTake)
					{
						Pawn ownerOf = CaravanInventoryUtility.GetOwnerOf(vehicleCaravan, thing);
						if (ownerOf is null)
						{
							Log.Error($"Error while stashing vehicle. {thing} has no owner.");
						}
						else
						{
							CaravanInventoryUtility.MoveInventoryToSomeoneElse(ownerOf, thing, pawns, vehicles, numToTake);
						}
					}, true, true);
				}
			}
			
			//Transfer vehicles to stashed vehicle object
			for (int i = vehicles.Count - 1; i >= 0; i--)
			{
				Pawn vehiclePawn = vehicles[i];
				stashedVehicle.stash.TryAddOrTransfer(vehiclePawn, false);
			}
			Find.WorldObjects.Add(stashedVehicle);
			vehicleCaravan.RemoveAllPawns();
			vehicleCaravan.Destroy();
			return stashedVehicle;
		}
	}
}

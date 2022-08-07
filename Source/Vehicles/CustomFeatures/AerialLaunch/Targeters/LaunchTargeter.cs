using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public class LaunchTargeter : BaseVehicleWorldTargeter
	{
		private const float BaseFeedbackTexSize = 0.8f;
		private int origin;

		private Func<GlobalTargetInfo, float, bool> action;
		private Func<GlobalTargetInfo, List<FlightNode>, float, string> extraLabelGetter;

		public LaunchTargeter()
		{
			FlightPath = new List<FlightNode>();
		}

		public static LaunchTargeter Instance { get; private set; }

		public static float TotalDistance { get; private set; }

		public static float TotalFuelCost { get; private set; }

		public static List<FlightNode> FlightPath { get; private set; }

		public override bool IsTargeting => action != null;

		public void BeginTargeting(VehiclePawn vehicle, Func<GlobalTargetInfo, float, bool> action, int origin, bool canTargetTiles, Texture2D mouseAttachment = null, bool closeWorldTabWhenFinished = false, Action onUpdate = null,
			Func<GlobalTargetInfo, List<FlightNode>, float, string> extraLabelGetter = null)
		{
			this.vehicle = vehicle;
			this.action = action;
			originOnMap = WorldHelper.GetTilePos(origin);
			this.canTargetTiles = canTargetTiles;
			this.mouseAttachment = mouseAttachment;
			this.closeWorldTabWhenFinished = closeWorldTabWhenFinished;
			this.onUpdate = onUpdate;
			this.extraLabelGetter = extraLabelGetter;
			FlightPath.Clear();
			TotalDistance = 0;
			TotalFuelCost = 0;
		}

		public void BeginTargeting(VehiclePawn vehicle, Func<GlobalTargetInfo, float, bool> action, AerialVehicleInFlight aerialVehicle, bool canTargetTiles, Texture2D mouseAttachment = null, bool closeWorldTabWhenFinished = false, Action onUpdate = null,
			Func<GlobalTargetInfo, List<FlightNode>, float, string> extraLabelGetter = null)
		{
			this.vehicle = vehicle;
			this.action = action;
			this.aerialVehicle = aerialVehicle;
			this.canTargetTiles = canTargetTiles;
			this.mouseAttachment = mouseAttachment;
			this.closeWorldTabWhenFinished = closeWorldTabWhenFinished;
			this.onUpdate = onUpdate;
			this.extraLabelGetter = extraLabelGetter;
			FlightPath.Clear();
			TotalDistance = 0;
			TotalFuelCost = 0;
		}

		public void ContinueTargeting(VehiclePawn vehicle, Func<GlobalTargetInfo, float, bool> action, int origin, bool canTargetTiles, Texture2D mouseAttachment = null, bool closeWorldTabWhenFinished = false, Action onUpdate = null,
			Func<GlobalTargetInfo, List<FlightNode>, float, string> extraLabelGetter = null)
		{
			this.vehicle = vehicle;
			this.action = action;
			originOnMap = WorldHelper.GetTilePos(origin);
			this.canTargetTiles = canTargetTiles;
			this.mouseAttachment = mouseAttachment;
			this.closeWorldTabWhenFinished = closeWorldTabWhenFinished;
			this.onUpdate = onUpdate;
			this.extraLabelGetter = extraLabelGetter;
		}

		public void ContinueTargeting(VehiclePawn vehicle, Func<GlobalTargetInfo, float, bool> action, AerialVehicleInFlight aerialVehicle, bool canTargetTiles, Texture2D mouseAttachment = null, bool closeWorldTabWhenFinished = false, Action onUpdate = null,
			Func<GlobalTargetInfo, List<FlightNode>, float, string> extraLabelGetter = null)
		{
			this.vehicle = vehicle;
			this.action = action;
			this.aerialVehicle = aerialVehicle;
			this.canTargetTiles = canTargetTiles;
			this.mouseAttachment = mouseAttachment;
			this.closeWorldTabWhenFinished = closeWorldTabWhenFinished;
			this.onUpdate = onUpdate;
			this.extraLabelGetter = extraLabelGetter;
		}

		public override void RegisterActionOnTile(int tile, AerialVehicleArrivalAction arrivalAction)
		{
			FlightPath.Pop();
			FlightPath.Add(new FlightNode(tile, arrivalAction));
		}

		public override void StopTargeting()
		{
			if (closeWorldTabWhenFinished)
			{
				CameraJumper.TryHideWorld();
			}
			action = null;
			canTargetTiles = false;
			mouseAttachment = null;
			closeWorldTabWhenFinished = false;
			onUpdate = null;
			extraLabelGetter = null;
			aerialVehicle = null;
		}

		public override void ProcessInputEvents()
		{
			if (Event.current.type == EventType.MouseDown)
			{
				if (Event.current.button == 0 && IsTargeting)
				{
					GlobalTargetInfo arg = CurrentTargetUnderMouse();
					bool maxNodesHit = FlightPath.Count == vehicle.CompVehicleLauncher.launchProtocol.MaxFlightNodes;
					int sourceTile = aerialVehicle?.Tile ?? vehicle.Map.Tile;
					bool sameTile = !vehicle.CompVehicleLauncher.inFlight && sourceTile == arg.Tile && FlightPath.NullOrEmpty();
					if (WorldHelper.WorldObjectAt(arg.Tile) is WorldObject && vehicle.CompVehicleLauncher.SpaceFlight && !maxNodesHit && !sameTile)
					{
						FlightPath.Add(new FlightNode(arg.Tile));
					}
					if (FlightPath.LastOrDefault().tile == arg.Tile)
					{
						if (action(arg, TotalFuelCost))
						{
							SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
							StopTargeting();
						}
					}
					else if (maxNodesHit)
					{
						Messages.Message("VehicleFlightPathMaxNodes".Translate(vehicle.LabelShortCap), MessageTypeDefOf.RejectInput);
					}
					else if (sameTile)
					{
						SoundDefOf.ClickReject.PlayOneShotOnCamera(null);
					}
					else
					{
						if (arg.IsValid)
						{
							FlightPath.Add(new FlightNode(arg.Tile));
						}
					}
					Event.current.Use();
				}
				if (Event.current.button == 1 && IsTargeting)
				{
					if (FlightPath.Count > 0)
					{
						FlightPath.Pop();
					}
					else
					{
						SoundDefOf.CancelMode.PlayOneShotOnCamera(null);
						StopTargeting();
					}
					Event.current.Use();
				}
			}
			if (KeyBindingDefOf.Cancel.KeyDownEvent && IsTargeting)
			{
				SoundDefOf.CancelMode.PlayOneShotOnCamera(null);
				StopTargeting();
				Event.current.Use();
			}
		}

		public override void TargeterOnGUI()
		{
			if (IsTargeting)
			{
				if (!Mouse.IsInputBlockedNow)
				{
					GlobalTargetInfo mouseTarget = CurrentTargetUnderMouse();

					Vector2 mousePosition = Event.current.mousePosition;
					Texture2D image = mouseAttachment ?? TexCommand.Attack;
					Rect position = new Rect(mousePosition.x + 8f, mousePosition.y + 8f, 32f, 32f);
					GUI.DrawTexture(position, image);

					CostAndDistanceCalculator(out float fuelOnPathCost, out float tileDistance);

					Vector3 flightPathPos = originOnMap;
					if (!FlightPath.NullOrEmpty())
					{
						flightPathPos = WorldHelper.GetTilePos(FlightPath.LastOrDefault().tile);
					}
					float finalFuelCost = fuelOnPathCost;
					float finalTileDistance = tileDistance;
					if (mouseTarget.IsValid && WorldHelper.GetTilePos(mouseTarget.Tile) != flightPathPos)
					{
						finalFuelCost += vehicle.CompVehicleLauncher.FuelNeededToLaunchAtDist(flightPathPos, mouseTarget.Tile);
						finalTileDistance += Ext_Math.SphericalDistance(flightPathPos, WorldHelper.GetTilePos(mouseTarget.Tile));
					}
					TotalFuelCost = (float)Math.Round(finalFuelCost, 0);
					TotalDistance = finalTileDistance;
					string fuelCostLabel = "VF_VehicleFuelCost".Translate(TotalFuelCost);
					Vector2 textSize = Text.CalcSize(fuelCostLabel);
					Rect labelPosition = new Rect(mousePosition.x, mousePosition.y + textSize.y + 20f, textSize.x, textSize.y);
					float bgWidth = textSize.x * 1.2f;
					var color = GUI.color;

					if (extraLabelGetter != null)
					{
						string text = extraLabelGetter(mouseTarget, FlightPath, TotalFuelCost);
						Vector2 labelGetterText = Text.CalcSize(text);
						Rect rect = new Rect(position.xMax, position.y, 9999f, 100f);
						Rect bgRect = new Rect(rect.x - labelGetterText.x * 0.1f, rect.y, labelGetterText.x * 1.2f, labelGetterText.y);
						var textColor = GUI.color;
						GUI.color = Color.white;
						GUI.DrawTexture(bgRect, TexUI.GrayTextBG);
						GUI.color = textColor;
						GUI.Label(rect, text);
					}
					GUI.color = Color.white;
					GUI.DrawTexture(new Rect(labelPosition.x - textSize.x * 0.1f, labelPosition.y, bgWidth, textSize.y), TexUI.GrayTextBG);

					GUI.Label(labelPosition, fuelCostLabel);
					GUI.color = color;
				}
			}
		}

		public virtual void DrawAirDefenseGrid()
		{
			foreach (AirDefense airDefense in AirDefensePositionTracker.airDefenseCache.Values)
			{
				if (airDefense.parent.Faction.HostileTo(Faction.OfPlayer))
				{
					RenderHelper.DrawWorldRadiusRing(airDefense.parent.Tile, Mathf.CeilToInt(airDefense.MaxDistance), TexData.OneSidedWorldLineMatRed);
				}
			}
		}

		public override void TargeterUpdate()
		{
			if (IsTargeting)
			{
				DrawAirDefenseGrid();
				if (aerialVehicle != null)
				{
					originOnMap = aerialVehicle.DrawPos;
				}
				Vector3 pos = Vector3.zero;
				GlobalTargetInfo arg = CurrentTargetUnderMouse();
				if (arg.HasWorldObject)
				{
					pos = arg.WorldObject.DrawPos;
				}
				else if (arg.Tile >= 0)
				{
					pos = WorldHelper.GetTilePos(arg.Tile);
				}
				if (arg.IsValid && !Mouse.IsInputBlockedNow)
				{
					if (vehicle.CompVehicleLauncher.launchProtocol.GetFloatMenuOptionsAt(arg.Tile).NotNullAndAny())
					{
						WorldRendererUtility.DrawQuadTangentialToPlanet(pos, BaseFeedbackTexSize * Find.WorldGrid.averageTileSize, 0.018f, WorldMaterials.CurTargetingMat);
					}
				}

				Vector3 start = originOnMap;
				var tiles = new List<int>(FlightPath.Select(n => n.tile));
				Material lineMat = null;
				switch (vehicle.CompVehicleLauncher.GetShuttleStatus(arg, start))
				{
					case ShuttleLaunchStatus.Valid:
						lineMat = TexData.WorldLineMatWhite;
						break;
					case ShuttleLaunchStatus.NoReturnTrip:
						lineMat = TexData.WorldLineMatYellow;
						break;
					case ShuttleLaunchStatus.Invalid:
						lineMat = TexData.WorldLineMatRed;
						break;
				}
				for (int n = 0; n < FlightPath.Count; n++)
				{
					int curTile = tiles.PopAt(0);
					Vector3 end = WorldHelper.GetTilePos(curTile);
					DrawTravelPoint(start, end, lineMat);
					start = end;
				}
				if (FlightPath.Count > 0)
				{
					string destLabel = "VF_DoubleClickShuttleTarget".Translate();
					Vector2 labelGetterText = Text.CalcSize(destLabel);
					Rect destPosition = new Rect(start.x, start.y, 32f, 32f);
					Rect rect = new Rect(destPosition.xMax, destPosition.y, 9999f, 100f);
					Rect bgRect = new Rect(rect.x - labelGetterText.x * 0.1f, rect.y, labelGetterText.x * 1.2f, labelGetterText.y);
					var textColor = GUI.color;
					GUI.color = Color.white;
					Graphics.DrawTexture(bgRect, TexUI.GrayTextBG);
					GUI.color = textColor;
					//GUI.Label(rect, destLabel);
					WorldRendererUtility.DrawQuadTangentialToPlanet(start, BaseFeedbackTexSize * Find.WorldGrid.averageTileSize, 0.018f, WorldMaterials.CurTargetingMat);
				}
				if (FlightPath.Count < vehicle.CompVehicleLauncher.launchProtocol.MaxFlightNodes && arg.IsValid)
				{
					DrawTravelPoint(start, WorldHelper.GetTilePos(arg.Tile), lineMat);
				}

				onUpdate?.Invoke();
			}
		}

		public bool IsTargetedNow(WorldObject worldObject, List<WorldObject> worldObjectsUnderMouse = null)
		{
			if (!IsTargeting)
			{
				return false;
			}
			if (worldObjectsUnderMouse == null)
			{
				worldObjectsUnderMouse = GenWorldUI.WorldObjectsUnderMouse(UI.MousePositionOnUI);
			}
			return worldObjectsUnderMouse.Any() && worldObject == worldObjectsUnderMouse[0];
		}

		public void CostAndDistanceCalculator(out float fuelCost, out float distance)
		{
			fuelCost = 0;
			distance = 0;
			int source = origin;
			foreach (FlightNode node in FlightPath)
			{
				int tile = node.tile;
				float nodeDistance = WorldHelper.GetTileDistance(source, tile);
				fuelCost += vehicle.CompVehicleLauncher.FuelNeededToLaunchAtDist(nodeDistance);
				distance += nodeDistance;
				source = tile;
			}
		}

		public static void DrawTravelPoint(Vector3 start, Vector3 end, Material material = null)
		{
			if (material is null)
			{
				material = TexData.WorldLineMatWhite;
			}
			double distance = Ext_Math.SphericalDistance(start, end);
			int steps = Mathf.CeilToInt((float)(distance * 100) / 5);
			start += start.normalized * 0.05f;
			end += end.normalized * 0.05f;
			Vector3 previous = start;

			for (int i = 1; i <= steps; i++)
			{
				float t = (float)i / steps;
				Vector3 midPoint = Vector3.Slerp(start, end, t);

				GenDraw.DrawWorldLineBetween(previous, midPoint, material, 0.5f);
				previous = midPoint;
			}
		}

		public override void PostInit()
		{
			Instance = this;
		}
	}
}

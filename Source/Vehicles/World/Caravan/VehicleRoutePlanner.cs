using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Vehicles.World;

[PublicAPI]
[StaticConstructorOnStartup]
public class VehicleRoutePlanner : WorldComponent
{
	private const float BottomWindowBotMargin = 45f;
	private const float BottomWindowEntryExtraBotMargin = 22f;
	private const float RouteButtonDimension = 57f;
	private const int MaxCount = 25;

	private static readonly Texture2D MouseAttachment =
		ContentFinder<Texture2D>.Get("UI/Overlays/WaypointMouseAttachment");

	private static readonly Vector2 BottomWindowSize = new(500f, 95f);
	private static readonly Vector2 BottomButtonSize = new(160f, 40f);


	private Action onFinishCallback;
	private Action<PlanetTile> onChooseRouteCallback;

	private List<VehicleDef> vehicleDefs = [];
	private VehicleCaravanInfo caravanInfo;

	private readonly List<WorldPath> paths = [];
	private readonly List<int> cachedTicksToWaypoint = [];
	private readonly List<RoutePlannerWaypoint> waypoints = [];

	private bool cantRemoveFirstWaypoint;

	public VehicleRoutePlanner(RimWorld.Planet.World world) : base(world)
	{
		this.world = world;
	}

	public bool IsActive { get; private set; }

	private bool IsCaravaning { get; set; }

	private bool ShouldStop
	{
		get
		{
			return !IsActive || !WorldRendererUtility.WorldSelected ||
				Current.ProgramState == ProgramState.Playing &&
				Find.TickManager.CurTimeSpeed != TimeSpeed.Paused;
		}
	}

	private int CaravanTicksPerMove
	{
		get
		{
			return caravanInfo != null ?
				VehicleCaravanTicksPerMoveUtility.GetTicksPerMove(caravanInfo) :
				VehicleCaravanTicksPerMoveUtility.GetTicksPerMove(vehicleDefs);
		}
	}

	private Caravan CaravanAtTheFirstWaypoint
	{
		get
		{
			return waypoints.Count > 0 ?
				Find.WorldObjects.PlayerControlledCaravanAt(waypoints[0].Tile) :
				null;
		}
	}

	private void Start(Action onFinishCallback, Action<PlanetTile> onChooseRouteCallback)
	{
		if (IsActive)
			Stop();
		if (onFinishCallback != null)
			this.onFinishCallback += onFinishCallback;
		if (onChooseRouteCallback != null)
			this.onChooseRouteCallback += onChooseRouteCallback;

		IsActive = true;
		if (Current.ProgramState == ProgramState.Playing)
		{
			AmbientSoundManager.EnsureWorldAmbientSoundCreated();
			Find.World.renderer.wantedMode = WorldRenderMode.Planet;
			Find.TickManager.Pause();
		}
	}

	public void Start([NotNull] List<VehicleDef> vehicleDefs, Action onFinishCallback = null,
		Action<PlanetTile> onChooseRouteCallback = null)
	{
		Start(onFinishCallback, onChooseRouteCallback);
		this.vehicleDefs = vehicleDefs;
	}

	// NOTE - Dependency injection is not an option here, Dialog_FormCaravan is the primary user of the
	// route planner and it lacks abstraction. Opted for the callbacks as opposed to multiple specialized
	// overloads + fields, it's much cleaner this way.
	public void Start([NotNull] VehicleCaravanInfo caravanInfo, Action onFinishCallback = null,
		Action<PlanetTile> onChooseRouteCallback = null)
	{
		Start(onFinishCallback, onChooseRouteCallback);
		this.caravanInfo = caravanInfo;
		vehicleDefs = caravanInfo.pawns.UniqueVehicleDefsInList();
		IsCaravaning = caravanInfo.caravaning;
		if (caravanInfo.tile.Valid)
		{
			TryAddWaypoint(caravanInfo.tile);
			cantRemoveFirstWaypoint = true;
		}
	}

	public void Stop()
	{
		IsActive = false;
		IsCaravaning = false;
		foreach (RoutePlannerWaypoint waypoint in waypoints)
		{
			waypoint.Destroy();
		}
		waypoints.Clear();
		cachedTicksToWaypoint.Clear();
		vehicleDefs.Clear();
		cantRemoveFirstWaypoint = false;
		onFinishCallback?.Invoke();
		onFinishCallback = null;
		onChooseRouteCallback = null;
		ReleasePaths();
	}

	public override void WorldComponentUpdate()
	{
		if (IsActive && ShouldStop)
			Stop();
		if (!IsActive)
			return;

		foreach (WorldPath path in paths)
		{
			path.DrawPath(null);
		}
	}

	public override void WorldComponentOnGUI()
	{
		if (!IsActive)
			return;

		if (KeyBindingDefOf.Cancel.KeyDownEvent)
		{
			if (!IsCaravaning)
				SoundDefOf.Tick_Low.PlayOneShotOnCamera();
			Stop();
			Event.current.Use();
			return;
		}
		GenUI.DrawMouseAttachment(MouseAttachment);
		if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
		{
			int tile =
				Find.WorldSelector.SelectableObjectsUnderMouse()
				 .FirstOrDefault() is VehicleCaravan caravan ?
					caravan.Tile :
					GenWorld.MouseTile(true);
			if (tile >= 0)
			{
				RoutePlannerWaypoint waypoint = MostRecentWaypointAt(tile);
				if (waypoint != null)
				{
					if (waypoint == waypoints[^1])
					{
						TryRemoveWaypoint(waypoint);
					}
					else
					{
						List<FloatMenuOption> list =
						[
							new("AddWaypoint".Translate(),
								delegate { TryAddWaypoint(tile); }),
							new("RemoveWaypoint".Translate(),
								delegate { TryRemoveWaypoint(waypoint); }),
						];
						Find.WindowStack.Add(new FloatMenu(list));
					}
				}
				else
				{
					TryAddWaypoint(tile);
				}
				Event.current.Use();
			}
		}
		DoRouteDetailsBox();
		if (IsCaravaning && DoChooseRouteButton())
			return;

		DoTileTooltips();
	}

	private void DoRouteDetailsBox()
	{
		Rect rect = new((UI.screenWidth - BottomWindowSize.x) / 2f,
			UI.screenHeight - BottomWindowSize.y - BottomWindowBotMargin, BottomWindowSize.x,
			BottomWindowSize.y);
		if (Current.ProgramState == ProgramState.Entry)
		{
			rect.y -= BottomWindowEntryExtraBotMargin;
		}
		Find.WindowStack.ImmediateWindow(1373514241, rect, WindowLayer.Dialog, delegate
		{
			const float TopPadding = 6;
			const float RowHeight = 25;

			if (!IsActive)
				return;

			using TextBlock textBlock = new(GameFont.Small, TextAnchor.UpperCenter, Color.white);

			float lineHeight = Text.LineHeight;
			float curY = TopPadding;
			if (waypoints.Count >= 2)
			{
				Widgets.Label(new Rect(0f, curY, rect.width, RowHeight),
					"RoutePlannerEstTimeToFinalDest".Translate(cachedTicksToWaypoint[^1]
					 .ToStringTicksToDays("0.#")));
			}
			else if (cantRemoveFirstWaypoint)
			{
				Widgets.Label(new Rect(0f, curY, rect.width, RowHeight),
					"RoutePlannerAddOneOrMoreWaypoints".Translate());
			}
			else
			{
				Widgets.Label(new Rect(0f, curY, rect.width, RowHeight),
					"RoutePlannerAddTwoOrMoreWaypoints".Translate());
			}
			curY += lineHeight;

			if (!IsCaravaning && CaravanAtTheFirstWaypoint != null)
			{
				GUI.color = Color.gray;
				Widgets.Label(new Rect(0f, curY, rect.width, RowHeight),
					"RoutePlannerUsingTicksPerMoveOfCaravan".Translate(CaravanAtTheFirstWaypoint
					 .LabelCap));
			}
			curY += lineHeight;

			GUI.color = Color.gray;
			Widgets.Label(new Rect(0f, curY, rect.width, RowHeight),
				"RoutePlannerPressRMBToAddAndRemoveWaypoints".Translate());
			curY += lineHeight;

			Widgets.Label(new Rect(0f, curY, rect.width, RowHeight),
				IsCaravaning ?
					"RoutePlannerPressEscapeToReturnToCaravanFormationDialog".Translate() :
					"RoutePlannerPressEscapeToExit".Translate());
		});
	}

	private bool DoChooseRouteButton()
	{
		if (!IsCaravaning || waypoints.Count < 2)
			return false;

		if (Widgets.ButtonText(new Rect((UI.screenWidth - BottomButtonSize.x) / 2f,
				UI.screenHeight - BottomWindowSize.y - BottomWindowBotMargin - 10f -
				BottomButtonSize.y, BottomButtonSize.x, BottomButtonSize.y),
			"ChooseRouteButton".Translate()))
		{
			onChooseRouteCallback?.Invoke(waypoints.Count >= 2 ? waypoints[1].Tile : PlanetTile.Invalid);
			SoundDefOf.Click.PlayOneShotOnCamera();
			Stop();
			return true;
		}
		return false;
	}

	private void DoTileTooltips()
	{
		if (Mouse.IsInputBlockedNow)
		{
			return;
		}
		int num = GenWorld.MouseTile(true);
		if (num == -1)
		{
			return;
		}
		for (int i = 0; i < paths.Count; i++)
		{
			if (paths[i].NodesReversed.Contains(num))
			{
				string str = GetTileTip(num, i);
				Text.Font = GameFont.Small;
				Vector2 vector = Text.CalcSize(str);
				vector.x += 20f;
				vector.y += 20f;
				Vector2 mouseAttachedWindowPos = GenUI.GetMouseAttachedWindowPos(vector.x, vector.y);
				Rect rect = new(mouseAttachedWindowPos, vector);
				Find.WindowStack.ImmediateWindow(1859615246, rect, WindowLayer.Super, delegate
				{
					Text.Font = GameFont.Small;
					Widgets.Label(rect.AtZero().ContractedBy(10f), str);
				});
				return;
			}
		}
	}

	private string GetTileTip(int tile, int pathIndex)
	{
		int tileIndex = paths[pathIndex].NodesReversed.IndexOf(tile);
		int tileStep;
		if (tileIndex > 0)
		{
			tileStep = paths[pathIndex].NodesReversed[tileIndex - 1];
		}
		else if (pathIndex < paths.Count - 1 && paths[pathIndex + 1].NodesReversed.Count >= 2)
		{
			tileStep = paths[pathIndex + 1].NodesReversed[^2];
		}
		else
		{
			tileStep = -1;
		}
		int estimatedTicks = VehicleCaravanPathingHelper.EstimatedTicksToArrive(vehicleDefs,
			paths[pathIndex].FirstNode, tile, paths[pathIndex], 0f, CaravanTicksPerMove,
			GenTicks.TicksAbs + cachedTicksToWaypoint[pathIndex]);
		int totalTicks = cachedTicksToWaypoint[pathIndex] + estimatedTicks;
		int ticksAbs = GenTicks.TicksAbs + totalTicks;
		StringBuilder stringBuilder = new();
		if (totalTicks != 0)
		{
			stringBuilder.AppendLine(
				"EstimatedTimeToTile".Translate(totalTicks.ToStringTicksToDays("0.##")));
		}
		stringBuilder.AppendLine("ForagedFoodAmount".Translate() + ": " +
			Find.WorldGrid[tile].PrimaryBiome.forageability.ToStringPercent());
		stringBuilder.Append(
			VirtualPlantsUtility.GetVirtualPlantsStatusExplanationAt(tile, ticksAbs));
		if (tileStep != -1)
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine();
			StringBuilder stringBuilder2 = new();
			float num5 =
				WorldPathGrid.CalculatedMovementDifficultyAt(tileStep, false, ticksAbs, stringBuilder2);
			float roadMovementDifficultyMultiplier =
				Find.WorldGrid.GetRoadMovementDifficultyMultiplier(tile, tileStep, stringBuilder2);
			stringBuilder.Append("TileMovementDifficulty".Translate() + ":\n" +
				stringBuilder2.ToString().Indented("  "));
			stringBuilder.AppendLine();
			stringBuilder.Append("  = ");
			stringBuilder.Append((num5 * roadMovementDifficultyMultiplier).ToString("0.#"));
		}
		return stringBuilder.ToString();
	}

	public void DoRoutePlannerButton(ref float curBaseY)
	{
		Rect rect = new(UI.screenWidth - 10f - RouteButtonDimension,
			curBaseY - 10f - RouteButtonDimension, RouteButtonDimension, RouteButtonDimension);
		if (Widgets.ButtonImage(rect, VehicleTex.RoutePlanner, Color.white,
			new Color(0.8f, 0.8f, 0.8f)))
		{
			if (IsActive)
			{
				Stop();
				SoundDefOf.Tick_Low.PlayOneShotOnCamera();
			}
			else
			{
				Find.WindowStack.Add(new Dialog_VehicleSelector());
				SoundDefOf.Tick_High.PlayOneShotOnCamera();
			}
		}
		TooltipHandler.TipRegion(rect, "VF_RoutePlannerButtonTip".Translate());
		curBaseY -= RouteButtonDimension + 20f;
	}

	private void TryAddWaypoint(PlanetTile tile, bool playSound = true)
	{
		if (vehicleDefs.NotNullAndAny(v => !WorldVehiclePathGrid.Instance.Passable(tile, v)))
		{
			Messages.Message("MessageCantAddWaypointBecauseImpassable".Translate(),
				MessageTypeDefOf.RejectInput, false);
			return;
		}
		if (waypoints.NotNullAndAny() && !vehicleDefs.All(vehicle =>
			WorldVehiclePathGrid.Instance.reachability.CanReach(vehicle,
				waypoints[^1].Tile, tile)))
		{
			Messages.Message("MessageCantAddWaypointBecauseUnreachable".Translate(),
				MessageTypeDefOf.RejectInput, false);
			return;
		}
		if (waypoints.Count >= MaxCount)
		{
			Messages.Message("MessageCantAddWaypointBecauseLimit".Translate(MaxCount),
				MessageTypeDefOf.RejectInput, false);
			return;
		}
		RoutePlannerWaypoint routePlannerWaypoint =
			(RoutePlannerWaypoint)WorldObjectMaker.MakeWorldObject(
				WorldObjectDefOf.RoutePlannerWaypoint);
		routePlannerWaypoint.Tile = tile;
		Find.WorldObjects.Add(routePlannerWaypoint);
		waypoints.Add(routePlannerWaypoint);
		RecreatePaths();
		if (playSound)
		{
			SoundDefOf.Tick_High.PlayOneShotOnCamera();
		}
	}

	private void TryRemoveWaypoint(RoutePlannerWaypoint point, bool playSound = true)
	{
		if (cantRemoveFirstWaypoint && waypoints.NotNullAndAny() && point == waypoints[0])
		{
			Messages.Message("MessageCantRemoveWaypointBecauseFirst".Translate(),
				MessageTypeDefOf.RejectInput, false);
			return;
		}
		point.Destroy();
		waypoints.Remove(point);
		for (int i = waypoints.Count - 1; i >= 1; i--)
		{
			if (waypoints[i].Tile == waypoints[i - 1].Tile)
			{
				waypoints[i].Destroy();
				waypoints.RemoveAt(i);
			}
		}
		RecreatePaths();
		if (playSound)
		{
			SoundDefOf.Tick_Low.PlayOneShotOnCamera();
		}
	}

	private void ReleasePaths()
	{
		foreach (WorldPath path in paths)
		{
			path.ReleaseToPool();
		}
		paths.Clear();
	}

	private void RecreatePaths()
	{
		ReleasePaths();

		for (int i = 1; i < waypoints.Count; i++)
		{
			paths.Add(WorldVehiclePathfinder.Instance.FindPath(waypoints[i - 1].Tile, waypoints[i].Tile,
				vehicleDefs));
		}
		cachedTicksToWaypoint.Clear();
		int num = 0;
		int caravanTicksPerMove = CaravanTicksPerMove;
		for (int j = 0; j < waypoints.Count; j++)
		{
			if (j == 0)
			{
				cachedTicksToWaypoint.Add(0);
			}
			else
			{
				num += VehicleCaravanPathingHelper.EstimatedTicksToArrive(vehicleDefs,
					waypoints[j - 1].Tile, waypoints[j].Tile, paths[j - 1], 0f, caravanTicksPerMove,
					GenTicks.TicksAbs + num);
				cachedTicksToWaypoint.Add(num);
			}
		}
	}

	private RoutePlannerWaypoint MostRecentWaypointAt(int tile)
	{
		for (int i = waypoints.Count - 1; i >= 0; i--)
		{
			if (waypoints[i].Tile == tile)
			{
				return waypoints[i];
			}
		}
		return null;
	}
}
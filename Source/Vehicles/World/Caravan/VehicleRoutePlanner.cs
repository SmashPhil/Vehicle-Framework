using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	[StaticConstructorOnStartup]
	public class VehicleRoutePlanner : WorldComponent
	{
		private const float BottomWindowBotMargin = 45f;
		private const float BottomWindowEntryExtraBotMargin = 22f;
		private const float RouteButtonDimension = 57f;
		private const int MaxCount = 25;

		private static readonly Texture2D ButtonTex = ContentFinder<Texture2D>.Get("UI/Gizmos/VehicleRoutePlanner", true);
		private static readonly Texture2D MouseAttachment = ContentFinder<Texture2D>.Get("UI/Overlays/WaypointMouseAttachment", true);
		private static readonly Vector2 BottomWindowSize = new Vector2(500f, 95f);
		private static readonly Vector2 BottomButtonSize = new Vector2(160f, 40f);

		private Dialog_FormVehicleCaravan currentFormCaravanDialog;

		public List<VehicleDef> vehicleDefs = new List<VehicleDef>();
		private VehicleCaravanTicksPerMoveUtility.VehicleCaravanInfo? caravanInfoFromFormCaravanDialog;
		private List<WorldPath> paths = new List<WorldPath>();
		private List<int> cachedTicksToWaypoint = new List<int>();
		public List<RoutePlannerWaypoint> waypoints = new List<RoutePlannerWaypoint>();

		private bool cantRemoveFirstWaypoint;

		public VehicleRoutePlanner(World world) : base(world)
		{
			this.world = world;
			vehicleDefs = new List<VehicleDef>();
			Instance = this;
		}

		public static VehicleRoutePlanner Instance { get; private set; }

		public bool Active { get; set; }

		public bool FormingCaravan
		{
			get
			{
				return Active && currentFormCaravanDialog != null;
			}
		}

		private bool ShouldStop
		{
			get
			{
				return !Active || !WorldRendererUtility.WorldRenderedNow || (Current.ProgramState == ProgramState.Playing && Find.TickManager.CurTimeSpeed != TimeSpeed.Paused && !Prefs.DevMode);
			}
		}

		private int CaravanTicksPerMove
		{
			get
			{
				VehicleCaravanTicksPerMoveUtility.VehicleCaravanInfo? caravanInfo = CaravanInfo;
				if (caravanInfo != null && caravanInfo.Value.pawns.NotNullAndAny(pawn => pawn is VehiclePawn))
				{
					return VehicleCaravanTicksPerMoveUtility.GetTicksPerMove(caravanInfo.Value, null);
				}
				return 3464;
			}
		}

		private VehicleCaravanTicksPerMoveUtility.VehicleCaravanInfo? CaravanInfo
		{
			get
			{
				if (currentFormCaravanDialog != null)
				{
					return caravanInfoFromFormCaravanDialog.Value;
				}
				Caravan caravanAtTheFirstWaypoint = CaravanAtTheFirstWaypoint;
				if (caravanAtTheFirstWaypoint != null)
				{
					return new VehicleCaravanTicksPerMoveUtility.VehicleCaravanInfo(caravanAtTheFirstWaypoint);
				}
				return null;
			}
		}

		private Caravan CaravanAtTheFirstWaypoint
		{
			get
			{
				if (!waypoints.NotNullAndAny())
				{
					return null;
				}
				return Find.WorldObjects.PlayerControlledCaravanAt(waypoints[0].Tile);
			}
		}

		public void Start()
		{
			if (Active)
			{
				Stop();
			}
			Find.WindowStack.Add(new Dialog_VehicleSelector());
		}

		public void Start(Dialog_FormVehicleCaravan formCaravanDialog)
		{
			if (Active)
			{
				Stop();
			}
			currentFormCaravanDialog = formCaravanDialog;
			caravanInfoFromFormCaravanDialog = new VehicleCaravanTicksPerMoveUtility.VehicleCaravanInfo(formCaravanDialog);
			formCaravanDialog.choosingRoute = true;
			Find.WindowStack.TryRemove(formCaravanDialog, true);
			vehicleDefs = caravanInfoFromFormCaravanDialog.Value.pawns.UniqueVehicleDefsInList();
			InitiateRoutePlanner();
			TryAddWaypoint(formCaravanDialog.CurrentTile, true);
			cantRemoveFirstWaypoint = true;
		}

		public void InitiateRoutePlanner()
		{
			Instance.Active = true;
			if (Current.ProgramState == ProgramState.Playing)
			{
				Find.World.renderer.wantedMode = WorldRenderMode.Planet;
				Find.TickManager.Pause();
			}
		}

		public void Stop()
		{
			Active = false;
			for (int i = 0; i < waypoints.Count; i++)
			{
				waypoints[i].Destroy();
			}
			waypoints.Clear();
			cachedTicksToWaypoint.Clear();
			vehicleDefs.Clear();
			if (currentFormCaravanDialog != null)
			{
				currentFormCaravanDialog.Notify_NoLongerChoosingRoute();
			}
			caravanInfoFromFormCaravanDialog = null;
			currentFormCaravanDialog = null;
			cantRemoveFirstWaypoint = false;
			ReleasePaths();
		}

		public void WorldRoutePlannerUpdate()
		{
			if (Active && ShouldStop)
			{
				Stop();
			}
			if (!Active)
			{
				return;
			}
			for (int i = 0; i < paths.Count; i++)
			{
				paths[i].DrawPath(null);
			}
		}

		public void WorldRoutePlannerOnGUI()
		{
			if (!Active)
			{
				return;
			}
			if (KeyBindingDefOf.Cancel.KeyDownEvent)
			{
				if (currentFormCaravanDialog != null)
				{
					Find.WindowStack.Add(currentFormCaravanDialog);
				}
				else
				{
					SoundDefOf.Tick_Low.PlayOneShotOnCamera(null);
				}
				Stop();
				Event.current.Use();
				return;
			}
			GenUI.DrawMouseAttachment(MouseAttachment);
			if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
			{
				int tile = Find.WorldSelector.SelectableObjectsUnderMouse().FirstOrDefault() is VehicleCaravan caravan ? caravan.Tile : GenWorld.MouseTile(true);
				if (tile >= 0)
				{
					RoutePlannerWaypoint waypoint = MostRecentWaypointAt(tile);
					if (waypoint != null)
					{
						if (waypoint == waypoints[waypoints.Count - 1])
						{
							TryRemoveWaypoint(waypoint, true);
						}
						else
						{
							List<FloatMenuOption> list = new List<FloatMenuOption>();
							list.Add(new FloatMenuOption("AddWaypoint".Translate(), delegate()
							{
								TryAddWaypoint(tile, true);
							}, MenuOptionPriority.Default, null, null, 0f, null, null));
							list.Add(new FloatMenuOption("RemoveWaypoint".Translate(), delegate()
							{
								TryRemoveWaypoint(waypoint, true);
							}, MenuOptionPriority.Default, null, null, 0f, null, null));
							Find.WindowStack.Add(new FloatMenu(list));
						}
					}
					else
					{
						TryAddWaypoint(tile, true);
					}
					Event.current.Use();
				}
			}
			DoRouteDetailsBox();
			if (DoChooseRouteButton())
			{
				return;
			}
			DoTileTooltips();
		}

		private void DoRouteDetailsBox()
		{
			Rect rect = new Rect((UI.screenWidth - BottomWindowSize.x) / 2f, UI.screenHeight - BottomWindowSize.y - BottomWindowBotMargin, BottomWindowSize.x, BottomWindowSize.y);
			if (Current.ProgramState == ProgramState.Entry)
			{
				rect.y -= BottomWindowEntryExtraBotMargin;
			}
			Find.WindowStack.ImmediateWindow(1373514241, rect, WindowLayer.Dialog, delegate
			{
				if (Active)
				{
					GUI.color = Color.white;
					Text.Anchor = TextAnchor.UpperCenter;
					Text.Font = GameFont.Small;
					float num = 6f;
					if (waypoints.Count >= 2)
					{
						Widgets.Label(new Rect(0f, num, rect.width, 25f), "RoutePlannerEstTimeToFinalDest".Translate(GetTicksToWaypoint(waypoints.Count - 1).ToStringTicksToDays("0.#")));
					}
					else if (cantRemoveFirstWaypoint)
					{
						Widgets.Label(new Rect(0f, num, rect.width, 25f), "RoutePlannerAddOneOrMoreWaypoints".Translate());
					}
					else
					{
						Widgets.Label(new Rect(0f, num, rect.width, 25f), "RoutePlannerAddTwoOrMoreWaypoints".Translate());
					}
					num += 20f;
					if (currentFormCaravanDialog == null && CaravanAtTheFirstWaypoint != null)
					{
						GUI.color = Color.gray;
						Widgets.Label(new Rect(0f, num, rect.width, 25f), "RoutePlannerUsingTicksPerMoveOfCaravan".Translate(CaravanAtTheFirstWaypoint.LabelCap));
					}
					num += 20f;
					GUI.color = Color.gray;
					Widgets.Label(new Rect(0f, num, rect.width, 25f), "RoutePlannerPressRMBToAddAndRemoveWaypoints".Translate());
					num += 20f;
					if (currentFormCaravanDialog != null)
					{
						Widgets.Label(new Rect(0f, num, rect.width, 25f), "RoutePlannerPressEscapeToReturnToCaravanFormationDialog".Translate());
					}
					else
					{
						Widgets.Label(new Rect(0f, num, rect.width, 25f), "RoutePlannerPressEscapeToExit".Translate());
					}
					num += 20f;
					GUI.color = Color.white;
					Text.Anchor = TextAnchor.UpperLeft;
				}
			});
		}

		private bool DoChooseRouteButton()
		{
			if (currentFormCaravanDialog == null || waypoints.Count < 2)
			{
				return false;
			}
			if (Widgets.ButtonText(new Rect((Verse.UI.screenWidth - BottomButtonSize.x) / 2f, Verse.UI.screenHeight - BottomWindowSize.y - BottomWindowBotMargin - 10f - BottomButtonSize.y, 
				BottomButtonSize.x, BottomButtonSize.y), "ChooseRouteButton".Translate(), true, true, true))
			{
				Find.WindowStack.Add(currentFormCaravanDialog);
				currentFormCaravanDialog.Notify_ChoseRoute(waypoints[1].Tile);
				SoundDefOf.Click.PlayOneShotOnCamera(null);
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
					Rect rect = new Rect(mouseAttachedWindowPos, vector);
					Find.WindowStack.ImmediateWindow(1859615246, rect, WindowLayer.Super, delegate
					{
						Text.Font = GameFont.Small;
						Widgets.Label(rect.AtZero().ContractedBy(10f), str);
					}, true, false, 1f);
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
				tileStep = paths[pathIndex + 1].NodesReversed[paths[pathIndex + 1].NodesReversed.Count - 2];
			}
			else
			{
				tileStep = -1;
			}
			int estimatedTicks = VehicleCaravanPathingHelper.EstimatedTicksToArrive(vehicleDefs, paths[pathIndex].FirstNode, tile, paths[pathIndex], 0f, CaravanTicksPerMove, GenTicks.TicksAbs + cachedTicksToWaypoint[pathIndex]);
			int totalTicks = cachedTicksToWaypoint[pathIndex] + estimatedTicks;
			int ticksAbs = GenTicks.TicksAbs + totalTicks;
			StringBuilder stringBuilder = new StringBuilder();
			if (totalTicks != 0)
			{
				stringBuilder.AppendLine("EstimatedTimeToTile".Translate(totalTicks.ToStringTicksToDays("0.##")));
			}
			stringBuilder.AppendLine("ForagedFoodAmount".Translate() + ": " + Find.WorldGrid[tile].biome.forageability.ToStringPercent());
			stringBuilder.Append(VirtualPlantsUtility.GetVirtualPlantsStatusExplanationAt(tile, ticksAbs));
			if (tileStep != -1)
			{
				stringBuilder.AppendLine();
				stringBuilder.AppendLine();
				StringBuilder stringBuilder2 = new StringBuilder();
				float num5 = WorldPathGrid.CalculatedMovementDifficultyAt(tileStep, false, new int?(ticksAbs), stringBuilder2);
				float roadMovementDifficultyMultiplier = Find.WorldGrid.GetRoadMovementDifficultyMultiplier(tile, tileStep, stringBuilder2);
				stringBuilder.Append("TileMovementDifficulty".Translate() + ":\n" + stringBuilder2.ToString().Indented("  "));
				stringBuilder.AppendLine();
				stringBuilder.Append("  = ");
				stringBuilder.Append((num5 * roadMovementDifficultyMultiplier).ToString("0.#"));
			}
			return stringBuilder.ToString();
		}

		public void DoRoutePlannerButton(ref float curBaseY)
		{
			Rect rect = new Rect(Verse.UI.screenWidth - 10f - RouteButtonDimension, curBaseY - 10f - RouteButtonDimension, RouteButtonDimension, RouteButtonDimension);
			if (Widgets.ButtonImage(rect, ButtonTex, Color.white, new Color(0.8f, 0.8f, 0.8f), true))
			{
				if (Active)
				{
					Stop();
					SoundDefOf.Tick_Low.PlayOneShotOnCamera(null);
				}
				else
				{
					Start();
					SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
				}
			}
			TooltipHandler.TipRegion(rect, "VehicleRoutePlannerButtonTip".Translate());
			curBaseY -= RouteButtonDimension + 20f;
		}

		public int GetTicksToWaypoint(int index)
		{
			return cachedTicksToWaypoint[index];
		}

		private void TryAddWaypoint(int tile, bool playSound = true)
		{
			if (vehicleDefs.NotNullAndAny(v => !WorldVehiclePathGrid.Instance.Passable(tile, v)))
			{
				Messages.Message("MessageCantAddWaypointBecauseImpassable".Translate(), MessageTypeDefOf.RejectInput, false);
				return;
			}
			if (waypoints.NotNullAndAny() && !vehicleDefs.All(vehicle => WorldVehicleReachability.Instance.CanReach(vehicle, waypoints[waypoints.Count - 1].Tile, tile)))
			{
				Messages.Message("MessageCantAddWaypointBecauseUnreachable".Translate(), MessageTypeDefOf.RejectInput, false);
				return;
			}
			if (waypoints.Count >= MaxCount)
			{
				Messages.Message("MessageCantAddWaypointBecauseLimit".Translate(MaxCount), MessageTypeDefOf.RejectInput, false);
				return;
			}
			RoutePlannerWaypoint routePlannerWaypoint = (RoutePlannerWaypoint)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.RoutePlannerWaypoint);
			routePlannerWaypoint.Tile = tile;
			Find.WorldObjects.Add(routePlannerWaypoint);
			waypoints.Add(routePlannerWaypoint);
			RecreatePaths();
			if (playSound)
			{
				SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
			}
		}

		public void TryRemoveWaypoint(RoutePlannerWaypoint point, bool playSound = true)
		{
			if (cantRemoveFirstWaypoint && waypoints.NotNullAndAny() && point == waypoints[0])
			{
				Messages.Message("MessageCantRemoveWaypointBecauseFirst".Translate(), MessageTypeDefOf.RejectInput, false);
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
				SoundDefOf.Tick_Low.PlayOneShotOnCamera(null);
			}
		}

		private void ReleasePaths()
		{
			for (int i = 0; i < paths.Count; i++)
			{
				paths[i].ReleaseToPool();
			}
			paths.Clear();
		}

		private void RecreatePaths()
		{
			ReleasePaths();

			for (int i = 1; i < waypoints.Count; i++)
			{
				List<string> explanations = new List<string>();
				paths.Add(WorldVehiclePathfinder.Instance.FindPath(waypoints[i - 1].Tile, waypoints[i].Tile, vehicleDefs));
				if (VehicleMod.settings.debug.debugLogging)
				{
					Log.Message($"------ RoutePlanner ------");
					explanations.ForEach(expl => Log.Message(expl));
					Log.Message($"--------------------------");
				}
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
					num += VehicleCaravanPathingHelper.EstimatedTicksToArrive(vehicleDefs, waypoints[j - 1].Tile, waypoints[j].Tile, paths[j - 1], 0f, caravanTicksPerMove, GenTicks.TicksAbs + num);
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
}

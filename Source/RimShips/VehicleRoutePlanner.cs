using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.Sound;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;

namespace Vehicles
{
	[StaticConstructorOnStartup]
    public class VehicleRoutePlanner
    {
		public bool Active
		{
			get
			{
				return active;
			}
		}

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
				return !active || !WorldRendererUtility.WorldRenderedNow || (Current.ProgramState == ProgramState.Playing && Find.TickManager.CurTimeSpeed != TimeSpeed.Paused);
			}
		}

		private int CaravanTicksPerMove
		{
			get
			{
				VehicleCaravanTicksPerMoveUtility.CaravanInfo? caravanInfo = CaravanInfo;
				if (caravanInfo != null && caravanInfo.Value.pawns.Any<Pawn>())
				{
					return VehicleCaravanTicksPerMoveUtility.GetTicksPerMove(caravanInfo.Value, null);
				}
				return 3464;
			}
		}

		private VehicleCaravanTicksPerMoveUtility.CaravanInfo? CaravanInfo
		{
			get
			{
				if (currentFormCaravanDialog != null)
				{
					return caravanInfoFromFormCaravanDialog;
				}
				Caravan caravanAtTheFirstWaypoint = this.CaravanAtTheFirstWaypoint;
				if (caravanAtTheFirstWaypoint != null)
				{
					return new VehicleCaravanTicksPerMoveUtility.CaravanInfo?(new VehicleCaravanTicksPerMoveUtility.CaravanInfo(caravanAtTheFirstWaypoint));
				}
				return null;
			}
		}

		private Caravan CaravanAtTheFirstWaypoint
		{
			get
			{
				if (!waypoints.Any())
				{
					return null;
				}
				return Find.WorldObjects.PlayerControlledCaravanAt(waypoints[0].Tile);
			}
		}

		public void Start()
		{
			if (active)
			{
				Stop();
			}
			active = true;
			if (Current.ProgramState == ProgramState.Playing)
			{
				Find.World.renderer.wantedMode = WorldRenderMode.Planet;
				Find.TickManager.Pause();
			}
		}

		public void Start(Dialog_FormVehicleCaravan formCaravanDialog)
		{
			if (active)
			{
				Stop();
			}
			currentFormCaravanDialog = formCaravanDialog;
			caravanInfoFromFormCaravanDialog = new VehicleCaravanTicksPerMoveUtility.CaravanInfo?(new VehicleCaravanTicksPerMoveUtility.CaravanInfo(formCaravanDialog));
			formCaravanDialog.choosingRoute = true;
			Find.WindowStack.TryRemove(formCaravanDialog, true);
			Start();
			TryAddWaypoint(formCaravanDialog.CurrentTile, true);
			cantRemoveFirstWaypoint = true;
		}

		public void Stop()
		{
			active = false;
			for (int i = 0; i < waypoints.Count; i++)
			{
				waypoints[i].Destroy();
			}
			waypoints.Clear();
			cachedTicksToWaypoint.Clear();
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
			if (active && ShouldStop)
			{
				Stop();
			}
			if (!active)
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
			if (!active)
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
				Caravan caravan = Find.WorldSelector.SelectableObjectsUnderMouse().FirstOrDefault<WorldObject>() as Caravan;
				int tile = (caravan != null) ? caravan.Tile : GenWorld.MouseTile(true);
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
			Rect rect = new Rect((Verse.UI.screenWidth - BottomWindowSize.x) / 2f, Verse.UI.screenHeight - BottomWindowSize.y - 45f, BottomWindowSize.x, BottomWindowSize.y);
			if (Current.ProgramState == ProgramState.Entry)
			{
				rect.y -= 22f;
			}
			Find.WindowStack.ImmediateWindow(1373514241, rect, WindowLayer.Dialog, delegate
			{
				if (active)
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
					if (!CaravanInfo.HasValue || !CaravanInfo.Value.pawns.Any())
					{
						GUI.color = new Color(0.8f, 0.6f, 0.6f);
						Widgets.Label(new Rect(0f, num, rect.width, 25f), "RoutePlannerUsingAverageTicksPerMoveWarning".Translate());
					}
					else if (currentFormCaravanDialog == null && CaravanAtTheFirstWaypoint != null)
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
			if (this.currentFormCaravanDialog == null || this.waypoints.Count < 2)
			{
				return false;
			}
			if (Widgets.ButtonText(new Rect((Verse.UI.screenWidth - BottomButtonSize.x) / 2f, Verse.UI.screenHeight - BottomWindowSize.y - 45f - 10f - BottomButtonSize.y, BottomButtonSize.x, BottomButtonSize.y), "ChooseRouteButton".Translate(), true, true, true))
			{
				Find.WindowStack.Add(this.currentFormCaravanDialog);
				this.currentFormCaravanDialog.Notify_ChoseRoute(this.waypoints[1].Tile);
				SoundDefOf.Click.PlayOneShotOnCamera(null);
				this.Stop();
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
			for (int i = 0; i < this.paths.Count; i++)
			{
				if (this.paths[i].NodesReversed.Contains(num))
				{
					string str = this.GetTileTip(num, i);
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
			int num = this.paths[pathIndex].NodesReversed.IndexOf(tile);
			int num2;
			if (num > 0)
			{
				num2 = this.paths[pathIndex].NodesReversed[num - 1];
			}
			else if (pathIndex < this.paths.Count - 1 && this.paths[pathIndex + 1].NodesReversed.Count >= 2)
			{
				num2 = this.paths[pathIndex + 1].NodesReversed[this.paths[pathIndex + 1].NodesReversed.Count - 2];
			}
			else
			{
				num2 = -1;
			}
			int num3 = this.cachedTicksToWaypoint[pathIndex] + CaravanArrivalTimeEstimator.EstimatedTicksToArrive(this.paths[pathIndex].FirstNode, tile, this.paths[pathIndex], 0f, this.CaravanTicksPerMove, GenTicks.TicksAbs + this.cachedTicksToWaypoint[pathIndex]);
			int num4 = GenTicks.TicksAbs + num3;
			StringBuilder stringBuilder = new StringBuilder();
			if (num3 != 0)
			{
				stringBuilder.AppendLine("EstimatedTimeToTile".Translate(num3.ToStringTicksToDays("0.##")));
			}
			stringBuilder.AppendLine("ForagedFoodAmount".Translate() + ": " + Find.WorldGrid[tile].biome.forageability.ToStringPercent());
			stringBuilder.Append(VirtualPlantsUtility.GetVirtualPlantsStatusExplanationAt(tile, num4));
			if (num2 != -1)
			{
				stringBuilder.AppendLine();
				stringBuilder.AppendLine();
				StringBuilder stringBuilder2 = new StringBuilder();
				float num5 = WorldPathGrid.CalculatedMovementDifficultyAt(num2, false, new int?(num4), stringBuilder2);
				float roadMovementDifficultyMultiplier = Find.WorldGrid.GetRoadMovementDifficultyMultiplier(tile, num2, stringBuilder2);
				stringBuilder.Append("TileMovementDifficulty".Translate() + ":\n" + stringBuilder2.ToString().Indented("  "));
				stringBuilder.AppendLine();
				stringBuilder.Append("  = ");
				stringBuilder.Append((num5 * roadMovementDifficultyMultiplier).ToString("0.#"));
			}
			return stringBuilder.ToString();
		}

		public void DoRoutePlannerButton(ref float curBaseY)
		{
			float num = 57f;
			float num2 = 33f;
			Rect rect = new Rect(Verse.UI.screenWidth - 10f - num, curBaseY - 10f - num2, num, num2);
			if (Widgets.ButtonImage(rect, ButtonTex, Color.white, new Color(0.8f, 0.8f, 0.8f), true))
			{
				if (active)
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
			TooltipHandler.TipRegionByKey(rect, "RoutePlannerButtonTip");
			curBaseY -= num2 + 20f;
		}

		public int GetTicksToWaypoint(int index)
		{
			return cachedTicksToWaypoint[index];
		}

		private void TryAddWaypoint(int tile, bool playSound = true)
		{
			if (Find.World.Impassable(tile))
			{
				Messages.Message("MessageCantAddWaypointBecauseImpassable".Translate(), MessageTypeDefOf.RejectInput, false);
				return;
			}
			if (waypoints.Any<RoutePlannerWaypoint>() && !Find.WorldReachability.CanReach(waypoints[waypoints.Count - 1].Tile, tile))
			{
				Messages.Message("MessageCantAddWaypointBecauseUnreachable".Translate(), MessageTypeDefOf.RejectInput, false);
				return;
			}
			if (waypoints.Count >= 25)
			{
				Messages.Message("MessageCantAddWaypointBecauseLimit".Translate(25), MessageTypeDefOf.RejectInput, false);
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
			if (cantRemoveFirstWaypoint && waypoints.Any() && point == waypoints[0])
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
			WorldPathFinder worldPathFinder = Find.WorldPathFinder;
			for (int i = 1; i < waypoints.Count; i++)
			{
				paths.Add(worldPathFinder.FindPath(waypoints[i - 1].Tile, waypoints[i].Tile, null, null));
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
					num += CaravanArrivalTimeEstimator.EstimatedTicksToArrive(waypoints[j - 1].Tile, waypoints[j].Tile, paths[j - 1], 0f, caravanTicksPerMove, GenTicks.TicksAbs + num);
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

		private bool active;

		private VehicleCaravanTicksPerMoveUtility.CaravanInfo? caravanInfoFromFormCaravanDialog;

		private Dialog_FormVehicleCaravan currentFormCaravanDialog;

		private List<WorldPath> paths = new List<WorldPath>();

		private List<int> cachedTicksToWaypoint = new List<int>();

		public List<RoutePlannerWaypoint> waypoints = new List<RoutePlannerWaypoint>();

		private bool cantRemoveFirstWaypoint;

		private const int MaxCount = 25;

		private static readonly Texture2D ButtonTex = ContentFinder<Texture2D>.Get("UI/Misc/WorldRoutePlanner", true);

		private static readonly Texture2D MouseAttachment = ContentFinder<Texture2D>.Get("UI/Overlays/WaypointMouseAttachment", true);

		private static readonly Vector2 BottomWindowSize = new Vector2(500f, 95f);

		private static readonly Vector2 BottomButtonSize = new Vector2(160f, 40f);

		private const float BottomWindowBotMargin = 45f;

		private const float BottomWindowEntryExtraBotMargin = 22f;
    }

	public static class VehicleCaravanTicksPerMoveUtility
	{
		public static int GetTicksPerMove(Caravan caravan, StringBuilder explanation = null)
		{
			if (caravan == null)
			{
				if (explanation != null)
				{
					AppendUsingDefaultTicksPerMoveInfo(explanation);
				}
				return 3300;
			}
			return GetTicksPerMove(new CaravanInfo(caravan), explanation);
		}

		public static int GetTicksPerMove(CaravanInfo caravanInfo, StringBuilder explanation = null)
		{
			return GetTicksPerMove(caravanInfo.pawns, caravanInfo.massUsage, caravanInfo.massCapacity, explanation);
		}

		public static int GetTicksPerMove(List<Pawn> pawns, float massUsage, float massCapacity, StringBuilder explanation = null)
		{
			if (pawns.Any())
			{
				if (explanation != null)
				{
					explanation.Append("CaravanMovementSpeedFull".Translate() + ":");
				}
				float num = 0f;
				for (int i = 0; i < pawns.Count; i++)
				{
					float num2 = (float)((pawns[i].Downed || pawns[i].CarriedByCaravan()) ? 450 : pawns[i].TicksPerMoveCardinal);
					num2 = Mathf.Min(num2, 150f) * 340f;
					float num3 = 60000f / num2;
					if (explanation != null)
					{
						explanation.AppendLine();
						explanation.Append(string.Concat(new string[]
						{
							"  - ",
							pawns[i].LabelShortCap,
							": ",
							num3.ToString("0.#"),
							" "
						}) + "TilesPerDay".Translate());
						if (pawns[i].Downed)
						{
							explanation.Append(" (" + "DownedLower".Translate() + ")");
						}
						else if (pawns[i].CarriedByCaravan())
						{
							explanation.Append(" (" + "Carried".Translate() + ")");
						}
					}
					num += num2 / (float)pawns.Count;
				}
				float moveSpeedFactorFromMass = GetMoveSpeedFactorFromMass(massUsage, massCapacity);
				if (explanation != null)
				{
					float num4 = 60000f / num;
					explanation.AppendLine();
					explanation.Append("  " + "Average".Translate() + ": " + num4.ToString("0.#") + " " + "TilesPerDay".Translate());
					explanation.AppendLine();
					explanation.Append("  " + "MultiplierForCarriedMass".Translate(moveSpeedFactorFromMass.ToStringPercent()));
				}
				int num5 = Mathf.Max(Mathf.RoundToInt(num / moveSpeedFactorFromMass), 1);
				if (explanation != null)
				{
					float num6 = 60000f / (float)num5;
					explanation.AppendLine();
					explanation.Append("  " + "FinalCaravanPawnsMovementSpeed".Translate() + ": " + num6.ToString("0.#") + " " + "TilesPerDay".Translate());
				}
				return num5;
			}
			if (explanation != null)
			{
				AppendUsingDefaultTicksPerMoveInfo(explanation);
			}
			return 3300;
		}

		private static float GetMoveSpeedFactorFromMass(float massUsage, float massCapacity)
		{
			if (massCapacity <= 0f)
			{
				return 1f;
			}
			float t = massUsage / massCapacity;
			return Mathf.Lerp(2f, 1f, t);
		}

		private static void AppendUsingDefaultTicksPerMoveInfo(StringBuilder sb)
		{
			sb.Append("CaravanMovementSpeedFull".Translate() + ":");
			float num = 18.181818f;
			sb.AppendLine();
			sb.Append("  " + "Default".Translate() + ": " + num.ToString("0.#") + " " + "TilesPerDay".Translate());
		}

		private const int MaxPawnTicksPerMove = 150;
		private const int DownedPawnMoveTicks = 450;
		public const float CellToTilesConversionRatio = 340f;
		public const int DefaultTicksPerMove = 3300;
		private const float MoveSpeedFactorAtZeroMass = 2f;

		public struct CaravanInfo
		{
			public CaravanInfo(Caravan caravan)
			{
				pawns = caravan.PawnsListForReading;
				massUsage = caravan.MassUsage;
				massCapacity = caravan.MassCapacity;
			}

			public CaravanInfo(Dialog_FormVehicleCaravan formCaravanDialog)
			{
				pawns = TransferableUtility.GetPawnsFromTransferables(formCaravanDialog.transferables);
				massUsage = formCaravanDialog.MassUsage;
				massCapacity = formCaravanDialog.MassCapacity;
			}

			public List<Pawn> pawns;
			public float massUsage;
			public float massCapacity;
		}
	}
}

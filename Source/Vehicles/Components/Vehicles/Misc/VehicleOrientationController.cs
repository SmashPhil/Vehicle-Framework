using System.Collections.Generic;
using System.Linq;
using RimWorld;
using SmashTools;
using SmashTools.Algorithms;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;
using Verse.Sound;
using Verse.Steam;

namespace Vehicles;

[StaticConstructorOnStartup]
public class VehicleOrientationController : BaseTargeter
{
  private const int RecomputeDestinationsFrequency = 15;
  private const float CameraViewerZoomRate = 0.55f;
  private const float DragThreshold = 1f;
  private const float HoldTimeThreshold = 0.5f;

  /* MultiPawnGotoController consts */

  private static readonly Color FeedbackColor = GenColor.FromBytes(153, 207, 135);
  private static readonly Color GotoBetweenLineColor = FeedbackColor * new Color(1f, 1f, 1f, 0.18f);
  private static readonly Color GotoCircleColor = FeedbackColor * new Color(1f, 1f, 1f, 0.2f);

  private static readonly Material GotoBetweenLineMaterial =
    MaterialPool.MatFrom("UI/Overlays/ThickLine", ShaderDatabase.Transparent, GotoBetweenLineColor);

  private static readonly Material GotoCircleMaterial =
    MaterialPool.MatFrom("UI/Overlays/Circle75Solid", ShaderDatabase.Transparent, GotoCircleColor);

  /* ------------------------------ */

  private Vector3 clickPos;
  private IntVec3 start;
  private IntVec3 end;

  private float timeHeldDown;

  private int lastUpdatedTick;

  private float[,] costMatrix;
  private KuhnMunkres assigner;
  private readonly List<IntVec3> potentialDests = [];
  private readonly List<IntVec3> dests = [];
  private readonly List<VehiclePawn> vehicles = [];

  private Rot8 Rotation { get; set; }

  private bool IsDragging { get; set; }

  private bool IsMultiSelect => vehicles.Count > 1;

  public override bool IsTargeting => vehicle is { Spawned: true };

  private Rot8 MouseTargetedRotation
  {
    get
    {
      IntVec3 mouseCell = UI.MouseCell();
      float angle = end.AngleToCell(mouseCell);
      return Rot8.FromAngle(angle);
    }
  }

  private static VehicleOrientationController Instance { get; set; }

  public static void StartOrienting(VehiclePawn dragging, IntVec3 cell, IntVec3 clickCell)
  {
    Instance.StopTargeting();
    Instance.Init([dragging], cell, clickCell);
  }

  public static void StartOrienting(List<VehiclePawn> dragging, IntVec3 cell, IntVec3 clickCell)
  {
    Instance.StopTargeting();
    Instance.Init(dragging, cell, clickCell);
  }

  private void Init(List<VehiclePawn> vehicles, IntVec3 cell, IntVec3 clickCell)
  {
    vehicle = vehicles.First();
    Assert.IsNotNull(vehicle);
    this.vehicles.AddRange(vehicles);
    dests.Populate(IntVec3.Invalid, vehicles.Count);
    potentialDests.Populate(IntVec3.Invalid, vehicles.Count);
    dests[0] = cell;
    Rotation = vehicle.FullRotation;
    start = clickCell;
    end = cell;
    clickPos = UI.MouseMapPosition();

    if (IsMultiSelect)
    {
      int n = vehicles.Count;
      costMatrix = new float[n, n];
      assigner = new KuhnMunkres(n);
    }

    OnStart();
  }

  protected override void OnStart()
  {
    base.OnStart();
    // If multiple vehicles are selected, just start orienting immediately
    // they won't be able to path without spreading their destinations out.
    if (IsMultiSelect)
      IsDragging = true;
  }

  private void ConfirmOrientation()
  {
    if (IsMultiSelect)
      RecomputeDestinations();

    for (int i = 0; i < vehicles.Count; i++)
    {
      VehiclePawn confirmVehicle = vehicles[i];
      IntVec3 cell = dests[i];
      if (!confirmVehicle.Spawned || !cell.IsValid)
        continue;
      if (!CanFitAt(confirmVehicle, cell, Rotation))
      {
        Messages.Message("VF_CannotFit".Translate(confirmVehicle), vehicle, MessageTypeDefOf.RejectInput,
          historical: false);
        continue;
      }

      FloatMenuOptionProvider_OrderVehicle.PawnGotoAction(end, confirmVehicle, cell,
        Rotation);
    }

    if (start == end)
    {
      LessonAutoActivator.TeachOpportunity(ConceptDefOf.GroupGotoHereDragging,
        OpportunityType.GoodToKnow);
    }
    else if (start.DistanceToSquared(end) > 1.9f)
    {
      PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.GroupGotoHereDragging,
        KnowledgeAmount.SpecificInteraction);
    }

    SoundDefOf.ColonistOrdered.PlayOneShotOnCamera();
    StopTargeting();
  }

  private static bool CanFitAt(VehiclePawn vehicle, IntVec3 cell, Rot8 rot)
  {
    if (vehicle.LocationRestrictedBySize(vehicle.Map, cell, rot))
      return false;

    return PathingHelper.AnyVehicleBlockingPathAt(cell, vehicle) == null;
  }

  private void RecomputeDestinations()
  {
    Assert.IsTrue(IsMultiSelect);
    dests.Populate(IntVec3.Invalid, dests.Count);

    // Calculate every expected destination
    int count = vehicles.Count;
    float denominator = count > 1 ? count - 1 : 1;
    for (int i = 0; i < count; i++)
    {
      VehiclePawn selVehicle = vehicles[i];
      if (!selVehicle.Spawned)
        continue;

      IntVec3 destPos;
      if (selVehicle.Map.exitMapGrid.IsExitCell(end))
      {
        destPos = end;
      }
      else
      {
        float frac = i / denominator;
        destPos = (start.ToVector3() + (end.ToVector3() - start.ToVector3()) * frac)
         .ToIntVec3();
      }
      if (!PathingHelper.TryFindNearestStandableCell(selVehicle, destPos, out IntVec3 result))
        result = IntVec3.Invalid;
      potentialDests[i] = result;
    }

    // Calculate cheapest path sum
    for (int i = 0; i < count; i++)
    {
      for (int j = 0; j < count; j++)
        costMatrix[i, j] = vehicles[i].Position.DistanceTo(dests[j]);
    }
    int[] assignment = assigner.Compute(costMatrix);

    // Assign
    for (int i = 0; i < count; i++)
    {
      VehiclePawn selVehicle = vehicles[i];
      if (!selVehicle.Spawned)
        continue;

      dests[i] = potentialDests[assignment[i]];
    }
  }

  public override void ProcessInputEvents()
  {
    if (KeyBindingDefOf.Cancel.KeyDownEvent)
    {
      StopTargeting();
      return;
    }
    if (!Input.GetMouseButton(1))
    {
      ConfirmOrientation();
    }
    else
    {
      if (KeyBindingDefOf.Designator_RotateLeft.KeyDownEvent)
      {
        SoundDefOf.DragSlider.PlayOneShotOnCamera();
        Rotation = Rotation.Rotated(RotationDirection.Counterclockwise);
      }
      else if (KeyBindingDefOf.Designator_RotateRight.KeyDownEvent)
      {
        SoundDefOf.DragSlider.PlayOneShotOnCamera();
        Rotation = Rotation.Rotated(RotationDirection.Clockwise);
      }
    }

    if (IsMultiSelect)
    {
      IntVec3 mouseCell = UI.MouseCell();
      int ticksGame = Find.TickManager.TicksGame;
      if (mouseCell != end || ticksGame > lastUpdatedTick + RecomputeDestinationsFrequency)
      {
        if (mouseCell != end)
        {
          SoundDefOf.DragGoto.PlayOneShotOnCamera();
        }
        end = mouseCell;
        lastUpdatedTick = ticksGame;
        RecomputeDestinations();
      }
    }
  }

  public override void TargeterUpdate()
  {
    if (!IsDragging)
    {
      // Will fall back to whatever orientation the vehicle ends in after pathing.
      // Also allows map edge exit grid checks to use full hitbox for validation.
      Rotation = Rot8.Invalid;

      timeHeldDown += Time.deltaTime;
      float dragDistance = Vector3.Distance(clickPos, UI.MouseMapPosition());
      if (timeHeldDown >= HoldTimeThreshold || dragDistance >= DragThreshold ||
        dragDistance <= -DragThreshold)
      {
        IsDragging = true;
      }
    }

    if (IsDragging)
    {
      if (!IsMultiSelect)
      {
        Rot8 targetRot = MouseTargetedRotation;
        if (Rotation != targetRot)
        {
          Rotation = targetRot;
          SoundDefOf.DragGoto.PlayOneShotOnCamera();
        }
      }

      Vector3 feedbackScaleVec = new(1.7f, 1f, 1.7f);
      float vehicleAltitude = AltitudeLayer.MetaOverlays.AltitudeFor();
      float circleAltitude = vehicleAltitude + Altitudes.AltInc;
      float lineAltitude = vehicleAltitude - Altitudes.AltInc;

      if (IsMultiSelect)
      {
        Vector3 a = start.ToVector3ShiftedWithAltitude(lineAltitude);
        Vector3 b = end.ToVector3ShiftedWithAltitude(lineAltitude);
        GenDraw.DrawLineBetween(a, b, GotoBetweenLineMaterial, 0.9f);
        for (int i = 0; i < vehicles.Count; i++)
        {
          VehiclePawn drawVehicle = vehicles[i];
          IntVec3 dest = dests[i];
          if (!drawVehicle.Spawned || !dest.IsValid || dest.Fogged(drawVehicle.Map))
            continue;
          IntVec2 vehicleSize = drawVehicle.VehicleDef.Size;
          Vector3 circlePos = dest.ToVector3ShiftedWithAltitude(circleAltitude);
          if (vehicleSize == IntVec2.One)
          {
            Graphics.DrawMesh(MeshPool.plane10,
              Matrix4x4.TRS(circlePos, Quaternion.identity, feedbackScaleVec),
              GotoCircleMaterial, 0);
          }
          drawVehicle.DrawAt(dest.ToVector3ShiftedWithAltitude(vehicleAltitude), Rotation, 0);
        }
      }
      else
      {
        Color drawColor = CanFitAt(vehicle, dests[0], Rotation) ?
          VehicleGhostUtility.whiteGhostColor :
          VehicleGhostUtility.RedGhostColor;
        VehicleGhostUtility.DrawGhostVehicleDef(dests[0], Rotation, vehicle.VehicleDef, drawColor,
          AltitudeLayer.MetaOverlays, vehicle);
      }
    }
  }

  public override void StopTargeting()
  {
    IsDragging = false;

    vehicle = null;
    vehicles.Clear();
    dests.Clear();

    start = IntVec3.Invalid;
    end = IntVec3.Invalid;
    clickPos = Vector3.zero;

    timeHeldDown = 0;
    lastUpdatedTick = 0;
  }

  public override void TargeterOnGUI()
  {
    if (IsMultiSelect)
    {
      MainTabWindow_Inspect inspectPane = (MainTabWindow_Inspect)MainButtonDefOf.Inspect.TabWindow;
      GUIShowRotationControls(0, inspectPane.PaneTopY);
    }
  }

  private static void GUIShowRotationControls(float leftX, float bottomY)
  {
    Rect winRect = new(leftX, bottomY - 90f, 200f, 90f);
    Find.WindowStack.ImmediateWindow(nameof(VehicleOrientationController).GetHashCode(), winRect,
      WindowLayer.GameUI, delegate
      {
        using TextBlock textBlock = new(GameFont.Medium, TextAnchor.MiddleCenter);

        Rect lButRect = new(winRect.width / 2f - 64f - 5f, 15f, 64f, 64f);
        GUI.DrawTexture(lButRect, TexUI.RotLeftTex);
        if (!SteamDeck.IsSteamDeck)
          Widgets.Label(lButRect, KeyBindingDefOf.Designator_RotateLeft.MainKeyLabel);

        Rect rButRect = new(winRect.width / 2f + 5f, 15f, 64f, 64f);
        GUI.DrawTexture(rButRect, TexUI.RotRightTex);
        if (!SteamDeck.IsSteamDeck)
          Widgets.Label(rButRect, KeyBindingDefOf.Designator_RotateRight.MainKeyLabel);
      });
  }

  public override void PostInit()
  {
    Instance = this;
  }
}
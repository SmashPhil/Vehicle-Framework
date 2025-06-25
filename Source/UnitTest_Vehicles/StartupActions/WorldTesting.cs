using RimWorld.Planet;
using SmashTools;
using Verse;

namespace Vehicles.World;

/// <summary>
/// Unit Testing
/// </summary>
public static class WorldTesting
{
  [StartupAction(Category = "Map", Name = "Strafe Targeting", GameState = GameState.Playing)]
  private static void StartupAction_StrafeTargeting()
  {
    Prefs.DevMode = true;
    LongEventHandler.ExecuteWhenFinished(delegate
    {
      Settlement settlement =
        Find.WorldObjects.Settlements.FirstOrDefault(settlement => settlement.Faction.IsPlayer);
      if (settlement == null)
      {
        SmashLog.Error(
          $"Unable to execute startup action {nameof(WorldTesting)}. No map to form player caravan from.");
        return;
      }
      Map map = Find.CurrentMap;
      VehiclePawn vehicle =
        (VehiclePawn)map.mapPawns.AllPawns.FirstOrDefault(p => p is VehiclePawn
        {
          CompVehicleLauncher: not null
        });
      CameraJumper.TryJump(vehicle);
      StrafeTargeter.Instance.BeginTargeting(vehicle, vehicle.CompVehicleLauncher.launchProtocol,
        delegate { }, null, null, null, true);
    });
  }

  [StartupAction(Category = "World", Name = "Caravan Formation", GameState = GameState.Playing)]
  private static void StartupAction_CaravanFormation()
  {
    Prefs.DevMode = true;
    LongEventHandler.ExecuteWhenFinished(delegate
    {
      CameraJumper.TryShowWorld();
      Settlement settlement =
        Find.WorldObjects.Settlements.FirstOrDefault(settlement => settlement.Faction.IsPlayer);
      if (settlement == null)
      {
        SmashLog.Error(
          $"Unable to execute startup action {nameof(WorldTesting)}. No map to form player caravan from.");
        return;
      }
      Find.WindowStack.Add(new Dialog_FormVehicleCaravan(settlement.Map));
    });
  }

  /// <summary>
  /// Load up game, open route planner
  /// </summary>
  [StartupAction(Category = "World", Name = "World Route Planner", GameState = GameState.Playing)]
  private static void StartupAction_RoutePlanner()
  {
    Prefs.DevMode = true;
    CameraJumper.TryShowWorld();
    Find.World.GetComponent<VehicleRoutePlanner>().StartWithSelector();
  }
}
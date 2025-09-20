using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Vehicles.World;

/// <summary>
/// Helper class to merge accessors from duplicate code in <see cref="Dialog_FormCaravan"/> and <see cref="Dialog_SplitCaravan"/>
/// </summary>
[PublicAPI]
public class FormationInfo : ICaravanInfo
{
	// Properties
	private static readonly MethodInfo MustChooseRouteGetter;
	private static readonly MethodInfo DaysWorthOfFoodGetter;
	private static readonly MethodInfo MostFoodWillRotSoonGetter;

	// Methods
	private static readonly MethodInfo FlashMassMethod;
	private static readonly MethodInfo AddItemsFromTransferablesToRandomInventoriesMethod;
	private static readonly MethodInfo ShouldShowWarningForUndesirableFoodMethod;
	private static readonly MethodInfo ShouldShowWarningForMechWithoutMechanitorMethod;
	private static readonly MethodInfo Notify_TransferablesChangedMethod;
	private static readonly MethodInfo SelectApproximateBestTravelSuppliesMethod;

	private static readonly AccessTools.FieldRef<Dialog_FormCaravan, bool>
		ChoosingRouteFieldRef;

	private static readonly AccessTools.FieldRef<Dialog_FormCaravan, bool>
		ReformFieldRef;

	private static readonly AccessTools.FieldRef<Dialog_FormCaravan, PlanetTile>
		StartingTileFieldRef;

	private static readonly AccessTools.FieldRef<Dialog_FormCaravan, PlanetTile>
		DestinationTileFieldRef;

	private static readonly AccessTools.FieldRef<Dialog_FormCaravan, bool>
		TicksToArriveDirtyFieldRef;

	private static readonly AccessTools.FieldRef<Dialog_FormCaravan, bool>
		DaysWorthOfFoodDirtyFieldRef;

	private readonly Func<bool> mustChooseRoute;
	private readonly Func<ValueTuple<float, float>> daysWorthOfFood;
	private readonly Func<bool> mostFoodWillRotSoon;

	private readonly Action flashMass;
	private readonly Func<bool> shouldShowWarningForUndesirableFood;
	private readonly Func<bool> shouldShowWarningForMechWithoutMechanitor;
	private readonly Action<List<Pawn>> addItemsFromTransferablesToRandomInventories;
	private readonly Action notifyTransferablesChanged;
	private readonly Action selectApproximateBestTravelSupplies;

	private VehiclePawn leadVehicle;
	public readonly List<Pawn> pawns = [];
	public readonly List<VehiclePawn> vehicles = [];
	public readonly List<Thing> things = [];
	public readonly List<VehiclePawn> unselectedVehicles = [];

	private readonly Map map;
	private readonly Dialog_FormCaravan formCaravan;

	static FormationInfo()
	{
		MustChooseRouteGetter =
			AccessTools.PropertyGetter(typeof(Dialog_FormCaravan), "MustChooseRoute");
		DaysWorthOfFoodGetter =
			AccessTools.PropertyGetter(typeof(Dialog_FormCaravan), "DaysWorthOfFood");
		MostFoodWillRotSoonGetter =
			AccessTools.PropertyGetter(typeof(Dialog_FormCaravan), "MostFoodWillRotSoon");

		FlashMassMethod = AccessTools.Method(typeof(Dialog_FormCaravan),
			"FlashMass");
		ShouldShowWarningForUndesirableFoodMethod = AccessTools.Method(typeof(Dialog_FormCaravan),
			"ShouldShowWarningForUndesirableFood");
		ShouldShowWarningForMechWithoutMechanitorMethod = AccessTools.Method(typeof(Dialog_FormCaravan),
			"ShouldShowWarningForMechWithoutMechanitor");
		AddItemsFromTransferablesToRandomInventoriesMethod = AccessTools
		 .Method(typeof(Dialog_FormCaravan), "AddItemsFromTransferablesToRandomInventories");
		Notify_TransferablesChangedMethod =
			AccessTools.Method(typeof(Dialog_FormCaravan), "Notify_TransferablesChanged");
		SelectApproximateBestTravelSuppliesMethod =
			AccessTools.Method(typeof(Dialog_FormCaravan), "SelectApproximateBestTravelSupplies");

		ChoosingRouteFieldRef =
			AccessTools.FieldRefAccess<bool>(typeof(Dialog_FormCaravan), "choosingRoute");
		ReformFieldRef = AccessTools.FieldRefAccess<bool>(typeof(Dialog_FormCaravan), "reform");
		StartingTileFieldRef =
			AccessTools.FieldRefAccess<PlanetTile>(typeof(Dialog_FormCaravan), "startingTile");
		DestinationTileFieldRef =
			AccessTools.FieldRefAccess<PlanetTile>(typeof(Dialog_FormCaravan), "destinationTile");
		TicksToArriveDirtyFieldRef =
			AccessTools.FieldRefAccess<bool>(typeof(Dialog_FormCaravan), "ticksToArriveDirty");
		DaysWorthOfFoodDirtyFieldRef =
			AccessTools.FieldRefAccess<bool>(typeof(Dialog_FormCaravan), "daysWorthOfFoodDirty");
	}

	public FormationInfo(Dialog_FormCaravan formCaravan, Map map)
	{
		this.formCaravan = formCaravan;
		this.map = map;

		mustChooseRoute =
			(Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), formCaravan, MustChooseRouteGetter);
		daysWorthOfFood =
			(Func<ValueTuple<float, float>>)Delegate.CreateDelegate(
				typeof(Func<ValueTuple<float, float>>), formCaravan, DaysWorthOfFoodGetter);
		mostFoodWillRotSoon =
			(Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), formCaravan,
				MostFoodWillRotSoonGetter);

		flashMass = (Action)Delegate.CreateDelegate(typeof(Action), formCaravan, FlashMassMethod);
		shouldShowWarningForUndesirableFood =
			(Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>),
				formCaravan, ShouldShowWarningForUndesirableFoodMethod);
		shouldShowWarningForMechWithoutMechanitor =
			(Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>),
				formCaravan, ShouldShowWarningForMechWithoutMechanitorMethod);
		addItemsFromTransferablesToRandomInventories = (Action<List<Pawn>>)Delegate.CreateDelegate(
			typeof(Action<List<Pawn>>),
			formCaravan, AddItemsFromTransferablesToRandomInventoriesMethod);
		notifyTransferablesChanged =
			(Action)Delegate.CreateDelegate(typeof(Action), formCaravan,
				Notify_TransferablesChangedMethod);
		selectApproximateBestTravelSupplies =
			(Action)Delegate.CreateDelegate(typeof(Action), formCaravan,
				SelectApproximateBestTravelSuppliesMethod);
	}

	public Dialog_FormCaravan Dialog => formCaravan;

	public Map Map => map;

	public VehiclePawn LeadVehicle => leadVehicle;

	public List<Pawn> AllPawnsAndVehicles => pawns.Concat(vehicles).ToList();

	public bool Reform => ReformFieldRef.Invoke(formCaravan);

	bool ICaravanInfo.AllowSelectionOfAllVehicles => Reform;

	public bool ChoosingRoute
	{
		get { return ChoosingRouteFieldRef.Invoke(formCaravan); }
		set { ChoosingRouteFieldRef.Invoke(formCaravan) = value; }
	}

	public PlanetTile StartingTile
	{
		get { return StartingTileFieldRef.Invoke(formCaravan); }
		set { StartingTileFieldRef.Invoke(formCaravan) = value; }
	}

	public PlanetTile DestinationTile
	{
		get { return DestinationTileFieldRef.Invoke(formCaravan); }
		set { DestinationTileFieldRef.Invoke(formCaravan) = value; }
	}

	public bool TicksToArriveDirty
	{
		get { return TicksToArriveDirtyFieldRef.Invoke(formCaravan); }
		set { TicksToArriveDirtyFieldRef.Invoke(formCaravan) = value; }
	}

	public bool DaysWorthOfFoodDirty
	{
		get { return DaysWorthOfFoodDirtyFieldRef.Invoke(formCaravan); }
		set { DaysWorthOfFoodDirtyFieldRef.Invoke(formCaravan) = value; }
	}

	public bool MustChooseRoute => mustChooseRoute();

	public (float days, float tillRot) DaysWorthOfFood => daysWorthOfFood();

	public bool MostFoodWillRotSoon => mostFoodWillRotSoon();

	public void FlashMass()
	{
		flashMass();
	}

	public bool ShouldShowWarningForUndesirableFood()
	{
		return shouldShowWarningForUndesirableFood();
	}

	public bool ShouldShowWarningForMechWithoutMechanitor()
	{
		return shouldShowWarningForMechWithoutMechanitor();
	}

	public void AddItemsFromTransferablesToRandomInventories(List<Pawn> pawns)
	{
		addItemsFromTransferablesToRandomInventories(pawns);
	}

	public void SelectApproximateBestTravelSupplies()
	{
		selectApproximateBestTravelSupplies();
	}

	public void NotifyTransferablesChanged()
	{
		notifyTransferablesChanged();
	}

	internal void RecacheTransferables()
	{
		leadVehicle = null;
		pawns.Clear();
		vehicles.Clear();
		things.Clear();
		unselectedVehicles.Clear();

		int largestMagnitude = -1;
		foreach (TransferableOneWay transferable in formCaravan.transferables)
		{
			if (transferable.AnyThing is null)
				continue;
			if (transferable.CountToTransfer == 0)
			{
				if (transferable.AnyThing is VehiclePawn vehicle)
					unselectedVehicles.Add(vehicle);
				continue;
			}

			foreach (Thing thing in transferable.things)
			{
				switch (thing)
				{
					case VehiclePawn vehicle:
						if (vehicle.def.size.Magnitude > largestMagnitude)
						{
							leadVehicle = vehicle;
							largestMagnitude = vehicle.def.size.MagnitudeManhattan;
						}
						vehicles.Add(vehicle);
					break;
					case Pawn pawn:
						pawns.Add(pawn);
					break;
					default:
						things.Add(thing);
					break;
				}
			}
		}
	}
}
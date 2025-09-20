using System.Collections.Generic;
using JetBrains.Annotations;
using SmashTools;
using Verse;

namespace Vehicles.Rendering;

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public struct BlitRequest
{
	// Required for drawing graphics tied to vehicle defs that aren't necessarily defined in their
	// own defs (eg. VehicleTurrets and the upgrade tree)
	public readonly VehicleDef vehicleDef;

	public Rot8 rot;
	public PatternData patternData;
	public List<IBlitTarget> blitTargets = [];

	public BlitRequest(Rot8 rot, PatternData patternData)
	{
		this.rot = rot;
		this.patternData = patternData;

		if (!VehicleMod.settings.main.useCustomShaders)
			patternData.patternDef = PatternDefOf.Default;
	}

	public BlitRequest(VehicleDef vehicleDef) : this(vehicleDef.drawProperties.displayRotation,
		VehicleMod.settings.vehicles.defaultGraphics.TryGetValue(vehicleDef.defName,
			fallback: vehicleDef.graphicData))
	{
		this.vehicleDef = vehicleDef;
	}

	public BlitRequest(VehiclePawn vehicle) : this(vehicle.VehicleDef.drawProperties.displayRotation,
		vehicle.patternData)
	{
		this.vehicleDef = vehicle.VehicleDef;
	}

	public static BlitRequest For(VehiclePawn vehicle)
	{
		VehicleDef vehicleDef = vehicle.VehicleDef;
		BlitRequest request = new(vehicle);
		request.blitTargets.Add(vehicleDef);
		if (vehicle.GetCachedComp<CompVehicleTurrets>() is { } compTurrets &&
			!compTurrets.Turrets.NullOrEmpty())
		{
			foreach (VehicleTurret turret in compTurrets.Turrets)
			{
				if (!turret.NoGraphic)
					request.blitTargets.Add(turret);
			}
		}
		if (!vehicle.DrawTracker.overlayRenderer.AllOverlaysListForReading.NullOrEmpty())
		{
			request.blitTargets.AddRange(vehicle.DrawTracker.overlayRenderer
			 .AllOverlaysListForReading);
		}
		return request;
	}

	public static BlitRequest For(VehicleDef vehicleDef)
	{
		BlitRequest request = new(vehicleDef);
		request.blitTargets.Add(vehicleDef);
		if (vehicleDef.GetSortedCompProperties<CompProperties_VehicleTurrets>() is { } props)
		{
			foreach (VehicleTurret turret in props.turrets)
			{
				if (!turret.NoGraphic)
					request.blitTargets.Add(turret);
			}
		}
		if (!vehicleDef.drawProperties.overlays.NullOrEmpty())
		{
			request.blitTargets.AddRange(vehicleDef.drawProperties.overlays);
		}
		return request;
	}
}
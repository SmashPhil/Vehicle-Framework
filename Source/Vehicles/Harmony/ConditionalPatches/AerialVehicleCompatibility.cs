using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using RimWorld;
using RimWorld.Planet;

namespace Vehicles.Compatibility;

public static class AerialVehicleCompatibility
{
  private static readonly Dictionary<Type, Settings> WorldObjectSettings = [];

	/// <summary>
	/// Register WorldObject type compatibility settings for aerial vehicle arrival options.
	/// </summary>
	/// <remarks><paramref name="type"/> must be a subclass of <see cref="WorldObject"/></remarks>
	/// <param name="type"><see cref="WorldObject"/> type being registered.</param>
	public static void RegisterWorldObjectType(Type type, Settings settings)
  {
		WorldObjectSettings.Add(type, settings);
  }

	/// <summary>
	/// Aerial vehicle is able to generate and land within the map.
	/// </summary>
	/// <param name="mapParent">MapParent being landed at.</param>
	/// <returns>
	/// <see langword="true"/> if aerial vehicle is able to generate and land in this map.
	/// </returns>
	public static bool CanLandIn(MapParent mapParent)
	{
		if (mapParent is Site or EscapeShip)
			return true;
		if (mapParent is SpaceMapParent)
			return true;

		if (WorldObjectSettings.TryGetValue(mapParent.GetType(), out Settings settings))
		{
			return settings.canLandInValidator != null ? settings.canLandInValidator(mapParent) : settings.canLandIn;
		}
		return false;
  }

	/// <summary>
	/// Claim map upon arrival, setting its faction to the faction of the <see cref="Faction.OfPlayer"/>
	/// </summary>
	/// <param name="mapParent">MapParent being arrived at.</param>
	/// <returns>
	/// <see langword="true"/> if world object should be set to player faction upon arrival. <see langword="false"/> if 
	/// it should leave the faction unchanged.
	/// </returns>
	public static bool ShouldClaimOnArrival(MapParent mapParent)
	{
		if (mapParent is EscapeShip)
			return true;

		if (WorldObjectSettings.TryGetValue(mapParent.GetType(), out Settings settings))
		{
			return settings.claimOnArrivalValidator != null ? settings.claimOnArrivalValidator(mapParent) : settings.claimOnArrival;
		}
		return false;
	}

	public class Settings
	{
		public required bool canLandIn;
		public required bool claimOnArrival;

		public CanLandIn canLandInValidator;
		public CanLandIn claimOnArrivalValidator;

		public delegate bool CanLandIn(MapParent mapParent);
		public delegate bool ShouldClaimOnArrival(MapParent mapParent);

		public Settings()
		{
		}

		[SetsRequiredMembers]
		public Settings(bool canLandIn, bool claimOnArrival)
		{
			this.canLandIn = canLandIn;
			this.claimOnArrival = claimOnArrival;
		}
	}
}
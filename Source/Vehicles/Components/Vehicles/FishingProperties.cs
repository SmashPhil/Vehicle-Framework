using System.Collections.Generic;
using JetBrains.Annotations;
using Verse;

namespace Vehicles.Compatibility;

[PublicAPI]
public class FishingProperties
{
	/// <summary>
	/// Cell offsets (relative to the vehicle) that are considered valid fishing spots.
	/// </summary>
	public List<IntVec2> fishingCells;

	/// <summary>
	/// Multiplier on fish yield/amount
	/// </summary>
	public float yieldModifier = 1;

	/// <summary>
	/// Multiplier on how often fish are pulled from the water.
	/// </summary>
	public float fishingTicksModifier = 1;

	/// <summary>
	/// If set, overrides the fisher’s animal skill with this fixed value; otherwise use the average
	/// skill level of all pawns onboard the vehicle.
	/// </summary>
	public int? animalSkillOverride;
}
using Verse;

namespace Vehicles;

public interface ISharedJobSearch
{
	ThingDef ThingDef { get; }

	bool ShouldConsiderPawn(Pawn pawn);

	bool IsMatchingThing(Thing thing);
}
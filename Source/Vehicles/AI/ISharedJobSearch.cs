using Verse;

namespace Vehicles;

public interface ISharedJobSearch
{
	bool ShouldConsiderPawn(Pawn pawn);

	bool IsMatchingThing(Thing thing);
}
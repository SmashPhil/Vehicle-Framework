using JetBrains.Annotations;
using Verse;

namespace Vehicles;

[PublicAPI]
public interface IAirDroppable : IExposable
{
	Thing Thing { get; }

	ThingDef SkyfallerDef { get; }

	void OnDropped(Map map, IntVec3 pos);

	void OnFailureToDrop(Map map, IntVec3 simPos);
}
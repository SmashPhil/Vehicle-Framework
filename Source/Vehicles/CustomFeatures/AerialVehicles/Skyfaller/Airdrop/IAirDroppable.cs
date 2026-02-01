using JetBrains.Annotations;
using Verse;

namespace Vehicles;

[PublicAPI]
public interface IAirDroppable : IExposable
{
	Thing Thing { get; }

	ThingDef SkyfallerDef { get; }

  int DropRadii { get; }

  bool TryDropAt(Map map, IntVec3 center, float angle);

	void OnFailureToDrop(Map map, IntVec3 pos);
}
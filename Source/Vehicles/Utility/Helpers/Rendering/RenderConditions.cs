using System;

namespace Vehicles.Rendering;

[Flags]
public enum RenderConditions
{
	None = 0,
	CurrentMap = 1 << 0,
	OnScreen = 1 << 1,

	Vanilla = CurrentMap | OnScreen
}
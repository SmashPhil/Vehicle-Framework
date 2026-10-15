using System;
using JetBrains.Annotations;
using Verse;

namespace SmashTools;

/// <summary>
/// RNG state block for RimWorld (unseeded).
/// </summary>
/// <remarks>Because Ludeon didn't add this to the default constructor of <see cref="RandBlock"/>.</remarks>
[PublicAPI]
public readonly struct RandStateBlock : IDisposable
{
	/// <summary>Pushes current <see cref="Verse.Rand"/> state.</summary>
	public RandStateBlock()
	{
		Rand.PushState();
	}

	/// <summary>Restores the previously pushed <see cref="Verse.Rand"/> state.</summary>
	void IDisposable.Dispose()
	{
		Rand.PopState();
	}
}
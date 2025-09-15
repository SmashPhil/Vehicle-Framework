using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace Vehicles;

[PublicAPI]
public class DropZone : IExposable
{
	public IntVec3 from;
	public IntVec3 to;
	private int count;

	public List<IntVec3> dropPoints;

	public DropZone(IntVec3 from, IntVec3 to, int count)
	{
		this.from = from;
		this.to = to;
		this.count = count;
		RecalculateDropPoints();
	}

	private void RecalculateDropPoints()
	{
		dropPoints = new List<IntVec3>(count);
		for (int i = 0; i < count; i++)
		{
			// Calculate 1 step in from endpoints
			float t = (float)(i + 1) / (count + 2);
			Vector2 pos = Vector2.Lerp(from.ToVector2(), to.ToVector2(), t);
			dropPoints.Add(new IntVec3(Mathf.RoundToInt(pos.x), 0, Mathf.RoundToInt(pos.y)));
		}
	}

	void IExposable.ExposeData()
	{
		Scribe_Values.Look(ref from, nameof(from));
		Scribe_Values.Look(ref to, nameof(from));
		Scribe_Values.Look(ref count, nameof(count));

		if (Scribe.mode == LoadSaveMode.LoadingVars)
		{
			RecalculateDropPoints();
		}
	}
}
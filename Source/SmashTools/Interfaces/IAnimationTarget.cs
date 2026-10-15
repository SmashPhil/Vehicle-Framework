using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace SmashTools
{
	public interface IAnimationTarget
	{
		(Vector3 drawPos, float rotation) DrawData { get; }

		ThingWithComps Thing { get; }

		IEnumerable<AnimationDriver> Animations { get; }
	}
}

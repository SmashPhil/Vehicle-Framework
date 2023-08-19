using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SmashTools;

namespace Vehicles
{
	public interface IMaterialCacheTarget
	{
		int MaterialCount { get; }

		PatternDef PatternDef { get; }

		string Name { get; }
	}
}

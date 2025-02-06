using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;

namespace Vehicles
{
	/// <summary>
	/// Interface for declaring category of Patch methods. Used for organization purposes only. (Use Attribute patching if you have questions. There's no difference here)
	/// </summary>
	public interface IPatchCategory
	{
		void PatchMethods();
	}
}

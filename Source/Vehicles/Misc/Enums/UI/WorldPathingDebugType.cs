using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vehicles
{
	//Cannot be flag based, debug drawer can only render 1 readable string at a time per tile
	public enum WorldPathingDebugType
	{
		None,
		PathCosts,
		Reachability,
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vehicles
{
	[Flags]
	public enum TargetLock : byte
	{
		None = 1 << 0,
		Cell = 1 << 1,
		Thing = 1 << 2,
		Pawn = 1 << 3,
	}
}

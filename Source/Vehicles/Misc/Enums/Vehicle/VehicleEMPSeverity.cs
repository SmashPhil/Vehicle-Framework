using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vehicles
{
	public enum VehicleEMPSeverity
	{
		None, // No Effect
		Tiny, // No damage, small chance to stun
		Minor, // Minor damage, small chance to stun
		Moderate, // Moderate damage, moderate chance to stun
		Severe, // Large damage, large chance to stun
	}
}
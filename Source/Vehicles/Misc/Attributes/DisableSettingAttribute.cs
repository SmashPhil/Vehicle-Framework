using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vehicles
{
	[AttributeUsage(AttributeTargets.Field)]
	public class DisableSettingAttribute : Attribute
	{
	}
}

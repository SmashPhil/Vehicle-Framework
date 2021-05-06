using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vehicles
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
	public class HeaderTitleAttribute : Attribute
	{
		/// <summary>
		/// Label of header displayed for class
		/// </summary>
		public string Label { get; set; }

		/// <summary>
		/// Translate using Label as translation key
		/// </summary>
		public bool Translate { get; set; }
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmashTools
{
	[AttributeUsage(AttributeTargets.Field)]
	public class GraphEditableAttribute : Attribute
	{
		public string Prefix { get; set; }

		public string Category { get; set; }

		public bool FunctionOfT { get; set; }
	}
}

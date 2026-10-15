using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmashTools
{
	public interface ITweakFields
	{
		string Category { get; }

		string Label { get; }

		void OnFieldChanged();
	}
}

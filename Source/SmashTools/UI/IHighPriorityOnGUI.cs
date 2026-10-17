using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmashTools
{
	/// <summary>
	/// Executes OnGUI method before all other windows and UI controls for input intercept
	/// </summary>
	public interface IHighPriorityOnGUI
	{
		void OnGUIHighPriority();
	}
}

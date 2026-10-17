using System.Collections.Generic;
using UnityEngine;

namespace SmashTools
{
	/// <summary>
	/// Render custom Inspect Pane for selected thing given interface method implementations
	/// </summary>
	public interface IInspectable
	{
		float DoInspectPaneButtons(float x);
	}
}

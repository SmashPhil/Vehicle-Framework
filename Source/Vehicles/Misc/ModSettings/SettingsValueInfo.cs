using System;
using System.Collections.Generic;
using System.Linq;
using SmashTools;

namespace Vehicles
{
	/// <summary>
	/// Use for storing display settings information for PostToSettings field with non-attribute settings
	/// </summary>
	public struct SettingsValueInfo
	{
		public float minValue;
		public float maxValue;

		public float endValue;
		public string endSymbol;
		public int roundDecimalPlaces;
		public float increment;
		public string minValueDisplay;
		public string maxValueDisplay;

		public UISettingsType settingsType;

		public bool IsValid => settingsType != UISettingsType.None;
	}
}

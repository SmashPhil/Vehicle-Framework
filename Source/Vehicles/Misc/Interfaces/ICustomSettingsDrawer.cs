using System.Reflection;

namespace Vehicles
{
	public interface ICustomSettingsDrawer
	{
		void DrawSetting(Listing_Settings lister, VehicleDef vehicleDef, FieldInfo field, string label, string tooltip, string disabledTooltip, bool locked, bool translate);
	}
}

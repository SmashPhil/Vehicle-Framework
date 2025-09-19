namespace Vehicles.World;

public interface ICaravanInfo
{
	bool AllowSelectionOfAllVehicles { get; }
	void NotifyTransferablesChanged();
}
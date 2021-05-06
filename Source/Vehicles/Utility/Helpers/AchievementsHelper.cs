using AchievementsExpanded;

namespace Vehicles.Achievements
{
	public static class AchievementsHelper
	{
		public static void TriggerVehicleConstructionEvent(VehiclePawn vehicle)
		{
			foreach (var card in AchievementPointManager.GetCards<VehicleBuilderTracker>())
			{
				if ((card.tracker as VehicleBuilderTracker).Trigger(vehicle))
				{
					card.UnlockCard();
				}
			}
		}
	}
}

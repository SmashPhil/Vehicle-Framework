namespace Vehicles.Config;

#pragma warning disable CS8793

internal class FeatureFlags
{
	public const string Raiders = "Raiders";
	public const string Paratroopers = "Paratroopers";

	public const bool RaidersEnabled =
		Build.Config == Build.Configuration.Debug ||
		Build.Config == Build.Configuration.Unstable;

	public const bool ParatroopersEnabled =
		Build.Config == Build.Configuration.Debug ||
		Build.Config == Build.Configuration.Unstable;
}
namespace Vehicles.Config;

internal interface IFeatureFlag
{
	string Name { get; }

	bool Enabled { get; }
}
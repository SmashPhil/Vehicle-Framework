namespace Vehicles.Config;

internal class Build
{
	public const Configuration Config =
#if DEBUG
		Configuration.Debug;
#elif UNSTABLE
		Configuration.Unstable;
#elif RELEASE
		Configuration.Release;
#endif

	internal enum Configuration
	{
		Debug,
		Unstable,
		Release
	}
}
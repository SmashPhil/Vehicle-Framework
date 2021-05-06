using Verse;

namespace Vehicles
{
	[StaticConstructorOnStartup]
	public static class Targeters
	{
		public static CannonTargeter CannonTargeter { get; } = new CannonTargeter();
		public static LaunchTargeter LaunchTargeter { get; } = new LaunchTargeter();
		public static LandingTargeter LandingTargeter { get; } = new LandingTargeter();
	}
}

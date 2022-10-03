using RimWorld;
using Verse;

namespace Vehicles
{
	public class VehicleFleshTypeDef : FleshTypeDef
	{
		///penetrating damage effect instead uses <see cref="FleshTypeDef.damageEffecter"/>
		public EffecterDef nonPenetrationEffect = EffecterDefOf.DamageDiminished_Metal;
		public EffecterDef diminishedEffect = EffecterDefOf.DamageDiminished_Metal;
		public EffecterDef deflectionEffect = EffecterDefOf.Deflect_Metal;
		public EffecterDef deflectionEffectBullet = EffecterDefOf.Deflect_Metal_Bullet;

		public SoundDef soundImpactDeflect;
		public SoundDef soundImpactDiminished;
		public SoundDef soundImpactNonPenetrated;
		public SoundDef soundImpactPenetrated;
	}
}

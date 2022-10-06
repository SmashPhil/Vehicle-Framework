using RimWorld;
using Verse;

namespace Vehicles
{
	public class VehicleFleshTypeDef : FleshTypeDef
	{
		///penetrating damage effect instead uses <see cref="FleshTypeDef.damageEffecter"/>
		public EffecterDef nonPenetrationEffect;
		public EffecterDef diminishedEffect;
		public EffecterDef deflectionEffect;
		public EffecterDef deflectionEffectBullet;

		public SoundDef soundImpactDeflect;
		public SoundDef soundImpactDiminished;
		public SoundDef soundImpactNonPenetrated;
		public SoundDef soundImpactPenetrated;

		public override void ResolveReferences()
		{
			base.ResolveReferences();
			nonPenetrationEffect ??= EffecterDefOf.DamageDiminished_Metal;
			diminishedEffect ??= EffecterDefOf.DamageDiminished_Metal;
			deflectionEffect ??= EffecterDefOf.Deflect_Metal;
			deflectionEffectBullet ??= EffecterDefOf.Deflect_Metal_Bullet;
		}
	}
}

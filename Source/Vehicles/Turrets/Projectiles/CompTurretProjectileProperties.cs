using Verse;

namespace Vehicles;

public class CompTurretProjectileProperties : ThingComp
{
  public float speed = -1;
  public CustomHitFlags hitflags;

  public CompTurretProjectileProperties(ThingWithComps parent)
  {
    this.parent = parent;
  }

  public override void PostExposeData()
  {
    base.PostExposeData();
    Scribe_Values.Look(ref speed, nameof(speed));
    Scribe_Defs.Look(ref hitflags, nameof(hitflags));
  }
}
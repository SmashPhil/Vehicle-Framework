using SmashTools.Animations;
using Verse;
using Verse.Sound;

namespace Vehicles;

/// <summary>
/// Rendering & Graphics
/// </summary>
public partial class VehiclePawn
{
  public ISustainerTarget SustainerTarget { get; private set; }

  public void SetSustainerTarget(ISustainerTarget sustainerTarget)
  {
    SustainerTarget = sustainerTarget;
  }

  public void ReleaseSustainerTarget()
  {
    sustainers.EndAll();
    SustainerTarget = null;
  }

  public virtual void SoundCleanup()
  {
    if (sustainers != null)
    {
      sustainers.EndAll();
    }
  }

  [AnimationEvent]
  private void PlaySound(SoundDef soundDef)
  {
    if (Spawned)
    {
      soundDef.PlayOneShot(this);
    }
  }

  [AnimationEvent]
  private void PlaySustainer(SoundDef soundDef)
  {
    if (Spawned)
    {
      sustainers.Spawn(this, soundDef);
    }
  }

  [AnimationEvent]
  private void EndSustainer(SoundDef soundDef)
  {
    sustainers.EndAll(soundDef);
  }

  [AnimationEvent]
  private void EndAllSustainers()
  {
    sustainers.EndAll();
  }
}

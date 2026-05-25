using JetBrains.Annotations;
using Verse;

namespace Vehicles;

public class UpgradeInProgress : IExposable
{
  private string nodeKey;

  [Unsaved]
  public UpgradeNode node;

  private VehiclePawn vehicle;
  private float workLeft;
  private bool removal;

  [UsedWithReflection]
  public UpgradeInProgress()
  {
  }

  public UpgradeInProgress(VehiclePawn vehicle, UpgradeNode node, bool removal)
  {
    nodeKey = node.key;

    this.node = node;
    this.vehicle = vehicle;

    WorkLeft = node.work;
    this.removal = removal;
  }

  public bool Removal => removal;

  public float WorkLeft
  {
    get
    {
      return workLeft;
    }
    set
    {
      workLeft = value;
    }
  }

  public void ExposeData()
  {
    Scribe_Values.Look(ref nodeKey, nameof(nodeKey));
    Scribe_References.Look(ref vehicle, nameof(vehicle));
    Scribe_Values.Look(ref workLeft, nameof(workLeft));
    Scribe_Values.Look(ref removal, nameof(removal));

    if (Scribe.mode == LoadSaveMode.PostLoadInit)
    {
      // Must get comp the normal way since comps will not be cached at this time
      node = vehicle.GetComp<CompUpgradeTree>().Props.def.GetNode(nodeKey);
    }
  }
}
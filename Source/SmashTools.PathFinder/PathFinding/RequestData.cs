using CoreLib.Collections;
using Unity.Mathematics;

namespace SmashTools.PathFinder
{
  internal struct RequestData
  {
    public int3 start;
    public int3 end;
    public int rotation;
    public TurnCost turnCost;
    public int turnCooldown;
    public int turnFactor;

    public bool useScalar;
    public int scalarFactor;
    public NativeBitArraySlim.ReadOnly scalar;
  }
}
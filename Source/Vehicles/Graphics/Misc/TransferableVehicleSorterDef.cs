using System;
using RimWorld;
using Verse;

namespace Vehicles;

public class TransferableVehicleSorterDef : Def
{
  public Type comparerClass;

  public TransferableComparer Comparer
  {
    get
    {
      field ??= (TransferableComparer)Activator.CreateInstance(comparerClass);
      return field;
    }
  }
}

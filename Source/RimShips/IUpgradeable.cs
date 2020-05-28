using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
namespace Vehicles
{
    public interface IUpgradeable
    {
        List<ThingDefCountClass> MaterialsNeeded();

        int GetSetTicks(int tickCount = 0);
    }
}

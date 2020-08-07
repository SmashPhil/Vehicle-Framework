using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;

namespace Vehicles
{
    public struct VehicleJobLimitations
    {
        public VehicleJobLimitations(string defName, int maxWorkers)
        {
            this.defName = defName;
            this.maxWorkers = maxWorkers;
        }

        public static VehicleJobLimitations Invalid => new VehicleJobLimitations(string.Empty, 0);

        public static VehicleJobLimitations FromString(string entry)
        {
            entry = entry.TrimStart(new char[] { '(' }).TrimEnd(new char[] { ')' });
            string[] data = entry.Split(new char[] { ',' });

            try
            {
                CultureInfo invariantCulture = CultureInfo.InvariantCulture;
	            string defName = Convert.ToString(data[0], invariantCulture);
	            int workers = Convert.ToInt32(data[1], invariantCulture);
                return new VehicleJobLimitations(defName, workers);
            }
            catch(Exception ex)
            {
                Log.Error($"{entry} is not a valid VehicleJobLimitations format. Exception: {ex}");
                return Invalid;
            }
        }

        public override string ToString()
        {
            return $"({defName},{maxWorkers})";
        }

        public string defName;
        public int maxWorkers;
    }
}

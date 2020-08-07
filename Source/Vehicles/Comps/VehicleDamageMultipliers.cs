using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Vehicles
{
    public struct VehicleDamageMultipliers
    {
        public float meleeDamageMultiplier;
        public float rangedDamageMultiplier;
        public float explosiveDamageMultiplier;

        public VehicleDamageMultipliers(float meleeDamageMultiplier, float rangedDamageMultiplier, float explosiveDamageMultiplier)
        {
            this.meleeDamageMultiplier = meleeDamageMultiplier;
            this.rangedDamageMultiplier = rangedDamageMultiplier;
            this.explosiveDamageMultiplier = explosiveDamageMultiplier;
        }

        public static VehicleDamageMultipliers Default => new VehicleDamageMultipliers(0.01f, 0.1f, 10f);

        public static VehicleDamageMultipliers FromString(string entry)
        { 
            entry = entry.TrimStart(new char[] { '(' }).TrimEnd(new char[] { ')' });
            string[] data = entry.Split(new char[] { ',' });

            try
            {
                CultureInfo invariantCulture = CultureInfo.InvariantCulture;
	            float meleeDamageMultiplier = Convert.ToSingle(data[0], invariantCulture);
	            float rangedDamageMultiplier = Convert.ToSingle(data[1], invariantCulture);
                float explosiveDamageMultiplier = Convert.ToSingle(data[2], invariantCulture);
                return new VehicleDamageMultipliers(meleeDamageMultiplier, rangedDamageMultiplier, explosiveDamageMultiplier);
            }
            catch(Exception ex)
            {
                Log.Error($"{entry} is not a valid VehicleDamageMultipliers format. Exception: {ex}");
                return Default;
            }
        }
        public override string ToString()
        {
            return $"({meleeDamageMultiplier},{rangedDamageMultiplier},{explosiveDamageMultiplier})";
        }
    }
}

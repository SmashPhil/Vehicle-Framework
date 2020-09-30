using System;
using System.Linq;
using System.Collections.Generic;
using Verse;

namespace Vehicles
{
    public static class PostLoadMethods
    {
        #region Parsing
        internal static void RegisterParsers()
        {
            ParseHelper.Parsers<FireMode>.Register(new Func<string, FireMode>(FireMode.FromString));
            ParseHelper.Parsers<RimworldTime>.Register(new Func<string, RimworldTime>(RimworldTime.FromString));
            ParseHelper.Parsers<VehicleJobLimitations>.Register(new Func<string, VehicleJobLimitations>(VehicleJobLimitations.FromString));
            ParseHelper.Parsers<VehicleDamageMultipliers>.Register(new Func<string, VehicleDamageMultipliers>(VehicleDamageMultipliers.FromString));
        }

        public static bool SameOrSubclass(this Type source, Type target)
        {
            return source == target || source.IsSubclassOf(target);
        }

        public static bool IsVehicleDef(this ThingDef td)
        {
            return td?.thingClass?.SameOrSubclass(typeof(VehiclePawn)) ?? false;
        }

        public static bool IsNumericType(this Type o)
        {   
            switch (Type.GetTypeCode(o))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsNumericType<T1, T2>(this SPTuple<T1, T2> o)
        {
            switch (Type.GetTypeCode(o.First.GetType()))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    break;
                default:
                    return false;
            }

            switch (Type.GetTypeCode(o.Second.GetType()))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    break;
                default:
                    return false;
            }
            return true;
        }
        #endregion
    }
}

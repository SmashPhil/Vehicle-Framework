using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using RimWorld;
using Verse;


namespace Vehicles.AI
{
    public class ShipReachabilityCache
    {
        public int Count
        {
            get
            {
                return this.cacheDict.Count;
            }
        }

        public BoolUnknown CachedResultFor(WaterRoom A, WaterRoom B, TraverseParms traverseParms)
        {
            bool flag;
            if (cacheDict.TryGetValue(new CachedEntry(A.ID, B.ID, traverseParms), out flag))
                return (!flag) ? BoolUnknown.False : BoolUnknown.True;
            return BoolUnknown.Unknown;
        }

        public void AddCachedResult(WaterRoom A, WaterRoom B, TraverseParms traverseParams, bool reachable)
        {
            Log.Message($"Null: {cacheDict is null} | {A is null} | {B is null}");
            CachedEntry key = new CachedEntry(A.ID, B.ID, traverseParams);
            if (!cacheDict.ContainsKey(key))
                cacheDict.Add(key, reachable);
        }

        public void Clear()
        {
            cacheDict.Clear();
        }

        public void ClearFor(Pawn p)
        {
            tmpCachedEntries.Clear();
            foreach(KeyValuePair<CachedEntry, bool> keyValuePair in this.cacheDict)
            {
                if (keyValuePair.Key.TraverseParms.pawn == p)
                    tmpCachedEntries.Add(keyValuePair.Key);
            }
            foreach (CachedEntry ce in tmpCachedEntries)
                cacheDict.Remove(ce);
            tmpCachedEntries.Clear();
        }

        public void ClearForHostile(Thing hostileTo)
        {
            tmpCachedEntries.Clear();
            foreach(KeyValuePair<CachedEntry, bool> keyValuePair in cacheDict)
            {
                Pawn p = keyValuePair.Key.TraverseParms.pawn;
                if (!(p is null) && p.HostileTo(hostileTo))
                    tmpCachedEntries.Add(keyValuePair.Key);
            }
            foreach (CachedEntry ce in tmpCachedEntries)
                cacheDict.Remove(ce);
            tmpCachedEntries.Clear();
        }

        private Dictionary<CachedEntry, bool> cacheDict = new Dictionary<CachedEntry, bool>();

        private static List<CachedEntry> tmpCachedEntries = new List<CachedEntry>();

        [StructLayout(LayoutKind.Sequential, Size = 1)]
        private struct CachedEntry : IEquatable<CachedEntry>
        {
            public CachedEntry(int firstRoomID, int secondRoomID, TraverseParms traverseParms)
            {
                this = default(CachedEntry);
                if(firstRoomID < secondRoomID)
                {
                    this.FirstRoomID = firstRoomID;
                    this.SecondRoomID = secondRoomID;
                }
                else
                {
                    this.FirstRoomID = secondRoomID;
                    this.SecondRoomID = firstRoomID;
                }
                this.TraverseParms = traverseParms;
            }

            public int FirstRoomID { get; private set; }

            public int SecondRoomID { get; private set; }

            public TraverseParms TraverseParms { get; private set; }

            public static bool operator ==(ShipReachabilityCache.CachedEntry lhs, ShipReachabilityCache.CachedEntry rhs)
            {
                return lhs.Equals(rhs);
            }

            public static bool operator !=(ShipReachabilityCache.CachedEntry lhs, ShipReachabilityCache.CachedEntry rhs)
            {
                return !lhs.Equals(rhs);
            }

            public override bool Equals(object obj)
            {
                return obj is ShipReachabilityCache.CachedEntry && this.Equals((ShipReachabilityCache.CachedEntry)obj);
            }

            public bool Equals(ShipReachabilityCache.CachedEntry other)
            {
                return this.FirstRoomID == other.FirstRoomID && this.SecondRoomID == other.SecondRoomID && this.TraverseParms == other.TraverseParms;
            }

            public override int GetHashCode()
            {
                int seed = Gen.HashCombineInt(this.FirstRoomID, this.SecondRoomID);
                return Gen.HashCombineStruct<TraverseParms>(seed, this.TraverseParms);
            }
        }
    }
}

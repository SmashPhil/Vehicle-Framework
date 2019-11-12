using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Harmony;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using UnityEngine;
using UnityEngine.AI;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;


namespace RimShips.AI
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
            if (this.cacheDict.TryGetValue(new ShipReachabilityCache.CachedEntry(A.ID, B.ID, traverseParms), out flag))
                return (!flag) ? BoolUnknown.False : BoolUnknown.True;
            return BoolUnknown.Unknown;
        }

        public void AddCachedResult(WaterRoom A, WaterRoom B, TraverseParms traverseParams, bool reachable)
        {
            return;
            ShipReachabilityCache.CachedEntry key = new ShipReachabilityCache.CachedEntry(A.ID, B.ID, traverseParams);
            if (!this.cacheDict.ContainsKey(key))
                this.cacheDict.Add(key, reachable);
        }

        public void Clear()
        {
            this.cacheDict.Clear();
        }

        public void ClearFor(Pawn p)
        {
            tmpCachedEntries.Clear();
            foreach(KeyValuePair<ShipReachabilityCache.CachedEntry, bool> keyValuePair in this.cacheDict)
            {
                if (keyValuePair.Key.TraverseParms.pawn == p)
                    tmpCachedEntries.Add(keyValuePair.Key);
            }
            foreach (ShipReachabilityCache.CachedEntry ce in tmpCachedEntries)
                this.cacheDict.Remove(ce);
            tmpCachedEntries.Clear();
        }

        public void ClearForHostile(Thing hostileTo)
        {
            tmpCachedEntries.Clear();
            foreach(KeyValuePair<ShipReachabilityCache.CachedEntry, bool> keyValuePair in this.cacheDict)
            {
                Pawn p = keyValuePair.Key.TraverseParms.pawn;
                if (!(p is null) && p.HostileTo(hostileTo))
                    tmpCachedEntries.Add(keyValuePair.Key);
            }
            foreach (ShipReachabilityCache.CachedEntry ce in tmpCachedEntries)
                this.cacheDict.Remove(ce);
            tmpCachedEntries.Clear();
        }

        private Dictionary<ShipReachabilityCache.CachedEntry, bool> cacheDict = new Dictionary<ShipReachabilityCache.CachedEntry, bool>();

        private static List<ShipReachabilityCache.CachedEntry> tmpCachedEntries = new List<ShipReachabilityCache.CachedEntry>();

        [StructLayout(LayoutKind.Sequential, Size = 1)]
        private struct CachedEntry : IEquatable<ShipReachabilityCache.CachedEntry>
        {
            public CachedEntry(int firstRoomID, int secondRoomID, TraverseParms traverseParms)
            {
                this = default(ShipReachabilityCache.CachedEntry);
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

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using RimWorld;
using Verse;


namespace Vehicles.AI
{
	/// <summary>
	/// Cache results from reachability calculations for faster retrieval
	/// </summary>
	public class VehicleReachabilityCache
	{
		private Dictionary<CachedEntry, bool> cacheDict = new Dictionary<CachedEntry, bool>();

		private static List<CachedEntry> tmpCachedEntries = new List<CachedEntry>();

		/// <summary>
		/// Cache count
		/// </summary>
		public int Count
		{
			get
			{
				return cacheDict.Count;
			}
		}

		/// <summary>
		/// Clear reachability cache
		/// </summary>
		public void Clear()
		{
			cacheDict.Clear();
		}

		/// <summary>
		/// Retrieve cached result for reachability from <paramref name="A"/> to <paramref name="B"/>
		/// </summary>
		/// <param name="A"></param>
		/// <param name="B"></param>
		/// <param name="traverseParms"></param>
		public BoolUnknown CachedResultFor(VehicleRoom A, VehicleRoom B, TraverseParms traverseParms)
		{
			if (cacheDict.TryGetValue(new CachedEntry(A.ID, B.ID, traverseParms), out bool reachable))
			{
				return reachable ? BoolUnknown.True : BoolUnknown.False;
			}
			return BoolUnknown.Unknown;
		}

		/// <summary>
		/// Add cached result for reachability from <paramref name="A"/> to <paramref name="B"/>
		/// </summary>
		/// <param name="A"></param>
		/// <param name="B"></param>
		/// <param name="traverseParams"></param>
		/// <param name="reachable"></param>
		public void AddCachedResult(VehicleRoom A, VehicleRoom B, TraverseParms traverseParams, bool reachable)
		{
			CachedEntry key = new CachedEntry(A.ID, B.ID, traverseParams);
			if (!cacheDict.ContainsKey(key))
				cacheDict.Add(key, reachable);
		}

		/// <summary>
		/// Clear all results for <paramref name="vehicle"/>
		/// </summary>
		/// <param name="vehicle"></param>
		public void ClearFor(VehiclePawn vehicle)
		{
			tmpCachedEntries.Clear();
			foreach(KeyValuePair<CachedEntry, bool> keyValuePair in cacheDict)
			{
				if (keyValuePair.Key.TraverseParms.pawn == vehicle)
				{
					tmpCachedEntries.Add(keyValuePair.Key);
				}
			}
			foreach (CachedEntry cachedEntry in tmpCachedEntries)
			{
				cacheDict.Remove(cachedEntry);
			}
			tmpCachedEntries.Clear();
		}

		/// <summary>
		/// Clear all results containing results targeting <paramref name="hostileTo"/>
		/// </summary>
		/// <param name="hostileTo"></param>
		public void ClearForHostile(Thing hostileTo)
		{
			tmpCachedEntries.Clear();
			foreach(KeyValuePair<CachedEntry, bool> keyValuePair in cacheDict)
			{
				if (keyValuePair.Key.TraverseParms.pawn is Pawn pawn && pawn.HostileTo(hostileTo))
				{
					tmpCachedEntries.Add(keyValuePair.Key);
				}
			}
			foreach (CachedEntry cachedEntry in tmpCachedEntries)
			{
				cacheDict.Remove(cachedEntry);
			}
			tmpCachedEntries.Clear();
		}

		/// <summary>
		/// Cached result data for reachability between two <see cref="VehicleRoom"/>
		/// </summary>
		[StructLayout(LayoutKind.Sequential, Size = 1)]
		private struct CachedEntry : IEquatable<CachedEntry>
		{
			public CachedEntry(int firstRoomID, int secondRoomID, TraverseParms traverseParms)
			{
				this = default;
				if(firstRoomID < secondRoomID)
				{
					FirstRoomID = firstRoomID;
					SecondRoomID = secondRoomID;
				}
				else
				{
					FirstRoomID = secondRoomID;
					SecondRoomID = firstRoomID;
				}
				TraverseParms = traverseParms;
			}

			public int FirstRoomID { get; private set; }

			public int SecondRoomID { get; private set; }

			public TraverseParms TraverseParms { get; private set; }

			public static bool operator ==(CachedEntry lhs, CachedEntry rhs)
			{
				return lhs.Equals(rhs);
			}

			public static bool operator !=(CachedEntry lhs, CachedEntry rhs)
			{
				return !lhs.Equals(rhs);
			}

			public override bool Equals(object obj)
			{
				return obj is CachedEntry entry && Equals(entry);
			}

			public bool Equals(CachedEntry other)
			{
				return FirstRoomID == other.FirstRoomID && SecondRoomID == other.SecondRoomID && TraverseParms == other.TraverseParms;
			}

			public override int GetHashCode()
			{
				int seed = Gen.HashCombineInt(FirstRoomID, SecondRoomID);
				return Gen.HashCombineStruct(seed, TraverseParms);
			}
		}
	}
}

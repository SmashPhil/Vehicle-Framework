using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using RimWorld;
using Verse;
using SmashTools;

namespace Vehicles
{
	/// <summary>
	/// Cache results from reachability calculations for faster retrieval
	/// </summary>
	public class VehicleReachabilityCache
	{
		private ConcurrentDictionary<CachedEntry, bool> cacheDict = new ConcurrentDictionary<CachedEntry, bool>();

		[ThreadStatic]
		private static HashSet<CachedEntry> tmpCachedEntries;

		private static HashSet<CachedEntry> CachedEntries
		{
			get
			{
				if (tmpCachedEntries == null)
				{
					tmpCachedEntries = new HashSet<CachedEntry>();
				}
				return tmpCachedEntries;
			}
		}

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
		public BoolUnknown CachedResultFor(VehicleRoom from, VehicleRoom to, TraverseParms traverseParms)
		{
			if (cacheDict.TryGetValue(new CachedEntry(from.id, to.id, traverseParms), out bool reachable))
			{
				return reachable ? BoolUnknown.True : BoolUnknown.False;
			}
			return BoolUnknown.Unknown;
		}

		/// <summary>
		/// Add cached result for reachability from <paramref name="A"/> to <paramref name="B"/>
		/// </summary>
		public void AddCachedResult(VehicleRoom from, VehicleRoom to, TraverseParms traverseParams, bool reachable)
		{
			CachedEntry key = new CachedEntry(from.id, to.id, traverseParams);
			cacheDict.TryAdd(key, reachable);
		}

		/// <summary>
		/// Clear all results for <paramref name="vehicle"/>
		/// </summary>
		/// <param name="vehicle"></param>
		public void ClearFor(VehiclePawn vehicle)
		{
			CachedEntries.Clear();
			foreach ((CachedEntry entry, bool result) in cacheDict)
			{
				if (entry.traverseParms.pawn == vehicle)
				{
					CachedEntries.Add(entry);
				}
			}
			foreach (CachedEntry cachedEntry in CachedEntries)
			{
				cacheDict.TryRemove(cachedEntry, out _);
			}
			CachedEntries.Clear();
		}

		/// <summary>
		/// Clear all results containing results targeting <paramref name="hostileTo"/>
		/// </summary>
		/// <param name="hostileTo"></param>
		public void ClearForHostile(Thing hostileTo)
		{
			CachedEntries.Clear();
			foreach ((CachedEntry entry, bool result) in cacheDict)
			{
				if (entry.traverseParms.pawn is Pawn pawn && pawn.HostileTo(hostileTo))
				{
					CachedEntries.Add(entry);
				}
			}
			foreach (CachedEntry cachedEntry in CachedEntries)
			{
				cacheDict.TryRemove(cachedEntry, out _);
			}
			CachedEntries.Clear();
		}

		/// <summary>
		/// Cached result data for reachability between two <see cref="VehicleRegion"/>
		/// </summary>
		private readonly struct CachedEntry : IEquatable<CachedEntry>
		{
			public readonly int from;
			public readonly int to;
			public readonly TraverseParms traverseParms;

			public CachedEntry(int from, int to, TraverseParms traverseParms)
			{
				this = default;
				if (from < to)
				{
					this.from = from;
					this.to = to;
				}
				else
				{
					this.from = to;
					this.to = from;
				}
				this.traverseParms = traverseParms;
			}

			public static bool operator ==(CachedEntry lhs, CachedEntry rhs)
			{
				return lhs.Equals(rhs);
			}

			public static bool operator !=(CachedEntry lhs, CachedEntry rhs)
			{
				return !lhs.Equals(rhs);
			}

			public override readonly bool Equals(object obj)
			{
				return obj is CachedEntry entry && Equals(entry);
			}

			public readonly bool Equals(CachedEntry other)
			{
				return from == other.from && to == other.to && traverseParms == other.traverseParms;
			}

			public override readonly int GetHashCode()
			{
				int seed = Gen.HashCombineInt(from, to);
				return Gen.HashCombineStruct(seed, traverseParms);
			}
		}
	}
}

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using JetBrains.Annotations;
using UnityEngine.Assertions;

namespace CoreLib.Performance;

/// <summary>
/// Static implementation of ObjectPool with thread safe operations for retrieving and returning
/// items to the pool.
/// </summary>
/// <typeparam name="T">Reference type with public default constructor.</typeparam>
[PublicAPI]
public static class AsyncPool<T> where T : class, new()
{
	private static ConcurrentBag<T> bag = [];

	/// <summary>
	/// Tracks the amount of objects created from AsyncPool retrievals and how many are returned.
	/// </summary>
	/// <remarks>If more are returned than created, this will cause a memory leak. 
	/// Return only what has been retrieved via <see cref="Get"/></remarks>
	[SuppressMessage("ReSharper", "StaticMemberInGenericType",
		Justification = "This is a per-type counter for debugging, we don't want this to be shared.")]
	private static int counter;

	public static int Count => bag.Count;

	/// <summary>
	/// Remove item from object pool.
	/// </summary>
	/// <remarks>If object pool is empty, a new item will be created.</remarks>
	public static T Get()
	{
		if (!bag.TryTake(out T item))
		{
			item = new T();
		}
		// Decrement even for new object instantiations, they're also expected to be returned
		// to the object pool.
		ItemRemoved();
		return item;
	}

	/// <summary>
	/// Return item to object pool.
	/// </summary>
	public static void Return(T item)
	{
		ItemReturned();
		bag.Add(item);
	}

  [Conditional("DEBUG")]
	private static void ItemReturned()
	{
		Interlocked.Increment(ref counter);
		// More items have been borrowed than returned to bag. Should this ever increment over 0,
		// it means more objects are being returned than retrieved.
		Assert.IsTrue(counter <= 0);
	}

	[Conditional("DEBUG")]
	private static void ItemRemoved()
	{
		Interlocked.Decrement(ref counter);
	}

	public static void PreWarm(int count)
	{
		int countToAdd = count - Count;
		if (countToAdd > 0)
		{
			for (int i = 0; i < countToAdd; i++)
			{
				// Decrement debug counter so it can expect the item to be returned.
				ItemRemoved();
				Return(new T());
			}
		}
	}

	internal static void Clear()
	{
		// Easier to do a reference swap than to take out every single item in a loop. We don't yet
		// have access to ConcurrentBag.Clear, it's only available in .Net Framework 5.0
		ConcurrentBag<T> newBag = [];
		bag = newBag;
		counter = 0;
	}


	/// <summary>
	/// Scoped acquisition of AsyncPool object.
	/// </summary>
	[PublicAPI]
	public readonly struct Scope : IDisposable
	{
		private readonly T item;

		public Scope(out T item)
		{
			this.item = Get();
			item = this.item;
		}

		void IDisposable.Dispose()
		{
			Return(item);
		}
	}
}
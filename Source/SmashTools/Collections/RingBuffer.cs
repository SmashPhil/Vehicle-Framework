using System;
using JetBrains.Annotations;
using Verse;

namespace SmashTools;

/// <summary>
/// A fixed-size circular buffer that overwrites the oldest elements when new elements are pushed beyond its capacity.
/// </summary>
/// <typeparam name="T">Type of elements stored in the circular buffer.</typeparam>
[PublicAPI]
public class RingBuffer<T>
{
	private readonly T[] array;
	private int head;
	private int tail;

	private readonly object syncRoot = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="RingBuffer{T}"/> class with the specified capacity.
	/// </summary>
	/// <param name="size">The fixed capacity of the circular array.</param>
	public RingBuffer(int size)
	{
		if (size < 0)
			throw new ArgumentOutOfRangeException(nameof(size));

		array = new T[size];
	}

	/// <summary>
	/// Gets the total capacity of the circular buffer.
	/// </summary>
	public int Length => array.Length;

	/// <summary>
	/// Gets the underlying backing array for inspection or advanced operations.
	/// </summary>
	public T[] InnerArray => array;

	/// <summary>
	/// Gets the element at the specified logical index, accounting for wrap-around.
	/// </summary>
	/// <param name="index">The zero-based logical index within the buffer.</param>
	/// <returns>The element at the given logical position.</returns>
	public T this[int index]
	{
		get
		{
			lock (syncRoot)
			{
				int realIndex = GenMath.PositiveMod(tail + index, Length);
				return array[realIndex];
			}
		}
	}

	/// <summary>
	/// Adds an item to the head of the buffer, overwriting the oldest element if the buffer is full.
	/// </summary>
	/// <param name="item">The item to push into the buffer.</param>
	/// <returns>The element that was dropped (overwritten), or default(T) if the slot was empty.</returns>
	public T Push(T item)
	{
		lock (syncRoot)
		{
			T dropped = array[head];
			array[head] = item;
			head = GenMath.PositiveMod(++head, Length);
			if (head == tail)
				tail = GenMath.PositiveMod(++tail, Length);
			return dropped;
		}
	}

	/// <summary>
	/// Removes (resets to default) the element at the specified logical index without shifting other elements.
	/// </summary>
	/// <param name="index">The zero-based logical index of the element to remove.</param>
	public void RemoveAt(int index)
	{
		lock (syncRoot)
		{
			int realIndex = GenMath.PositiveMod(tail + index, Length);
			array[realIndex] = default!;
		}
	}
}
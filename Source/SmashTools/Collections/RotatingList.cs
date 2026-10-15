using System.Collections.Generic;

namespace SmashTools;

public class RotatingList<T> : List<T>
{
	private int currentIndex;

	public RotatingList() : base()
	{
		currentIndex = 0;
	}

	public RotatingList(int capacity) : base(capacity)
	{
		currentIndex = 0;
	}

	public RotatingList(IEnumerable<T> enumerable) : base(enumerable)
	{
		currentIndex = 0;
	}

	public int Index => currentIndex;

	public T Next
	{
		get
		{
			T item = this[currentIndex++];
			if (currentIndex >= Count)
			{
				currentIndex = 0;
			}
			return item;
		}
	}

	public T Previous
	{
		get
		{
			T item = this[currentIndex--];
			if (currentIndex < 0)
			{
				currentIndex = Count - 1;
			}
			return item;
		}
	}

	public T Current
	{
		get
		{
			return this[currentIndex];
		}
	}

	public void PostItemRemove()
	{
		if (Count == 0)
		{
			currentIndex = 0;
		}
		else
		{
			currentIndex %= Count;
		}
	}

	public void SetIndex(int index)
	{
		currentIndex = index % Count;
	}
}
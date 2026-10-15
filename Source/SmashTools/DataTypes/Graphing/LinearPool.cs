using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;

namespace SmashTools
{
	/// <summary>
	/// Linearly distributes generic items across a range. Can fetch based on some in-range value rather than using out if / switch blocks
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class LinearPool<T>
	{
		public List<T> items = new List<T>();
		public FloatRange range = FloatRange.ZeroToOne;

		public LinearPool()
		{
		}

		public LinearPool(List<T> items)
		{
			this.items = items;
		}

		public LinearPool(List<T> items, FloatRange range)
		{
			this.items = items;
			this.range = range;
			if (range.max <= range.min)
			{
				Log.Error($"Attempting to initialize LinearPool with non-sequential bounderies.  This is not allowed!");
				range = FloatRange.ZeroToOne;
			}
		}

		public int PointsCount => items.Count;

		public bool IsValid => !items.NullOrEmpty();

		public T this[float value] => Evaluate(value);

		public virtual T Evaluate(float value)
		{
			if (items.NullOrEmpty())
			{
				return default(T);
			}
			if (items.Count == 1)
			{
				return items[0];
			}
			if (value <= range.min)
			{
				return items.FirstOrDefault();
			}
			else if (value >= range.max)
			{
				return items.LastOrDefault();
			}

			float t = value / (range.max - range.min);
			int index = Mathf.Clamp(Mathf.RoundToInt(items.Count * t), 0, items.Count - 1);
			return items[index];
		}
	}
}

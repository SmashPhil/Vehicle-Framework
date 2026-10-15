using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmashTools
{
	public static class Ext_LinkedList
	{
		public static void Populate<T>(this LinkedList<T> list, IEnumerable<T> collection)
		{
			if (list == null)
			{
				throw new ArgumentNullException(nameof(list));
			}
			if (collection == null)
			{
				throw new ArgumentNullException(nameof(collection));
			}
			
			using IEnumerator<T> enumerator = collection.GetEnumerator();
			while (enumerator.MoveNext())
			{
				T item = enumerator.Current;
				LinkedListNode<T> node = list.AddLast(item);
			}
		}

		public static LinkedListNode<T> Pop<T>(this LinkedList<T> list)
		{
			LinkedListNode<T> item = list.First;
			list.RemoveFirst();
			return item;
		}
	}
}
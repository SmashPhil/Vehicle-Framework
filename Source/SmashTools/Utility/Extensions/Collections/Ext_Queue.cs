using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmashTools
{
	public static class Ext_Queue
	{
		public static bool NullOrEmpty<T>(this Queue<T> queue)
		{
			return queue is null || queue.Count == 0;
		}
	}
}

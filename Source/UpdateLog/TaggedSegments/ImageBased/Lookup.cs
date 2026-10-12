using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpdateLogTool
{
	public class Lookup
	{
		private Dictionary<string, object> innerDictionary = new Dictionary<string, object>();

		public object this[string key]
		{
			set
			{
				innerDictionary[key.ToUpperInvariant()] = value;
			}
		}

		public T Get<T>(string key, T fallback = default)
		{
			if (innerDictionary.TryGetValue(key.ToUpperInvariant(), out object value))
			{
				return (T)value;
			}
			return fallback;
		}
	}
}

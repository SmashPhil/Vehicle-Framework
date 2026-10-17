using Verse;
using UnityEngine;

namespace UpdateLogTool
{
	public abstract class TaggedSegment
	{
		/// <summary>
		/// Defines start and end Tags
		/// </summary>
		/// <value>(Opening Tag, Closing Tag)</value>
		public abstract (string open, string close) Tags { get; }

		/// <param name="innerText"></param>
		/// <returns>Height reserved for Scrollview in Update Dialog</returns>
		public virtual int HeightOccupied(UpdateLog log, string fullText) => 0;

		/// <summary>
		/// Performs action given text between tags
		/// </summary>
		/// <param name="lister"></param>
		/// <param name="log"></param>
		/// <param name="innerText"></param>
		public abstract void SegmentAction(Listing_Rich lister, string innerText);
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace UpdateLogTool
{
	public class DescriptionData
	{
		public DescriptionData()
		{
		}

		public DescriptionData(string text)
		{
			this.text = text;
		}

		/// <summary>
		/// InnerText either between brackets or freeform
		/// </summary>
		public string text;

		/// <summary>
		/// Assigned TagSegment for custom drawing / actions in UpdateNews Window
		/// </summary>
		public TaggedSegment tag;

		/// <summary>
		/// All segments of texted currently enveloped in &lt;u/&gt; brackets
		/// </summary>
		public string[] underlineText;

		/// <summary>
		/// All hyperlinks in original format: &lt;link&gt;url...&lt;/link&gt;(display name)
		/// </summary>
		public string[] hyperlinks;
	}
}

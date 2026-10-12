using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;

namespace UpdateLogTool
{
	public static class SegmentParser
	{
		public static readonly List<TaggedSegment> tags = new List<TaggedSegment>();

		public static string regexTags;

		public static void ParseAndCreateSegments()
		{
			foreach (Type type in GenTypes.AllTypes.Where(t => t.IsSubclassOf(typeof(TaggedSegment)) && !t.IsAbstract))
			{
				TaggedSegment newSegment = (TaggedSegment)Activator.CreateInstance(type);
				tags.Add(newSegment);
			}
		}

		public static void GenerateRegexText()
		{
			//Final Result = "(<tag>.*?<\/tag>)|(<tag2>.*?<\/tag2>)|(.+?(?=<tag>|<tag2>|$))"
			regexTags = string.Empty;
			for (int i = 0; i < tags.Count; i++, regexTags += "|")
			{
				TaggedSegment tag = tags[i];
				if (!tag.Tags.close.NullOrEmpty())
				{
					regexTags += "(";
					regexTags += tag.Tags.open;
					regexTags += ".*?";
					regexTags += tag.Tags.close.Replace("/", @"\/");
					regexTags += ")";
				}
				else
				{
					regexTags += "(";
					regexTags += tag.Tags.open.Replace("/", @"\/");
					regexTags += ")";
				}
			}
			regexTags += "(.+?(?=";
			for (int i = 0; i < tags.Count; i++, regexTags += "|")
			{
				TaggedSegment tag = tags[i];
				if (tag.Tags.close.NullOrEmpty())
				{
					regexTags += tag.Tags.open;
				}
				else
				{
					regexTags += tag.Tags.open.Replace("/", @"\/");;
				}
			}
			regexTags += "$))";
		}

		public static string ColoredRegexForOutput(string regex)
		{
			string coloredRegex = regex;
			int indexOffset = 0;

			//Starting color is Teal
			string startingTag = ColorFor(RColor.Teal);
			coloredRegex = coloredRegex.Insert(0, startingTag);
			indexOffset += startingTag.Length;
			Color currentColor = RColor.Teal;

			void CloseBrackets(int index)
			{
				coloredRegex = coloredRegex.Insert(index + indexOffset, "</color>");
				indexOffset += "</color>".Length;
			}
			
			try
			{
				for (int i = 0; i < regex.Length; i++)
				{
					if (regex[i] == '*' || regex[i] == '?' || regex[i] == '+')
					{
						if (currentColor != RColor.HotPink)
						{
							CloseBrackets(i);
							string colorTag = ColorFor(RColor.HotPink);
							coloredRegex = coloredRegex.Insert(i + indexOffset, colorTag);
							indexOffset += colorTag.Length;
							currentColor = RColor.HotPink;
						}
					}
					else if ((regex[i] == '.' || regex[i] == '(' || regex[i] == ')') && (i == 0 || regex[i - 1] != '\\'))
					{
						if (currentColor != RColor.Teal)
						{
							CloseBrackets(i);
							string colorTag = ColorFor(RColor.Teal);
							coloredRegex = coloredRegex.Insert(i + indexOffset, colorTag); 
							indexOffset += colorTag.Length;
							currentColor = RColor.Teal;
						}
					}
					else
					{
						if (currentColor != RColor.LightOrange)
						{
							CloseBrackets(i);
							string colorTag = ColorFor(RColor.LightOrange);
							coloredRegex = coloredRegex.Insert(i + indexOffset, colorTag);
							indexOffset += colorTag.Length;
							currentColor = RColor.LightOrange;
						}
					}
				}
			}
			catch (ArgumentOutOfRangeException)
			{
				Log.Error($"Unable to colorify regex. Offset: {indexOffset} Length: {coloredRegex.Length} Regex=\"{regex}\"");
			}
			
			return @coloredRegex;
		}

		public static string ColorFor(Color color)
		{
			string hex = ColorUtility.ToHtmlStringRGBA(color);
			return $"<color=#{hex}>";
		}

		public struct RColor
		{
			public static Color Teal => new Color(0.177f, 0.71f, 0.451f);

			public static Color LightOrange => new Color(0.9f, 0.59f, 0.275f);

			public static Color HotPink => new Color(0.843f, 0.275f, 0.785f);
		}
	}
}

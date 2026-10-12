using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using Verse;
using UnityEngine;

namespace UpdateLogTool
{
    public static class EnhancedText
    {
        public const string hyperlinkTag = "<link>";
        public const string hyperLinkEndTag = "</link>";

        public const string underlineTag = "<u>";
        public const string underlineEndTag = "</u>";

        private static TaggedSegment TaggedSegment(string segment)
        {
            return SegmentParser.tags.FirstOrDefault(t => segment.Contains(t.Tags.open));
        }

        /// <summary>
        /// Parse UpdateLog's description into DescriptionData with RichText and custom brackets.
        /// </summary>
        /// <param name="log"></param>
        public static IEnumerable<DescriptionData> ParseDescriptionData(UpdateLog log) => ParseDescriptionData(log.UpdateData.EnhancedDescription);

        /// <summary>
        /// Parse specific string of data. 
        /// </summary>
        /// <remarks>If using this to parse outside text and draw with DescriptionData tagged segments not within the Dialog_NewUpdate window, the &lt;img/&gt; tag will not work.</remarks>
        /// <param name="description"></param>
        public static IEnumerable<DescriptionData> ParseDescriptionData(string description)
        {
            string[] segments = Regex.Split(description, 
                SegmentParser.regexTags, 
                RegexOptions.Singleline, TimeSpan.FromSeconds(3));
            for (int i = 0; i < segments.Length; i++)
            {
                string segment = segments[i];
                if (segment.NullOrEmpty() || segment == Environment.NewLine)
                {
                    continue;
                }
                else if (TaggedSegment(segment) is TaggedSegment tag)
                {
                    string strippedText = segment.Replace(tag.Tags.open, string.Empty);
                    if (!tag.Tags.close.NullOrEmpty())
                    {
                        strippedText = strippedText.Replace(tag.Tags.close, string.Empty);
                    }
                    yield return new DescriptionData(strippedText)
                    {
                        tag = tag
                    };
                }
                else
                {
                    string[] underlinedSegments = Regex.Matches(segment, @"(<u>.*?<\/u>)",
                        RegexOptions.Singleline, TimeSpan.FromSeconds(1)).Cast<Match>().Select(m => m.Value)
                                                                         .Where(s => !string.IsNullOrWhiteSpace(s) && !s.NullOrEmpty() && s != Environment.NewLine).ToArray();
                    string[] hyperlinkSegments = Regex.Matches(segment, @"(<link>.*?<\/link>.+?(?=\))\))",
                        RegexOptions.Singleline, TimeSpan.FromSeconds(1)).Cast<Match>().Select(m => m.Value)
                                                                         .Where(s => !string.IsNullOrWhiteSpace(s) && !s.NullOrEmpty() && s != Environment.NewLine).ToArray();
                    yield return new DescriptionData(segment)
                    {
                        underlineText = underlinedSegments,
                        hyperlinks = hyperlinkSegments
                    };
                }
            }
        }
    }
}

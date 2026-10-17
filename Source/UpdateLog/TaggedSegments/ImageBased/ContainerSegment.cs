using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace UpdateLogTool
{
	public abstract class ContainerSegment : TaggedSegment
	{
		protected virtual IEnumerable<(string name, Type type)> Attributes
		{
			get
			{
				yield return (TagNames.Width, typeof(int));
				yield return (TagNames.Height, typeof(int));
			}
		}

		protected virtual string GetInnerText(string fullText)
		{
			string[] innerTexts = fullText.Split('>');
			if (innerTexts.Length > 1)
			{
				return innerTexts[1].Split('<').FirstOrDefault();
			}
			return fullText;
		}

		public override int HeightOccupied(UpdateLog log, string fullText)
		{
			string[] fullTexts = fullText.Split('>');
			if (fullTexts.Length < 1)
			{
				Log.ErrorOnce($"Incorrect split for bracketText in {fullText}.", fullText.GetHashCode());
				return Mathf.FloorToInt(Dialog_NewUpdate.PreviewImageHeight);
			}
			string innerText = fullTexts.Last();
			int width = Mathf.FloorToInt(Dialog_NewUpdate.DialogWidth);
			int height = width;
			if (fullTexts.Length > 1 && !fullTexts.FirstOrDefault().NullOrEmpty())
			{
				string bracketText = fullTexts[0];
				innerText = fullTexts.Last();
				Lookup lookup = ContainerAttributes(bracketText);

				width = lookup.Get(TagNames.Width, width);
				height = lookup.Get(TagNames.Height, width);
			}
			string innerInnerText = GetInnerText(innerText);
			if (log.cachedTextures.TryGetValue(innerInnerText, out Texture2D texture))
			{
				if (height == width && texture)
				{
					height = Mathf.CeilToInt(texture.height / (float)texture.width * width);
				}
			}
			else if (log.cachedDownloadedTextures.TryGetValue(innerInnerText, out WebTexture webTexture))
			{
				if (height == width && webTexture.texture)
				{
					height = Mathf.CeilToInt(webTexture.texture.height / (float)webTexture.texture.width * width);
				}
			}
			return height;
		}

		protected virtual (string name, string value) ParseAttribute(string fullAttribute)
		{
			string[] propertyData = fullAttribute.Split('=');
			string name = propertyData[0];
			string value = propertyData[1];
			return (name, value);
		}

		protected Lookup ContainerAttributes(string bracketText)
		{
			Lookup lookup = new Lookup();
			string step = "Splitting bracketText";
			try
			{
				string[] attributes = bracketText.Split('>');
				if (attributes.Length > 0)
				{
					List<(string key, Type type)> registeredAttributes = Attributes.ToList();
					string[] properties = attributes[0].Split(' ');
					foreach (string attribute in properties.Where(s => !string.IsNullOrWhiteSpace(s)))
					{
						step = attribute;
						(string name, string value) = ParseAttribute(attribute);
						value = value.Trim('\"');

						foreach ((string key, Type type) in registeredAttributes)
						{
							if (name.ToUpperInvariant() == key.ToUpperInvariant())
							{
								object result = ParseHelper.FromString(value, type);
								lookup[name.ToUpperInvariant()] = result;
								break;
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Log.ErrorOnce($"Exception thrown grabbing inner properties {bracketText}\nFailed: {step}\nException={ex}", bracketText.GetHashCode());
			}
			return lookup;
		}
	}
}

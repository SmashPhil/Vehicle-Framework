using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using JetBrains.Annotations;
using UnityEngine;

namespace SmashTools.Xml;

[PublicAPI]
public static class XmlExporter
{
	private static XmlWriter writer;
	private static FileStream stream;
	private static string filePath;

	private const string ListItemNodeName = "li";

	public static void StartDocument(string filePath)
	{
		XmlExporter.filePath = filePath;
		stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
		writer = XmlWriter.Create(stream, new XmlWriterSettings
		{
			Indent = true,
			IndentChars = "\t",
			NewLineChars = "\n",
			Encoding = Encoding.UTF8,
		});
		writer.WriteStartDocument();
	}

	public static void Export()
	{
		Application.OpenURL(filePath);
	}

	public static void Close()
	{
		writer.WriteEndDocument();
		writer.Flush();
		writer.Close();
		writer.Dispose();

		stream.Close();
		stream.Dispose();

		stream = null;
		writer = null;
		filePath = string.Empty;
	}

	public static void OpenNode(string name, params (string name, string value)[] attributes)
	{
		try
		{
			writer.WriteStartElement(name);
			if (!attributes.NullOrEmpty())
			{
				foreach ((string name, string value) attribute in attributes)
				{
					if (attribute.name.NullOrEmpty())
					{
						continue;
					}
					writer.WriteAttributeString(attribute.name, attribute.value);
				}
			}
		}
		catch
		{
			Close();
			throw;
		}
	}

	public static void CloseNode(bool emptyText = false)
	{
		try
		{
			if (emptyText)
			{
				writer.WriteFullEndElement();
			}
			else
			{
				writer.WriteEndElement();
			}
		}
		catch
		{
			Close();
			throw;
		}
	}

	public static void WriteObject<T>(string localName, T value)
	{
		try
		{
			if (value == null)
			{
				OpenNode(localName, ("IsNull", "TRUE"));
				CloseNode();
			}
			else
			{
				writer.WriteElementString(localName, value.ToString());
			}
		}
		catch
		{
			Close();
			throw;
		}
	}

	public static void WriteElement(string localName, string value)
	{
		try
		{
			writer.WriteElementString(localName, value);
		}
		catch
		{
			Close();
			throw;
		}
	}

	public static void WriteNullElement(string localName)
	{
		try
		{
			OpenNode(localName, ("IsNull", "TRUE"));
			CloseNode();
		}
		catch
		{
			Close();
			throw;
		}
	}

	public static void WriteElement(string localName, IXmlExport value)
	{
		try
		{
			if (value == null)
			{
				WriteNullElement(localName);
				return;
			}
			OpenNode(localName);
			{
				value.Export();
			}
			CloseNode();
		}
		catch
		{
			Close();
			throw;
		}
	}

	public static void WriteString(string text)
	{
		try
		{
			writer.WriteString(text);
		}
		catch
		{
			Close();
			throw;
		}
	}

	public static void WriteCollection<T>(string localName, IEnumerable<T> value,
		Func<T, (string name, string value)> attributeGetter = null) where T : IXmlExport
	{
		try
		{
			if (value == null)
			{
				WriteNullElement(localName);
				return;
			}
			OpenNode(localName);
			foreach (T item in value)
			{
				OpenNode(ListItemNodeName, attributeGetter != null ? new (string, string)[] { attributeGetter(item) } : null);
				item.Export();
				CloseNode();
			}
			CloseNode();
		}
		catch
		{
			Close();
			throw;
		}
	}

	public static void WriteList<T>(string localName, IList<T> value, Action<T> itemWriter)
	{
		try
		{
			if (value == null)
			{
				WriteNullElement(localName);
				return;
			}
			OpenNode(localName);
			foreach (T item in value)
			{
				OpenNode(ListItemNodeName);
				itemWriter(item);
				CloseNode();
			}
			CloseNode();
		}
		catch
		{
			Close();
			throw;
		}
	}

	public static void WriteList<T>(string localName, IList<T> value, Action<T> itemWriter,
		Func<T, string> listItemNameGetter)
	{
		try
		{
			if (value == null)
			{
				WriteNullElement(localName);
				return;
			}
			OpenNode(localName);
			foreach (T item in value)
			{
				OpenNode(listItemNameGetter(item));
				itemWriter(item);
				CloseNode();
			}
			CloseNode();
		}
		catch
		{
			Close();
			throw;
		}
	}
}
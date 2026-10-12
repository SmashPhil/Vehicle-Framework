using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Collections;
using System.Xml;
using System.Xml.Linq;
using System.Threading;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;
using UnityEngine.Networking;

namespace UpdateLogTool
{
	public class UpdateLog : IDisposable
	{
		public static string[] AllowedImageExtensions = { ".png", ".jpg", ".jpeg", ".psd", ".bmp" };
		
		public ModContentPack Mod { get; private set; }

		public UpdateLogData UpdateData { get; private set; }

		public string CurrentFolder { get; private set; }

		public bool Disposed { get; private set; }


		public Dictionary<string, Texture2D> cachedTextures = new Dictionary<string, Texture2D>();

		public Dictionary<string, WebTexture> cachedDownloadedTextures = new Dictionary<string, WebTexture>();

		public UpdateLog (ModContentPack mod, string loadFolder, string path, bool updateVersion = true)
		{
			Mod = mod;
			CurrentFolder = loadFolder;
			UpdateData = FileReader.ParseUpdateData(path);
			if (updateVersion)
			{
				UpdateData.currentVersion = UpdateLogVersionFile(UpdateData.currentVersion);
			}
			UpdateData.rightIconBar ??= new List<UpdateLogData.HyperlinkedIcon>();
			UpdateData.leftIconBar ??= new List<UpdateLogData.HyperlinkedIcon>();
			UpdateData.images ??= new List<UpdateLogData.UploadedImages>();
			Disposed = true;
		}

		public UpdateLog(ModContentPack mod, string loadFolder, bool updateVersion = true)
		{
			Mod = mod;
			CurrentFolder = loadFolder;
			UpdateData = FileReader.ParseUpdateData(Path.Combine(FileReader.UpdateLogDirectory(Mod, CurrentFolder), FileReader.UpdateLogFileName));
			if (updateVersion)
			{
				UpdateData.currentVersion = UpdateLogVersionFile(UpdateData.currentVersion);
			}
			if (UpdateData.rightIconBar is null)
			{
				UpdateData.rightIconBar = new List<UpdateLogData.HyperlinkedIcon>();
			}
			if (UpdateData.leftIconBar is null)
			{
				UpdateData.leftIconBar = new List<UpdateLogData.HyperlinkedIcon>();
			}
			Disposed = true;
		}

		public void Open()
		{
			if (!Disposed)
			{
				Dispose();
			}
			CacheImages();
			DownloadImages();
			Disposed = false;
		}

		public void Dispose()
		{
			foreach (Texture2D texture in cachedTextures.Values)
			{
				GameObject.Destroy(texture);
			}
			foreach (WebTexture webTexture in cachedDownloadedTextures.Values)
			{
				webTexture.Dispose();
			}
			cachedTextures.Clear();
			Disposed = true;
			
		}

		private void CacheImages()
		{
			if (Directory.Exists(FileReader.UpdateImagesDirectory(Mod, CurrentFolder)))
			{
				foreach (string file in Directory.GetFiles(FileReader.UpdateImagesDirectory(Mod, CurrentFolder), "*", SearchOption.AllDirectories))
				{
					if (!AllowedImageExtensions.Contains(Path.GetExtension(file)))
					{
						continue;
					}
					try
					{
						byte[] fileData = File.ReadAllBytes(file);
						Texture2D tex = new Texture2D(2, 2, TextureFormat.Alpha8, true);
						tex.LoadImage(fileData);
						tex.name = file.Split('\\').Last();
						cachedTextures.Add(Path.GetFileNameWithoutExtension(tex.name), tex);
					}
					catch (Exception ex)
					{
						Log.Error($"Unable to load file {file} into Texture2D. Are you using an unsupported image type? Exception=\"{ex}\"");
					}
				}
			}
		}

		private void DownloadImages()
		{
			if (!UpdateData.images.NullOrEmpty())
			{
				//Populate keys so the UI knows to wait before rendering
				foreach (UpdateLogData.UploadedImages image in UpdateData.images)
				{
					if (!cachedDownloadedTextures.ContainsKey(image.name))
					{
						cachedDownloadedTextures[image.name] = new WebTexture();
					}
				}
				BeginDownloadingAsync();
			}
		}

		private void BeginDownloadingAsync()
		{
			//Download textures
			Task.Run(async delegate ()
			{
				foreach (UpdateLogData.UploadedImages image in UpdateData.images)
				{
					WebTexture webTexture = cachedDownloadedTextures[image.name];
					webTexture.Status = DownloadStatus.InProgress;
					try
					{
						Texture2D texture = await FileReader.GetTextureFromURL(image.url);
						if (texture)
						{
							webTexture.SetTexture(texture);
							if (webTexture.texture)
							{
								webTexture.Status = DownloadStatus.Success;
							}
							else
							{
								webTexture.Status = DownloadStatus.Failed;
							}
						}
						else
						{
							webTexture.SetTexture(null);
							webTexture.Status = DownloadStatus.Failed;
						}
					}
					catch (Exception ex)
					{
						Log.Error($"Exception thrown while trying to fetch image from {image.url}.\nException={ex}");
						webTexture.SetTexture(null);
						webTexture.Status = DownloadStatus.Failed;
					}
				}
			});
		}

		public string UpdateLogVersionFile(string xmlVersion)
		{
			if (File.Exists(Path.Combine(Mod.RootDir, "Version.txt")))
			{
				string assemblyVersion = File.ReadAllText(Path.Combine(Mod.RootDir, "Version.txt"));
				return assemblyVersion;
			}
			return xmlVersion;
		}

		public void NotifyModUpdated()
		{
			UpdateData.InvokeActionOnUpdate();
			UpdateData.update = false;
		}

		public void SaveUpdateStatus()
		{
			if (UpdateData.testing) return;
			if (!Directory.Exists(FileReader.UpdateLogDirectory(Mod, CurrentFolder)))
			{
				Log.Error($"[{Mod.Name}] Unable to save UpdateLog info as directory {FileReader.UpdateLogDirectory(Mod, CurrentFolder)} does not exist.");
				return;
			}
			try
			{
				XDocument doc = new XDocument(new XElement("UpdateLog",
											  new XComment("Can utilize Version.txt file placed in mod's root directory"),
											  new XElement("currentVersion", UpdateData.currentVersion), 
											  new XComment(string.Join(",", Enum.GetNames(typeof(UpdateFor)))),
											  new XElement("updateOn", UpdateData.updateOn),
											  new XComment("Full description shown in update page"),
											  new XElement("description", UpdateData.description),
											  new XComment("Static parameterless method to execute when update log is executed"),
											  new XElement("actionOnUpdate", UpdateData.actionOnUpdate),
											  new XComment("Show update log on next startup."),
											  new XElement("update", UpdateData.update),
											  new XComment("Testing mode prevents the update from saving over the UpdateLog file"),
											  new XElement("testing", UpdateData.testing)));
				if (!UpdateData.rightIconBar.NullOrEmpty())
				{
					doc.Element("UpdateLog").Add(new XComment("Icon bar shown to the right of the mod's name."));
					doc.Element("UpdateLog").Add(new XElement("rightIconBar"));
					foreach (var item in UpdateData.rightIconBar)
					{
						doc.Element("UpdateLog").Element("rightIconBar").Add(new XElement("li",
																		new XElement("name", item.name),
																		new XElement("icon", item.icon),
																		new XElement("url", item.url)));
					}
				}
				if (!UpdateData.leftIconBar.NullOrEmpty())
				{
					doc.Element("UpdateLog").Add(new XComment("Icon bar shown to the left of the mod's name."));
					doc.Element("UpdateLog").Add(new XElement("leftIconBar"));
					foreach (var item in UpdateData.leftIconBar)
					{
						doc.Element("UpdateLog").Element("leftIconBar").Add(new XElement("li",
																		new XElement("name", item.name),
																		new XElement("icon", item.icon),
																		new XElement("url", item.url)));
					}
				}
				if (!UpdateData.images.NullOrEmpty())
				{
					doc.Element("UpdateLog").Add(new XComment("Images to download off the web to reference from the description."));
					doc.Element("UpdateLog").Add(new XElement("images"));
					foreach (var item in UpdateData.images)
					{
						doc.Element("UpdateLog").Element("images").Add(new XElement("li", 
																	new XElement("name", item.name), 
																	new XElement("url", item.url)));
					}
				}
				doc.Save(Path.Combine(FileReader.UpdateLogDirectory(Mod, CurrentFolder), FileReader.UpdateLogFileName));
			}
			catch (Exception ex)
			{
				Log.Error($"[UpdateLog] Unable to save UpdateLog config info. Exception=\"{ex}\"");
			}
		}

		public static bool operator ==(UpdateLog lhs, UpdateLog rhs)
		{
			if (lhs is null)
			{
				return rhs is null;
			}
			if (rhs is null)
			{
				return lhs is null;
			}
			return lhs.Equals(rhs);
		}

		public static bool operator !=(UpdateLog lhs, UpdateLog rhs)
		{
			return !(lhs == rhs);
		}

		public override bool Equals(object obj)
		{
			return obj is UpdateLog log && Equals(log);
		}

		public bool Equals(UpdateLog log)
		{
			return Mod == log.Mod && UpdateData.currentVersion == log.UpdateData.currentVersion;
		}

		public override int GetHashCode()
		{
			return Gen.HashCombine(Mod.Name.GetHashCode(), UpdateData.currentVersion);
		}

		public class UpdateLogData
		{
			public string currentVersion;
			public UpdateFor updateOn = UpdateFor.GameInit;
			public string description;

			public string actionOnUpdate;

			public List<HyperlinkedIcon> rightIconBar;
			public List<HyperlinkedIcon> leftIconBar;

			public List<UploadedImages> images;

			public bool testing;
			public bool update;

			public void InvokeActionOnUpdate()
			{
				if (actionOnUpdate.NullOrEmpty())
				{
					return;
				}
				try
				{
					string[] methodFullName = actionOnUpdate.Split('.');
					Type classType = GenTypes.GetTypeInAnyAssembly(methodFullName[0] + "." + methodFullName[1], methodFullName[0]);
					classType.GetMethod(methodFullName[2], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
				}
				catch (Exception ex)
				{
					Log.Error($"Unable to invoke method on update. Method could not be found: {actionOnUpdate ?? "Null"} Ex={ex}");
				}
			}

			public string EnhancedDescription
			{
				get
				{
					string enhancedDesc = description;
					//enhancedDesc = enhancedDesc.Replace('[', '<');
					//enhancedDesc = enhancedDesc.Replace(']', '>');
					return enhancedDesc;
				}
			}

			public struct HyperlinkedIcon
			{
				/// <summary>
				/// Name of link to be displayed upon highlight
				/// </summary>
				public string name;
				/// <summary>
				/// Texture file name relative to UpgradeLog/Images files. Only include name, not additional file path
				/// </summary>
				public string icon;
				/// <summary>
				/// URL to webpage that will be linked upon clicking the icon
				/// </summary>
				public string url;
			}

			public struct UploadedImages
			{
				public string name;
				/// <summary>
				/// url to directory containing image
				/// </summary>
				public string url;
			}
		}
	}
}

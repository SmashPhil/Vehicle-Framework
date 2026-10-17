using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Networking;
using Verse;

namespace UpdateLogTool;

[PublicAPI]
public static class FileReader
{
  public const string UpdateLogFolder = "Updates";
  public const string UpdateLogOldFolder = "Previous";
  public const string UpdateLogFileName = "UpdateLog.xml";
  public const string UpdateLogImageFolder = "Images";
  public const string UpdateLogGifFolder = "Gifs";

  public static string UpdateLogDirectory(ModContentPack mod, string folderName)
  {
    return Path.Combine(mod.RootDir, folderName, UpdateLogFolder);
  }

  public static string UpdateLogOldDirectory(ModContentPack mod, string folderName)
  {
    return Path.Combine(mod.RootDir, folderName, UpdateLogFolder, UpdateLogOldFolder);
  }

  public static string UpdateImagesDirectory(ModContentPack mod, string folderName)
  {
    return Path.Combine(mod.RootDir, folderName, UpdateLogFolder, UpdateLogImageFolder);
  }

  public static string UpdateImagesDirectory(UpdateLog log)
  {
    return UpdateImagesDirectory(log.Mod, log.CurrentFolder);
  }

  public static string UpdateGifDirectory(ModContentPack mod, string folderName)
  {
    return Path.Combine(mod.RootDir, folderName, UpdateLogFolder, UpdateLogGifFolder);
  }

  public static string UpdateGifDirectory(UpdateLog log)
  {
    return UpdateImagesDirectory(log.Mod, log.CurrentFolder);
  }

  public static UpdateLog LoadUpdateLog(ModContentPack mod)
  {
    try
    {
      List<string> loadFolders = ModFoldersForVersion(mod);
      if (!loadFolders.NullOrEmpty())
      {
        foreach (string folder in loadFolders)
        {
          if (File.Exists(Path.Combine(UpdateLogDirectory(mod, folder), UpdateLogFileName)))
            return new UpdateLog(mod, folder);
        }
      }
    }
    catch (Exception ex)
    {
      Log.Error(
        $"Exception thrown while attempting to read in UpdateLog data for {mod.Name}.\nException=\"{ex}\"");
    }
    return null;
  }

  public static List<UpdateLog> ReadPreviousFiles(this ModContentPack mod)
  {
    List<UpdateLog> updates = [];
    try
    {
      List<string> loadFolders = ModFoldersForVersion(mod);
      if (loadFolders.NullOrEmpty())
        return updates;

      foreach (string folder in loadFolders)
      {
        if (!Directory.Exists(UpdateLogDirectory(mod, folder)))
          continue;

        if (File.Exists(Path.Combine(UpdateLogDirectory(mod, folder), UpdateLogFileName)))
        {
          updates.Add(new UpdateLog(mod, folder));
        }
        if (Directory.Exists(UpdateLogOldDirectory(mod, folder)))
        {
          foreach (string filePath in Directory.EnumerateFiles(
            UpdateLogOldDirectory(mod, folder), "*.xml"))
          {
            if (File.Exists(filePath))
            {
              updates.Add(new UpdateLog(mod, folder, filePath, false));
            }
          }
        }
      }
    }
    catch (Exception ex)
    {
      Log.Error(
        $"Exception thrown while attempting to read in UpdateLog data for {mod.Name}.\nException=\"{ex}\"");
    }
    return updates;
  }

  public static List<string> ModFoldersForVersion(ModContentPack mod)
  {
    ModMetaData metaData = ModLister.GetActiveModWithIdentifier(mod.PackageId);
    if (metaData == null)
    {
      Log.Warning($"Unable to load folders for {mod.PackageId}");
      return null;
    }
    List<LoadFolder> loadFolders;
    if (metaData.loadFolders != null && metaData.loadFolders.DefinedVersions().Count > 0)
    {
      loadFolders =
        metaData.LoadFoldersForVersion(VersionControl.CurrentVersionStringWithoutBuild);
      if (!loadFolders.NullOrEmpty())
        return loadFolders.Select(lf => lf.folderName).ToList();
    }
    int major = VersionControl.CurrentVersion.Major;
    int minor = VersionControl.CurrentVersion.Minor;
    do
    {
      if (minor == 0)
      {
        major--;
        minor = 9;
      }
      else
      {
        minor--;
      }
      if (major < 1)
      {
        loadFolders = metaData.LoadFoldersForVersion("default");
        if (loadFolders != null)
        {
          return loadFolders.Select(lf => lf.folderName).ToList();
        }
        return DefaultFoldersForVersion(mod).ToList();
      }
      loadFolders = metaData.LoadFoldersForVersion($"{major}.{minor}");
    } while (loadFolders.NullOrEmpty());
    return loadFolders.Select(lf => lf.folderName).ToList();
  }

  public static IEnumerable<string> DefaultFoldersForVersion(ModContentPack mod)
  {
    ModMetaData metaData = mod.ModMetaData;
    string rootDir = mod.RootDir;
    string text = Path.Combine(rootDir, VersionControl.CurrentVersionStringWithoutBuild);
    if (Directory.Exists(text))
    {
      yield return text;
    }
    else
    {
      Version version = new(0, 0);
      DirectoryInfo[] directories = metaData.RootDir.GetDirectories();
      foreach (DirectoryInfo dir in directories)
      {
        if (VersionControl.TryParseVersionString(dir.Name, out Version parsedVersion) &&
          parsedVersion > version)
        {
          version = parsedVersion;
        }
      }
      if (version.Major > 0)
        yield return Path.Combine(rootDir, version.ToString());
    }
    string common = Path.Combine(rootDir, ModContentPack.CommonFolderName);
    yield return Directory.Exists(common) ? common : rootDir;
  }

  /// <summary>
  /// Manually parsing UpdateLog.UpdateLogData due to issue with <see cref="DirectXmlToObject.ObjectFromXml{T}"/>
  /// parsing lists in direct DocumentElement object
  /// </summary>
  /// <param name="filePath"></param>
  public static UpdateLog.UpdateLogData ParseUpdateData(string filePath)
  {
    string xmlContent = File.ReadAllText(filePath);
    UpdateLog.UpdateLogData data = new();
    try
    {
      XmlDocument xmlDocument = new();
      xmlDocument.LoadXml(xmlContent);
      Assert.IsNotNull(xmlDocument.DocumentElement);
      foreach (XmlNode node in xmlDocument.DocumentElement.ChildNodes)
      {
        switch (node.Name)
        {
          case "currentVersion":
            data.currentVersion = node.InnerText;
          break;
          case "updateOn":
            data.updateOn = (UpdateFor)Enum.Parse(typeof(UpdateFor), node.InnerText);
          break;
          case "description":
            data.description = node.InnerText;
          break;
          case "rightIconBar":
            data.rightIconBar = ListFromXml(node);
          break;
          case "leftIconBar":
            data.leftIconBar = ListFromXml(node);
          break;
          case "actionOnUpdate":
            data.actionOnUpdate = node.InnerText;
          break;
          case "images":
            data.images = ImageListFromXml(node);
          break;
          case "testing":
          {
            data.testing = bool.TryParse(node.InnerText, out bool result) && result;
          }
          break;
          case "update":
          {
            data.update = bool.TryParse(node.InnerText, out bool result) && result;
          }
          break;
          case "#comment":
            continue;
          default:
            Log.Error($"Failed to find {node.Name} in manual parsing.");
          break;
        }
      }
    }
    catch (Exception ex)
    {
      Log.Error(
        $"Exception loading file at {filePath}. Loading defaults instead. Exception={ex}");
    }
    return data;
  }

  private static List<UpdateLog.UpdateLogData.HyperlinkedIcon> ListFromXml(XmlNode listRootNode)
  {
    List<UpdateLog.UpdateLogData.HyperlinkedIcon> list = [];
    try
    {
      foreach (XmlNode xmlNode in listRootNode.ChildNodes)
      {
        try
        {
          list.Add(DirectXmlToObject.ObjectFromXml<UpdateLog.UpdateLogData.HyperlinkedIcon>(xmlNode, true));
        }
        catch (Exception ex)
        {
          Log.Error(
            $"Exception loading list element from XML. Ex={ex}\nXml={listRootNode.OuterXml}");
        }
      }
    }
    catch (Exception ex2)
    {
      Log.Error(
        $"Exception loading list element from XML. Ex={ex2.Message}\nXml={listRootNode.OuterXml}");
    }
    return list;
  }

  private static List<UpdateLog.UpdateLogData.UploadedImages> ImageListFromXml(
    XmlNode listRootNode)
  {
    List<UpdateLog.UpdateLogData.UploadedImages> list = [];
    try
    {
      foreach (XmlNode xmlNode in listRootNode.ChildNodes)
      {
        try
        {
          list.Add(
            DirectXmlToObject
             .ObjectFromXml<UpdateLog.UpdateLogData.UploadedImages>(xmlNode, true));
        }
        catch (Exception ex)
        {
          Log.Error(
            $"Exception loading list element from XML. Ex={ex}\nXml={listRootNode.OuterXml}");
        }
      }
    }
    catch (Exception ex2)
    {
      Log.Error(
        $"Exception loading list element from XML. Ex={ex2}\nXml={listRootNode.OuterXml}");
    }
    return list;
  }

  public static async Task<Texture2D> GetTextureFromURL(string url)
  {
    using UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url);
    UnityWebRequestAsyncOperation operation = webRequest.SendWebRequest();

    while (!operation.isDone)
      await Task.Delay(33);

    if (webRequest.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.ProtocolError)
      return null;

    return DownloadHandlerTexture.GetContent(webRequest);
  }
}
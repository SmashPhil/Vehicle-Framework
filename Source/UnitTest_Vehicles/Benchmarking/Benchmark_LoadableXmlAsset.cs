using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevTools.Benchmarking;
using UnityEngine.Assertions;
using Verse;

// ReSharper disable all
namespace Vehicles.Benchmarking;

[BenchmarkClass("LoadableXmlAsset")]
internal class Benchmark_LoadableXmlAsset
{
  [Benchmark(Label = "Sequentially")]
  private static void Sequentially(in XmlAssetContext context)
  {
    List<FileInfo> files = context.files.Values.ToList();
    LoadableXmlAsset[] assets = new LoadableXmlAsset[files.Count];

    for (int i = 0; i < files.Count; i++)
    {
      FileInfo file = files[i];
      assets[i] = new LoadableXmlAsset(file, context.mod);
    }
  }

  [Benchmark(Label = "Tasks")]
  private static void TaskTask(in XmlAssetContext context)
  {
    ModContentPack mod = context.mod;
    List<FileInfo> files = context.files.Values.ToList();
    LoadableXmlAsset[] assets = new LoadableXmlAsset[files.Count];

    SemaphoreSlim semaphore = new(Math.Min(Environment.ProcessorCount, 4));
    Task[] array = new Task[files.Count];
    for (int i = 0; i < files.Count; i++)
    {
      int index = i;
      FileInfo file = files[i];
      array[i] = Task.Run(async delegate
      {
        await semaphore.WaitAsync();
        try
        {
          assets[index] = new LoadableXmlAsset(file, mod);
        }
        finally
        {
          semaphore.Release();
        }
      });
    }
    Task.WaitAll(array);
  }

  [Benchmark(Label = "Threads")]
  private static void Threaded(in XmlAssetContext context)
  {
    ModContentPack mod = context.mod;
    List<FileInfo> files = context.files.Values.ToList();
    LoadableXmlAsset[] assets = new LoadableXmlAsset[files.Count];
    ConcurrentBag<KeyValuePair<int, FileInfo>> toLoad = [];
    for (int i = 0; i < files.Count; i++)
    {
      toLoad.Add(new KeyValuePair<int, FileInfo>(i, files[i]));
    }
    Thread[] threads = new Thread[2];
    for (int l = 0; l < threads.Length; l++)
    {
      threads[l] = new Thread((ThreadStart)delegate
      {
        while (toLoad.TryTake(out KeyValuePair<int, FileInfo> kvp))
        {
          assets[kvp.Key] = new LoadableXmlAsset(kvp.Value, mod);
        }
      })
      {
        Name = $"DirectXmlLoader Thread {l + 1} of {2}"
      };
      threads[l].Start();
    }

    while (toLoad.TryTake(out KeyValuePair<int, FileInfo> kvp))
    {
      assets[kvp.Key] = new LoadableXmlAsset(kvp.Value, mod);
    }
    foreach (Thread thread in threads)
    {
      thread.Join();
    }
  }

  [Benchmark(Label = "ParallelFor")]
  private static void ParallelFor(in XmlAssetContext context)
  {
    ModContentPack mod = context.mod;
    List<FileInfo> files = context.files.Values.ToList();
    LoadableXmlAsset[] assets = new LoadableXmlAsset[files.Count];
    Parallel.For(0, files.Count, i =>
    {
      try
      {
        assets[i] = new LoadableXmlAsset(files[i], mod);
      }
      catch (Exception ex)
      {
        Log.Error($"Exception thrown loading xml file. index={i}\n{ex}");
      }
    });
  }

  [Benchmark(Label = "Partitioner")]
  private static void PartitionerLib(in XmlAssetContext context)
  {
    ModContentPack mod = context.mod;
    List<FileInfo> files = context.files.Values.ToList();
    LoadableXmlAsset[] assets = new LoadableXmlAsset[files.Count];
    Parallel.ForEach(Partitioner.Create(0, files.Count), (range, _) =>
    {
      for (int i = range.Item1; i < range.Item2; i++)
      {
        try
        {
          assets[i] = new LoadableXmlAsset(files[i], mod);
        }
        catch (Exception ex)
        {
          Log.Error(
            $"Exception thrown loading xml file. range = {range.Item1} to {range.Item2}\n{ex}");
        }
      }
    });
  }

  [Benchmark(Label = "GenThreading")]
  private static void GenThreadingParallel(in XmlAssetContext context)
  {
    ModContentPack mod = context.mod;
    List<FileInfo> files = context.files.Values.ToList();
    LoadableXmlAsset[] assets = new LoadableXmlAsset[files.Count];
    GenThreading.ParallelFor(0, files.Count, delegate(int i)
    {
      LoadableXmlAsset loadableXmlAsset = new LoadableXmlAsset(files[i], mod);
      assets[i] = loadableXmlAsset;
    });
  }

  private readonly struct XmlAssetContext
  {
    public readonly ModContentPack mod;
    public readonly Dictionary<string, FileInfo> files = [];

    public XmlAssetContext()
    {
      mod = VehicleMod.content;

      List<string> folders = mod.foldersToLoadDescendingOrder;
      foreach (string folderPath in folders)
      {
        DirectoryInfo directoryInfo = new(Path.Combine(folderPath, "Defs/"));
        if (!directoryInfo.Exists)
          continue;
        FileInfo[] xmlFiles = directoryInfo.GetFiles("*.xml", SearchOption.AllDirectories);
        foreach (FileInfo fileInfo in xmlFiles)
        {
          string key = fileInfo.FullName.Substring(folderPath.Length + 1);
          files.TryAdd(key, fileInfo);
        }
      }
      //Log.Message($"Mod: {mod.Name} Files: {files.Count}");
      Assert.IsFalse(files.NullOrEmpty());
    }
  }
}
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Verse;

namespace SmashTools.Performance;

public static class QuickIter
{
	public delegate void TypeProcessor(Type type);

	/// <summary>
	/// Quickly enumerate all loaded types from all assemblies.
	/// </summary>
	public static void EnumerateAllTypes(TypeProcessor processor)
	{
		// Ensure results are cached immediately. It's just easier to debug this way
		List<Type> typeList = GenTypes.AllTypes;
		Parallel.ForEach(Partitioner.Create(0, typeList.Count), (range, _) =>
		{
			for (int i = range.Item1; i < range.Item2; i++)
			{
				Type type = typeList[i];
				processor(type);
			}
		});
	}

	/// <summary>
	/// Quickly enumerate all loaded types from mod-provided assemblies.
	/// </summary>
	/// <remarks>Specifically excludes RimWorld and DLC adjacent types.</remarks>
	public static void EnumerateAllModTypes(TypeProcessor processor)
	{
		List<Type> types = [];
		foreach (ModContentPack mod in LoadedModManager.RunningModsListForReading)
		{
			foreach (Assembly assembly in mod.assemblies.loadedAssemblies)
			{
				try
				{
					Type[] typesFromMod = assembly.GetTypes();
					types.AddRange(typesFromMod);
				}
				catch (Exception ex) when (ex is ReflectionTypeLoadException or TypeLoadException)
				{
					Log.Error(
						$"Exception loading types from {assembly.FullName}. Mod compatible with RimWorld version: {mod.ModMetaData.VersionCompatible.ToStringYesNo()}\n{ex}");
				}
			}
		}

		Parallel.ForEach(Partitioner.Create(0, types.Count), (range, _) =>
		{
			for (int i = range.Item1; i < range.Item2; i++)
			{
				Type type = types[i];
				processor(type);
			}
		});
	}
}
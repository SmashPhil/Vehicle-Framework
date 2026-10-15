using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Verse;

namespace SmashTools.Patching;

[PublicAPI]
public static class HarmonyPatcher
{
	private static Task asyncPatchTask;
	private static Type typePatching;
	private static string methodPatching = string.Empty;

	private static readonly List<Unpatcher> Unpatchers = [];
	private static readonly Dictionary<PatchSequence, List<IPatchCategory>> Patches = [];

	private static ModContentPack Mod { get; set; }

	internal static Harmony Harmony { get; private set; }

	private static bool RunningPatcher { get; set; }

	internal static void Init(ModContentPack mod)
	{
#if DEBUG
		//Harmony.DEBUG = true;
#endif

		Mod = mod;
		Harmony = new Harmony(mod.ModMetaData.PackageIdPlayerFacing);

		ConditionalPatches.Init(mod);
		foreach (Type type in mod.AllInterfaceClassImplementations<IPatchCategory>())
		{
			IPatchCategory patch = (IPatchCategory)Activator.CreateInstance(type, nonPublic: true);
			Patches.AddOrAppend(patch.PatchAt, patch);
		}
	}

	public static void Run(PatchSequence sequence)
	{
		Assert.IsFalse(RunningPatcher);
		if (!Patches.ContainsKey(sequence))
			return;
		if (sequence == PatchSequence.Async)
		{
			if (!Patches.TryGetValue(PatchSequence.Async, out List<IPatchCategory> categories) ||
				categories.NullOrEmpty())
				return;

			Assert.IsNull(asyncPatchTask,
				"Patching async patch categories but the task has already been kicked off.");
			asyncPatchTask = Task.Run(RunAsyncPatches);
			LongEventHandler.ExecuteWhenFinished(delegate
			{
				if (!asyncPatchTask.IsCompleted)
				{
					if (Prefs.DevMode)
					{
						Log.Warning(
							$"[{Mod.Name}] Patching took longer than expected, delaying startup until it's finished.");
					}
					// Wait for patches to finish otherwise the player could enter into a game while the
					// patches are still running, causing the game to enter into a corrupted state.
					asyncPatchTask.GetAwaiter().GetResult();
				}
			});
			return;
		}

		using PatchStatusEnabler pse = new();
		DeepProfiler.Start($"ConditionalPatches_{sequence}");
		{
			ConditionalPatches.Run(sequence);
		}
		DeepProfiler.End();

		DeepProfiler.Start($"HarmonyPatcher_{sequence}");
		{
			foreach (IPatchCategory patch in Patches[sequence])
			{
				try
				{
					if (patch.PatchAt != sequence)
						continue;

					typePatching = patch.GetType();
					DeepProfiler.Start(typePatching.Name);
					patch.PatchMethods();
					DeepProfiler.End();
				}
				catch (Exception ex)
				{
					Log.Error(
						$"Failed to Patch {patch.GetType().FullName}. Method=\"{methodPatching}\"\n{ex}");
				}
			}
		}
		DeepProfiler.End();
	}

	private static void RunAsyncPatches()
	{
		ConditionalPatches.Run(PatchSequence.Async);
		foreach (IPatchCategory patch in Patches[PatchSequence.Async])
		{
			try
			{
				patch.PatchMethods();
			}
			catch (Exception ex)
			{
				Log.Error($"Failed to Patch {patch.GetType().FullName}.\n{ex}");
			}
		}
	}

	public static void Patch(MethodBase original, HarmonyMethod prefix = null,
		HarmonyMethod postfix = null,
		HarmonyMethod transpiler = null, HarmonyMethod finalizer = null)
	{
		methodPatching = original?.Name ?? $"Null\", Previous=\"{methodPatching}";
		try
		{
			Harmony.Patch(original, prefix, postfix, transpiler, finalizer);
		}
		catch (Exception ex)
		{
			Log.Error($"Exception thrown patching {typePatching}::{methodPatching}.\n{ex}");
		}
	}

	public static void Unpatch(MethodBase original, HarmonyPatchType patchType, string harmonyId = "")
	{
		Unpatchers.Add(new Unpatcher(original, patchType, harmonyId));
	}

	public static void RunUnpatches()
	{
		foreach (Unpatcher unpatcher in Unpatchers)
		{
			unpatcher.Execute();
		}
		Unpatchers.Clear();
	}

	[Conditional("DEBUG")]
	internal static void DumpPatchReport()
	{
		if (!Prefs.DevMode)
			return;

		int prefixes = 0;
		int postfixes = 0;
		int transpilers = 0;
		int finalizers = 0;
		List<MethodBase> patchMethods = Harmony.GetPatchedMethods().ToList();
		foreach (MethodBase method in patchMethods)
		{
			Patches patches = Harmony.GetPatchInfo(method);
			prefixes += CountPatches(patches.Prefixes);
			postfixes += CountPatches(patches.Postfixes);
			transpilers += CountPatches(patches.Transpilers);
			finalizers += CountPatches(patches.Finalizers);
		}
		SmashLog.Message(
			$"<color=orange>[{Mod.Name.Replace(" ", "")}]</color> <success>{prefixes + postfixes + transpilers + finalizers} " +
			$"patches successfully applied.</success>\nPrefixes: {prefixes} Postfixes: {postfixes} Transpilers: {transpilers} Finalizers: {finalizers}");
		return;

		static int CountPatches(IReadOnlyCollection<Patch> patches)
		{
			int count = 0;
			foreach (Patch patch in patches)
			{
				if (patch.owner == Harmony.Id)
					count++;
			}
			return count;
		}
	}

	private readonly struct Unpatcher(MethodBase original, HarmonyPatchType patchType, string harmonyId = "")
	{
		public void Execute()
		{
			Harmony.Unpatch(original, patchType, harmonyId);
		}

		public override string ToString()
		{
			return $"{patchType}::{original.Name}";
		}
	}

	private readonly struct PatchStatusEnabler : IDisposable
	{
		public PatchStatusEnabler()
		{
			RunningPatcher = true;
		}

		void IDisposable.Dispose()
		{
			RunningPatcher = false;
			typePatching = null;
			methodPatching = null;
		}
	}
}
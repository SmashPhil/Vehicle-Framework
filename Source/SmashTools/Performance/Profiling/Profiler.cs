using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using CoreLib.Performance;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace SmashTools.Performance;

[PublicAPI]
public static class Profiler
{
	private const int MaxPoolSize = 100;
	private const int PreWarmSize = 50;

	private const string HarmonyId = "SmashTools.Profiler";

	private static readonly MethodInfo PatchInjectionMethod =
		AccessTools.Method(typeof(Profiler), nameof(InjectProfileInstructions));

	private static readonly MethodInfo ProfileStartMethod =
		AccessTools.Method(typeof(Profiler), nameof(Begin));

	private static readonly MethodInfo ProfileStopMethod =
		AccessTools.Method(typeof(Profiler), nameof(End));

	private static readonly Harmony Harmony;

	private static readonly ObjectPool<Timer> TimerPool = new(MaxPoolSize, PreWarmSize);
	private static readonly ObjectPool<Result> ResultPool = new(MaxPoolSize, PreWarmSize);
	private static readonly ThreadLocal<Stack<Timer>> Blocks = new(() => new Stack<Timer>());
	private static readonly ConcurrentDictionary<string, Summary> ResultBuffer = [];

	static Profiler()
	{
#if RELEASE
		Log.Error($"ProfilerWatch initialized in release build! This will affect performance.");
#endif

		Harmony = new Harmony(HarmonyId);
	}

	internal static void Enable()
	{
		ApplyPatches();
		//UnityThread.StartUpdate(UpdatePerFrameCounts);
	}

	internal static void Disable()
	{
		Harmony.UnpatchAll(HarmonyId);
	}

	public static Timer Begin(string label)
	{
		Timer block = TimerPool.Get();
		Blocks.Value.Push(block);
		block.Label = label;
		block.Start();
		return block;
	}

  public static void End(Timer timer)
	{
		timer.Stop();
		Result result = ResultPool.Get();
		result.Record(timer);
		Summary summary = ResultBuffer.GetOrAdd(timer.Label, SummaryFactory());
		ResultPool.Return(summary.Push(result));
		TimerPool.Return(timer);
		return;

		static Summary SummaryFactory()
		{
			const int BufferSize = 1000;
			return new Summary(BufferSize);
		}
	}

	[MustDisposeResource]
	internal static IEnumerator<KeyValuePair<string, Summary>> GetResults()
	{
		return ResultBuffer.GetEnumerator();
	}

	private static void ApplyPatches()
	{
		_ = TaskManager.Run(ProcessMethods, CancellationToken.None);
		return;

		static void ProcessMethods()
		{
			foreach (ModContentPack mod in LoadedModManager.RunningModsListForReading)
			{
				foreach (Assembly assembly in mod.assemblies.loadedAssemblies)
				{
					foreach (Type type in assembly.GetTypes())
					{
						foreach (MethodInfo method in type.GetMethods(AccessTools.allDeclared))
						{
							if (method.IsAbstract)
								continue;

							if (method.TryGetAttribute(out ProfileAttribute _))
							{
								Harmony.Patch(method, transpiler: PatchInjectionMethod);
								Messages.Message($"{method.Name} patched for profiling.", MessageTypeDefOf.SilentInput,
									historical: false);
							}
						}
					}
				}
			}
		}
	}

	private static IEnumerable<CodeInstruction> InjectProfileInstructions(IEnumerable<CodeInstruction> instructions,
		ILGenerator ilg,
		MethodBase __originalMethod)
	{
		LocalBuilder timerLocal = ilg.DeclareLocal(typeof(Timer));
		yield return new CodeInstruction(opcode: OpCodes.Ldstr,
			operand: $"{__originalMethod.DeclaringType?.Name}.{__originalMethod.Name}");
		yield return new CodeInstruction(opcode: OpCodes.Call, operand: ProfileStartMethod);
		yield return new CodeInstruction(opcode: OpCodes.Stloc_S, operand: timerLocal.LocalIndex);

		foreach (CodeInstruction instruction in instructions)
		{
			if (instruction.opcode == OpCodes.Ret)
			{
				yield return new CodeInstruction(opcode: OpCodes.Ldloc_S, operand: timerLocal.LocalIndex);
				yield return new CodeInstruction(opcode: OpCodes.Call, operand: ProfileStopMethod);
			}
			yield return instruction;
		}
	}

	public enum Measurement
	{
		Seconds,
		Milliseconds,
		Microseconds,
		Nanoseconds,
	}

	internal sealed class Summary
	{
		private readonly RingBuffer<Result> buffer;
		private readonly int size;

		private long total;
		private int count;
		private double average;

		private readonly object syncRoot = new();

		public Summary(int size)
		{
			this.size = size;
			buffer = new RingBuffer<Result>(size);
			for (int i = 0; i < size; i++)
			{
				buffer.Push(new Result());
			}
		}

		public long Total => total;

		public int Count => count;

		public double Average => average;

		internal Result Push(Result current)
		{
			Result removed;
			lock (syncRoot)
			{
				removed = buffer.Push(current);
				Recalculate(removed.Ticks, current.Ticks);
			}
			return removed;
		}

		private void Recalculate(long removed, long added)
		{
			total -= removed;
			total += added;

			if (count < size)
				count++;

			average = (double)total / count;
		}
	}

	public sealed class Timer : IPoolable
	{
		private readonly Stopwatch stopwatch = new();

		public string Label { get; set; }

		bool IPoolable.InPool { get; set; }

		public long ElapsedTicks => stopwatch.ElapsedTicks;

		void IPoolable.Reset()
		{
			stopwatch.Reset();
			Label = "Invalid";
		}

		public void Start()
		{
			stopwatch.Restart();
		}

		public void Stop()
		{
			stopwatch.Stop();
		}
	}

	internal sealed record Result : IPoolable
	{
		private string name;
		private long ticks;

		public string Name => name;

		public long Ticks => ticks;

		bool IPoolable.InPool { get; set; }

		public void Record(Timer timer)
		{
			name = timer.Label;
			ticks = timer.ElapsedTicks;
		}

		void IPoolable.Reset()
		{
			name = null;
			ticks = 0;
		}
	}
}
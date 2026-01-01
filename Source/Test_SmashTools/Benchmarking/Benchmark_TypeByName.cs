using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using DevTools.Benchmarking;
using HarmonyLib;
using Verse;

namespace SmashTools.Performance;

[BenchmarkClass("TypeByName")]
internal class Benchmark_TypeByName
{
	[Benchmark(Label = "AccessTools")]
	private static void TypeByName_AccessTools()
	{
		AccessTools.TypeByName(TypeContext.Verse);
		AccessTools.TypeByName(TypeContext.MsCorLib);
		AccessTools.TypeByName(TypeContext.Current);
		AccessTools.TypeByName(TypeContext.Harmony);
	}

	[Benchmark(Label = "GenTypes")]
	private static void GenTypes_GetTypeInAnyAssembly()
	{
		GenTypeWrapper.Invoke(TypeContext.Verse);
		GenTypeWrapper.Invoke(TypeContext.MsCorLib);
		GenTypeWrapper.Invoke(TypeContext.Current);
		GenTypeWrapper.Invoke(TypeContext.Harmony);
	}

	private static unsafe class GenTypeWrapper
	{
		private static readonly delegate*<string, string, Type> GetTypeByName;

		static GenTypeWrapper()
		{
			MethodInfo method = AccessTools.Method(typeof(GenTypes), "GetTypeInAnyAssemblyInt");
			GetTypeByName = (delegate*<string, string, Type>)method.MethodHandle.GetFunctionPointer();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Invoke(string typeName, string namespaceIfAmbiguous = null)
		{
			GetTypeByName(typeName, namespaceIfAmbiguous);
		}
	}

	private readonly struct TypeContext
	{
		public const string Verse = "Verse.GenTypes";
		public const string MsCorLib = "System.String";
		public const string Current = "SmashTools.Performance.Benchmark_TypeByName";
		public const string Harmony = "HarmonyLib.AccessTools";
	}
}
using System;
using System.Reflection;
using DevTools.Benchmarking;
using HarmonyLib;

namespace SmashTools.Performance;

[BenchmarkClass("Invocation")]
internal class Benchmark_Invocation
{
	private const bool Arg1 = false;
	private const int Arg2 = 1;

	[Benchmark(Label = "Reflection")]
	private static void InvokeReflection(ref readonly InvocationContext context)
	{
		context.method.Invoke(null, [Arg1, Arg2]);
	}

	[Benchmark(Label = "Delegate")]
	private static void InvokeDelegate(ref readonly InvocationContext context)
	{
		context.action(Arg1, Arg2);
	}

	[Benchmark(Label = "StaticFunctionPtr")]
	private static void InvokeStaticFunctionPtr(ref readonly InvocationContext context)
	{
		context.functionPtr.Invoke(Arg1, Arg2);
	}

	private static void NoOp(bool arg1, int arg2)
	{
	}

	private readonly struct InvocationContext
	{
		public readonly MethodInfo method;
		public readonly Action<bool, int> action;
		public readonly StaticVoidFuncPtr<bool, int> functionPtr;

		public InvocationContext()
		{
			method = AccessTools.Method(typeof(Benchmark_Invocation), "NoOp");
			action = (Action<bool, int>)method.CreateDelegate(typeof(Action<bool, int>));
			functionPtr = new StaticVoidFuncPtr<bool, int>(method);
		}
	}
}
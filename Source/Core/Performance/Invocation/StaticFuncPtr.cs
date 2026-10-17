using System;
using System.Reflection;

namespace CoreLib.Performance;

public sealed unsafe class StaticFuncPtr<R>
{
	private readonly delegate*<R> function;

	public StaticFuncPtr(MethodInfo method)
	{
		if (method is null)
			throw new ArgumentNullException(nameof(method));
		if (!method.IsStatic)
			throw new ArgumentException("Non-static method cannot be used as static delegate.", nameof(method));
		if (method.ReturnType != typeof(R))
			throw new ArgumentException($"Unable to get function pointer from {method.Name}, does not have return type {typeof(R).Name}.");
		if (method.GetParameters().Length != 0)
			throw new ArgumentException($"Unable to get function pointer from {method.Name}, mismatched parameters.");

		function = (delegate*<R>)method.MethodHandle.GetFunctionPointer();
	}

	public R Invoke()
	{
		return function();
	}
}

public sealed unsafe class StaticFuncPtr<T, R>
{
	private readonly delegate*<T, R> function;

	public StaticFuncPtr(MethodInfo method)
	{
		if (method is null)
			throw new ArgumentNullException(nameof(method));
		if (!method.IsStatic)
			throw new ArgumentException("Non-static method cannot be used as static delegate.", nameof(method));
		if (method.ReturnType != typeof(R))
			throw new ArgumentException($"Unable to get function pointer from {method.Name}, does not have return type {typeof(R).Name}.");
		if (method.GetParameters().Length != 1)
			throw new ArgumentException($"Unable to get function pointer from {method.Name}, mismatched parameters.");

		function = (delegate*<T, R>)method.MethodHandle.GetFunctionPointer();
	}

	public R Invoke(T arg)
	{
		return function(arg);
	}
}

public sealed unsafe class StaticFuncPtr<T1, T2, R>
{
	private readonly delegate*<T1, T2, R> function;

	public StaticFuncPtr(MethodInfo method)
	{
		if (method is null)
			throw new ArgumentNullException(nameof(method));
		if (!method.IsStatic)
			throw new ArgumentException("Non-static method cannot be used as static delegate.", nameof(method));
		if (method.ReturnType != typeof(R))
			throw new ArgumentException($"Unable to get function pointer from {method.Name}, does not have return type {typeof(R).Name}.");
		if (method.GetParameters().Length != 2)
			throw new ArgumentException($"Unable to get function pointer from {method.Name}, mismatched parameters.");

		function = (delegate*<T1, T2, R>)method.MethodHandle.GetFunctionPointer();
	}

	public R Invoke(T1 arg1, T2 arg2)
	{
		return function(arg1, arg2);
	}
}

public sealed unsafe class StaticFuncPtr<T1, T2, T3, R>
{
	private readonly delegate*<T1, T2, T3, R> function;

	public StaticFuncPtr(MethodInfo method)
	{
		if (method is null)
			throw new ArgumentNullException(nameof(method));
		if (!method.IsStatic)
			throw new ArgumentException("Non-static method cannot be used as static delegate.", nameof(method));
		if (method.ReturnType != typeof(R))
			throw new ArgumentException($"Unable to get function pointer from {method.Name}, does not have return type {typeof(R).Name}.");
		if (method.GetParameters().Length != 3)
			throw new ArgumentException($"Unable to get function pointer from {method.Name}, mismatched parameters.");

		function = (delegate*<T1, T2, T3, R>)method.MethodHandle.GetFunctionPointer();
	}

	public R Invoke(T1 arg1, T2 arg2, T3 arg3)
	{
		return function(arg1, arg2, arg3);
	}
}

public sealed unsafe class StaticFuncPtr<T1, T2, T3, T4, R>
{
	private readonly delegate*<T1, T2, T3, T4, R> function;

	public StaticFuncPtr(MethodInfo method)
	{
		if (method is null)
			throw new ArgumentNullException(nameof(method));
		if (!method.IsStatic)
			throw new ArgumentException("Non-static method cannot be used as static delegate.", nameof(method));
		if (method.ReturnType != typeof(R))
			throw new ArgumentException($"Unable to get function pointer from {method.Name}, does not have return type {typeof(R).Name}.");
		if (method.GetParameters().Length != 3)
			throw new ArgumentException($"Unable to get function pointer from {method.Name}, mismatched parameters.");

		function = (delegate*<T1, T2, T3, T4, R>)method.MethodHandle.GetFunctionPointer();
	}

	public R Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
	{
		return function(arg1, arg2, arg3, arg4);
	}
}
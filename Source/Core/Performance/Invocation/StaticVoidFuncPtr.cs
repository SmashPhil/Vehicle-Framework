using System;
using System.Reflection;

namespace CoreLib.Performance;

public sealed unsafe class StaticVoidFuncPtr
{
	private readonly delegate*<void> function;

	public StaticVoidFuncPtr(MethodInfo method)
	{
		if (method is null)
			throw new ArgumentNullException(nameof(method));
		if (!method.IsStatic)
			throw new ArgumentException("Non-static method cannot be used as static delegate.", nameof(method));
		if (method.ReturnType != typeof(void))
			throw new InvalidOperationException($"Unable to get function pointer from {method.Name}, does not have return type void.");
		if (method.GetParameters().Length != 0)
			throw new InvalidOperationException($"Unable to get function pointer from {method.Name}, mismatched parameters.");

		function = (delegate*<void>)method.MethodHandle.GetFunctionPointer();
	}

	public void Invoke()
	{
		function();
	}
}

public sealed unsafe class StaticVoidFuncPtr<T>
{
	private readonly delegate*<T, void> function;

	public StaticVoidFuncPtr(MethodInfo method)
	{
		if (method is null)
			throw new ArgumentNullException(nameof(method));
		if (!method.IsStatic)
			throw new ArgumentException("Non-static method cannot be used as static delegate.", nameof(method));
		if (method.ReturnType != typeof(void))
			throw new InvalidOperationException($"Unable to get function pointer from {method.Name}, does not have return type void.");
		if (method.GetParameters().Length != 0)
			throw new InvalidOperationException($"Unable to get function pointer from {method.Name}, mismatched parameters.");

		function = (delegate*<T, void>)method.MethodHandle.GetFunctionPointer();
	}

	public void Invoke(T arg)
	{
		function(arg);
	}
}

public sealed unsafe class StaticVoidFuncPtr<T1, T2>
{
	private readonly delegate*<T1, T2, void> function;

	public StaticVoidFuncPtr(MethodInfo method)
	{
		if (method is null)
			throw new ArgumentNullException(nameof(method));
		if (!method.IsStatic)
			throw new ArgumentException("Non-static method cannot be used as static delegate.", nameof(method));
		if (method.ReturnType != typeof(void))
			throw new InvalidOperationException($"Unable to get function pointer from {method.Name}, does not have return type void.");
		if (method.GetParameters().Length != 2)
			throw new InvalidOperationException($"Unable to get function pointer from {method.Name}, mismatched parameters.");

		function = (delegate*<T1, T2, void>)method.MethodHandle.GetFunctionPointer();
	}

	public void Invoke(T1 arg1, T2 arg2)
	{
		function(arg1, arg2);
	}
}

public sealed unsafe class StaticVoidFuncPtr<T1, T2, T3>
{
	private readonly delegate*<T1, T2, T3, void> function;

	public StaticVoidFuncPtr(MethodInfo method)
	{
		if (method is null)
			throw new ArgumentNullException(nameof(method));
		if (!method.IsStatic)
			throw new ArgumentException("Non-static method cannot be used as static delegate.", nameof(method));
		if (method.ReturnType != typeof(void))
			throw new InvalidOperationException($"Unable to get function pointer from {method.Name}, does not have return type void.");
		if (method.GetParameters().Length != 3)
			throw new InvalidOperationException($"Unable to get function pointer from {method.Name}, mismatched parameters.");

		function = (delegate*<T1, T2, T3, void>)method.MethodHandle.GetFunctionPointer();
	}

	public void Invoke(T1 arg1, T2 arg2, T3 arg3)
	{
		function(arg1, arg2, arg3);
	}
}

public sealed unsafe class StaticVoidFuncPtr<T1, T2, T3, T4>
{
	private readonly delegate*<T1, T2, T3, T4, void> function;

	public StaticVoidFuncPtr(MethodInfo method)
	{
		if (method is null)
			throw new ArgumentNullException(nameof(method));
		if (!method.IsStatic)
			throw new ArgumentException("Non-static method cannot be used as static delegate.", nameof(method));
		if (method.ReturnType != typeof(void))
			throw new InvalidOperationException($"Unable to get function pointer from {method.Name}, does not have return type void.");
		if (method.GetParameters().Length != 3)
			throw new InvalidOperationException($"Unable to get function pointer from {method.Name}, mismatched parameters.");

		function = (delegate*<T1, T2, T3, T4, void>)method.MethodHandle.GetFunctionPointer();
	}

	public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
	{
		function(arg1, arg2, arg3, arg4);
	}
}
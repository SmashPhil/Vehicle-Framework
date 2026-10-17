using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Verse;

namespace SmashTools;

/// <summary>
/// Type / Reflection utilities
/// </summary>
[PublicAPI]
public static class Ext_Type
{
	/// <summary>
	/// Determines whether <paramref name="type"/> implements the generic <see cref="IList{T}"/> interface.
	/// </summary>
	/// <param name="type">The type to inspect.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="type"/> implements any constructed <see cref="IList{T}"/>; otherwise <see langword="false"/>.
	/// </returns>
	/// <remarks>
	/// This checks only the generic interface <see cref="IList{T}"/>. It does not consider the non-generic
	/// <see cref="System.Collections.IList"/>. Arrays (e.g., <c>T[]</c>) will return <see langword="true"/>.
	/// </remarks>
	public static bool IsIList(this Type type)
	{
		foreach (Type iType in type.GetInterfaces())
		{
			if (iType.IsGenericType && iType.GetGenericTypeDefinition() == typeof(IList<>))
				return true;
		}
		return false;
	}

	/// <summary>
	/// Sets all <see langword="static"/> fields declared on <paramref name="type"/> (public and non-public) to their
	/// default value (e.g., <c>default(T)</c>).
	/// </summary>
	/// <param name="type">The type whose static fields will be reset.</param>
	/// <remarks>
	/// Useful for clearing mod static states between tests or reloads. This will attempt to set every static field returned
	/// by <see cref="Type.GetFields(BindingFlags)"/> with
	/// <see cref="BindingFlags.Public"/> | <see cref="BindingFlags.NonPublic"/> | <see cref="BindingFlags.Static"/>.
	/// <para>
	/// Constants are skipped by the runtime and cannot be assigned; readonly fields are skipped as modifying the value
	/// through reflection is an illegal operation which results in undefined behavior, and this operation does not throw
	/// for Unity Mono so this restriction is not enforced.
	/// </para>
	/// </remarks>
	/// <exception cref="FieldAccessException">
	/// Thrown if a field is init-only (readonly) or inaccessible for assignment in the current runtime.
	/// </exception>
	public static void SetStaticFieldsDefault(this Type type)
	{
		foreach (FieldInfo fieldInfo in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
		{
			if (fieldInfo.IsInitOnly)
				continue;

			fieldInfo.SetValue(null, fieldInfo.FieldType.GetDefaultValue());
		}
	}

	/// <summary>
	/// Returns the default value for the specified <paramref name="type"/>.
	/// </summary>
	/// <param name="type">The type whose default value is requested.</param>
	/// <returns>
	/// <see langword="null"/> for reference types and <see cref="Nullable{T}"/> with no value;
	/// otherwise a zero-initialized boxed instance of the value type.
	/// </returns>
	/// <remarks>
	/// <paramref name="type"/> must be a concrete (closed) type. Passing an open generic value type
	/// (e.g., <c>Nullable&lt;&gt;</c>) will throw.
	/// </remarks>
	/// <exception cref="MissingMethodException">
	/// Thrown if <paramref name="type"/> is an open generic value type or otherwise cannot be constructed.
	/// </exception>
	public static object GetDefaultValue(this Type type)
	{
		return type.IsValueType ? Activator.CreateInstance(type) : null;
	}

	/// <summary>
	/// Determines whether <paramref name="type"/> implements the interface <paramref name="interfaceType"/>.
	/// </summary>
	/// <param name="type">The concrete type to inspect.</param>
	/// <param name="interfaceType">The interface to test for. Must be an interface type.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="type"/> implements <paramref name="interfaceType"/> (or any constructed version of it);
	/// otherwise <see langword="false"/>. Returns <see langword="false"/> if <paramref name="type"/> equals <paramref name="interfaceType"/>.
	/// </returns>
	public static bool HasInterface(this Type type, Type interfaceType)
	{
		if (!interfaceType.IsInterface)
		{
			Log.Error(
				$"Attempting to find type implementation as interface for non-interface type {interfaceType}.");
			return false;
		}
		if (type == interfaceType)
			return false;
		if (interfaceType.IsAssignableFrom(type))
			return true;

		Type[] interfaces = type.GetInterfaces();
		foreach (Type @interface in interfaces)
		{
			if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == interfaceType)
				return true;
		}
		return false;
	}

	/// <summary>
	/// Scans the <see cref="ModContentPack"/> and yields every non-abstract class type that implements the
	/// interface <typeparamref name="T"/>.
	/// Use this to discover implementations of a mod-facing hook/contract within a single mod's assemblies.
	/// </summary>
	/// <typeparam name="T">
	/// The interface to search for. Must be an interface on a class type; enforced by a debug assertion.
	/// </typeparam>
	/// <param name="mod">
	/// The mod whose <c>assemblies.loadedAssemblies</c> will be inspected.
	/// </param>
	/// <returns>
	/// A lazy sequence of <see cref="Type"/> objects representing concrete classes that implement <typeparamref name="T"/>
	/// in the given mod's loaded assemblies.
	/// </returns>
	/// <remarks>
	/// <list type="bullet">
	/// <item><description>Only classes are returned; abstract types are skipped.</description></item>
	/// <item><description>The search is limited to <paramref name="mod"/>; it does not scan other active mods.</description></item>
	/// </list>
	/// </remarks>
	/// <exception cref="ReflectionTypeLoadException">
	/// Thrown when one or more types in a scanned assembly cannot be loaded (e.g., unresolved dependencies)
	/// </exception>
	public static IEnumerable<Type> AllInterfaceClassImplementations<T>(this ModContentPack mod)
	{
		Assert.IsTrue(typeof(T).IsInterface);
		foreach (Assembly assembly in mod.assemblies.loadedAssemblies)
		{
			foreach (Type type in assembly.GetTypes())
			{
				if (type.HasInterface(typeof(T)) && type.IsClass && !type.IsAbstract)
				{
					yield return type;
				}
			}
		}
	}
}
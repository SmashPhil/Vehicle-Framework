using System;
using System.Diagnostics;

namespace JetBrains.Annotations;

/// <summary>
/// Indicates that the resource disposal must be handled at the use site,
/// meaning that the resource ownership is transferred to the caller.
/// This annotation can be used to annotate disposable types or their constructors individually to enable
/// the IDE code analysis for resource disposal in every context where the new instance of this type is created.
/// Factory methods and <c>out</c> parameters can also be annotated to indicate that the return value
/// of the disposable type needs handling.
/// </summary>
/// <remarks>
/// Annotation of input parameters with this attribute is meaningless.<br />
/// Constructors inherit this attribute from their type if it is annotated,
/// but not from the base constructors they delegate to (if any).<br />
/// Resource disposal is expected via <c>using (resource)</c> statement,
/// <c>using var</c> declaration, explicit <c>Dispose()</c> call, or passing the resource as an argument
/// to a parameter annotated with the <see cref="T:JetBrains.Annotations.HandlesResourceDisposalAttribute" /> attribute.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor |
	AttributeTargets.Method | AttributeTargets.Parameter)]
[Conditional("JETBRAINS_ANNOTATIONS")]
public sealed class MustDisposeResourceAttribute : Attribute
{
	public MustDisposeResourceAttribute()
	{
		Value = true;
	}

	public MustDisposeResourceAttribute(bool value)
	{
		Value = value;
	}

	/// <summary>
	/// When set to <c>false</c>, disposing of the resource is not obligatory.
	/// The main use-case for explicit <c>[MustDisposeResource(false)]</c> annotation
	/// is to loosen the annotation for inheritors.
	/// </summary>
	public bool Value { get; }
}
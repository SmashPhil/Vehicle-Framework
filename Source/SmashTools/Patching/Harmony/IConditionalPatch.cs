using System;
using JetBrains.Annotations;
using Verse;

namespace SmashTools.Patching;

[PublicAPI]
public interface IConditionalPatch
{
	/// <summary>
	/// Mod implementing this conditional patch
	/// </summary>
	/// <remarks>For debugging purposes</remarks>
	string SourceId { get; }

	/// <summary>
	/// Mod this patch will be applied if active in the mod list
	/// </summary>
	string PackageId { get; }

	PatchSequence PatchAt { get; }

	/// <summary>
	/// Patch implementations.  Only supports manual patches
	/// </summary>
	/// <param name="mod"></param>
	void PatchAll(ModMetaData mod);

	[PublicAPI]
	public class Result
	{
		public string PackageId { get; set; }
		public string FriendlyName { get; set; }
		public bool Active { get; set; }
		public Exception ExceptionThrown { get; set; }
	}
}
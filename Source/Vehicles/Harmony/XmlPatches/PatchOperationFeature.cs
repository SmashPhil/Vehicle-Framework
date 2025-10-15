using System.Xml;
using Vehicles.Config;
using Verse;

namespace Vehicles;

public class PatchOperationFeature : PatchOperation
{
	public string feature;
	public PatchOperation patch;

	private readonly FeatureFlags featureFlags;

	public PatchOperationFeature()
	{
		featureFlags = FeatureFlags.Default;
	}

	internal PatchOperationFeature(FeatureFlags featureFlags)
	{
		this.featureFlags = featureFlags;
	}

	protected override bool ApplyWorker(XmlDocument xml)
	{
		if (!featureFlags.IsEnabled(feature))
			return true;

		return patch != null && patch.Apply(xml);
	}
}
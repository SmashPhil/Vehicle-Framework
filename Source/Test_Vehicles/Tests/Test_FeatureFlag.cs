using System.Xml;
using DevTools.Testing;
using Vehicles.Config;
using Verse;

namespace Vehicles.Testing;

[TestFixture(TestType.MainMenu)]
[TestDescription("Feature flag toggles based on build configuration.")]
internal sealed class Test_FeatureFlag
{
  private FeatureFlags featureFlags;

  private const string EnabledFeature = "EnabledFeature";
  private const string DisabledFeature = "DisabledFeature";
  private const string BogusFeature = "SomeBogusFeature";

  [SetUp]
  private void CreateData()
  {
    featureFlags = new FeatureFlags([
      new FeatureMock
      {
        name = EnabledFeature,
        enabled = true
      },
      new FeatureMock
      {
        name = DisabledFeature,
        enabled = false
      }
    ]);
  }

  [TearDown]
  private void DeleteData()
  {
    featureFlags = null;
  }

  [Test]
  private void EnabledFeaturePatchOperation()
  {
    PatchOperationMock patch = new();
    PatchOperationFeature patchOp = new(featureFlags)
    {
      feature = EnabledFeature,
      patch = patch
    };
    patchOp.Apply(xml: null);
    Expect.IsTrue(patch.applied);
  }

  [Test]
  private void DisabledFeaturePatchOperation()
  {
    PatchOperationMock patch = new();
    PatchOperationFeature patchOp = new(featureFlags)
    {
      feature = DisabledFeature,
      patch = patch
    };
    patchOp.Apply(xml: null);
    Expect.IsFalse(patch.applied);
  }

  [Test]
  private void BogusFeaturePatchOperation()
  {
    PatchOperationMock patch = new();
    PatchOperationFeature patchOp = new(featureFlags)
    {
      feature = BogusFeature,
      patch = patch
    };
    patchOp.Apply(xml: null);
    Expect.IsFalse(patch.applied);
  }

  private sealed class PatchOperationMock : PatchOperation
  {
    public bool applied;

    protected override bool ApplyWorker(XmlDocument xml)
    {
      // If this is called at all, the feature is assumed to be enabled.
      applied = true;
      return true;
    }
  }

  private sealed class FeatureMock : IFeatureFlag
  {
    public string name;
    public bool enabled;

    string IFeatureFlag.Name => name;

    bool IFeatureFlag.Enabled => enabled;
  }
}
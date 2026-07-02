using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DevTools.Testing;
using JetBrains.Annotations;

namespace AnimationKit.Tests;

#pragma warning disable 0649

[TestFixture(TestType.MainMenu)]
[TestDescription("Animation manager for core logic around an entity's animations.")]
internal class Test_Animator
{
  [Test]
  private void FieldSearch()
  {
    using MockEntity entity = new();
    foreach (FieldInfo field in PropertyDatabase.GetProperties(entity))
    {

    }
  }

  private class MockEntity : IAnimator, IDisposable
  {
    private readonly Animator animator;
    private readonly AnimationController controller;

    [AnimationProperty]
    private readonly MockComp comp = new();

    public MockEntity()
    {
      controller = new AnimationController();
      animator = new Animator(this, controller);
      this.SerializeProperties();
    }

    int IAnimator.EntityId => 1234;

    public void Dispose()
    {
      animator.Dispose();
    }

    public void Update()
    {
      animator.Tick();
    }
  }

  [UsedImplicitly]
  private class MockComp
  {
    [AnimationProperty]
    public int int32;

    [AnimationProperty]
    public float float32;

    [AnimationProperty]
    public bool boolean;
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevTools.Testing;
using UnityEngine.Assertions;

namespace AnimationKit.Tests;

[TestFixture(TestType.MainMenu)]
[TestDescription("Unmanaged buffer directly written to by the animator.")]
internal class Test_NativeBuffer
{
  [Test]
  private void Create()
  {
    using NativeBuffer buffer = new(1);
    Assert.IsFalse(buffer.Disposed);
    Assert.AreEqual(expected: 1, buffer.Length);
    buffer.Dispose();
    Assert.IsTrue(buffer.Disposed);
  }

  [Test]
  private void ZeroInit()
  {
    const int Size = 4;
    using NativeBuffer buffer = new(Size);
    Assert.IsFalse(buffer.Disposed);
    for (int i = 0; i < Size; i++)
    {
      Expect.AreEqual(expected: 0, buffer[i]);
    }
  }
}

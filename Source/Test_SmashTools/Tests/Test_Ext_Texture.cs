using CoreLib;
using DevTools.Testing;
using UnityEngine;
using UnityEngine.Assertions;

namespace SmashTools.Testing;

[TestFixture(TestType.MainMenu)]
[TestCategory(TestCategoryNames.Utils)]
[TestDescription("Texture extension methods related to copying or replacing read-only textures.")]
internal sealed class Test_Ext_Texture
{
  [Test]
  private void CreateReadableTexture()
  {
    Color32[] pixels =
    [
      Pixel(1), Pixel(2),
      Pixel(3), Pixel(4)
    ];
    Texture2D source = CreateTexture(2, 2, "Source", TextureWrapMode.MirrorOnce,
      FilterMode.Point, 2, makeUnreadable: true, pixels);
    Assert.IsFalse(source.isReadable);
    using DestroyOnDispose ds = new(source);
    Texture2D readableCopy = Ext_Texture.CreateReadableTexture(source);
    using DestroyOnDispose dr = new(source);
    Assert.IsTrue(readableCopy.isReadable);
    Assert.AreEqual(source.name, readableCopy.name);
    Assert.AreEqual(source.wrapMode, readableCopy.wrapMode);
    AssertPixelsEqual(pixels, readableCopy.GetPixels32());
  }

  [Test]
  private void WrapTexture()
  {
    Texture2D source = CreateTexture(2, 2, "Source", TextureWrapMode.Clamp,
      FilterMode.Bilinear, 1, makeUnreadable: false,
      Pixel(1), Pixel(2),
      Pixel(3), Pixel(4));
    using DestroyOnDispose ds = new(source);
    Texture2D wrapped = Ext_Texture.WrapTexture(source, TextureWrapMode.Repeat);
    using DestroyOnDispose dr = new(wrapped);
    Assert.IsTrue(ReferenceEquals(source, wrapped));
    Assert.AreEqual(TextureWrapMode.Repeat, source.wrapMode);
    Assert.IsTrue(source.isReadable);
  }

  [Test]
  private void WrapTextureUnreadable()
  {
    Color32[] pixels =
    [
      Pixel(1), Pixel(2),
      Pixel(3), Pixel(4)
    ];
    Texture2D source = CreateTexture(2, 2, "Source", TextureWrapMode.Clamp,
      FilterMode.Bilinear, 1, makeUnreadable: false, pixels);
    using DestroyOnDispose ds = new(source);
    Texture2D wrapped = Ext_Texture.WrapTexture(source, TextureWrapMode.Repeat);
    using DestroyOnDispose dr = new(wrapped);
    Expect.ReferencesAreEqual(source, wrapped);
    Expect.AreEqual(TextureWrapMode.Repeat, source.wrapMode);
  }

  [Test]
  private void Rotate()
  {
    Color32[] pixels =
    [
      Pixel(1), Pixel(2), Pixel(3), Pixel(4), Pixel(5),
      Pixel(6), Pixel(7), Pixel(8), Pixel(9), Pixel(10),
      Pixel(11), Pixel(12), Pixel(13), Pixel(14), Pixel(15),
      Pixel(16), Pixel(17), Pixel(18), Pixel(19), Pixel(20),
      Pixel(21), Pixel(22), Pixel(23), Pixel(24), Pixel(25),
    ];
    Color32[] expected =
    [
      Pixel(25), Pixel(24), Pixel(23), Pixel(22), Pixel(21),
      Pixel(20), Pixel(19), Pixel(18), Pixel(17), Pixel(16),
      Pixel(15), Pixel(14), Pixel(13), Pixel(12), Pixel(11),
      Pixel(10), Pixel(9), Pixel(8), Pixel(7), Pixel(6),
      Pixel(5), Pixel(4), Pixel(3), Pixel(2), Pixel(1),
    ];

    Texture2D source = CreateTexture(5, 5, "Source", TextureWrapMode.Clamp,
      FilterMode.Point, 1, makeUnreadable: true, pixels);
    using DestroyOnDispose ds = new(source);
    Texture2D rotated = source.Rotate(180);
    using DestroyOnDispose dr = new(rotated);
    Texture2D readableRotated = Ext_Texture.CreateReadableTexture(rotated);
    using DestroyOnDispose drr = new(readableRotated);

    Assert.IsFalse(ReferenceEquals(source, rotated));
    Assert.AreEqual(source.name, rotated.name);
    AssertPixelsEqual(expected, readableRotated.GetPixels32());
  }

  private static Texture2D CreateTexture(int width, int height, string name, TextureWrapMode wrapMode,
    FilterMode filterMode, int anisoLevel, bool makeUnreadable, params Color32[] pixels)
  {
    Texture2D texture = new(width, height, TextureFormat.RGBA32, mipChain: false)
    {
      name = name,
      wrapMode = wrapMode,
      filterMode = filterMode,
      anisoLevel = anisoLevel
    };
    texture.SetPixels32(pixels);
    texture.Apply(updateMipmaps: false, makeNoLongerReadable: makeUnreadable);
    return texture;
  }

  private static void AssertPixelsEqual(Color32[] expected, Color32[] actual)
  {
    Assert.AreEqual(expected.Length, actual.Length);
    for (int i = 0; i < expected.Length; i++)
    {
      Assert.AreEqual(expected[i], actual[i]);
    }
  }

  private static Color32 Pixel(byte value)
  {
    return new(value, 0, 0, byte.MaxValue);
  }
}

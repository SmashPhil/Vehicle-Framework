using System;
using System.Collections.Generic;
using System.Linq;
using SmashTools.Xml;
using UnityEngine.Assertions;
using Verse;

namespace SmashTools.Animations
{
  public class AnimationController : IAnimationFile
  {
    public const string DefaultControllerName = "New-Controller";
    public const string FileExtension = ".ctlr";

    private Guid guid;

    public List<AnimationParameter> parameters = new List<AnimationParameter>();
    public List<AnimationLayer> layers = new List<AnimationLayer>();

    public Guid Guid => guid;

    public string FilePath { get; set; }

    public string FileName { get; set; }

    public string FileNameWithExtension => FileName + FileExtension;

    public void AddLayer(string name)
    {
      name = AnimationLoader.GetAvailableName(layers.Select(layer => layer.name), name);
      AnimationLayer layer = AnimationLayer.CreateLayer(name);
      layer.Controller = this;
      Assert.IsNotNull(layer);
      layers.Add(layer);
    }

    void IXmlExport.Export()
    {
      XmlExporter.WriteObject(nameof(guid), guid);
      XmlExporter.WriteCollection(nameof(parameters), parameters, attributeGetter: (parameter) =>
      {
        string typeName = GenTypes.GetTypeNameWithoutIgnoredNamespaces(parameter.GetType());
        return ("Class", typeName);
      });
      XmlExporter.WriteCollection(nameof(layers), layers);
    }

    void IAnimationFile.ResolveReferences()
    {
      foreach (AnimationLayer layer in layers)
      {
        layer.Controller = this;
        layer.ResolveReferences();
      }
    }

    public static implicit operator bool(AnimationController controller)
    {
      return controller != null;
    }

    public static AnimationController EmptyController()
    {
      AnimationController controller = new AnimationController();
      controller.FileName = DefaultControllerName;
      controller.AddLayer("Base Layer");
      controller.guid = Guid.NewGuid();
      return controller;
    }
  }
}
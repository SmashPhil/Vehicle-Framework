using System.Collections.Generic;
using JetBrains.Annotations;
using SmashTools;
using Vehicles.Rendering;

namespace Vehicles;

[PublicAPI, VehicleSettingsClass]
[HeaderTitle(Label = "VF_LightProperties", Translate = true)]
public sealed class LightProperties
{
  public List<PointLightProperties> lights;
}

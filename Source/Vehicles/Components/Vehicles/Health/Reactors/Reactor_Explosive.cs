using JetBrains.Annotations;
using SmashTools;
using Verse;

namespace Vehicles;

[PublicAPI]
public class Reactor_Explosive : Reactor, ITweakFields
{
  [TweakField(SettingsType = UISettingsType.SliderFloat)]
  [SliderValues(MinValue = 0, MaxValue = 1, Increment = 0.01f, RoundDecimalPlaces = 2)]
  public float chance = 1;

  [TweakField(SettingsType = UISettingsType.SliderFloat)]
  [SliderValues(MinValue = 0, MaxValue = 1, Increment = 0.05f, RoundDecimalPlaces = 2)]
  [LoadAlias("maxHealth")]
  public float healthPercent = 1;

  [TweakField(SettingsType = UISettingsType.IntegerBox)]
  public int damage = -1;

  [TweakField(SettingsType = UISettingsType.FloatBox)]
  public float armorPenetration = -1;

  [TweakField(SettingsType = UISettingsType.IntegerBox)]
  public int radius;

  public DamageDef damageDef;

  [TweakField(SettingsType = UISettingsType.IntegerBox)]
  public int wickTicks = 180;

  [TweakField]
  public DrawOffsets drawOffsets;

  string ITweakFields.Category => string.Empty;

  string ITweakFields.Label => nameof(Reactor_Explosive);

  public override void Hit(VehiclePawn vehicle, VehicleComponent component, ref DamageInfo dinfo,
    VehicleComponent.Penetration penetration)
  {
    if (!vehicle.Spawned)
      return;

    if (component.Health > 0 && component.HealthPercent <= healthPercent && Rand.Chance(chance))
    {
      SpawnExploder(vehicle, component);
    }
  }

  protected virtual TimedExplosion CreateExploder(VehiclePawn vehicle, VehicleComponent component)
  {
    if (!component.props.hitbox.cells.TryRandomElement(out IntVec2 offset))
    {
      offset = IntVec2.Zero;
    }
    TimedExplosion.Data data = new(offset, wickTicks, radius, damageDef, damage,
      armorPenetration: armorPenetration);
    TimedExplosion exploder = new(vehicle, data, drawOffsets: drawOffsets);
    return exploder;
  }

  internal virtual void SpawnExploder(VehiclePawn vehicle, VehicleComponent component)
  {
    TimedExplosion exploder = CreateExploder(vehicle, component);
    vehicle.AddTimedExplosion(exploder);
  }

  void ITweakFields.OnFieldChanged()
  {
  }
}
# Tutorial
So you want to make your own boats huh? Great choice! I'm really looking forward to seeing what ideas and designs come out. One thing you
should keep in mind, I am constantly working on improving this mod for both mechanics and content. If you find out that
later on I have added a new mechanic, attachment, or comp class, you are absolutely more than welcome to expand on that. 

**Before We Start:** Please understand that this was a fairly big project, spanning over 87 classes and 95 patches, which solely accounts
for the amount of c# within this mod. In other words... there's around 20k lines of code within this mod which took me around 5 months of 
work as this was a solo project. With that in mind, please do not copy/paste the project file to avoid having a dependency on this mod. 
I put a lot of hard work into this and I really do appreciate being given credit, not to mention that some of the projects on this github 
account as well as my personal one do go on my résumé.

---

## Let's Begin

**Texture**

First, you are going to need a texture to work with. This doesn't necessarily need to be your *finished* texture but you need one nonetheless.
Make sure your image is appropriately sized as stretching or compressing it can cause your texture to be blurry or pixelated. 

In other words... not too big, not too small. 

To give you an idea the Galleon is 1220 x 720 but you can go smaller.

If you know what masking is, and know how to do it, I highly recommend doing it. I intend on adding a button that will allow the player to change
the color. For the boats that I currently have, this means that the flags would change color. For you, it could be whatever you end up masking.

**Your Own Project File**

I know this doesn't really need to be said for some of you that are reading this guide, but for those that aren't completely familiar
with modding in RimWorld, make sure you have adequately set up your project folder. [Here](https://rimworldwiki.com/wiki/Modding_Tutorials)
is a decent guide on helping you do that.

What we will be doing is an XML mod. This will be a bit different from just adding a normal pawn as you will need a few more things.
Once you have all of the correct folders and have headed over into the **Defs** folder, you can start adding the necessary files.

**PawnKindDef**

The first thing we will add is the PawnKindDef. This is what defines the Pawn and its properties. For a boat, this really isn't much.

Adding in your file, it should be named in a way that you can recognize what contents are located
inside the file, however the only actual requirement is that when you name it you end it in .xml so that it is registered as an xml file.

Example: Ship_Races_BoatThatFloats.xml

At the top of the file you're also going to want to add: `<?xml version="1.0" encoding="utf-8"?>`

Now let's dig into the contents. I will be going through all of the parts that you can change to better tailor the boat to what you want.
I will be using the Caravel as the example. Full file is [here](https://github.com/SmashPhil/Boats/blob/master/Defs/Ships/Races/Ship_Races_Caravel.xml)

If I don't list something that is in the file, **don't touch it**. It's not that you *would* break the pawn if you changed it, it's that
I don't recommend playing around with it unless you know what you're doing. I won't put something down here unless it has a meaningful impact
on the boat when you change it. But do make sure that all of the necessary values are included, even if you did not change it.

`defName`: This is what the game stores and finds your def by. It cannot share the same name with any other thing, so make sure it is unique.

`label`: This is the name that appears ingame. It can be whatever you want it to be.

`race`: We will come back to this later, but essentially it's the ThingDef that is associated with this pawn.

`backstoryCategories`: Not really all that important, as I don't believe the game generates stories for the boats.

`combatPower`: Not relevant right now as I do not have any sea based raids yet, but if there were any, this is in relation to the point system for raids.

`texPath`: The texture for your boat that should be stored inside the **Textures** folder. It should also contain 3-4 images. 
ImageName_north, ImageName_south, ImageName_east, (and optional ImageName_west, otherwise it will mirror east)

`drawSize`: How large the image is ingame.

`color`: Only relevant for those that included a mask.

`BodyPartDef`: You can have as many or as little as you want. This is basically what will get damaged (and what accounts for the boats stats 
should a part get damaged). For example, I have a mast that holds the sails, if it is completely broken I want there to be a penalty for movement.
Other than the defName, label, hitPoints, and tags, I don't really recommend changing much else. 

When you are finished making all of your Body Parts, you will link them all together in the `BodyDef`

Once you have finished the BodyDef you are ready to move onto the pawn ThingDef.

## ThingDef 1

ThingDef 1...? Yessir, there are 2 ThingDefs associated with boats, one for the pawn and one for the building. This first one is the more
complicated of the two, so if you can get through this, you're in the homestretch.

Like the PawnKindDef, you're going to need a unique `defName`. Once you have that, go back to the `PawnKindDef` and make sure that the `race`
property is the same as the `defName` you have here.

`label`: Like before this is just the default name so you can put whatever you want.

`description`: This is the description they will see ingame for the Pawn boat.

`statBases`: Here you can change the stats of the boat. Different stats you can (and probably should) change are things such as
- MoveSpeed
- Mass
- ArmorRating_Blunt and ArmorRating_Sharp
- MarketValue

**[Here](#statNotes1)** is a list of all stats. This doesn't mean they all apply to boats however.

Next up is the size of the boat. This is that white box that surrounds the pawn. For basically every pawn in the game and 99% of the mods
out there, this is 1x1. For boats however, you really do want this to fit the shape of the boat snug. Is your hitbox a bit off since the texture
extends past where you want the hitbox to be? Not to worry, I have a way to shift the hitbox left, right, up, and down coming up later.

`fleshType` Don't mess with this part too much. If you make it Mechanoid, it will spawn in ship parts and ancient dangers, if you make it
flesh type, it will reallyyyy screw with the boat and have some very weird behavior. For this reason I made my own flesh type. Appropriately
you have 3 to choose from. `WoodenShip`, `MetalShip`, and `SpacerShip`. Later down the road I intend to add some differences between the 3, but
for now there really is no difference. But I would choose the one that suits your boat best.

`baseBodySize` This is what determines how much your boat can carry. MassCarried = 35 * baseBodySize. So if it has a baseBodySize of 10, it can carry 350kg.

---

## CompClass

Now we enter into the CompClass. This is the heart of what makes a boat, a **boat**. Without this comp class, your "boat" is nothing.

Every value in here matters, so feel free to have some fun with it.

`downable`: I would suggest not touching this, as downing a boat will result in some weird behaviors, but basically this is what 
determines whether or not a boat will be downed.

`movesWhenDowned`: If for some reason you decide to allow your boat to be downable, this determines whether or not it wiggles like a regular pawn. 

`moveable`: This is what determines whethe a ship can be drafted or not. There are 3 options. NotAllowed, DriverNeeded, and NoDriverNeeded. 

`riverTraversability`: The smallest possible river this boat can go through. There are 4 types of rivers: Creek, River, LargeRiver, and HugeRiver.

`shipPowerType`: Right now it only affects pawn rest rates and traveling at night for caravans. In the future these will change.
Options:
- Paddles
- Sails
- Steam
- Fuel
- Nuclear

`nameable`: Whether or not the player may name the boat.

`buildDef`: This will be the ThingDef building that is associated with this boat. When a boat "dies" on shallow water it despawns and 
spawns a damaged building of the boat. When the boat building is fully repaired, it despawns the building and spawns the boat pawn. 
This is to provide as much realism to boats sinking. We will come back to this later.

`ticksBetweenRepair` This is how many ticks need to pass in order for the next injury on the boat to be repaired. Each "repair" is 0.1, like how 
how vanilla natural healing is. Keep this in mind when setting large or small values.

`healthLabels` These are what display for the boat in the GUI. You can put whatever string you want. Right now the only one that doesn't
show is `healthLabel_Dead`, but I have future plans for it. The rest are completely up to you.

`hitboxOffsetX` and `hitboxOffsetZ`: move the boats hitbox based on these values. 

---

**Roles**

These are what define the different roles your pawns can board into.

For the Caravel, these include the Captain, Crew, Cannons, and Passenger. You don't have to have the same amount as I do, nor do they
need to have the same names or requirements, but I do recommend *at least* having 1 role. Otherwise, your pawns can't board.

`label`: Name that appears ingame.

`handlingType`: What the role affects. If you choose something other than None, you will need to fill the slots needed to operate
that role in order to activate that handlingType. So if you have 2 roles that affect Movement, both must be filled in order to move. 
Turrets is also currently WIP so don't choose that.
Options:
- None
- Cannons
- Turret
- Movement

`slots`: Maximum number of pawns that can board in this role.

`slotsToOperate`: Minimum number of pawns in order to operate this role. If not included, it will default to 0.

## Cannons
*Did you say cannons??* Why yes! Yes I did. Right now the only cannons available for boats are broadside cannons, or cannons that fire
from the side of the boat. I will add turret based cannons (think... 16"/50 caliber Mark 7 from WW2 Battleships) and also some other variations
such as torpedos, but these are in the future of course. When I do add them, I will update this guide.

**Setting up your Cannons**

Each list entry `<li> </li>` is 1 cannon side. You can technically add as many as you want but for now we will just look at 1.

`label`: The label that will appear on the gizmo associated with this cannon.

`weaponType`: The only weapon type that works right now is Broadside. More to come in the future.

`weaponLocation`: Since they are broadside, you can choose between Port and Starboard.

`projectile`: The projectile def that will spawn when this cannon fires.

`cannonSound`: The sound def that will play when this cannon fires.

`numberCannons`: The number of cannons on the side specified by `weaponLocation`. Can be as many as you would like.

`baseTicksBetweenShots`: The number of ticks that pass between each cannon firing. If 0, all cannons will fire simultaneously.

`cooldownTimer`: The number of seconds until you can fire these cannons again.

`spacing`: The size of spacing between each cannon on the boat.

`offset`: The vertical offset from the boats centerpoint for the center of the cannons. To put this into perspective... say you want 4 cannons
on the portside, but you want them a bit towards the front of the boat. This value will allow you to push the group of cannons forward or backward.

`projectileOffset`: The offset from the edge of the hitbox in which the projectile will spawn. This is moreso for smaller boats where the projectile
might spawn inside the cannon or a bit behind it.

**-----------------------------------**

**Optional Settings**

If you look at the Galleon's file, you'll notice some extra values. These are for some special circumstances.

`splitCannonGroups`: Split the number of cannons on this side, into groups.

`centerPoints`: The center points for each group. 

`cannonsPerPoint`: Number of cannons in the corresponding group.

**note:** number of entries in `centerPoints` must match the number of entries in `cannonsPerPoint`. The total number of cannons in `cannonsPerPoint`
must also add up to the number of cannons specified in `numberCannons`.

What exactly are these optional values for? For groups of cannons that might not be equally spaced along the side of the boat.

Example: A boat with 3 cannons towards the front and 3 cannons towards the back (with a noticeable space in the middle between these 
2 groups of cannons... like the Galleon!).

**-----------------------------------**

`hitFlags`: The hitflags of the projectile. This determines when the projectile will explode (or explode prematurely).
Flags: 
- None
- IntendedTarget
- NonTargetPawns
- NonTargetWorld
- All

`spreadRadius`: the radius in which the projectiles will spread at max range.

`minRange`: Minimum range of cannon

`maxRange`: Maximum range of cannon

## Boat Building

Now that we have cannons under wrap, you have one more thing to add. The building! Why? Well because if there wasn't a building you
wouldn't be able to build the boat or repair the sunken boat. You'd also get a nasty error from the game complaining about a null
corpse from the boat, since I have no intention of ever having a "corpse" for boats.

Rather than going through *every* single value like we have for the previous, I'll tell you what you really need that is different than just
your average building ThingDef.

1. Same DrawSize and Size as the pawn variant, unless you're cool with shrinking / expanding boats.
2. Same Texture (or similar texture if you want to change how it looks as a broken down ship apart from the damage markers)
3. defName needs to be added to `buildDef` inside `CompProperties_Ships` in your pawn ThingDef. This is so the boat pawn knows what building to
replace itself with if it dies.
4. ModExtension `RimShips.Build.SpawnThingBuilt`: This is for building the boat, and what resulting boat pawn will be spawned once it is
built. `thingToSpawn` should be the PawnKindDef `defName` and soundFinished is the sound it plays when the boat is constructed. If you don't put
anything it won't play anything.


### Optional
- `ResearchDef`: so that the player must research in order to unlock the boat.
- `Projectiles`: Create your own projectile to launch from the cannons.
- `SoundDefs`: Create your own sounds to play from the cannons, build completion, and in the near future sound during travel.

---

And that's about it! You should now have a functional boat inside RimWorld.

If you publish your mod on the steam workshop please leave Boats as a dependency and let me know that you've updloaded a boat mod. This second
part isn't because I want to keep tabs on you, it's so I can pay attention to what players want more of. If there is an apparent desire for
WW2 era ships, I'll definitely be focusing on adding mechanics for those kinds of ships. Same goes for any other style of boats.

Hope you are as happy with boats as I am. Have fun!

---

<b name="statNotes1">List of All Stats</b>: 
```
public static StatDef MaxHitPoints;

// Token: 0x04002217 RID: 8727
public static StatDef MarketValue;

// Token: 0x04002218 RID: 8728
public static StatDef SellPriceFactor;

// Token: 0x04002219 RID: 8729
public static StatDef Beauty;

// Token: 0x0400221A RID: 8730
public static StatDef Cleanliness;

// Token: 0x0400221B RID: 8731
public static StatDef Flammability;

// Token: 0x0400221C RID: 8732
public static StatDef DeteriorationRate;

// Token: 0x0400221D RID: 8733
public static StatDef WorkToMake;

// Token: 0x0400221E RID: 8734
public static StatDef WorkToBuild;

// Token: 0x0400221F RID: 8735
public static StatDef Mass;

// Token: 0x04002220 RID: 8736
public static StatDef ConstructionSpeedFactor;

// Token: 0x04002221 RID: 8737
public static StatDef Nutrition;

// Token: 0x04002222 RID: 8738
public static StatDef FoodPoisonChanceFixedHuman;

// Token: 0x04002223 RID: 8739
public static StatDef MoveSpeed;

// Token: 0x04002224 RID: 8740
public static StatDef GlobalLearningFactor;

// Token: 0x04002225 RID: 8741
public static StatDef HungerRateMultiplier;

// Token: 0x04002226 RID: 8742
public static StatDef RestRateMultiplier;

// Token: 0x04002227 RID: 8743
public static StatDef PsychicSensitivity;

// Token: 0x04002228 RID: 8744
public static StatDef ToxicSensitivity;

// Token: 0x04002229 RID: 8745
public static StatDef MentalBreakThreshold;

// Token: 0x0400222A RID: 8746
public static StatDef EatingSpeed;

// Token: 0x0400222B RID: 8747
public static StatDef ComfyTemperatureMin;

// Token: 0x0400222C RID: 8748
public static StatDef ComfyTemperatureMax;

// Token: 0x0400222D RID: 8749
public static StatDef Comfort;

// Token: 0x0400222E RID: 8750
public static StatDef MeatAmount;

// Token: 0x0400222F RID: 8751
public static StatDef LeatherAmount;

// Token: 0x04002230 RID: 8752
public static StatDef MinimumHandlingSkill;

// Token: 0x04002231 RID: 8753
public static StatDef MeleeDPS;

// Token: 0x04002232 RID: 8754
public static StatDef PainShockThreshold;

// Token: 0x04002233 RID: 8755
public static StatDef ForagedNutritionPerDay;

// Token: 0x04002234 RID: 8756
public static StatDef WorkSpeedGlobal;

// Token: 0x04002235 RID: 8757
public static StatDef MiningSpeed;

// Token: 0x04002236 RID: 8758
public static StatDef MiningYield;

// Token: 0x04002237 RID: 8759
public static StatDef ResearchSpeed;

// Token: 0x04002238 RID: 8760
public static StatDef ConstructionSpeed;

// Token: 0x04002239 RID: 8761
public static StatDef HuntingStealth;

// Token: 0x0400223A RID: 8762
public static StatDef PlantWorkSpeed;

// Token: 0x0400223B RID: 8763
public static StatDef SmoothingSpeed;

// Token: 0x0400223C RID: 8764
public static StatDef FoodPoisonChance;

// Token: 0x0400223D RID: 8765
public static StatDef CarryingCapacity;

// Token: 0x0400223E RID: 8766
public static StatDef PlantHarvestYield;

// Token: 0x0400223F RID: 8767
public static StatDef FixBrokenDownBuildingSuccessChance;

// Token: 0x04002240 RID: 8768
public static StatDef ConstructSuccessChance;

// Token: 0x04002241 RID: 8769
public static StatDef UnskilledLaborSpeed;

// Token: 0x04002242 RID: 8770
public static StatDef MedicalTendSpeed;

// Token: 0x04002243 RID: 8771
public static StatDef MedicalTendQuality;

// Token: 0x04002244 RID: 8772
public static StatDef MedicalSurgerySuccessChance;

// Token: 0x04002245 RID: 8773
public static StatDef NegotiationAbility;

// Token: 0x04002246 RID: 8774
public static StatDef TradePriceImprovement;

// Token: 0x04002247 RID: 8775
public static StatDef SocialImpact;

// Token: 0x04002248 RID: 8776
public static StatDef AnimalGatherSpeed;

// Token: 0x04002249 RID: 8777
public static StatDef AnimalGatherYield;

// Token: 0x0400224A RID: 8778
public static StatDef TameAnimalChance;

// Token: 0x0400224B RID: 8779
public static StatDef TrainAnimalChance;

// Token: 0x0400224C RID: 8780
public static StatDef ShootingAccuracyPawn;

// Token: 0x0400224D RID: 8781
public static StatDef ShootingAccuracyTurret;

// Token: 0x0400224E RID: 8782
public static StatDef AimingDelayFactor;

// Token: 0x0400224F RID: 8783
public static StatDef MeleeHitChance;

// Token: 0x04002250 RID: 8784
public static StatDef MeleeDodgeChance;

// Token: 0x04002251 RID: 8785
public static StatDef MeleeWeapon_AverageDPS;

// Token: 0x04002252 RID: 8786
public static StatDef MeleeWeapon_DamageMultiplier;

// Token: 0x04002253 RID: 8787
public static StatDef MeleeWeapon_CooldownMultiplier;

// Token: 0x04002254 RID: 8788
public static StatDef SharpDamageMultiplier;

// Token: 0x04002255 RID: 8789
public static StatDef BluntDamageMultiplier;

// Token: 0x04002256 RID: 8790
public static StatDef StuffPower_Armor_Sharp;

// Token: 0x04002257 RID: 8791
public static StatDef StuffPower_Armor_Blunt;

// Token: 0x04002258 RID: 8792
public static StatDef StuffPower_Armor_Heat;

// Token: 0x04002259 RID: 8793
public static StatDef StuffPower_Insulation_Cold;

// Token: 0x0400225A RID: 8794
public static StatDef StuffPower_Insulation_Heat;

// Token: 0x0400225B RID: 8795
public static StatDef RangedWeapon_Cooldown;

// Token: 0x0400225C RID: 8796
public static StatDef RangedWeapon_DamageMultiplier;

// Token: 0x0400225D RID: 8797
public static StatDef AccuracyTouch;

// Token: 0x0400225E RID: 8798
public static StatDef AccuracyShort;

// Token: 0x0400225F RID: 8799
public static StatDef AccuracyMedium;

// Token: 0x04002260 RID: 8800
public static StatDef AccuracyLong;

// Token: 0x04002261 RID: 8801
public static StatDef StuffEffectMultiplierArmor;

// Token: 0x04002262 RID: 8802
public static StatDef StuffEffectMultiplierInsulation_Cold;

// Token: 0x04002263 RID: 8803
public static StatDef StuffEffectMultiplierInsulation_Heat;

// Token: 0x04002264 RID: 8804
public static StatDef ArmorRating_Sharp;

// Token: 0x04002265 RID: 8805
public static StatDef ArmorRating_Blunt;

// Token: 0x04002266 RID: 8806
public static StatDef ArmorRating_Heat;

// Token: 0x04002267 RID: 8807
public static StatDef Insulation_Cold;

// Token: 0x04002268 RID: 8808
public static StatDef Insulation_Heat;

// Token: 0x04002269 RID: 8809
public static StatDef EnergyShieldRechargeRate;

// Token: 0x0400226A RID: 8810
public static StatDef EnergyShieldEnergyMax;

// Token: 0x0400226B RID: 8811
public static StatDef SmokepopBeltRadius;

// Token: 0x0400226C RID: 8812
public static StatDef EquipDelay;

// Token: 0x0400226D RID: 8813
public static StatDef MedicalPotency;

// Token: 0x0400226E RID: 8814
public static StatDef MedicalQualityMax;

// Token: 0x0400226F RID: 8815
public static StatDef ImmunityGainSpeed;

// Token: 0x04002270 RID: 8816
public static StatDef ImmunityGainSpeedFactor;

// Token: 0x04002271 RID: 8817
public static StatDef DoorOpenSpeed;

// Token: 0x04002272 RID: 8818
public static StatDef BedRestEffectiveness;

// Token: 0x04002273 RID: 8819
public static StatDef TrapMeleeDamage;

// Token: 0x04002274 RID: 8820
public static StatDef TrapSpringChance;

// Token: 0x04002275 RID: 8821
public static StatDef ResearchSpeedFactor;

// Token: 0x04002276 RID: 8822
public static StatDef MedicalTendQualityOffset;

// Token: 0x04002277 RID: 8823
public static StatDef WorkTableWorkSpeedFactor;

// Token: 0x04002278 RID: 8824
public static StatDef WorkTableEfficiencyFactor;

// Token: 0x04002279 RID: 8825
public static StatDef JoyGainFactor;

// Token: 0x0400227A RID: 8826
public static StatDef SurgerySuccessChanceFactor;
```

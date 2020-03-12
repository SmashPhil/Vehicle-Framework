using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Globalization;
using Verse;
using RimWorld;
using SPExtended;

namespace RimShips
{
    public enum BiomeCategory { None, Temperate, Tropical, Cold}

    [StaticConstructorOnStartup]
    internal static class FishingCompatibility
    {
        static FishingCompatibility()
        {
            List<ModMetaData> mods = ModLister.AllInstalledMods.ToList();
            fishingActivated = false;
            for(int i = 0; i < mods.Count; i++)
            {
                ModMetaData mod = mods[i];
                if(ModLister.HasActiveModWithName(mod.Name) && AddFishesFromMod(mod))
                {
                    Log.Message("[Boats] Adding Fishing Compatibility for: " + mod.Name);
                    fishingActivated = true;
                    break;
                }
            }
        }

        static bool AddFishesFromMod(ModMetaData mod)
        {
            fishDictionaryTemperateBiomeFreshWater.Clear();
            fishDictionaryTropicalBiomeFreshWater.Clear();
            fishDictionaryColdBiomeFreshWater.Clear();
            fishDictionarySaltWater.Clear();

            if (string.Equals(mod.PackageId, "VanillaExpanded.VCEF", StringComparison.OrdinalIgnoreCase))
            {
                string[] versionControl = VersionControl.CurrentVersionString.Split('.');
                string currentVersion = string.Concat(new string[]{ versionControl[0], ".", versionControl[1] });
                string modDirectory = string.Concat(new string[] { mod.RootDir.ToString(), "/", currentVersion, "/Defs/FishDefs/FishDefs.xml" });
                if(Prefs.DevMode || ShipHarmony.debug)
                    Log.Message(string.Concat(new string[] { "[Debug] Loading Fishing Mod Info for [Boats] from: \n", modDirectory, "\n using version ", currentVersion }));
                XmlDocument doc = new XmlDocument();
                try
                {
                    doc.Load(modDirectory);
                }
                catch
                {
                    Log.Error(string.Concat(new string[]{
                    "[Boats] Failed to load document from ",
                    mod.Name,
                    ". Terminating Fishing Compatibility but game should load normally."}));
                    return false;
                }

                XmlNodeList fishDefs = doc.GetElementsByTagName("VCE_Fishing.FishDef");

                for (int i = 0; i < fishDefs.Count; i++)
                {
                    ThingDef td = null;
                    List<BiomeCategory> biomesAllowed = new List<BiomeCategory>();
                    bool oceanFishing = false;

                    int yield = 0;
                    for(int j = 0; j < fishDefs[i].ChildNodes.Count; j++)
                    {
                        if(fishDefs[i].ChildNodes[j].Name == "fishSizeCategory" && fishDefs[i].ChildNodes[j].InnerText == "Special")
                            goto Block_Skip;
                        switch(fishDefs[i].ChildNodes[j].Name)
                        {
                            case "thingDef":
                                td = DefDatabase<ThingDef>.GetNamed(fishDefs[i].ChildNodes[j].InnerText);
                                break;
                            case "allowedBiomes":
                                for(int k = 0; k < fishDefs[i].ChildNodes[j].ChildNodes.Count; k++)
                                {
                                    switch(fishDefs[i].ChildNodes[j].ChildNodes[k].InnerText)
                                    {
                                        case "Cold":
                                            biomesAllowed.Add(BiomeCategory.Cold);
                                            break;
                                        case "Warm":
                                            biomesAllowed.Add(BiomeCategory.Temperate);
                                            break;
                                        case "Hot":
                                            biomesAllowed.Add(BiomeCategory.Tropical);
                                            break;
                                    }
                                }
                                break;
                            case "canBeSaltwater":
                                oceanFishing = true;
                                break;
                            case "baseFishingYield":
                                if(!Int32.TryParse(fishDefs[i].ChildNodes[j].InnerText, out yield))
                                {
                                    Log.Warning("Unable to parse yield value in " + mod.Name + " for XmlNode " + fishDefs[i].ChildNodes[j].Name + ". Defaulting value to 0");
                                }
                                break;
                        }
                    }
                    if (td != null)
                    {
                        if(ShipHarmony.debug) Log.Message("Adding: " + td.defName + " with yield: " + yield);

                        foreach(BiomeCategory bc in biomesAllowed)
                        {
                            switch(bc)
                            {
                                case BiomeCategory.Cold:
                                    fishDictionaryColdBiomeFreshWater.Add(td, yield);
                                    break;
                                case BiomeCategory.Temperate:
                                    fishDictionaryTemperateBiomeFreshWater.Add(td, yield);
                                    break;
                                case BiomeCategory.Tropical:
                                    fishDictionaryTropicalBiomeFreshWater.Add(td, yield);
                                    break;
                            }
                        }

                        if(oceanFishing) fishDictionarySaltWater.Add(td, yield);
                    }
                Block_Skip:;
                }
            }
            else if(mod.Name == "[RF] Fishing [1.0]")
            {
                string modDirectory = mod.RootDir + "/Defs/ThingDefsMisc.xml";
                if (Prefs.DevMode || ShipHarmony.debug)
                    Log.Message("[Debug] Loading Fishing Mod Info for [Boats] from: " + modDirectory);
                XmlDocument doc = new XmlDocument();
                doc.Load(modDirectory);

                XmlNodeList fishDefs = doc.GetElementsByTagName("ThingDef");

                for (int i = 0; i < fishDefs.Count; i++)
                {
                    ThingDef td = null;
                    List<BiomeCategory> biomesAllowed = new List<BiomeCategory>();
                    bool oceanFishing = false;
                    bool categoryFlag = false;
                    int yield = 0;
                    for (int j = 0; j < fishDefs[i].ChildNodes.Count; j++)
                    {
                        switch (fishDefs[i].ChildNodes[j].Name)
                        {
                            case "defName":
                                td = DefDatabase<ThingDef>.GetNamed(fishDefs[i].ChildNodes[j].InnerText);
                                break;
                            case "canBeSaltwater":
                                oceanFishing = true;
                                break;
                            case "statBases":
                                for(int k = 0; k < fishDefs[i].ChildNodes[j].ChildNodes.Count; k++)
                                {
                                    if(fishDefs[i].ChildNodes[j].ChildNodes[k].Name == "Nutrition")
                                    {
                                        float nutrition = float.Parse(fishDefs[i].ChildNodes[j].ChildNodes[k].InnerText, CultureInfo.InvariantCulture.NumberFormat);
                                        yield = ((int)((1 - nutrition) * 20)).Clamp(1,20);
                                        goto Block_Loop;
                                    }
                                }
                                goto Block_Skip;
                            case "thingCategories":
                                categoryFlag = true;
                                for (int k = 0; k < fishDefs[i].ChildNodes[j].ChildNodes.Count; k++)
                                {
                                    if(fishDefs[i].ChildNodes[j].ChildNodes[k].InnerText == "CorpsesFish")
                                        goto Block_Loop;
                                }
                                goto Block_Skip;
                        }
                        Block_Loop:;
                    }
                    if (td != null && categoryFlag)
                    {
                        if(ShipHarmony.debug) Log.Message("Adding: " + td.defName + " with yield: " + yield);

                        foreach (BiomeCategory bc in biomesAllowed)
                        {
                            switch (bc)
                            {
                                case BiomeCategory.Cold:
                                    fishDictionaryColdBiomeFreshWater.Add(td, yield);
                                    break;
                                case BiomeCategory.Temperate:
                                    fishDictionaryTemperateBiomeFreshWater.Add(td, yield);
                                    break;
                                case BiomeCategory.Tropical:
                                    fishDictionaryTropicalBiomeFreshWater.Add(td, yield);
                                    break;
                            }
                        }

                        if(oceanFishing || !biomesAllowed.Any()) fishDictionarySaltWater.Add(td, yield);
                    }
                    Block_Skip:;
                }
            }
            return fishDictionarySaltWater.Any();
        }


        public static Dictionary<ThingDef, int> fishDictionaryTemperateBiomeFreshWater = new Dictionary<ThingDef, int>();
        public static Dictionary<ThingDef, int> fishDictionaryTropicalBiomeFreshWater = new Dictionary<ThingDef, int>();
        public static Dictionary<ThingDef, int> fishDictionaryColdBiomeFreshWater = new Dictionary<ThingDef, int>();
        public static Dictionary<ThingDef, int> fishDictionarySaltWater = new Dictionary<ThingDef, int>();
        public static bool fishingActivated;
    }
}

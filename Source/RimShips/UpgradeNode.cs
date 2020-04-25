using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;

namespace RimShips
{
    public class UpgradeNode : IExposable, ILoadReferenceable
    {
        public string label;

        public string upgradeID;

        public string rootNodeLabel;

        public string informationHighlighted;

        public string disableIfUpgradeNodeEnabled;

        public StatUpgrade upgradeCategory;

        public Dictionary<StatUpgrade, float> values = new Dictionary<StatUpgrade, float>();

        public List<CannonHandler> cannonsUnlocked = new List<CannonHandler>();
        
        public Dictionary<ThingDef, int> cost = new Dictionary<ThingDef,int>();

        public List<ResearchProjectDef> researchPrerequisites = new List<ResearchProjectDef>();

        public List<string> prerequisiteNodes = new List<string>();

        public string imageFilePath;

        public IntVec2 gridCoordinate;

        public string upgradeTime;

        public int upgradeTicksLeft; //Uninitialized

        public UpgradeNode()
        {
        }

        public UpgradeNode(UpgradeNode reference)
        {
            nodeID = Find.UniqueIDsManager.GetNextThingID();

            label = reference.label;
            upgradeID = reference.upgradeID;
            rootNodeLabel = reference.rootNodeLabel;
            informationHighlighted = reference.informationHighlighted;
            disableIfUpgradeNodeEnabled = reference.disableIfUpgradeNodeEnabled;
            upgradeCategory = reference.upgradeCategory;
            values = reference.values;
            cannonsUnlocked = reference.cannonsUnlocked;
            cost = reference.cost;
            researchPrerequisites = reference.researchPrerequisites;
            prerequisiteNodes = reference.prerequisiteNodes;
            imageFilePath = reference.imageFilePath;
            gridCoordinate = reference.gridCoordinate;
            upgradeTime = reference.upgradeTime;
        }

        public int UpgradeTimeParsed
        {
            get
            {
                if (string.IsNullOrEmpty(upgradeTime))
                    return 0;
                int totalTicks = 0;
			    foreach (string timeStamp in upgradeTime.Split(','))
			    {
				    bool parsed = int.TryParse(string.Concat(timeStamp.Where(char.IsDigit)), out int numeric);
				    totalTicks += numeric * HelperMethods.GetTickMultiplier(timeStamp[timeStamp.Length - 1]);
			    }
			    return totalTicks;
            }
        }

        public void ResetTimer()
        {
            upgradeTicksLeft = UpgradeTimeParsed;
        }

        public IntVec2 GridCoordinate
        {
            get
            {
                if(gridCoordinate.x > 29 || gridCoordinate.z > 25)
                {
                    throw new NotSupportedException($"Maximum grid coordinate size is 29x25. Larger coordinates are not supported. GridCoord: ({gridCoordinate.x},{gridCoordinate.z})");
                }
                return gridCoordinate;
            }
        }

        public bool upgradeActive;

        public int nodeID;

        private Texture2D upgradeImage;


        public Texture2D UpgradeImage
        {
            get
            {
                if(string.IsNullOrEmpty(imageFilePath))
                {
                    return HelperMethods.missingIcon;
                }
                if(upgradeImage is null)
                {
                    upgradeImage = ContentFinder<Texture2D>.Get(imageFilePath, true);
                }
                return upgradeImage;
            }
        }

        public void InitializeLists()
        {
            if (values is null)
                values = new Dictionary<StatUpgrade, float>();

            if(cannonsUnlocked is null)
                cannonsUnlocked = new List<CannonHandler>();

            foreach(CannonHandler cannon in cannonsUnlocked)
            {
                if(cannon.uniqueID < 0)
                {
                    cannon.uniqueID = Find.UniqueIDsManager.GetNextThingID();
                }
            }

            if(cost is null)
                cost = new Dictionary<ThingDef,int>();

            if(researchPrerequisites is null)
                researchPrerequisites = new List<ResearchProjectDef>();

            if(prerequisiteNodes is null)
                prerequisiteNodes = new List<string>();
        }

        public override bool Equals(object obj)
        {
            return obj is UpgradeNode && Equals((UpgradeNode)obj);
        }

        public bool Equals(UpgradeNode u)
        {
            return upgradeID == u.upgradeID;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(UpgradeNode u1, UpgradeNode u2)
        {
            return u1?.upgradeID == u2?.upgradeID;
        }

        public static bool operator !=(UpgradeNode u1, UpgradeNode u2)
        {
            return u1?.upgradeID != u2?.upgradeID;
        }

        public string GetUniqueLoadID()
        {
            return $"UpgradeNode_{upgradeID}-{nodeID}";
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref label, "label");
            Scribe_Values.Look(ref upgradeID, "upgradeID");
            Scribe_Values.Look(ref rootNodeLabel, "rootNodeLabel");
            Scribe_Values.Look(ref informationHighlighted, "informationHighlighted");
            Scribe_Values.Look(ref disableIfUpgradeNodeEnabled, "disableIfUpgradeNodeEnabled");
            Scribe_Values.Look(ref upgradeCategory, "upgradeCategory");
            Scribe_Values.Look(ref upgradeTicksLeft, "upgradeTicksLeft");

            Scribe_Values.Look(ref upgradeActive, "upgradeActive");

            Scribe_Collections.Look<ThingDef, int>(ref cost, "cost", LookMode.Def, LookMode.Value);
            Scribe_Collections.Look<StatUpgrade, float>(ref values, "values", LookMode.Value, LookMode.Value);

            Scribe_Collections.Look(ref researchPrerequisites, "researchPrerequisites", LookMode.Def);
            
            Scribe_Collections.Look(ref cannonsUnlocked, "cannonsUnlocked", LookMode.Deep);

            Scribe_Collections.Look(ref prerequisiteNodes, "prerequisiteNodes", LookMode.Value);
            Scribe_Values.Look(ref imageFilePath, "imageFilePath");
            Scribe_Values.Look(ref gridCoordinate, "gridCoordinate");
            Scribe_Values.Look(ref nodeID, "nodeID");
        }
    }
}

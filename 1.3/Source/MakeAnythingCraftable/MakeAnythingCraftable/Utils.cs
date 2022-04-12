using System.Collections.Generic;
using Verse;
using RimWorld;

namespace MakeAnythingCraftable
{
    [StaticConstructorOnStartup]
    public static class Utils
    {
        public static List<ThingDef> workbenches = new List<ThingDef>();
        public static List<ThingDef> craftableItems = new List<ThingDef>();
        public static List<ThingDef> unfinishedThings = new List<ThingDef>();

        public static HashSet<StatDef> workSpeedStats = new HashSet<StatDef>();
        public static HashSet<StatDef> efficiencyStats = new HashSet<StatDef>();
        public static HashSet<EffecterDef> effecterDefs = new HashSet<EffecterDef>();
        public static HashSet<SoundDef> soundDefs = new HashSet<SoundDef>();

        static Utils()
        {
            foreach (var item in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if ((DebugThingPlaceHelper.IsDebugSpawnable(item) || item.Minifiable)
                    && !typeof(Filth).IsAssignableFrom(item.thingClass) 
                    && !typeof(Mote).IsAssignableFrom(item.thingClass)
                    && item.category != ThingCategory.Ethereal && item.plant is null 
                    && (item.building is null || item.Minifiable))
                {
                    craftableItems.Add(item);
                }
                if (item.IsWorkTable)
                {
                    workbenches.Add(item);
                }
                if (item.isUnfinishedThing)
                {
                    unfinishedThings.Add(item);
                }
            }

            foreach (var recipe in DefDatabase<RecipeDef>.AllDefs)
            {
                if (recipe.workSpeedStat != null)
                {
                    workSpeedStats.Add(recipe.workSpeedStat);
                }
                if (recipe.efficiencyStat != null)
                {
                    efficiencyStats.Add(recipe.efficiencyStat);
                }
                if (recipe.soundWorking != null)
                {
                    soundDefs.Add(recipe.soundWorking);
                }
                if (recipe.effectWorking != null)
                {
                    effecterDefs.Add(recipe.effectWorking);
                }
            }
            MakeAnythingCraftableMod.settings.ApplySettings();
        }
    }
}

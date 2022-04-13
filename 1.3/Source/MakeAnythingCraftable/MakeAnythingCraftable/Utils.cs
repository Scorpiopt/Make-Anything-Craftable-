using System.Collections.Generic;
using Verse;
using RimWorld;
using System.Linq;
using HarmonyLib;
using UnityEngine;

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
        public static HashSet<RecipeDef> medicalRecipes = new HashSet<RecipeDef>();
        public static List<RecipeDef> craftingRecipes = new List<RecipeDef>();
        static Utils()
        {
            new Harmony("MakeAnythingCraftable.Mod").PatchAll();
            foreach (var item in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (typeof(Pawn).IsAssignableFrom(item.thingClass))
                {
                    foreach (var recipe in item.AllRecipes)
                    {
                        medicalRecipes.Add(recipe);
                    }
                }

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
                if (!medicalRecipes.Contains(recipe) && recipe.products != null 
                    && recipe.products.Count == 1)
                {
                    craftingRecipes.Add(recipe);
                }
            }
            MakeAnythingCraftableMod.settings.ApplySettings();
        }
    }

    [HarmonyPatch(typeof(StatsReportUtility), "Reset")]
    public static class StatsReportUtility_Reset_Patch
    {
        public static bool Prefix()
        {
            if (Current.Game?.World?.factionManager is null)
            {
                Reset();
                return false;
            }
            return true;
        }

        public static void Reset()
        {
            StatsReportUtility.scrollPosition = default(Vector2);
            StatsReportUtility.scrollPositionRightPanel = default(Vector2);
            StatsReportUtility.selectedEntry = null;
            StatsReportUtility.scrollPositioner.Arm(armed: false);
            StatsReportUtility.mousedOverEntry = null;
            StatsReportUtility.cachedDrawEntries.Clear();
            StatsReportUtility.quickSearchWidget.Reset();
            PermitsCardUtility.selectedPermit = null;
            PermitsCardUtility.selectedFaction = null;
        }
    }
}

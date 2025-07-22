using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace InfoCardPatches
{
    [StaticConstructorOnStartup]
    public static class Startup
    {
        static Startup()
        {
            new Harmony("InfoCardPatches.Mod").PatchAll();
        }
    }

    [HarmonyPatch(typeof(StatWorker), nameof(StatWorker.ShouldShowFor))]
    public static class StatWorker_ShouldShowFor_Patch
    {
        public static bool Prefix(ref bool __result, StatWorker __instance, StatRequest req)
        {
            if (Current.Game?.World?.factionManager is null)
            {
                __result = ShouldShowFor(__instance, req);
                return false;
            }
            return true;
        }
        public static bool ShouldShowFor(StatWorker __instance, StatRequest req)
        {
            var stat = __instance.stat;
            if (stat.alwaysHide)
            {
                return false;
            }
            var def = req.Def;
            if (!stat.showIfUndefined && !req.StatBases.StatListContains(stat))
            {
                return false;
            }
            if (!stat.CanShowWithLoadedMods())
            {
                return false;
            }
            if (stat.parts != null)
            {
                foreach (var part in stat.parts)
                {
                    if (part.ForceShow(req))
                    {
                        return true;
                    }
                }
            }
            if (req.Thing is Pawn pawn)
            {
                if (pawn.health != null && !stat.showIfHediffsPresent.NullOrEmpty())
                {
                    for (int i = 0; i < stat.showIfHediffsPresent.Count; i++)
                    {
                        if (!pawn.health.hediffSet.HasHediff(stat.showIfHediffsPresent[i]))
                        {
                            return false;
                        }
                    }
                }
                if (stat.showOnSlavesOnly && !pawn.IsSlave)
                {
                    return false;
                }
            }
            if (stat == StatDefOf.MaxHitPoints && req.HasThing)
            {
                return false;
            }
            if (!stat.showOnUntradeables && !StatWorker.DisplayTradeStats(req))
            {
                return false;
            }
            var thingDef = def as ThingDef;
            if (thingDef != null)
            {
                if (thingDef.category == ThingCategory.Pawn)
                {
                    if (!stat.showOnPawns)
                    {
                        return false;
                    }
                    if (!stat.showOnHumanlikes && thingDef.race.Humanlike)
                    {
                        return false;
                    }
                    if (!stat.showOnNonWildManHumanlikes && thingDef.race.Humanlike && (!(req.Thing is Pawn p) || !p.IsWildMan()))
                    {
                        return false;
                    }
                    if (!stat.showOnAnimals && thingDef.race.Animal)
                    {
                        return false;
                    }
                    if (!stat.showOnEntities && thingDef.race.IsAnomalyEntity)
                    {
                        return false;
                    }
                    if (!stat.showOnMechanoids && thingDef.race.IsMechanoid)
                    {
                        return false;
                    }
                    if (!stat.showOnDrones && thingDef.race.IsDrone)
                    {
                        return false;
                    }
                    if (req.Thing is Pawn pawn2 && !stat.showDevelopmentalStageFilter.Has(pawn2.DevelopmentalStage))
                    {
                        return false;
                    }
                }
                if (!stat.showOnUnhaulables && !thingDef.EverHaulable && !thingDef.Minifiable)
                {
                    return false;
                }
            }
            if (stat.category == StatCategoryDefOf.BasicsPawn || stat.category == StatCategoryDefOf.BasicsPawnImportant
                || stat.category == StatCategoryDefOf.PawnCombat || stat.category == StatCategoryDefOf.Animals
                || stat.category == StatCategoryDefOf.PawnResistances || stat.category == StatCategoryDefOf.PawnHealth
                || stat.category == StatCategoryDefOf.PawnFood || stat.category == StatCategoryDefOf.PawnPsyfocus)
            {
                if (thingDef != null)
                {
                    return thingDef.category == ThingCategory.Pawn;
                }
                return false;
            }
            if (stat.category == StatCategoryDefOf.PawnMisc || stat.category == StatCategoryDefOf.PawnSocial || stat.category == StatCategoryDefOf.PawnWork)
            {
                if (thingDef == null || thingDef.category != ThingCategory.Pawn)
                {
                    return false;
                }
                if (req.Thing is Pawn pawn3)
                {
                    if (pawn3.IsColonyMech && stat.showOnPlayerMechanoids)
                    {
                        return true;
                    }
                    if (stat.showOnPawnKind.NotNullAndContains(pawn3.kindDef))
                    {
                        return true;
                    }
                }
                return thingDef.race.Humanlike;
            }
            if (stat.category == StatCategoryDefOf.Building)
            {
                if (thingDef == null)
                {
                    return false;
                }
                if (stat == StatDefOf.DoorOpenSpeed)
                {
                    return thingDef.IsDoor;
                }
                if (!stat.showOnNonWorkTables && !thingDef.IsWorkTable)
                {
                    return false;
                }
                if (!stat.showOnNonPowerPlants && !thingDef.HasAssignableCompFrom(typeof(CompPowerPlant)))
                {
                    return false;
                }
                return thingDef.category == ThingCategory.Building;
            }
            if (stat.category == StatCategoryDefOf.Apparel)
            {
                if (thingDef != null)
                {
                    return thingDef.IsApparel || thingDef.category == ThingCategory.Pawn;
                }
                return false;
            }
            if (stat.category == StatCategoryDefOf.Weapon)
            {
                if (thingDef != null)
                {
                    return thingDef.IsMeleeWeapon || thingDef.IsRangedWeapon;
                }
                return false;
            }
            if (stat.category == StatCategoryDefOf.Weapon_Ranged)
            {
                return thingDef?.IsRangedWeapon ?? false;
            }
            if (stat.category == StatCategoryDefOf.Weapon_Melee)
            {
                return thingDef?.IsMeleeWeapon ?? false;
            }
            if (stat.category == StatCategoryDefOf.BasicsNonPawn || stat.category == StatCategoryDefOf.BasicsNonPawnImportant)
            {
                if (thingDef == null || thingDef.category != ThingCategory.Pawn)
                {
                    return !req.ForAbility;
                }
                return false;
            }
            if (stat.category == StatCategoryDefOf.Terrain)
            {
                return def is TerrainDef;
            }
            if (ModsConfig.AnomalyActive && stat.category == StatCategoryDefOf.PsychicRituals)
            {
                return false;
            }
            if (req.ForAbility)
            {
                return stat.category == StatCategoryDefOf.Ability;
            }
            if (stat.category.displayAllByDefault)
            {
                return true;
            }
            Log.Error($"Unhandled case: {stat?.ToString()}, {def}");
            return false;
        }
    }

    [HarmonyPatch(typeof(PlantProperties), "SpecialDisplayStats")]
    public static class PlantProperties_SpecialDisplayStats_Patch
    {
        public static void Postfix(ref IEnumerable<StatDrawEntry> __result)
        {
            if (Current.Game?.World?.factionManager != null)
            {
                return;
            }

            __result = __result.SafelyEnumerateStats();
        }

        public static IEnumerable<StatDrawEntry> SafelyEnumerateStats(this IEnumerable<StatDrawEntry> originalStats)
        {
            using (var enumerator = originalStats.GetEnumerator())
            {
                while (true)
                {
                    try
                    {
                        if (!enumerator.MoveNext())
                        {
                            break;
                        }
                    }
                    catch (System.NullReferenceException e)
                    {
                        Log.Warning($"[YourModName] Safely skipped a stat entry that threw an NRE: {e.Message}");
                        continue;
                    }

                    yield return enumerator.Current;
                }
            }
        }
    }
    [HarmonyPatch(typeof(RaceProperties), nameof(RaceProperties.SpecialDisplayStats))]
    public static class RaceProperties_SpecialDisplayStats_Patch
    {
        public static void Postfix(ref IEnumerable<StatDrawEntry> __result)
        {
            if (Current.Game?.World?.factionManager != null)
            {
                return;
            }
            __result = __result.SafelyEnumerateStats();
        }
    }

        [HarmonyPatch(typeof(StatWorker), nameof(StatWorker.GetExplanationFinalizePart))]
    public static class StatWorker_GetExplanationFinalizePart_Patch
    {
        public static bool Prefix(ref string __result, StatWorker __instance, StatRequest req, ToStringNumberSense numberSense, float finalVal)
        {
            if (Current.Game?.World?.factionManager is null)
            {
                __result = GetExplanationFinalizePart(__instance, req, numberSense, finalVal);
                return false;
            }
            return true;
        }

        public static string GetExplanationFinalizePart(StatWorker __instance, StatRequest req, ToStringNumberSense numberSense, float finalVal)
        {
            var stringBuilder = new StringBuilder();
            if (__instance.stat.parts != null)
            {
                for (int i = 0; i < __instance.stat.parts.Count; i++)
                {
                    string text = __instance.stat.parts[i].ExplanationPart(req);
                    if (!text.NullOrEmpty())
                    {
                        stringBuilder.AppendLine(text);
                    }
                }
            }
            if (__instance.stat.postProcessCurve != null)
            {
                float value = __instance.GetValue(req, applyPostProcess: false);
                float num = __instance.stat.postProcessCurve.Evaluate(value);
                if (!Mathf.Approximately(value, num))
                {
                    string text2 = __instance.ValueToString(value, finalized: false);
                    string text3 = __instance.stat.ValueToString(num, numberSense);
                    stringBuilder.AppendLine("StatsReport_PostProcessed".Translate() + ": " + text2 + " => " + text3);
                }
            }
            if (__instance.stat.postProcessStatFactors != null)
            {
                stringBuilder.AppendLine("StatsReport_OtherStats".Translate());
                for (int j = 0; j < __instance.stat.postProcessStatFactors.Count; j++)
                {
                    var statDef = __instance.stat.postProcessStatFactors[j];
                    stringBuilder.AppendLine($"    {statDef.LabelCap}: x{statDef.Worker.GetValue(req).ToStringPercent()}");
                }
            }
            stringBuilder.Append("StatsReport_FinalValue".Translate() + ": " + __instance.stat.ValueToString(finalVal, __instance.stat.toStringNumberSense));
            return stringBuilder.ToString();
        }
    }

    [HarmonyPatch(typeof(BuildingProperties), nameof(BuildingProperties.SpecialDisplayStats))]
    public static class BuildingProperties_SpecialDisplayStats_Patch
    {
        public static void Postfix(ref IEnumerable<StatDrawEntry> __result)
        {
            if (Current.Game?.World?.factionManager != null)
            {
                return;
            }
            __result = __result.SafelyEnumerateStats();
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
            StatsReportUtility.scrollPosition = default;
            StatsReportUtility.scrollPositionRightPanel = default;
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

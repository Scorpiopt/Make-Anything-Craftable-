using RimWorld;
using System.Collections.Generic;
using Verse;

namespace MakeAnythingCraftable
{
    public class RecipeDefExposable : RecipeDef, IExposable
    {
        public List<string> recipeUsersString = new List<string>();
        public List<string> researchPrerequisitesString = new List<string>();
        public List<SkillRequirementString> skillRequirementsString = new List<SkillRequirementString>();
        public string workSpeedStatString;
        public string efficiencyStatString;

        public string effectWorkingString;
        public string soundWorkingString;

        public string workSkillString;
        public string unfinishedThingDefString;

        public ThingDefCountClassString productString;
        public List<ThingDefCountClassString> ingredientsString = new List<ThingDefCountClassString>();
        public override void ResolveReferences()
        {
            recipeUsers = new List<ThingDef>();
            foreach (var defName in recipeUsersString)
            {
                var def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
                if (def != null)
                {
                    recipeUsers.Add(def);
                }
            }
            if (!researchPrerequisitesString.NullOrEmpty())
            {
                researchPrerequisites = new List<ResearchProjectDef>();
                foreach (var defName in researchPrerequisitesString)
                {
                    var def = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(defName);
                    if (def != null)
                    {
                        researchPrerequisites.Add(def);
                    }
                }
            }

            if (!effectWorkingString.NullOrEmpty())
            {
                effectWorking = DefDatabase<EffecterDef>.GetNamedSilentFail(effectWorkingString);
            }

            if (!soundWorkingString.NullOrEmpty())
            {
                soundWorking = DefDatabase<SoundDef>.GetNamedSilentFail(soundWorkingString);
            }

            if (!workSpeedStatString.NullOrEmpty())
            {
                workSpeedStat = DefDatabase<StatDef>.GetNamedSilentFail(workSpeedStatString);
            }

            if (!efficiencyStatString.NullOrEmpty())
            {
                efficiencyStat = DefDatabase<StatDef>.GetNamedSilentFail(efficiencyStatString);
            }

            if (!workSkillString.NullOrEmpty())
            {
                workSkill = DefDatabase<SkillDef>.GetNamedSilentFail(workSkillString);
            }
            
            if (!unfinishedThingDefString.NullOrEmpty())
            {
                unfinishedThingDef = DefDatabase<ThingDef>.GetNamedSilentFail(unfinishedThingDefString);
            }
            if (!skillRequirementsString.NullOrEmpty())
            {
                skillRequirements = new List<SkillRequirement>();
                foreach (var skillRequirement in skillRequirementsString)
                {
                    skillRequirements.Add(new SkillRequirement
                    {
                        minLevel = skillRequirement.minLevel,
                        skill = DefDatabase<SkillDef>.GetNamed(skillRequirement.skill)
                    });
                }
            }

            products = new List<ThingDefCountClass>();
            var product = new ThingDefCountClass(DefDatabase<ThingDef>.GetNamed(productString.defName), productString.count);
            products.Add(product);
            this.adjustedCount = product.count;
            if (ingredientsString != null)
            {
                foreach (var ingredient in ingredientsString)
                {
                    var def = DefDatabase<ThingDef>.GetNamedSilentFail(ingredient.defName);
                    if (def != null)
                    {
                        IngredientCount ingredientCount = new IngredientCount();
                        ingredientCount.SetBaseCount(def.smallVolume ? ingredient.count / 10 : ingredient.count);
                        ingredientCount.filter.SetAllow(def, allow: true);
                        this.ingredients.Add(ingredientCount);
                        this.fixedIngredientFilter.SetAllow(def, allow: true);
                    }
                }
            }
            base.ResolveReferences();
        }
        public void ExposeData()
        {
            Scribe_Values.Look(ref defName, "defName");
            Scribe_Values.Look(ref label, "label");
            Scribe_Values.Look(ref description, "description");
            Scribe_Values.Look(ref workAmount, "workAmount");
            Scribe_Values.Look(ref effectWorkingString, "effectWorkingString");
            Scribe_Values.Look(ref soundWorkingString, "soundWorkingString");
            Scribe_Values.Look(ref workSpeedStatString, "workSpeedStatString");
            Scribe_Values.Look(ref efficiencyStatString, "efficiencyStatString");
            Scribe_Values.Look(ref workSkillString, "workSkillString");
            Scribe_Values.Look(ref unfinishedThingDefString, "unfinishedThingDefString");
            Scribe_Collections.Look(ref recipeUsersString, "recipeUsersString", LookMode.Value);
            Scribe_Collections.Look(ref researchPrerequisitesString, "researchPrerequisitesString", LookMode.Value);
            Scribe_Collections.Look(ref skillRequirementsString, "skillRequirementsString", LookMode.Deep);
            Scribe_Collections.Look(ref ingredientsString, "ingredientsString", LookMode.Deep);
            Scribe_Deep.Look(ref productString, "productString");
            Scribe_Values.Look(ref jobString, "jobString");
        }
    }
}

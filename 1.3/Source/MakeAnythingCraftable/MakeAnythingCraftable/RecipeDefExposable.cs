using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Verse;

namespace MakeAnythingCraftable
{

    [HotSwappable]
    public class RecipeDefExposable : RecipeDef, IExposable
    {
        public DefCount<ThingDef> productString;
        public string workSpeedStatString;
        public string efficiencyStatString;
        public string effectWorkingString;
        public string soundWorkingString;
        public string workSkillString;
        public string unfinishedThingDefString;
        public List<string> recipeUsersString = new List<string>();
        public List<string> researchPrerequisitesString = new List<string>();
        public List<SkillRequirementString> skillRequirementsString = new List<SkillRequirementString>();
        public List<IngredientCountExposable> ingredientsCount = new List<IngredientCountExposable>();
        public List<string> disallowedIngredients = new List<string>();

        public bool copied;
        public RecipeDefExposable()
        {

        }
        public RecipeDefExposable(RecipeDef source)
        {
            recipeUsersString = source.AllRecipeUsers.Select(x => x.defName).ToList();
            if (!source.researchPrerequisites.NullOrEmpty())
            {
                researchPrerequisitesString = source.researchPrerequisites.Select(x => x.defName).ToList();
            }
            if (source.researchPrerequisite != null)
            {
                researchPrerequisitesString.Add(source.researchPrerequisite.defName);
            }
            if (!source.skillRequirements.NullOrEmpty())
            {
                foreach (var sk in source.skillRequirements)
                {
                    skillRequirementsString.Add(new SkillRequirementString
                    {
                        skill = sk.skill.defName,
                        minLevel = sk.minLevel,
                    });
                }
            }
            workSpeedStatString = source.workSpeedStat?.defName;
            efficiencyStatString = source.efficiencyStat?.defName;
            effectWorkingString = source.effectWorking?.defName;
            soundWorkingString = source.soundWorking?.defName;
            workSkillString = source.workSkill?.defName;
            unfinishedThingDefString = source.unfinishedThingDef?.defName;
            productString = new DefCount<ThingDef>(source.products[0].thingDef.defName, source.products[0].count);
            if (!source.ingredients.NullOrEmpty())
            {
                foreach (var ingredient in source.ingredients)
                {
                    var ingredientCategory = new IngredientCountExposable { count = (int)ingredient.count };
                    ingredientsCount.Add(ingredientCategory);
                    if (ingredient.filter.thingDefs != null)
                    {
                        foreach (var def in ingredient.filter.thingDefs)
                        {
                            ingredientCategory.thingDefs.Add(def.defName);
                        }
                    }
                    if (ingredient.filter.categories != null)
                    {
                        foreach (var def in ingredient.filter.categories)
                        {
                            ingredientCategory.categories.Add(def);
                        }
                    }
                }
            }
            this.workAmount = source.WorkAmountTotal(null);
            copied = true;
        }
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
            this.fixedIngredientFilter = new ThingFilter();
            if (ingredientsCount != null)
            {
                foreach (var ingredientCategory in ingredientsCount)
                {
                    IngredientCount ingredientCount = new IngredientCount();
                    ingredientCount.SetBaseCount(ingredientCategory.count);
                    if (ingredientCategory.categories != null)
                    {
                        foreach (var item in ingredientCategory.categories)
                        {
                            var categoryDef = DefDatabase<ThingCategoryDef>.GetNamedSilentFail(item);
                            if (categoryDef != null)
                            {
                                fixedIngredientFilter.SetAllow(categoryDef, true);
                                ingredientCount.filter.SetAllow(categoryDef, allow: true);
                                foreach (var def in categoryDef.DescendantThingDefs)
                                {
                                    if (disallowedIngredients?.Contains(def.defName) ?? false)
                                    {
                                        ingredientCount.filter.SetAllow(def, false);
                                        fixedIngredientFilter.SetAllow(def, false);
                                    }
                                }
                            }
                        }
                    }
                    if (ingredientCategory.thingDefs != null)
                    {
                        foreach (var defName in ingredientCategory.thingDefs)
                        {
                            var def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
                            if (def != null)
                            {
                                ingredientCount.SetBaseCount(def.smallVolume ? ingredientCategory.count / 10 : ingredientCategory.count);
                                ingredientCount.filter.SetAllow(def, allow: true);
                                this.fixedIngredientFilter.SetAllow(def, allow: true);
                            }
                        }
                    }

                    this.ingredients.Add(ingredientCount);
                }
            }

            this.fixedIngredientFilter.ResolveReferences();
            Log.Message("created fixedIngredientFilter: " + fixedIngredientFilter.Summary);
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
            Scribe_Collections.Look(ref ingredientsCount, "thingCategoryIngredients", LookMode.Deep);
            Scribe_Collections.Look(ref disallowedIngredients, "disallowedIngredients", LookMode.Value);
            Scribe_Deep.Look(ref productString, "productString");
            Scribe_Values.Look(ref jobString, "jobString");
            Scribe_Values.Look(ref copied, "copied");
        }
    }
}

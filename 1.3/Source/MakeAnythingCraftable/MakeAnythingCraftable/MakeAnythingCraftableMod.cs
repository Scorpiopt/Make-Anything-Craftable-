using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using UnityEngine.Diagnostics;
using Verse;
using Verse.Noise;

namespace MakeAnythingCraftable
{
    public class MakeAnythingCraftableMod : Mod
    {
        public static MakeAnythingCraftableSettings settings;
        public MakeAnythingCraftableMod(ModContentPack pack) : base(pack)
        {
            settings = GetSettings<MakeAnythingCraftableSettings>();
        }
        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            settings.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return this.Content.Name;
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            settings.ResetRecipe();
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class HotSwappableAttribute : Attribute
    {
    }

    [HotSwappable]
    public class MakeAnythingCraftableSettings : ModSettings
    {
        public List<RecipeDefExposable> newRecipeDefs = new List<RecipeDefExposable>();
		public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref newRecipeDefs, "newRecipeDefs", LookMode.Deep);
        }

        public void ApplySettings()
        {
            foreach (var recipe in newRecipeDefs)
            {
                if (DefDatabase<RecipeDef>.GetNamedSilentFail(recipe.defName) is null)
                {
                    recipe.ResolveReferences();
                    DefDatabase<RecipeDef>.Add(recipe);
                }
                recipe.modContentPack = this.Mod.Content;
            }
        }
        public void DoSettingsWindowContents(Rect inRect)
        {
            Rect rect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height);
            Widgets.BeginGroup(rect);
            DrawPage(rect);
            Widgets.EndGroup();
        }

        public RecipeDefExposable curRecipe;
        private int scrollHeightCount = 0;
        private Vector2 firstColumnPos;
        private Vector2 secondColumnPos;
        private Vector2 scrollPosition;
        private string recipeLabel;
        string buf1, buf2, buf3, buf4;
        public string GetRecipeLabel()
        {
            if (!recipeLabel.NullOrEmpty())
            {
                return recipeLabel;
            }
            return GetRecipeLabelBase();
        }

        private string GetRecipeLabelBase()
        {
            if (curRecipe.label != null)
            {
                return curRecipe.label;
            }
            if (curRecipe.productString != null)
            {
                var def = curRecipe.productString.Def;
                string text = def.label;
                if (curRecipe.productString.count != 1)
                {
                    text = text + " x" + curRecipe.productString.count;
                }
                return "RecipeMake".Translate(text);
            }
            return "";
        }

        public void DrawPage(Rect rect)
        {
            if (curRecipe is null)
            {
                ResetPositions();
                ResetRecipe();
            }

            var outRect = new Rect(0, 0, rect.width, rect.height);
            var viewRect = new Rect(0, 0, rect.width - 30, scrollHeightCount);
            scrollHeightCount = 0;
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

            DoButton(ref firstColumnPos, "MAC.SelectRecipe".Translate(), delegate
            {
                Find.WindowStack.Add(new Window_SelectItem<RecipeDef>(Utils.craftingRecipes, delegate (RecipeDef selected)
                {
                    curRecipe = new RecipeDefExposable(selected);
                }));
            });

            firstColumnPos.y += 12;

            if (curRecipe.productString != null && curRecipe.recipeUsersString.Any() && curRecipe.workAmount > 0)
            {
                if (Widgets.ButtonText(new Rect(firstColumnPos.x, firstColumnPos.y, 250, 32), "MAC.SaveRecipe".Translate()))
                {
                    curRecipe.defName = "MAC_Custom_Make_" + curRecipe.productString.Def.defName;
                    if (curRecipe.label.NullOrEmpty())
                    {
                        curRecipe.label = GetRecipeLabel();
                    }
                    curRecipe.description = curRecipe.label;
                    string text = curRecipe.productString.Def.label;
                    if (curRecipe.productString.count != 1)
                    {
                        text = text + " x" + curRecipe.productString.count;
                    }
                    curRecipe.jobString = "RecipeMakeJobString".Translate(text);
                    curRecipe.modContentPack = this.Mod.Content;
                    newRecipeDefs.Add(curRecipe);
                    ResetPositions();
                    ResetRecipe();
                    ApplySettings();
                    Widgets.EndScrollView();
                    return;
                }
                firstColumnPos.y += 32;
                firstColumnPos.y += 12;
            }
            var labelRect = new Rect(firstColumnPos.x, firstColumnPos.y, 100, 24);
            Widgets.Label(labelRect, "MAC.RecipeLabel".Translate());
            var inputRect = new Rect(labelRect.xMax, firstColumnPos.y, 250, 24);
            Rect removeRect;
            recipeLabel = Widgets.TextField(inputRect, GetRecipeLabel());
            if (recipeLabel == GetRecipeLabelBase())
            {
                recipeLabel = "";
            }
            firstColumnPos.y += 24;
            firstColumnPos.y += 12;

            labelRect = DoLabel(ref firstColumnPos, "MAC.SelectProduct".Translate());
            Rect buttonRect = DoButton(ref firstColumnPos, curRecipe.productString != null ? curRecipe.productString.Def.LabelCap.ToString() : "-", delegate
            {
                Find.WindowStack.Add(new Window_SelectItem<ThingDef>(Utils.craftableItems, delegate (ThingDef selected)
                {
                    curRecipe.productString = new DefCount<ThingDef>
                    {
                        defName = selected.defName,
                        count = 1
                    };
                }, x => x.FirstThingCategory?.index ?? 0));
            });

            if (curRecipe.productString != null)
            {
                DoInput(buttonRect.xMax + 15, buttonRect.y, "MAC.Count".Translate(), ref curRecipe.productString.count, ref buf1);
            }

            firstColumnPos.y += 12;

            DoLabel(ref firstColumnPos, "MAC.SetUnfinishedThing".Translate());
            DoButton(ref firstColumnPos, curRecipe.unfinishedThingDefString.NullOrEmpty() ? "-" :
                DefDatabase<ThingDef>.GetNamed(curRecipe.unfinishedThingDefString).LabelCap.ToString(), delegate
                {
                    var floatList = new List<FloatMenuOption>();
                    foreach (var def in Utils.unfinishedThings)
                    {
                        floatList.Add(new FloatMenuOption(def.LabelCap, delegate
                        {
                            curRecipe.unfinishedThingDefString = def.defName;
                        }));
                    }
                    Find.WindowStack.Add(new FloatMenu(floatList));
                });

            firstColumnPos.y += 12;


            labelRect = DoLabel(ref firstColumnPos, "MAC.SelectWorkbenches".Translate());
            string toRemove = null;
            for (var i = 0; i < curRecipe.recipeUsersString.ListFullCopy().Count; i++)
            {
                var recipeUser = curRecipe.recipeUsersString[i];
                Rect recipeUserRect = new Rect(firstColumnPos.x, firstColumnPos.y, buttonRect.width - 30, 24);
                var def = DefDatabase<ThingDef>.GetNamed(recipeUser);
                if (Widgets.ButtonText(recipeUserRect, def.LabelCap))
                {
                    Find.WindowStack.Add(new Window_SelectItem<ThingDef>(Utils.workbenches.Where(x => !curRecipe.recipeUsersString.Contains(x.defName)).ToList(),
                    delegate (ThingDef selected)
                    {
                        var index = curRecipe.recipeUsersString.IndexOf(recipeUser);
                        curRecipe.recipeUsersString.RemoveAt(index);
                        curRecipe.recipeUsersString.Insert(index, selected.defName);
                    }, x => x.FirstThingCategory?.index ?? 0));
                }

                removeRect = new Rect(recipeUserRect.xMax + 5, firstColumnPos.y, 20, 21f);
                if (Widgets.ButtonImage(removeRect, TexButton.DeleteX))
                {
                    toRemove = recipeUser;
                }
                firstColumnPos.y += 24;
            }
            if (!toRemove.NullOrEmpty())
            {
                curRecipe.recipeUsersString.Remove(toRemove);
            }
            buttonRect = DoButton(ref firstColumnPos, "Add".Translate().CapitalizeFirst(), delegate
            {
                Find.WindowStack.Add(new Window_SelectItem<ThingDef>(Utils.workbenches.Where(x => !curRecipe.recipeUsersString.Contains(x.defName)).ToList(),
                delegate (ThingDef selected)
                {
                    curRecipe.recipeUsersString.Add(selected.defName);
                }, x => x.FirstThingCategory?.index ?? 0));
            });

            firstColumnPos.y += 12;
            var value = (int)(curRecipe.workAmount / 60);
            DoInput(firstColumnPos.x, firstColumnPos.y, "MAC.SetWorkAmount".Translate(), ref value, ref buf2, 170);
            curRecipe.workAmount = value * 60;
            firstColumnPos.y += 24 + 12;

            DoLabel(ref firstColumnPos, "MAC.SetWorkSpeedStat".Translate());
            DoButton(ref firstColumnPos, curRecipe.workSpeedStatString.NullOrEmpty() ? "-" :
                DefDatabase<StatDef>.GetNamed(curRecipe.workSpeedStatString).LabelCap.ToString(), delegate
                {
                    var floatList = new List<FloatMenuOption>();
                    foreach (var def in Utils.workSpeedStats)
                    {
                        floatList.Add(new FloatMenuOption(def.LabelCap, delegate
                        {
                            curRecipe.workSpeedStatString = def.defName;
                        }));
                    }
                    Find.WindowStack.Add(new FloatMenu(floatList));
                });

            firstColumnPos.y += 12;

            DoLabel(ref firstColumnPos, "MAC.SetEfficiencySpeedStat".Translate());
            DoButton(ref firstColumnPos, curRecipe.efficiencyStatString.NullOrEmpty() ? "-" :
                DefDatabase<StatDef>.GetNamed(curRecipe.efficiencyStatString).LabelCap.ToString(), delegate
                {
                    var floatList = new List<FloatMenuOption>();
                    foreach (var def in Utils.efficiencyStats)
                    {
                        floatList.Add(new FloatMenuOption(def.LabelCap, delegate
                        {
                            curRecipe.efficiencyStatString = def.defName;
                        }));
                    }
                    Find.WindowStack.Add(new FloatMenu(floatList));
                });
            firstColumnPos.y += 12;

            DoLabel(ref firstColumnPos, "MAC.SetWorkEffect".Translate());
            DoButton(ref firstColumnPos, curRecipe.effectWorkingString.NullOrEmpty() ? "-" :
                DefDatabase<EffecterDef>.GetNamed(curRecipe.effectWorkingString).defName, delegate
                {
                    var floatList = new List<FloatMenuOption>();
                    foreach (var def in Utils.effecterDefs)
                    {
                        floatList.Add(new FloatMenuOption(def.defName, delegate
                        {
                            curRecipe.effectWorkingString = def.defName;
                        }));
                    }
                    Find.WindowStack.Add(new FloatMenu(floatList));
                });

            firstColumnPos.y += 12;

            DoLabel(ref firstColumnPos, "MAC.SetSoundWorking".Translate());
            DoButton(ref firstColumnPos, curRecipe.soundWorkingString.NullOrEmpty() ? "-" :
                DefDatabase<SoundDef>.GetNamed(curRecipe.soundWorkingString).defName, delegate
                {
                    var floatList = new List<FloatMenuOption>();
                    foreach (var def in Utils.soundDefs)
                    {
                        floatList.Add(new FloatMenuOption(def.defName, delegate
                        {
                            curRecipe.soundWorkingString = def.defName;
                        }));
                    }
                    Find.WindowStack.Add(new FloatMenu(floatList));
                });

            labelRect = DoLabel(ref secondColumnPos, "MAC.SetSkillRequirements".Translate());
            toRemove = "";
            for (var i = 0; i < curRecipe.skillRequirementsString.ListFullCopy().Count; i++)
            {
                var skillRequirement = curRecipe.skillRequirementsString[i];
                Rect skillRect = new Rect(secondColumnPos.x, secondColumnPos.y, buttonRect.width - 30, 24);
                var def = DefDatabase<SkillDef>.GetNamed(skillRequirement.skill);
                if (Widgets.ButtonText(skillRect, def.LabelCap))
                {
                    var floatList = new List<FloatMenuOption>();
                    foreach (var skill in DefDatabase<SkillDef>.AllDefs.Where(x => !curRecipe.skillRequirementsString.Any(y => x.defName == y.skill)))
                    {
                        floatList.Add(new FloatMenuOption(skill.skillLabel.CapitalizeFirst(), delegate
                        {
                            var index = curRecipe.skillRequirementsString.IndexOf(skillRequirement);
                            curRecipe.skillRequirementsString.RemoveAt(index);
                            curRecipe.skillRequirementsString.Insert(index, new SkillRequirementString
                            {
                                skill = skill.defName,
                                minLevel = 1
                            });
                        }));
                    }
                    Find.WindowStack.Add(new FloatMenu(floatList));
                }

                removeRect = new Rect(skillRect.xMax + 5, secondColumnPos.y, 20, 21f);
                if (Widgets.ButtonImage(removeRect, TexButton.DeleteX))
                {
                    toRemove = skillRequirement.skill;
                }

                var sliderRect = new Rect(removeRect.xMax + 15, secondColumnPos.y + 8, 150, 24);
                skillRequirement.minLevel = (int)Widgets.HorizontalSlider(sliderRect, skillRequirement.minLevel, 1, 20,
                    true, null, "MAC.SkillLevel".Translate(skillRequirement.minLevel));
                secondColumnPos.y += 24;
            }
            if (!toRemove.NullOrEmpty())
            {
                curRecipe.skillRequirementsString.RemoveAll(x => x.skill == toRemove);
            }

            buttonRect = DoButton(ref secondColumnPos, "Add".Translate().CapitalizeFirst(), delegate
            {
                var floatList = new List<FloatMenuOption>();
                foreach (var skill in DefDatabase<SkillDef>.AllDefs.Where(x => !curRecipe.skillRequirementsString.Any(y => x.defName == y.skill)))
                {
                    floatList.Add(new FloatMenuOption(skill.skillLabel.CapitalizeFirst(), delegate
                    {
                        curRecipe.skillRequirementsString.Add(new SkillRequirementString
                        {
                            skill = skill.defName,
                            minLevel = 1
                        });
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(floatList));
            });
            secondColumnPos.y += 12;

            DoLabel(ref secondColumnPos, "MAC.SetWorkSkill".Translate());
            DoButton(ref secondColumnPos, curRecipe.workSkillString.NullOrEmpty() ? "-" :
                DefDatabase<SkillDef>.GetNamed(curRecipe.workSkillString).LabelCap.ToString(), delegate
                {
                    var floatList = new List<FloatMenuOption>();
                    foreach (var def in DefDatabase<SkillDef>.AllDefs)
                    {
                        floatList.Add(new FloatMenuOption(def.LabelCap, delegate
                        {
                            curRecipe.workSkillString = def.defName;
                        }));
                    }
                    Find.WindowStack.Add(new FloatMenu(floatList));
                });

            secondColumnPos.y += 12;

            labelRect = DoLabel(ref secondColumnPos, "MAC.SetResearchRequirements".Translate());
            toRemove = "";
            for (var i = 0; i < curRecipe.researchPrerequisitesString.ListFullCopy().Count; i++)
            {
                var researchRequirement = curRecipe.researchPrerequisitesString[i];
                Rect skillRect = new Rect(secondColumnPos.x, secondColumnPos.y, buttonRect.width - 30, 24);
                var def = DefDatabase<ResearchProjectDef>.GetNamed(researchRequirement);
                if (Widgets.ButtonText(skillRect, def.LabelCap))
                {
                    var floatList = new List<FloatMenuOption>();
                    foreach (var researchProject in DefDatabase<ResearchProjectDef>.AllDefs.Where(x => !curRecipe.researchPrerequisitesString.Any(y => x.defName == y)))
                    {
                        floatList.Add(new FloatMenuOption(researchProject.LabelCap, delegate
                        {
                            var index = curRecipe.researchPrerequisitesString.IndexOf(researchRequirement);
                            curRecipe.researchPrerequisitesString.RemoveAt(index);
                            curRecipe.researchPrerequisitesString.Insert(index, researchProject.defName);
                        }));
                    }
                    Find.WindowStack.Add(new FloatMenu(floatList));
                }

                removeRect = new Rect(skillRect.xMax + 5, secondColumnPos.y, 20, 21f);
                if (Widgets.ButtonImage(removeRect, TexButton.DeleteX))
                {
                    toRemove = researchRequirement;
                }
                secondColumnPos.y += 24;
            }

            if (!toRemove.NullOrEmpty())
            {
                curRecipe.researchPrerequisitesString.RemoveAll(x => x == toRemove);
            }

            buttonRect = DoButton(ref secondColumnPos, "Add".Translate().CapitalizeFirst(), delegate
            {
                var floatList = new List<FloatMenuOption>();
                foreach (var researchProject in DefDatabase<ResearchProjectDef>.AllDefs.Where(x => !curRecipe.researchPrerequisitesString.Any(y => x.defName == y)))
                {
                    floatList.Add(new FloatMenuOption(researchProject.LabelCap, delegate
                    {
                        curRecipe.researchPrerequisitesString.Add(researchProject.defName);
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(floatList));
            });
            secondColumnPos.y += 12;

            labelRect = DoLabel(ref secondColumnPos, "MAC.SetIngredientRequirements".Translate());
            int removeID = -1;

            for (var i = 0; i < curRecipe.ingredientsCount.Count; i++)
            {
                var thingCategoryIngredient = curRecipe.ingredientsCount[i];
                bool countDone = false;
                toRemove = "";
                for (var j = 0; j < thingCategoryIngredient.categories.ListFullCopy().Count; j++)
                {
                    var ingredientRequirement = curRecipe.ingredientsCount[i].categories[j];
                    Rect ingredientRect = new Rect(secondColumnPos.x, secondColumnPos.y, buttonRect.width - 30, 24);
                    var def = DefDatabase<ThingCategoryDef>.GetNamed(ingredientRequirement);
                    if (Widgets.ButtonText(ingredientRect, "MAC.CategoryMark".Translate() + def.LabelCap))
                    {
                        Find.WindowStack.Add(new Window_SelectItem<ThingCategoryDef>(DefDatabase<ThingCategoryDef>.AllDefsListForReading,
                            delegate (ThingCategoryDef selected)
                            {
                                var index = thingCategoryIngredient.categories.IndexOf(ingredientRequirement);
                                thingCategoryIngredient.categories.RemoveAt(index);
                                thingCategoryIngredient.categories.Insert(index, selected.defName);
                            }, x => x.index));
                    }

                    removeRect = new Rect(ingredientRect.xMax + 5, secondColumnPos.y, 20, 21f);
                    if (Widgets.ButtonImage(removeRect, TexButton.DeleteX))
                    {
                        toRemove = ingredientRequirement;
                    }
                    if (!countDone)
                    {
                        DoInput(removeRect.xMax + 15, removeRect.y, "MAC.Count".Translate(), ref thingCategoryIngredient.count, ref buf3);
                        countDone = true;
                    }
                    secondColumnPos.y += 24;
                }
                if (!toRemove.NullOrEmpty())
                {
                    curRecipe.ingredientsCount[i].categories.RemoveAll(x => x == toRemove);
                }

                toRemove = "";
                for (var j = 0; j < thingCategoryIngredient.thingDefs.ListFullCopy().Count; j++)
                {
                    var ingredientRequirement = curRecipe.ingredientsCount[i].thingDefs[j];
                    Rect ingredientRect = new Rect(secondColumnPos.x, secondColumnPos.y, buttonRect.width - 30, 24);
                    var def = DefDatabase<ThingDef>.GetNamed(ingredientRequirement);
                    if (Widgets.ButtonText(ingredientRect, "MAC.ThingDefMark".Translate() + def.LabelCap))
                    {
                        Find.WindowStack.Add(new Window_SelectItem<ThingDef>(Utils.craftableItems, delegate (ThingDef selected)
                        {
                            var index = thingCategoryIngredient.thingDefs.IndexOf(ingredientRequirement);
                            thingCategoryIngredient.thingDefs.RemoveAt(index);
                            thingCategoryIngredient.thingDefs.Insert(index, selected.defName);
                        }, x => x.FirstThingCategory?.index ?? 0));
                    }

                    removeRect = new Rect(ingredientRect.xMax + 5, secondColumnPos.y, 20, 21f);
                    if (Widgets.ButtonImage(removeRect, TexButton.DeleteX))
                    {
                        toRemove = ingredientRequirement;
                    }
                    if (!countDone)
                    {
                        DoInput(removeRect.xMax + 15, removeRect.y, "MAC.Count".Translate(), ref thingCategoryIngredient.count, ref buf4);
                        countDone = true;
                    }
                    secondColumnPos.y += 24;
                }
                if (!toRemove.NullOrEmpty())
                {
                    curRecipe.ingredientsCount[i].thingDefs.RemoveAll(x => x == toRemove);
                }

                buttonRect = DoButton(ref secondColumnPos, "MAC.AddThingDef".Translate().CapitalizeFirst(), delegate
                {
                    Find.WindowStack.Add(new Window_SelectItem<ThingDef>(Utils.craftableItems, delegate (ThingDef selected)
                    {
                        thingCategoryIngredient.thingDefs.Add(selected.defName);
                    }, x => x.FirstThingCategory?.index ?? 0));
                });

                buttonRect = DoButton(ref secondColumnPos, "MAC.AddCategory".Translate().CapitalizeFirst(), delegate
                {
                    Find.WindowStack.Add(new Window_SelectItem<ThingCategoryDef>(DefDatabase<ThingCategoryDef>.AllDefsListForReading,
                        delegate (ThingCategoryDef selected)
                        {
                            thingCategoryIngredient.categories.Add(selected.defName);
                        }, x => x.index));
                });

                buttonRect = DoButton(ref secondColumnPos, "MAC.RemoveIngredient".Translate().CapitalizeFirst(), delegate
                {
                    removeID = i;
                });

                secondColumnPos.y += 12;
                Color color = GUI.color;
                GUI.color = color * new Color(1f, 1f, 1f, 0.4f);
                Widgets.DrawLineHorizontal(secondColumnPos.x, secondColumnPos.y, 250);
                GUI.color = color;
                secondColumnPos.y += 12;
            }

            if (removeID != -1)
            {
                curRecipe.ingredientsCount.RemoveAt(removeID);
            }

            buttonRect = DoButton(ref secondColumnPos, "Add".Translate().CapitalizeFirst(), delegate
            {
                curRecipe.ingredientsCount.Add(new IngredientCountExposable { count = 1});
            });
            secondColumnPos.y += 12;

            labelRect = DoLabel(ref secondColumnPos, "MAC.DisallowThingDefs".Translate());
            toRemove = null;
            for (var i = 0; i < curRecipe.disallowedIngredients.ListFullCopy().Count; i++)
            {
                var disallowedThingDef = curRecipe.disallowedIngredients[i];
                Rect recipeUserRect = new Rect(secondColumnPos.x, secondColumnPos.y, buttonRect.width - 30, 24);
                var def = DefDatabase<ThingDef>.GetNamed(disallowedThingDef);
                if (Widgets.ButtonText(recipeUserRect, def.LabelCap))
                {
                    Find.WindowStack.Add(new Window_SelectItem<ThingDef>(Utils.craftableItems,
                    delegate (ThingDef selected)
                    {
                        var index = curRecipe.disallowedIngredients.IndexOf(disallowedThingDef);
                        curRecipe.disallowedIngredients.RemoveAt(index);
                        curRecipe.disallowedIngredients.Insert(index, selected.defName);
                    }, x => x.FirstThingCategory?.index ?? 0));
                }

                removeRect = new Rect(recipeUserRect.xMax + 5, secondColumnPos.y, 20, 21f);
                if (Widgets.ButtonImage(removeRect, TexButton.DeleteX))
                {
                    toRemove = disallowedThingDef;
                }
                secondColumnPos.y += 24;
            }
            if (!toRemove.NullOrEmpty())
            {
                curRecipe.disallowedIngredients.Remove(toRemove);
            }
            buttonRect = DoButton(ref secondColumnPos, "Add".Translate().CapitalizeFirst(), delegate
            {
                Find.WindowStack.Add(new Window_SelectItem<ThingDef>(Utils.craftableItems,
                delegate (ThingDef selected)
                {
                    curRecipe.disallowedIngredients.Add(selected.defName);
                }, x => x.FirstThingCategory?.index ?? 0));
            });

            Widgets.EndScrollView();
            scrollHeightCount = (int)Mathf.Max(rect.height, Mathf.Max(firstColumnPos.y, secondColumnPos.y));
            ResetPositions();
        }

        private void DoInput(float x, float y, string label, ref int count, ref string buffer, float width = 50)
        {
            Rect labelRect = new Rect(x, y, width, 24);
            Widgets.Label(labelRect, label);
            Rect inputRect = new Rect(labelRect.xMax, labelRect.y, 75, 24);
            buffer = count.ToString();
            Widgets.TextFieldNumeric<int>(inputRect, ref count, ref buffer);
        }

        private void ResetPositions()
        {
            firstColumnPos = new Vector2(0, 0);
            secondColumnPos = new Vector2(420, 0);
        }
        private static Rect DoLabel(ref Vector2 pos, string label)
        {
            var labelRect = new Rect(pos.x, pos.y, 250, 24);
            Widgets.Label(labelRect, label);
            pos.y += 24;
            return labelRect;
        }

        private static Rect DoButton(ref Vector2 pos, string label, Action action)
        {
            var buttonRect = new Rect(pos.x, pos.y, 250, 24);
            pos.y += 24;
            if (Widgets.ButtonText(buttonRect, label))
            {
                UI.UnfocusCurrentControl();
                action();
            }
            return buttonRect;
        }

        public void ResetRecipe()
        {
            curRecipe = new RecipeDefExposable();
            recipeLabel = "";
        }
    }
}

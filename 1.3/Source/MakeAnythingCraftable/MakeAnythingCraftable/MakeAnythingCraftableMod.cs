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
            settings.ApplySettings();
        }
    }

    public enum Tab
    {
        CreateNewRecipe,
        ExistingRecipes
    };

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class HotSwappableAttribute : Attribute
    {
    }

    [HotSwappable]
    public class MakeAnythingCraftableSettings : ModSettings
    {
        public List<RecipeDefExposable> newRecipeDefs = new List<RecipeDefExposable>();
        private List<TabRecord> tabs = new List<TabRecord>();
		private Tab curTab;
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
            Rect rect = new Rect(inRect.x, inRect.y + 30, inRect.width, inRect.height - 30);
			tabs.Clear();
			tabs.Add(new TabRecord("MAC.CreateNewRecipe".Translate().CapitalizeFirst(), delegate
			{
				curTab = Tab.CreateNewRecipe;
			}, curTab == Tab.CreateNewRecipe));
			tabs.Add(new TabRecord("MAC.ExistingRecipes".Translate().CapitalizeFirst(), delegate
			{
				curTab = Tab.ExistingRecipes;
			}, curTab == Tab.ExistingRecipes));
			Widgets.DrawMenuSection(rect);
			TabDrawer.DrawTabs(rect, tabs);
			rect = rect.ContractedBy(18f);
            Widgets.BeginGroup(rect);
            switch (curTab)
			{
				case Tab.CreateNewRecipe:
                    DrawCreateNewRecipe(rect);

                    break;
				case Tab.ExistingRecipes:
					break;
			}
            Widgets.EndGroup();
        }

        public RecipeDefExposable curRecipe;

        private int scrollHeightCount = 0;
        private Vector2 firstColumnPos;
        private Vector2 secondColumnPos;
        private Vector2 scrollPosition;
        private string recipeLabel;
        string buf1, buf2, buf3;
        public string GetRecipeLabel()
        {
            if (curRecipe.label != null)
            {
                return curRecipe.label;
            }
            if (curRecipe.productString != null)
            {
                var def = curRecipe.productString.ThingDef;
                string text = def.label;
                if (curRecipe.productString.count != 1)
                {
                    text = text + " x" + curRecipe.productString.count;
                }
                return "RecipeMake".Translate(text);
            }
            return "";
        }
        public void DrawCreateNewRecipe(Rect rect)
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

            if (curRecipe.productString != null && curRecipe.recipeUsersString.Any() && curRecipe.workAmount > 0)
            {
                if (Widgets.ButtonText(new Rect(firstColumnPos.x, firstColumnPos.y, 250, 32), "MAC.SaveRecipe".Translate()))
                {
                    curRecipe.defName = "MAC_Custom_Make_" + curRecipe.productString.ThingDef.defName;
                    if (curRecipe.label.NullOrEmpty())
                    {
                        if (!recipeLabel.NullOrEmpty())
                        {
                            curRecipe.label = recipeLabel;
                        }
                        else
                        {
                            curRecipe.label = GetRecipeLabel();
                        }
                    }
                    curRecipe.description = curRecipe.label;
                    string text = curRecipe.productString.ThingDef.label;
                    if (curRecipe.productString.count != 1)
                    {
                        text = text + " x" + curRecipe.productString.count;
                    }
                    curRecipe.jobString = "RecipeMakeJobString".Translate(text);
                    curRecipe.modContentPack = this.Mod.Content;
                    newRecipeDefs.Add(curRecipe);
                    ResetPositions();
                    ResetRecipe();
                    Widgets.EndScrollView();
                    return;
                }
                firstColumnPos.y += 32;
                firstColumnPos.y += 12;
            }
            var labelRect = new Rect(firstColumnPos.x, firstColumnPos.y, 100, 24);
            Widgets.Label(labelRect, "MAC.RecipeLabel".Translate());
            var inputRect = new Rect(labelRect.xMax, firstColumnPos.y, 250, 24);
            recipeLabel = Widgets.TextField(inputRect, GetRecipeLabel());
            if (recipeLabel == GetRecipeLabel())
            {
                recipeLabel = "";
            }
            firstColumnPos.y += 24;
            firstColumnPos.y += 12;

            labelRect = DoLabel(ref firstColumnPos, "MAC.SelectProduct".Translate());
            Rect buttonRect = DoButton(ref firstColumnPos, curRecipe.productString != null ? curRecipe.productString.ThingDef.LabelCap.ToString() : "-", delegate
            {
                Find.WindowStack.Add(new Window_SelectItem(Utils.craftableItems, delegate (ThingDef selected)
                {
                    curRecipe.productString = new ThingDefCountClassString
                    {
                        defName = selected.defName,
                        count = 1
                    };
                }));
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
            for (var i = 0; i < curRecipe.recipeUsersString.Count; i++)
            {
                var recipeUser = curRecipe.recipeUsersString[i];
                Rect recipeUserRect = new Rect(firstColumnPos.x, firstColumnPos.y, buttonRect.width - 30, 24);
                var def = DefDatabase<ThingDef>.GetNamed(recipeUser);
                if (Widgets.ButtonText(recipeUserRect, def.LabelCap))
                {
                    AddRecipeUserOptions();
                }

                var removeRect = new Rect(recipeUserRect.xMax + 5, firstColumnPos.y, 20, 21f);
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
                AddRecipeUserOptions();
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
            for (var i = 0; i < curRecipe.skillRequirementsString.Count; i++)
            {
                var skillRequirement = curRecipe.skillRequirementsString[i];
                Rect skillRect = new Rect(secondColumnPos.x, secondColumnPos.y, buttonRect.width - 30, 24);
                var def = DefDatabase<SkillDef>.GetNamed(skillRequirement.skill);
                if (Widgets.ButtonText(skillRect, def.LabelCap))
                {
                    AddSkillRequirementOptions();
                }

                var removeRect = new Rect(skillRect.xMax + 5, secondColumnPos.y, 20, 21f);
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
                AddSkillRequirementOptions();
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
            for (var i = 0; i < curRecipe.researchPrerequisitesString.Count; i++)
            {
                var skillRequirement = curRecipe.researchPrerequisitesString[i];
                Rect skillRect = new Rect(secondColumnPos.x, secondColumnPos.y, buttonRect.width - 30, 24);
                var def = DefDatabase<ResearchProjectDef>.GetNamed(skillRequirement);
                if (Widgets.ButtonText(skillRect, def.LabelCap))
                {
                    AddResearchRequirementOptions();
                }

                var removeRect = new Rect(skillRect.xMax + 5, secondColumnPos.y, 20, 21f);
                if (Widgets.ButtonImage(removeRect, TexButton.DeleteX))
                {
                    toRemove = skillRequirement;
                }
                secondColumnPos.y += 24;
            }

            if (!toRemove.NullOrEmpty())
            {
                curRecipe.researchPrerequisitesString.RemoveAll(x => x == toRemove);
            }

            buttonRect = DoButton(ref secondColumnPos, "Add".Translate().CapitalizeFirst(), delegate
            {
                AddResearchRequirementOptions();
            });
            secondColumnPos.y += 12;

            labelRect = DoLabel(ref secondColumnPos, "MAC.SetIngredientRequirements".Translate());
            toRemove = "";
            for (var i = 0; i < curRecipe.ingredientsString.Count; i++)
            {
                var ingredientRequirement = curRecipe.ingredientsString[i];
                Rect ingredientRect = new Rect(secondColumnPos.x, secondColumnPos.y, buttonRect.width - 30, 24);
                var def = DefDatabase<ThingDef>.GetNamed(ingredientRequirement.defName);
                if (Widgets.ButtonText(ingredientRect, def.LabelCap))
                {
                    AddIngredientRequirementOptions();
                }

                var removeRect = new Rect(ingredientRect.xMax + 5, secondColumnPos.y, 20, 21f);
                if (Widgets.ButtonImage(removeRect, TexButton.DeleteX))
                {
                    toRemove = ingredientRequirement.defName;
                }
                DoInput(removeRect.xMax + 15, removeRect.y, "MAC.Count".Translate(), ref ingredientRequirement.count, ref buf3);
                secondColumnPos.y += 24;
            }
            if (!toRemove.NullOrEmpty())
            {
                curRecipe.ingredientsString.RemoveAll(x => x.defName == toRemove);
            }

            buttonRect = DoButton(ref secondColumnPos, "Add".Translate().CapitalizeFirst(), delegate
            {
                AddIngredientRequirementOptions();
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
        private void AddRecipeUserOptions()
        {
            Find.WindowStack.Add(new Window_SelectItem(Utils.workbenches.Where(x => !curRecipe.recipeUsersString.Contains(x.defName)).ToList(),
            delegate (ThingDef selected)
            {
                curRecipe.recipeUsersString.Add(selected.defName);
            }));
        }

        private void AddSkillRequirementOptions()
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
        }

        private void AddResearchRequirementOptions()
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
        }

        private void AddIngredientRequirementOptions()
        {
            Find.WindowStack.Add(new Window_SelectItem(Utils.craftableItems, delegate (ThingDef selected)
            {
                MakeAnythingCraftableMod.settings.curRecipe.ingredientsString.Add(new ThingDefCountClassString
                {
                    defName = selected.defName,
                    count = 1
                });
            }));
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

        private static Rect DoButtonLabeled(ref Vector2 pos, string label, string buttonLabel, Action action)
        {
            var labelRect = new Rect(pos.x, pos.y, 120, 24);
            Widgets.Label(labelRect, label);
            var buttonRect = new Rect(labelRect.xMax, pos.y, 130, 24);
            if (Widgets.ButtonText(buttonRect, buttonLabel))
            {
                action();
            }
            pos.y += 24;
            return labelRect;
        }

        public void ResetRecipe()
        {
            curRecipe = new RecipeDefExposable();
            recipeLabel = "";
        }
    }
}

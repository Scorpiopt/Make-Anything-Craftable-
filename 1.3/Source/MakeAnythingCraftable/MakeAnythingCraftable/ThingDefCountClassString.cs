using Verse;

namespace MakeAnythingCraftable
{
    public sealed class ThingDefCountClassString : IExposable
    {
        public string defName;

        public int count;
        public ThingDefCountClassString()
        {
        }

        public ThingDefCountClassString(string defName, int count)
        {
            this.defName = defName;
            this.count = count;
        }

        public ThingDef ThingDef => DefDatabase<ThingDef>.GetNamed(defName);

        public void ExposeData()
        {
            Scribe_Values.Look(ref defName, "thingDef");
            Scribe_Values.Look(ref count, "count", 1);
        }
    }
}

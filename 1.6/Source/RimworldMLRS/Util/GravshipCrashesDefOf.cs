using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimworldMLRS.Util
{
    [DefOf]
    public static class RimworldMLRSDefOf
    {
        public static SitePartDef CrashedGravshipSitePart;
        public static WorldObjectDef CrashedGravshipSite;
        public static ThingSetMakerDef GravshipCrashLoot;
        public static IncidentDef CrashedGravshipIncident;
        public static FactionDef Gravship_Survivors;

        static RimworldMLRSDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(RimworldMLRSDefOf));
        }
    }
}

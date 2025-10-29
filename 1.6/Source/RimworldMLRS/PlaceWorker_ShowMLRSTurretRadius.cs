using Verse;
using RimWorld;

namespace RimworldMLRS
{
    class PlaceWorker_ShowMLRSTurretRadius : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map,
            Thing thingToIgnore = null, Thing thing = null)
        {
            // Fetch the beam verb, if present
            VerbProperties verbProperties = ((ThingDef)checkingDef)
                ?.building?.turretGunDef?.Verbs?
                .Find(v => v.verbClass == typeof(Verb_ShootMLRS));

            // ✅ Use configurable range from settings, fallback to verbProps.range if missing
            float maxRange = RimworldMLRSMod.settings.range;

            GenDraw.DrawRadiusRing(loc, maxRange);

            // Minimum range stays from XML (usually small and static)
            if (verbProperties?.minRange > 0f)
            {
                GenDraw.DrawRadiusRing(loc, verbProperties.minRange);
            }

            return true;
        }
    }
}

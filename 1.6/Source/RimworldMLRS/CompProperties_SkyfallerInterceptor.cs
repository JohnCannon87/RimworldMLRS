using Verse;

namespace RimworldMLRS
{
    public class CompProperties_SkyfallerInterceptor : CompProperties
    {
        public int tickInterval = 60;           // check every 1s
        public int maxTargetsPerCycle = 5;
        public float interceptRange = 75f;
        public string projectileDef = "Bullet_MLRS_Rocket"; // what to shoot

        public CompProperties_SkyfallerInterceptor()
        {
            compClass = typeof(CompSkyfallerInterceptor);
        }
    }
}

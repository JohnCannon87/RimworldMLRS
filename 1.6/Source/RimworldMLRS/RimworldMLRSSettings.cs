using Verse;

namespace RimworldMLRS
{
    public class RimworldMLRSSettings : ModSettings
    {
        public bool useCustomParameters = false;

        // Weapon tunables
        public float range = 75f;
        public float warmupTime = 0.66f;
        public int ticksBetweenBurstShots = 30;
        public int burstShotCount = 5;
        public int maxIntercepts = 5;
        public int interceptCooldownTicks = 600;

        // Building tunables
        public int fuelCapacity = 100;
        public float turretBurstWarmupTime = 4.0f;
        public float turretBurstCooldownTime = 25f;

        public bool enableDebugLogging = false;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref useCustomParameters, "useCustomParameters", false);
            Scribe_Values.Look(ref range, "range", 75f);
            Scribe_Values.Look(ref warmupTime, "warmupTime", 0.66f);
            Scribe_Values.Look(ref ticksBetweenBurstShots, "ticksBetweenBurstShots", 30);
            Scribe_Values.Look(ref burstShotCount, "burstShotCount", 5);
            Scribe_Values.Look(ref fuelCapacity, "fuelCapacity", 100);
            Scribe_Values.Look(ref turretBurstWarmupTime, "turretBurstWarmupTime", 4.0f);
            Scribe_Values.Look(ref turretBurstCooldownTime, "turretBurstCooldownTime", 25f);
            Scribe_Values.Look(ref maxIntercepts, "maxIntercepts", 5);
            Scribe_Values.Look(ref interceptCooldownTicks, "interceptCooldownTicks", 5);
            Scribe_Values.Look(ref enableDebugLogging, "enableDebugLogging", false);
        }
    }
}

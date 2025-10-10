using RimWorld;
using Verse;

namespace RimworldMLRS
{
    [StaticConstructorOnStartup]
    public static class RimworldMLRS_Initializer
    {
        static RimworldMLRS_Initializer()
        {
            LongEventHandler.ExecuteWhenFinished(ApplySettings);
        }

        public static void ApplySettings()
        {
            var settings = RimworldMLRSMod.settings;
            if (!settings.useCustomParameters) return;

            // Weapon def
            var weapon = DefDatabase<ThingDef>.GetNamedSilentFail("MLRSLauncher");
            if (weapon != null && weapon.Verbs?.Count > 0)
            {
                var verb = weapon.Verbs[0];
                verb.range = settings.range;
                verb.warmupTime = settings.warmupTime;
                verb.ticksBetweenBurstShots = settings.ticksBetweenBurstShots;
                verb.burstShotCount = settings.burstShotCount;
            }

            // Turret building def
            var turret = DefDatabase<ThingDef>.GetNamedSilentFail("BigBlastTurretGun");
            if (turret != null)
            {
                turret.GetCompProperties<CompProperties_Refuelable>().fuelCapacity = settings.fuelCapacity;

                if (turret.building != null)
                {
                    turret.building.turretBurstWarmupTime = new FloatRange(settings.turretBurstWarmupTime, settings.turretBurstWarmupTime);
                    turret.building.turretBurstCooldownTime = settings.turretBurstCooldownTime;
                }
            }

            Log.Message("[MLRS Launcher] Custom parameters applied.");
        }
    }
}

using System;
using System.Linq;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;

namespace RimworldMLRS
{
    public class CompSkyfallerInterceptor : ThingComp
    {
        private int tickCounter;
        private int ticksUntilNextIntercept;
        private static List<ThingDef> cachedSkyfallerDefs;
        private CompProperties_SkyfallerInterceptor Props => (CompProperties_SkyfallerInterceptor)props;

        private const int DefaultCooldownTicks = 600; // 10 seconds (60 ticks/sec)

        static CompSkyfallerInterceptor()
        {
            CacheSkyfallerDefs();
        }

        private static void CacheSkyfallerDefs()
        {
            cachedSkyfallerDefs = DefDatabase<ThingDef>.AllDefs
                .Where(d => typeof(Skyfaller).IsAssignableFrom(d.thingClass))
                .ToList();

            Logger.Message($"Cached {cachedSkyfallerDefs.Count} skyfaller defs for interception.");
        }

        public override void CompTick()
        {
            base.CompTick();

            tickCounter++;
            if (tickCounter < Props.tickInterval)
                return;
            tickCounter = 0;

            if (ticksUntilNextIntercept > 0)
            {
                ticksUntilNextIntercept--;
                return;
            }

            if (!IsActive()) return;

            InterceptSkyfallers();
        }

        private bool IsActive()
        {
            if (!parent.Spawned) return false;

            var power = parent.TryGetComp<CompPowerTrader>();
            if (power != null && !power.PowerOn) return false;

            var refuelable = parent.TryGetComp<CompRefuelable>();
            if (refuelable != null && refuelable.Fuel <= 0) return false;

            return true;
        }

        private void InterceptSkyfallers()
        {
            var map = parent.Map;
            if (map == null) return;

            var hostileSkyfallers = cachedSkyfallerDefs
                .SelectMany(def => map.listerThings.ThingsOfDef(def))
                .Where(IsHostileSkyfaller)
                .Where(t => t.Position.InHorDistOf(parent.Position, Props.interceptRange))
                .ToList();

            if (hostileSkyfallers.Count == 0)
            {
                Logger.Message("No hostile skyfallers found in range this cycle.");
                return;
            }

            var refuelable = parent.TryGetComp<CompRefuelable>();
            int availableAmmo = refuelable != null ? Mathf.FloorToInt(refuelable.Fuel) : RimworldMLRSMod.settings.maxIntercepts;
            int maxIntercepts = Math.Min(RimworldMLRSMod.settings.maxIntercepts, availableAmmo);

            Logger.Message($"[{parent.Label}] Found {hostileSkyfallers.Count} hostile skyfallers. Firing up to {maxIntercepts} rockets (Ammo: {availableAmmo}).");

            var targets = hostileSkyfallers
                .InRandomOrder()
                .Distinct()
                .Take(maxIntercepts)
                .ToList();

            foreach (var target in targets)
            {
                Logger.Message($" - Target: {target.def.defName} at {target.Position}");
                Vector3 interceptPoint = GetRandomInterceptPoint(target);
                SimulateRocketTrail(interceptPoint);
                DestroySkyfaller(target);
                refuelable?.ConsumeFuel(1f);
            }

            Logger.Message($"Intercept cycle complete. Remaining fuel: {refuelable?.Fuel ?? -1}");

            // Start cooldown timer
            ticksUntilNextIntercept = RimworldMLRSMod.settings.interceptCooldownTicks > 0
                ? RimworldMLRSMod.settings.interceptCooldownTicks
                : DefaultCooldownTicks;

            Logger.Message($"Entering cooldown for {ticksUntilNextIntercept} ticks.");
        }

        private bool IsHostileSkyfaller(Thing t)
        {
            if (t is DropPodIncoming pod)
            {
                var inner = pod.Contents?.innerContainer;
                if (inner == null) return false;
                bool hostile = inner.Any(th => th.Faction != null && th.Faction.HostileTo(Faction.OfPlayer));
                if (hostile)
                    Logger.Message($"Hostile drop pod detected at {t.Position}");
                return hostile;
            }

            if (t.Faction != null && t.Faction.HostileTo(Faction.OfPlayer))
            {
                Logger.Message($"Hostile skyfaller ({t.def.defName}) detected at {t.Position}");
                return true;
            }

            return false;
        }

        private Vector3 GetRandomInterceptPoint(Thing skyfaller)
        {
            Vector3 start = skyfaller.TrueCenter();
            Vector3 end = skyfaller.Position.ToVector3Shifted();

            float t = Rand.Range(0.3f, 0.7f);
            Vector3 intercept = Vector3.Lerp(start, end, t);
            intercept += new Vector3(Rand.Range(-1.5f, 1.5f), 0f, Rand.Range(-1.5f, 1.5f));

            Logger.Message($"Intercept point for {skyfaller.def.defName}: {intercept}");
            return intercept;
        }

        private void SimulateRocketTrail(Vector3 targetPos)
        {
            var map = parent.Map;
            if (map == null) return;

            SoundDef.Named("Missile_Launch").PlayOneShot(new TargetInfo(parent.Position, map));
            Logger.Message($"Simulating rocket trail from {parent.Position} to {targetPos}");

            Vector3 start = parent.DrawPos;
            Vector3 end = targetPos;
            Vector3 delta = end - start;
            float distance = delta.magnitude;
            int steps = Mathf.CeilToInt(distance / 0.5f);
            Vector3 dir = delta.normalized;

            for (int i = 0; i < steps; i++)
            {
                Vector3 pos = start + dir * (i * 0.5f);
                FleckMaker.Static(pos, map, FleckDefOf.Smoke, 1.0f);
                if (Rand.Chance(0.25f))
                    FleckMaker.Static(pos, map, FleckDefOf.MicroSparks, 0.8f);
                if (Rand.Chance(0.5f))
                    FleckMaker.Static(pos, map, FleckDefOf.FireGlow, 0.6f);
            }

            GenExplosion.DoExplosion(IntVec3.FromVector3(end), map, 2f, DamageDefOf.Flame, parent);
        }

        private void DestroySkyfaller(Thing target)
        {
            if (target.Destroyed) return;
            var map = target.Map;

            Logger.Message($"Destroying skyfaller: {target.def.defName} at {target.Position}");
            GenExplosion.DoExplosion(target.Position, map, 3f, DamageDefOf.Bomb, parent);

            int debrisCount = Rand.RangeInclusive(3, 6);
            for (int i = 0; i < debrisCount; i++)
            {
                var steel = ThingMaker.MakeThing(ThingDefOf.ChunkSlagSteel);
                steel.stackCount = Rand.RangeInclusive(0, 2);
                if (steel.stackCount > 0)
                    GenPlace.TryPlaceThing(steel, target.Position, map, ThingPlaceMode.Near);
            }

            target.Destroy(DestroyMode.Vanish);
        }
    }
}

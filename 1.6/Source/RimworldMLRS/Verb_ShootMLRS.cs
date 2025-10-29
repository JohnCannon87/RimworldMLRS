// RimworldMLRS.Verb_ShootMLRS.cs
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;

namespace RimworldMLRS
{
    public class Verb_ShootMLRS : Verb
    {
        private readonly List<IntVec3> evenDispersal = new List<IntVec3>();

        public virtual ThingDef Projectile =>
            EquipmentSource?.GetComp<CompChangeableProjectile>()?.Loaded == true
                ? EquipmentSource.GetComp<CompChangeableProjectile>().Projectile
                : verbProps.defaultProjectile;

        public override void WarmupComplete()
        {
            base.WarmupComplete();
            Logger.Message($"[MLRS] WarmupComplete: verb={GetType().Name}, proj={Projectile?.defName ?? "null"}");
            Find.BattleLog.Add(new BattleLogEntry_RangedFire(caster, currentTarget.HasThing ? currentTarget.Thing : null, EquipmentSource?.def, Projectile, ShotsPerBurst > 1));
        }

        protected override bool TryCastShot()
        {
            Logger.Message($"[MLRS] TryCastShot START. target={currentTarget.Cell}, casterPos={caster.Position}");

            if (currentTarget.HasThing && currentTarget.Thing.Map != caster.Map)
            {
                Logger.Message("[MLRS] Abort: target on different map.");
                return false;
            }

            var projDef = Projectile;
            if (projDef == null)
            {
                Logger.Message("[MLRS] Abort: Projectile is null (check verbProps.defaultProjectile or CompChangeableProjectile).");
                return false;
            }

            if (!TryFindShootLineFromTo(caster.Position, currentTarget, out ShootLine line))
            {
                Logger.Message($"[MLRS] No shoot line. stopBurstWithoutLos={verbProps.stopBurstWithoutLos}");
                if (verbProps.stopBurstWithoutLos) return false;
            }
            else
            {
                Logger.Message($"[MLRS] Shoot line: src={line.Source}, dest={line.Dest}");
            }

            EquipmentSource?.GetComp<CompChangeableProjectile>()?.Notify_ProjectileLaunched();
            EquipmentSource?.GetComp<CompApparelVerbOwner_Charged>()?.UsedOnce();
            lastShotTick = Find.TickManager.TicksGame;

            Thing shooter = caster;
            Thing equip = EquipmentSource;
            var mannable = caster.TryGetComp<CompMannable>();
            if (mannable?.ManningPawn != null) { shooter = mannable.ManningPawn; equip = caster; }

            Vector3 drawPos = caster.DrawPos;
            var proj = (Projectile)GenSpawn.Spawn(projDef, line.Source, caster.Map);
            Logger.Message($"[MLRS] Spawned projectile {projDef.defName} at {line.Source}");

            // Forced miss path
            if (verbProps.ForcedMissRadius > 0.5f)
            {
                float miss = verbProps.ForcedMissRadius;
                if (shooter is Pawn p) miss *= verbProps.GetForceMissFactorFor(equip, p);
                float adj = VerbUtility.CalculateAdjustedForcedMiss(miss, currentTarget.Cell - caster.Position);
                Logger.Message($"[MLRS] ForcedMiss check: base={verbProps.ForcedMissRadius}, adj={adj}");
                if (adj > 0.5f)
                {
                    var missCell = GetForcedMissTarget(adj);
                    if (missCell != currentTarget.Cell)
                    {
                        var flags = ProjectileHitFlags.NonTargetWorld;
                        if (Rand.Chance(0.5f)) flags = ProjectileHitFlags.All;
                        if (!canHitNonTargetPawnsNow) flags &= ~ProjectileHitFlags.NonTargetPawns;
                        Logger.Message($"[MLRS] FORCED MISS → {missCell} flags={flags}");
                        proj.Launch(shooter, drawPos, missCell, currentTarget, flags, preventFriendlyFire, equip);
                        return true;
                    }
                }
            }

            // Wild shot path
            ShotReport report = ShotReport.HitReportFor(caster, this, currentTarget);
            Thing cover = report.GetRandomCoverToMissInto();
            ThingDef coverDef = cover?.def;

            if (verbProps.canGoWild && !Rand.Chance(report.AimOnTargetChance_IgnoringPosture))
            {
                bool fly = proj?.def?.projectile != null && proj.def.projectile.flyOverhead;
                line.ChangeDestToMissWild(report.AimOnTargetChance_StandardTarget, fly, caster.Map);
                var flags = ProjectileHitFlags.NonTargetWorld;
                if (Rand.Chance(0.5f) && canHitNonTargetPawnsNow) flags |= ProjectileHitFlags.NonTargetPawns;
                Logger.Message($"[MLRS] WILD → {line.Dest} flags={flags}");
                proj.Launch(shooter, drawPos, line.Dest, currentTarget, flags, preventFriendlyFire, equip, coverDef);
                return true;
            }

            // Cover path
            if (currentTarget.Thing != null && currentTarget.Thing.def.CanBenefitFromCover && !Rand.Chance(report.PassCoverChance))
            {
                var flags = ProjectileHitFlags.NonTargetWorld;
                if (canHitNonTargetPawnsNow) flags |= ProjectileHitFlags.NonTargetPawns;
                Logger.Message($"[MLRS] COVER → {cover?.Position} flags={flags}");
                proj.Launch(shooter, drawPos, cover, currentTarget, flags, preventFriendlyFire, equip, coverDef);
                return true;
            }

            // Direct hit path
            var hitFlags = ProjectileHitFlags.IntendedTarget;
            if (canHitNonTargetPawnsNow) hitFlags |= ProjectileHitFlags.NonTargetPawns;
            if (!currentTarget.HasThing || currentTarget.Thing.def.Fillage == FillCategory.Full)
                hitFlags |= ProjectileHitFlags.NonTargetWorld;

            if (currentTarget.Thing != null)
            {
                Logger.Message($"[MLRS] DIRECT → thing={currentTarget.Thing.LabelCap} flags={hitFlags}");
                proj.Launch(shooter, drawPos, currentTarget, currentTarget, hitFlags, preventFriendlyFire, equip, coverDef);
            }
            else
            {
                Logger.Message($"[MLRS] DIRECT → cell={line.Dest} flags={hitFlags}");
                proj.Launch(shooter, drawPos, line.Dest, currentTarget, hitFlags, preventFriendlyFire, equip, coverDef);
            }

            Logger.Message($"[MLRS] Fired {projDef.defName} at {currentTarget.Cell}");
            return true;
        }

        private IntVec3 GetForcedMissTarget(float r)
        {
            if (verbProps.forcedMissEvenDispersal)
            {
                if (evenDispersal.Count == 0)
                {
                    evenDispersal.AddRange(GenerateEvenDispersal(currentTarget.Cell, r, burstShotsLeft));
                    evenDispersal.SortByDescending(p => p.DistanceToSquared(Caster.Position));
                }
                if (evenDispersal.Count > 0) return evenDispersal.Pop();
            }
            int n = GenRadial.NumCellsInRadius(r);
            return currentTarget.Cell + GenRadial.RadialPattern[Rand.Range(0, n)];
        }

        private static IEnumerable<IntVec3> GenerateEvenDispersal(IntVec3 root, float radius, int count)
        {
            float rot = Rand.Range(0f, 360f);
            float phi = (1f + Mathf.Sqrt(5f)) / 2f;
            for (int i = 0; i < count; i++)
            {
                float a = Mathf.PI * 2f * i / phi;
                float b = Mathf.Acos(1f - 2f * ((i + 0.5f) / (float)count));
                int x = (int)(Mathf.Cos(a) * Mathf.Sin(b) * radius);
                int z = (int)(Mathf.Cos(b) * radius);
                yield return root + new Vector3(x, 0f, z).RotatedBy(rot).ToIntVec3();
            }
        }

        public override void Reset()
        {
            base.Reset();
            evenDispersal.Clear();
        }
    }
}

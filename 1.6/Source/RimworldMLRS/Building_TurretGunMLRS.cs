using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RimworldMLRS
{
    [StaticConstructorOnStartup]
    public class Building_TurretGunMLRS : Building_Turret
    {
        protected int burstCooldownTicksLeft;
        protected int burstWarmupTicksLeft;
        protected LocalTargetInfo currentTargetInt = LocalTargetInfo.Invalid;

        private bool holdFire;
        private bool burstActivated;

        public Thing gun;
        protected TurretTop top;
        protected CompPowerTrader powerComp;
        protected CompCanBeDormant dormantComp;
        protected CompInitiatable initiatableComp;
        protected CompMannable mannableComp;
        protected CompInteractable interactableComp;
        public CompRefuelable refuelableComp;
        protected Effecter progressBarEffecter;
        protected CompMechPowerCell powerCellComp;
        protected CompHackable hackableComp;

        public static Material ForcedTargetLineMat =
            MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.Transparent, new Color(1f, 0.5f, 0.5f));

        public bool Active =>
            (powerComp == null || powerComp.PowerOn) &&
            (dormantComp == null || dormantComp.Awake) &&
            (initiatableComp == null || initiatableComp.Initiated) &&
            (interactableComp == null || burstActivated) &&
            (powerCellComp == null || !powerCellComp.depleted) &&
            (hackableComp == null || !hackableComp.IsHacked);

        private static string VerbPropsSummary(VerbProperties vp)
        {
            if (vp == null) return "(null VerbProperties)";
            var cls = vp.verbClass?.FullName ?? "(null -> defaults to Verse.Verb_Shoot)";
            var proj = vp.defaultProjectile?.defName ?? "(null)";
            return $"verbClass={cls} | defaultProjectile={proj} | requireLOS={vp.requireLineOfSight} | range={vp.range} | min={vp.minRange}";
        }

        // Verb accessors with logging
        public CompEquippable GunCompEq
        {
            get
            {
                if (gun == null)
                {
                    Logger.Message("[MLRS] GunCompEq accessed but gun is null!");
                    return null;
                }
                CompEquippable comp = gun.TryGetComp<CompEquippable>();
                if (comp == null)
                    Logger.Message("[MLRS] GunCompEq: gun has no CompEquippable!");
                return comp;
            }
        }

        public override LocalTargetInfo CurrentTarget => currentTargetInt;
        private bool WarmingUp => burstWarmupTicksLeft > 0;

        public override Verb AttackVerb
        {
            get
            {
                var eq = GunCompEq;
                var verb = eq?.PrimaryVerb;
                if (verb == null)
                {
                    Logger.Message($"[MLRS] AttackVerb accessed but was null. Gun={gun?.def?.defName ?? "null"} | CompEq={(eq != null)}");
                }
                return verb;
            }
        }

        public bool IsMannable => mannableComp != null;

        private bool PlayerControlled =>
            (Faction == Faction.OfPlayer || MannedByColonist) &&
            !MannedByNonColonist &&
            !IsActivable;

        protected virtual bool CanSetForcedTarget => mannableComp != null ? PlayerControlled : false;
        private bool CanToggleHoldFire => PlayerControlled;
        private bool IsMortar => def.building.IsMortar;
        private bool IsMortarOrProjectileFliesOverhead => AttackVerb.ProjectileFliesOverhead() || IsMortar;
        private bool IsActivable => interactableComp != null;
        protected virtual bool HideForceTargetGizmo => false;
        public TurretTop Top => top;

        private bool CanExtractShell =>
            PlayerControlled && (gun.TryGetComp<CompChangeableProjectile>()?.Loaded ?? false);

        private bool MannedByColonist =>
            mannableComp?.ManningPawn?.Faction == Faction.OfPlayer;

        private bool MannedByNonColonist =>
            mannableComp?.ManningPawn != null && mannableComp.ManningPawn.Faction != Faction.OfPlayer;

        public Building_TurretGunMLRS()
        {
            top = new TurretTop(this);
        }

        public override void PostMake()
        {
            base.PostMake();
            Logger.Message($"[MLRS] PostMake(): turret def={def?.defName} | building.turretGunDef={def?.building?.turretGunDef?.defName ?? "null"}");
            burstCooldownTicksLeft = def.building.turretInitialCooldownTime.SecondsToTicks();
            MakeGun();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            dormantComp = GetComp<CompCanBeDormant>();
            initiatableComp = GetComp<CompInitiatable>();
            powerComp = GetComp<CompPowerTrader>();
            mannableComp = GetComp<CompMannable>();
            interactableComp = GetComp<CompInteractable>();
            refuelableComp = GetComp<CompRefuelable>();
            powerCellComp = GetComp<CompMechPowerCell>();
            hackableComp = GetComp<CompHackable>();

            var pv = GunCompEq?.PrimaryVerb;
            Logger.Message($"[MLRS] SpawnSetup(): Gun={gun?.def?.defName ?? "null"} | PrimaryVerb={pv?.GetType().FullName ?? "null"} | props.verbClass={pv?.verbProps?.verbClass?.FullName ?? "null"}");

            if (!respawningAfterLoad)
                top.SetRotationFromOrientation();
        }

        public void MakeGun()
        {
            var gunDef = def?.building?.turretGunDef;
            Logger.Message($"[MLRS] MakeGun(): requested turretGunDef={gunDef?.defName ?? "null"} (from mod={gunDef?.modContentPack?.Name ?? "?"}/{gunDef?.modContentPack?.PackageId ?? "?"})");

            gun = ThingMaker.MakeThing(gunDef);
            if (gun == null)
            {
                Logger.Error("[MLRS] MakeGun(): ThingMaker.MakeThing returned NULL!");
                return;
            }

            Logger.Message($"[MLRS] MakeGun(): created gun thing={gun.def.defName} (mod={gun.def.modContentPack?.Name}/{gun.def.modContentPack?.PackageId})");

            // Dump the verb defs as they are on the ThingDef
            var vpropsList = gun.def.Verbs;
            if (vpropsList == null || vpropsList.Count == 0)
            {
                Logger.Warning("[MLRS] gun.def.Verbs is NULL or EMPTY — there are no verb defs on the gun ThingDef!");
            }
            else
            {
                Logger.Message($"[MLRS] gun.def.Verbs count={vpropsList.Count}");
                for (int i = 0; i < vpropsList.Count; i++)
                {
                    var vp = vpropsList[i];
                    Logger.Message($"[MLRS]   def.Verbs[{i}]: {VerbPropsSummary(vp)}");
                }
            }

            // CompEquippable present?
            var compEq = gun.TryGetComp<CompEquippable>();
            Logger.Message($"[MLRS] CompEquippable present? {(compEq != null ? "YES" : "NO")}");

            if (compEq == null)
            {
                Logger.Error("[MLRS] MakeGun(): gun has NO CompEquippable → verb tracker will NOT exist. Check BaseWeapon/BaseArtilleryWeapon inheritance and <comps>.");
                return;
            }

            // Build verbs (this populates the VerbTracker)
            UpdateGunVerbs();

            // Dump the live verbs on the tracker
            var verbs = compEq.AllVerbs;
            Logger.Message($"[MLRS] Equippable.AllVerbs count={verbs.Count}");
            for (int i = 0; i < verbs.Count; i++)
            {
                var v = verbs[i];
                var dt = v.verbProps?.defaultProjectile?.defName ?? "null";
                var cls = v.GetType().FullName;
                var propsClass = v.verbProps?.verbClass?.FullName ?? "(null -> Verse.Verb_Shoot default)";
                Logger.Message($"[MLRS]   Verb[{i}] type={cls} | props.verbClass={propsClass} | defaultProj={dt}");
            }

            var primary = compEq.PrimaryVerb;
            Logger.Message($"[MLRS] PrimaryVerb type={primary?.GetType().FullName ?? "null"} | props.verbClass={primary?.verbProps?.verbClass?.FullName ?? "null"}");

            // Hard check: is it our custom verb?
            if (primary == null)
            {
                Logger.Error("[MLRS] PrimaryVerb is NULL! Gun cannot fire. Check verb defs.");
            }
            else if (primary.GetType().FullName != "RimworldMLRS.Verb_ShootMLRS")
            {
                Logger.Warning($"[MLRS] PrimaryVerb is NOT our verb. Got {primary.GetType().FullName}. " +
                               $"Likely causes: (1) XML verbClass typo/namespace mismatch, (2) duplicate defName 'MLRSLauncher' elsewhere overriding, " +
                               $"(3) verbClass failed to resolve → defaulted to Verse.Verb_Shoot.");
            }
        }

        private void UpdateGunVerbs()
        {
            var compEq = gun.TryGetComp<CompEquippable>();
            if (compEq == null)
            {
                Logger.Error("[MLRS] UpdateGunVerbs(): NO CompEquippable, cannot wire verbs.");
                return;
            }

            var verbs = compEq.AllVerbs;
            Logger.Message($"[MLRS] UpdateGunVerbs(): AllVerbs={verbs.Count}");
            for (int i = 0; i < verbs.Count; i++)
            {
                var v = verbs[i];
                v.caster = this;
                v.castCompleteCallback = BurstComplete; // keep cooldown behavior

                var cls = v.GetType().FullName;
                var propsClass = v.verbProps?.verbClass?.FullName ?? "(null -> Verse.Verb_Shoot default)";
                var proj = v.verbProps?.defaultProjectile?.defName ?? "null";
                Logger.Message($"[MLRS]   Wire Verb[{i}] type={cls} | props.verbClass={propsClass} | defaultProj={proj}");
            }
        }


        protected virtual void BeginBurst()
        {
            Logger.Message($"[MLRS] BeginBurst at {currentTargetInt.Cell}");
            Logger.Message($"[MLRS] Verb={AttackVerb?.GetType().Name ?? "null"} | Projectile={AttackVerb?.verbProps?.defaultProjectile?.defName ?? "null"}");

            if (AttackVerb == null)
            {
                Logger.Message("[MLRS] BeginBurst() -> AttackVerb is null, cannot fire!");
                return;
            }

            AttackVerb.TryStartCastOn(CurrentTarget);
            OnAttackedTarget(CurrentTarget);
        }

        protected virtual void BurstComplete()
        {
            Logger.Message("[MLRS] BurstComplete()");
            burstCooldownTicksLeft = BurstCooldownTime().SecondsToTicks();
        }

        protected virtual float BurstCooldownTime() =>
            def.building.turretBurstCooldownTime >= 0f
                ? def.building.turretBurstCooldownTime
                : AttackVerb.verbProps.defaultCooldownTime;

        public override void OrderAttack(LocalTargetInfo targ)
        {
            Logger.Message($"[MLRS] OrderAttack({targ})");
            if (!targ.IsValid)
            {
                if (forcedTarget.IsValid)
                    ResetForcedTarget();
                return;
            }

            if ((targ.Cell - Position).LengthHorizontal < AttackVerb.verbProps.EffectiveMinRange(targ, this))
            {
                Messages.Message("MessageTargetBelowMinimumRange".Translate(), this, MessageTypeDefOf.RejectInput, false);
                return;
            }

            if ((targ.Cell - Position).LengthHorizontal > AttackVerb.EffectiveRange)
            {
                Messages.Message("MessageTargetBeyondMaximumRange".Translate(), this, MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (forcedTarget != targ)
            {
                forcedTarget = targ;
                if (burstCooldownTicksLeft <= 0)
                    TryStartShootSomething(false);
            }

            if (holdFire)
                Messages.Message("MessageTurretWontFireBecauseHoldFire".Translate(def.label), this, MessageTypeDefOf.RejectInput, false);
        }

        public virtual LocalTargetInfo TryFindNewTarget()
        {
            Logger.Message("[MLRS] TryFindNewTarget()");
            IAttackTargetSearcher searcher = TargSearcher();
            Faction faction = searcher.Thing.Faction;
            float range = AttackVerb.EffectiveRange;

            TargetScanFlags flags = TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable;
            if (!AttackVerb.ProjectileFliesOverhead())
                flags |= TargetScanFlags.NeedLOSToAll | TargetScanFlags.LOSBlockableByGas;
            if (AttackVerb.IsIncendiary_Ranged())
                flags |= TargetScanFlags.NeedNonBurning;
            if (IsMortar)
                flags |= TargetScanFlags.NeedNotUnderThickRoof;

            var result = (Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(searcher, flags, IsValidTarget);
            if (result == null)
                Logger.Message("[MLRS] TryFindNewTarget() -> no valid target found");
            else
                Logger.Message($"[MLRS] TryFindNewTarget() -> {result.LabelShort} at {result.Position}");
            return result;
        }

        private IAttackTargetSearcher TargSearcher()
        {
            if (mannableComp != null && mannableComp.MannedNow)
                return mannableComp.ManningPawn;
            return this;
        }

        private bool IsValidTarget(Thing t)
        {
            Pawn pawn = t as Pawn;
            if (pawn != null)
            {
                if (Faction == Faction.OfPlayer && pawn.IsPrisoner)
                    return false;
                if (AttackVerb.ProjectileFliesOverhead())
                {
                    RoofDef roof = Map.roofGrid.RoofAt(t.Position);
                    if (roof != null && roof.isThickRoof)
                        return false;
                }
                if (mannableComp == null)
                    return !GenAI.MachinesLike(Faction, pawn);
                if (pawn.RaceProps.Animal && pawn.Faction == Faction.OfPlayer)
                    return false;
            }
            return true;
        }

        public void TryStartShootSomething(bool canBeginBurstImmediately)
        {
            Logger.Message("[MLRS] TryStartShootSomething() called");
            if (!Spawned)
            {
                Logger.Message("[MLRS] Not spawned, aborting.");
                return;
            }

            if (gun == null)
            {
                Logger.Message("[MLRS] Gun is null during TryStartShootSomething!");
                MakeGun();
                if (gun == null)
                {
                    Logger.Message("[MLRS] Gun creation failed again, aborting fire.");
                    return;
                }
            }

            if (!AttackVerb.Available())
            {
                Logger.Message("[MLRS] AttackVerb not available.");
                return;
            }

            bool hadTarget = currentTargetInt.IsValid;
            currentTargetInt = forcedTarget.IsValid ? forcedTarget : TryFindNewTarget();

            if (!hadTarget && currentTargetInt.IsValid && def.building.playTargetAcquiredSound)
                SoundDefOf.TurretAcquireTarget.PlayOneShot(new TargetInfo(Position, Map));

            if (currentTargetInt.IsValid)
            {
                float warmup = def.building.turretBurstWarmupTime.RandomInRange;
                if (warmup > 0f)
                {
                    burstWarmupTicksLeft = warmup.SecondsToTicks();
                    Logger.Message($"[MLRS] Target acquired, warming up for {warmup:F1}s at {currentTargetInt.Cell}");
                }
                else if (canBeginBurstImmediately)
                {
                    Logger.Message("[MLRS] Target acquired, firing immediately.");
                    BeginBurst();
                }
                else
                {
                    burstWarmupTicksLeft = 1;
                }
            }
            else
            {
                Logger.Message("[MLRS] No valid target found.");
                ResetCurrentTarget();
            }
        }

        private void ResetCurrentTarget()
        {
            Logger.Message("[MLRS] ResetCurrentTarget()");
            currentTargetInt = LocalTargetInfo.Invalid;
            burstWarmupTicksLeft = 0;
        }

        private void ResetForcedTarget()
        {
            forcedTarget = LocalTargetInfo.Invalid;
            burstWarmupTicksLeft = 0;
            if (burstCooldownTicksLeft <= 0)
                TryStartShootSomething(false);
        }
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            // Draw the top gun part
            Vector3 drawOffset = Vector3.zero;
            float angleOffset = 0f;

            // Optional: apply recoil if you want to replicate mortar look
            if (IsMortar)
                EquipmentUtility.Recoil(def.building.turretGunDef, (Verb_LaunchProjectile)AttackVerb, out drawOffset, out angleOffset, top.CurRotation);

            top.DrawTurret(drawLoc, drawOffset, angleOffset);
            base.DrawAt(drawLoc, flip);
        }
    }
}

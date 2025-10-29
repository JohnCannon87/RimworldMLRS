using HarmonyLib;
using Verse;
using Verse.AI;

namespace RimworldMLRS
{
    /// <summary>
    /// Allows beam or MLRS turrets to target through walls and thin roofs
    /// while still avoiding thick (mountain) roofs.
    /// </summary>
    [HarmonyPatch(typeof(AttackTargetFinder), nameof(AttackTargetFinder.BestShootTargetFromCurrentPosition))]
    public static class Patch_AllowFiringWithinWalls
    {
        // --- PREFIX ---
        static void Prefix(IAttackTargetSearcher searcher, ref TargetScanFlags flags)
        {
            try
            {
                var verb = searcher.CurrentEffectiveVerb;
                if (!(verb is Verb_ShootMLRS))
                    return;

                // Allow firing through walls & thin roofs
                flags &= ~TargetScanFlags.NeedLOSToAll;
                flags &= ~TargetScanFlags.NeedLOSToPawns;
                flags &= ~TargetScanFlags.NeedLOSToNonPawns;

                // Still block shots under thick roofs
                flags |= TargetScanFlags.NeedNotUnderThickRoof;

                Logger.Message($"[RoofCheck] Prefix: {verb.GetType().Name} scanning targets for {searcher.Thing.Label} at {searcher.Thing.Position}");
            }
            catch (System.Exception ex)
            {
                Logger.Error($"[RoofCheck] Prefix error: {ex}");
            }
        }

        // --- POSTFIX ---
        static void Postfix(ref IAttackTarget __result, IAttackTargetSearcher searcher)
        {
            try
            {
                // Skip if no target or not our turret
                if (__result == null || searcher == null)
                    return;

                var verb = searcher.CurrentEffectiveVerb as Verb_ShootMLRS;
                if (verb == null)
                    return;

                var map = searcher.Thing?.Map;
                if (map == null)
                    return;

                IntVec3 src = searcher.Thing.Position;
                IntVec3 dst;

                // Prefer the Thing position if it exists, otherwise fall back to TargetCurrentlyAimingAt.Cell
                if (__result.Thing != null && __result.Thing.Spawned)
                    dst = __result.Thing.Position;
                else if (__result.TargetCurrentlyAimingAt.IsValid)
                    dst = __result.TargetCurrentlyAimingAt.Cell;
                else
                    return; // no valid position to check

                // Safety: if target doesn't have a valid cell
                if (!dst.IsValid)
                    return;

                Logger.Message($"[RoofCheck] Postfix: clear LOS from {src} → {dst}");
            }
            catch (System.Exception ex)
            {
                Logger.Error($"[RoofCheck] Postfix error: {ex}");
            }
        }
    }
}

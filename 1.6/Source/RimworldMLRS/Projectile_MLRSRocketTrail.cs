using RimWorld;
using Verse;
using UnityEngine;
using Verse.Sound;

namespace RimWorldMLRS
{

public class Projectile_MLRSRocketTrail : Projectile_Explosive
{
    private Sustainer sustainer;
    private Vector3 lastPos;

    protected override void Tick()
    {
        base.Tick();

        // Play continuous engine sound
        if (def.projectile.soundAmbient != null && sustainer == null)
            sustainer = def.projectile.soundAmbient.TrySpawnSustainer(this);
        sustainer?.Maintain();

        // Compute travel vector
        Vector3 currentPos = DrawPos;
        if (lastPos == Vector3.zero)
            lastPos = currentPos;

        // Spawn smoke between last and current position
        Vector3 delta = currentPos - lastPos;
        float distance = delta.magnitude;
        int steps = Mathf.CeilToInt(distance / 0.2f); // spacing between puffs

        for (int i = 0; i < steps; i++)
        {
            Vector3 pos = lastPos + delta * (i / (float)steps);

            // Thicker smoke trail
            FleckMaker.Static(pos, Map, FleckDefOf.Smoke, 1.0f);
            if (Rand.Chance(0.25f))
                FleckMaker.Static(pos, Map, FleckDefOf.MicroSparks, 0.8f);
            if (Rand.Chance(0.5f))
                FleckMaker.Static(pos, Map, FleckDefOf.FireGlow, 0.6f);
        }

        lastPos = currentPos;
    }

    protected override void Impact(Thing hitThing, bool blockedByShield = false)
    {
        base.Impact(hitThing, blockedByShield);
        sustainer?.End();
    }
    }
}

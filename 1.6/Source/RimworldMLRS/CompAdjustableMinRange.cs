using RimWorld;
using UnityEngine;
using Verse;
using System.Collections.Generic;

namespace RimworldMLRS
{
    public class CompProperties_AdjustableMinRange : CompProperties
    {
        public float defaultMinRange = 29.9f;
        public float minLimit = 5f;
        public float maxLimit = 160f;

        public CompProperties_AdjustableMinRange()
        {
            compClass = typeof(CompAdjustableMinRange);
        }
    }

    public class CompAdjustableMinRange : ThingComp
    {
        public float currentMinRange;

        public CompProperties_AdjustableMinRange Props => (CompProperties_AdjustableMinRange)props;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            currentMinRange = Props.defaultMinRange;
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref currentMinRange, "currentMinRange", Props.defaultMinRange);
        }

        // Draw a gizmo slider for this turret
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra()) yield return g;

            if (parent.Faction == Faction.OfPlayer)
            {
                yield return new Command_Action
                {
                    defaultLabel = $"Min Range: {currentMinRange:F1} c",
                    defaultDesc = "Adjust the minimum range at which this turret can fire.",
                    icon = ContentFinder<Texture2D>.Get("UI/Misc/Compass"), // Use any suitable icon
                    action = () => Find.WindowStack.Add(new FloatMenu(MakeMenuOptions()))
                };
            }
        }

        private List<FloatMenuOption> MakeMenuOptions()
        {
            var options = new List<FloatMenuOption>();
            for (float i = Props.minLimit; i <= Props.maxLimit; i += 5f)
            {
                float val = i;
                options.Add(new FloatMenuOption($"{val} cells", () => SetMinRange(val)));
            }
            return options;
        }

        private void SetMinRange(float newRange)
        {
            currentMinRange = newRange;
            UpdateVerbRange();
            Messages.Message($"Min range set to {newRange} cells", parent, MessageTypeDefOf.TaskCompletion);
        }

        private void UpdateVerbRange()
        {
            if (parent is Building_TurretGun turret)
            {
                // Each turret has its own Verb instance in turret.AttackVerb
                var verb = turret.AttackVerb;
                if (verb?.verbProps != null)
                {
                    // Create a shallow copy of the verb properties so this turret has its own instance
                    var newProps = verb.verbProps.MemberwiseClone() as VerbProperties;
                    newProps.minRange = currentMinRange;
                    verb.verbProps = newProps;
                }
            }
        }
    }
}

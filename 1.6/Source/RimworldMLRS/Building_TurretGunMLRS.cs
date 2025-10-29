using RimWorld;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace RimworldMLRS
{
    public class Building_TurretGunMLRS : Building_TurretGun
    {
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Vector3 drawOffset = Vector3.zero;
            float angleOffset = 0f;

            // No recoil logic, we draw the top directly
            top.DrawTurret(drawLoc, drawOffset, angleOffset);
            Graphic.Draw(drawLoc, flip ? Rotation.Opposite : Rotation, this);
            SilhouetteUtility.DrawGraphicSilhouette(this, drawLoc);
            Comps_DrawAt(drawLoc, flip);
            Comps_PostDraw();
        }

        public override string GetInspectString()
        {
			StringBuilder stringBuilder = new StringBuilder();
			string inspectString = "";
			if (!inspectString.NullOrEmpty())
			{
				stringBuilder.AppendLine(inspectString);
			}
			if (AttackVerb.verbProps.minRange > 0f)
			{
				stringBuilder.AppendLine("MinimumRange".Translate() + ": " + AttackVerb.verbProps.minRange.ToString("F0"));
			}
			if (base.Spawned && burstCooldownTicksLeft > 0 && BurstCooldownTime() > 5f)
			{
				stringBuilder.AppendLine("CanFireIn".Translate() + ": " + burstCooldownTicksLeft.ToStringSecondsFromTicks());
			}
			return stringBuilder.ToString().TrimEndNewlines();
		}

        public override IEnumerable<Gizmo> GetGizmos()
        {
			foreach (Gizmo gizmo in base.GetGizmos())
			{
				yield return gizmo;
			}
			if (base.Spawned)
			{
				Command_VerbTarget command_VerbTarget = new Command_VerbTarget();
				command_VerbTarget.defaultLabel = "CommandSetForceAttackTarget".Translate();
				command_VerbTarget.defaultDesc = "CommandSetForceAttackTargetDesc".Translate();
				command_VerbTarget.icon = ContentFinder<Texture2D>.Get("UI/Commands/Attack");
				command_VerbTarget.verb = AttackVerb;
				command_VerbTarget.hotKey = KeyBindingDefOf.Misc4;
				command_VerbTarget.drawRadius = false;
				command_VerbTarget.requiresAvailableVerb = false;
				if (base.Spawned)
				{
				    float curWeatherMaxRangeCap = base.Map.weatherManager.CurWeatherMaxRangeCap;
				    if (curWeatherMaxRangeCap > 0f && curWeatherMaxRangeCap < AttackVerb.verbProps.minRange)
				    {
					    command_VerbTarget.Disable("CannotFire".Translate() + ": " + base.Map.weatherManager.curWeather.LabelCap);
				    }
				}
				yield return command_VerbTarget;
			}
			if (forcedTarget.IsValid)
			{
				Command_Action command_Action2 = new Command_Action();
				command_Action2.defaultLabel = "CommandStopForceAttack".Translate();
				command_Action2.defaultDesc = "CommandStopForceAttackDesc".Translate();
				command_Action2.icon = ContentFinder<Texture2D>.Get("UI/Commands/Halt");
				command_Action2.action = delegate
				{
					ResetForcedTarget();
				};
				if (!forcedTarget.IsValid)
				{
					command_Action2.Disable("CommandStopAttackFailNotForceAttacking".Translate());
				}
				command_Action2.hotKey = KeyBindingDefOf.Misc5;
				yield return command_Action2;
			}
		}
		private void ResetForcedTarget()
		{
			forcedTarget = LocalTargetInfo.Invalid;
			burstWarmupTicksLeft = 0;
			if (burstCooldownTicksLeft <= 0)
			{
				TryStartShootSomething(canBeginBurstImmediately: false);
			}
		}
	}
}

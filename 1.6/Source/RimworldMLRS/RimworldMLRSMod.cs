using System;
using UnityEngine;
using Verse;

namespace RimworldMLRS
{
    public class RimworldMLRSMod : Mod
    {
        public static RimworldMLRSSettings settings;

        public RimworldMLRSMod(ModContentPack pack) : base(pack)
        {
            settings = GetSettings<RimworldMLRSSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard list = new Listing_Standard();
            list.Begin(inRect);

            list.CheckboxLabeled("Enable Custom MLRS Parameters", ref settings.useCustomParameters);

            if (settings.useCustomParameters)
            {
                // 🔹 Info note at the top
                Text.Font = GameFont.Small;
                list.Label("Note: A game restart or reload is required for changes to take effect.");
                list.GapLine();

                DrawFloatSetting(list, "Range", ref settings.range, 0f, 200f);
                DrawFloatSetting(list, "Warmup Time", ref settings.warmupTime, 0f, 10f);
                DrawIntSetting(list, "Ticks Between Burst Shots", ref settings.ticksBetweenBurstShots, 1, 200);
                DrawIntSetting(list, "Burst Shot Count", ref settings.burstShotCount, 1, 20);
                DrawIntSetting(list, "Fuel Capacity", ref settings.fuelCapacity, 1, 200);
                DrawFloatSetting(list, "Turret Burst Warmup Time", ref settings.turretBurstWarmupTime, 0f, 10f);
                DrawFloatSetting(list, "Turret Burst Cooldown Time", ref settings.turretBurstCooldownTime, 0f, 30f);
            }

            list.End();
        }

        public override string SettingsCategory() => "MLRS Launcher";

        private void DrawFloatSetting(Listing_Standard list, string label, ref float value, float min, float max)
        {
            list.Label($"{label}: {value:F2}");
            float newValue = Widgets.HorizontalSlider(list.GetRect(22f), value, min, max);
            value = (float)Math.Round(newValue, 2);

            Rect textRect = list.GetRect(24f);
            string buffer = value.ToString("F2");
            Widgets.TextFieldNumeric(textRect, ref value, ref buffer, min, max);
            list.Gap(6f);
        }

        private void DrawIntSetting(Listing_Standard list, string label, ref int value, int min, int max)
        {
            list.Label($"{label}: {value}");
            Rect textRect = list.GetRect(24f);
            string buffer = value.ToString();
            Widgets.IntEntry(textRect, ref value, ref buffer, 1);
            value = Mathf.Clamp(value, min, max);
            list.Gap(6f);
        }
    }


}

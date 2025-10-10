using System;
using UnityEngine;
using Verse;

namespace RimworldMLRS
{
    public class RimworldMLRSMod : Mod
    {
        public static RimworldMLRSSettings settings;
        private Vector2 scrollPosition; // <-- for scroll tracking
        private float scrollViewHeight; // <-- tracks total height of content

        public RimworldMLRSMod(ModContentPack pack) : base(pack)
        {
            settings = GetSettings<RimworldMLRSSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            // Prepare scroll variables
            float scrollBarWidth = 20f;
            Rect outRect = inRect;
            outRect.width -= scrollBarWidth;

            // Estimate a big enough height for content initially to prevent clipping
            Rect viewRect = new Rect(0f, 0f, outRect.width - 10f, 1200f);

            // Begin scroll view
            Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);
            Listing_Standard list = new Listing_Standard();
            list.Begin(viewRect);

            // --- General ---
            list.CheckboxLabeled("Enable Debug Logging", ref settings.enableDebugLogging);
            list.CheckboxLabeled("Enable Custom MLRS Parameters", ref settings.useCustomParameters);
            list.Gap(10f);

            // --- Custom settings ---
            if (settings.useCustomParameters)
            {
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
                list.GapLine();
                list.CheckboxLabeled("Enable Droppod Interception", ref settings.canInterceptSkyfallers);
                DrawIntSetting(list, "Max Aerial Targets Intercepted", ref settings.maxIntercepts, 0, 100);
                DrawIntSetting(list, "Interceptor Cooldown (ticks)", ref settings.interceptCooldownTicks, 60, 1800);
            }

            list.End();
            Widgets.EndScrollView();

            // Update for next frame — ensures the viewRect is tall enough
            scrollViewHeight = list.CurHeight + 50f;
        }


        public override string SettingsCategory() => "MLRS Launcher";

        // --- Helper Methods ---
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

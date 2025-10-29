using HarmonyLib;
using Verse;

namespace RimworldMLRS
{
    // Ensures PatchAll runs when the game loads the assembly
    [StaticConstructorOnStartup]
    public static class HarmonyInit
    {
        static HarmonyInit()
        {
            var harmony = new Harmony("com.arcjc.mlrs");
            harmony.PatchAll();
            Logger.Message("Harmony patches applied.");
        }
    }
}

using Verse;

namespace RimworldMLRS
{
    public static class Logger
    {
        /// <summary>
        /// Logs a debug/info message if debug logging is enabled in mod settings.
        /// </summary>
        public static void Message(string msg)
        {
            if (IsDebugEnabled)
            {
                Log.Message($"[MLRS] {msg}");
            }
        }

        /// <summary>
        /// Logs a warning message if debug logging is enabled.
        /// Critical warnings (game-breaking) should still use Log.Warning directly.
        /// </summary>
        public static void Warning(string msg)
        {
            if (IsDebugEnabled)
            {
                Log.Warning($"[MLRS] {msg}");
            }
        }

        /// <summary>
        /// Logs an error message (always logs regardless of settings).
        /// Use this for exceptions or critical failures.
        /// </summary>
        public static void Error(string msg)
        {
            Log.Error($"[MLRS] {msg}");
        }

        /// <summary>
        /// Reads the mod setting. Defaults to false if unavailable.
        /// </summary>
        private static bool IsDebugEnabled
        {
            get
            {
                try
                {
                    var settings = LoadedModManager.GetMod<RimworldMLRSMod>()?.GetSettings<RimworldMLRSSettings>();
                    return settings?.enableDebugLogging ?? false;
                }
                catch
                {
                    // If settings can't be loaded early in startup, fail silently
                    return false;
                }
            }
        }
    }
}

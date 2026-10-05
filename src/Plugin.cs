using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace LooplerToolBelt;

[BepInPlugin(Guid, Name, Version)]
public class Plugin : BasePlugin
{
    public const string Guid = "renokk.loopler.toolbelt";
    public const string Name = "Loopler Tool Belt";
    public const string Version = "0.1.0";

    internal static ManualLogSource Logger;
    internal static ConfigEntry<float> ScoreMult, LuckBonus, DriftFill, GoldMult;
    internal static ConfigEntry<bool> ForceDriftMeter;

    public override void Load()
    {
        Logger = Log;
        ScoreMult = Config.Bind("Trainer", "ScoreMult", 1f, "Multiplies the game's global score mult");
        LuckBonus = Config.Bind("Trainer", "LuckBonus", 0f, "Percentage points added to lucky chance");
        DriftFill = Config.Bind("Trainer", "DriftFill", 1f, "Multiplies drift meter fill per tick");
        ForceDriftMeter = Config.Bind("Trainer", "ForceDriftMeter", false, "Drift meter active without the charm");
        GoldMult = Config.Bind("Trainer", "GoldMult", 1f, "Multiplies every gold gain");

        var harmony = new Harmony(Guid);
        harmony.PatchAll(typeof(LeaderboardBlock));
        harmony.PatchAll(typeof(Patches));
        AddComponent<TrainerUI>();
        Log.LogInfo($"{Name} {Version} loaded, F1 toggles the overlay. Leaderboard uploads are disabled while this mod is installed.");
    }
}

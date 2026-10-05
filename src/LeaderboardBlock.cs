using HarmonyLib;
using Steamworks;

namespace LooplerToolBelt;

// Runs scored with a trainer don't belong on the leaderboards, so while this mod is
// installed nothing is uploaded, whatever the trainer settings are. Blocked twice: at
// the game's two submit entry points, and at the only Steam call that posts a score,
// in case the game reaches it some other way. Reading the boards still works.
static class LeaderboardBlock
{
    [HarmonyPrefix, HarmonyPatch(typeof(LeaderboardManager), nameof(LeaderboardManager.SubmitLeaderboardScore))]
    static bool SubmitScore()
    {
        Plugin.Logger.LogWarning("Blocked leaderboard score submission");
        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(LeaderboardManager), nameof(LeaderboardManager.SubmitTrackInfinityRounds))]
    static bool SubmitInfinityRounds()
    {
        Plugin.Logger.LogWarning("Blocked leaderboard infinity rounds submission");
        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(SteamUserStats), nameof(SteamUserStats.UploadLeaderboardScore))]
    static bool SteamUpload(ref SteamAPICall_t __result)
    {
        Plugin.Logger.LogWarning("Blocked Steam leaderboard upload");
        __result = SteamAPICall_t.Invalid;
        return false;
    }
}

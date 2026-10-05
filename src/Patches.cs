using System;
using HarmonyLib;

namespace LooplerToolBelt;

// The trainer's hooks into the game's own values.
static class Patches
{
    // Last values the game produced before our changes, shown in the overlay.
    internal static float RawScoreMult = 1f, RawLuck;
    internal static int UiColorIndex;

    // Final multiplier every score is scaled by (parts, gates, drift meter level).
    [HarmonyPostfix, HarmonyPatch(typeof(CarManager), nameof(CarManager.GetGlobalScoreMult))]
    static void ScoreMult(ref BigNumber __result)
    {
        RawScoreMult = (float)__result.ToDouble();
        if (Plugin.ScoreMult.Value != 1f) __result = __result * Plugin.ScoreMult.Value;
    }

    // Lucky chance in percentage points; lucky rolls add it to their base chance.
    [HarmonyPostfix, HarmonyPatch(typeof(CarManager), nameof(CarManager.GetLuckChance))]
    static void Luck(ref float __result)
    {
        RawLuck = __result;
        __result += Plugin.LuckBonus.Value;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(CardManager), nameof(CardManager.AddDriftMeterProgress))]
    static void DriftFill(ref BigNumber ticks)
    {
        if (Plugin.DriftFill.Value != 1f) ticks = ticks * Plugin.DriftFill.Value;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(CardManager), nameof(CardManager.DriftMeterEnabled), MethodType.Getter)]
    static void DriftMeter(ref bool __result) => __result |= Plugin.ForceDriftMeter.Value;

    // Every gold change (round bonus, gates, cards, card sales) ends up here; spending is negative.
    [HarmonyPrefix, HarmonyPatch(typeof(RunManager), nameof(RunManager.ApplyGoldChange))]
    static void Gold(ref int amount)
    {
        if (amount > 0 && Plugin.GoldMult.Value != 1f) amount = (int)Math.Round(amount * Plugin.GoldMult.Value);
    }

    [HarmonyPostfix, HarmonyPatch(typeof(UIColorManager), nameof(UIColorManager.ChangeUIColor))]
    static void UiColor(int _index) => UiColorIndex = _index;
}

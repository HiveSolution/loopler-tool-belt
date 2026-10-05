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

    // Every car stat from parts, charms and garage upgrades comes out of these two, per StatType.
    [HarmonyPostfix, HarmonyPatch(typeof(CarManager), nameof(CarManager.GetRealValue))]
    static void RealValue(StatType _type, ref float __result)
    {
        // Boost gain reads 0 here; its getter adds the base and temporary boosts on top.
        if (_type == StatType.GlobalBoostGain) return;
        var s = Stats.Get(_type);
        if (s != null) __result = s.Apply(__result);
    }

    [HarmonyPostfix, HarmonyPatch(typeof(CarManager), nameof(CarManager.GetBigRealValue))]
    static void BigRealValue(StatType _type, ref BigNumber __result)
    {
        var s = Stats.Get(_type);
        if (s == null) return;
        float raw = (float)__result.ToDouble();
        s.Apply(raw);
        if (s.Entry.Value != s.Neutral && s.Mode == Mode.Multiply) __result = __result * s.Entry.Value;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(CarManager), nameof(CarManager.GetGlobalBoostGain))]
    static void BoostGain(ref float __result) => __result = Stats.Get(StatType.GlobalBoostGain).Apply(__result);

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
        if (amount <= 0 || Plugin.GoldMult.Value == 1f) return;
        // Gold is an int: stop the balance at int.MaxValue instead of wrapping to negative.
        long current = RunData.instance != null ? (int)RunData.instance.gold : 0;
        double wanted = Math.Round(amount * (double)Plugin.GoldMult.Value);
        amount = (int)Math.Max(0, Math.Min(wanted, int.MaxValue - current));
    }

    [HarmonyPostfix, HarmonyPatch(typeof(UIColorManager), nameof(UIColorManager.ChangeUIColor))]
    static void UiColor(int _index) => UiColorIndex = _index;
}

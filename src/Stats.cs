using System;
using System.Collections.Generic;
using BepInEx.Configuration;

namespace LooplerToolBelt;

enum Mode { Multiply, Add, Divide }

// One tweakable car or run stat from the game's StatType list.
class Stat
{
    public string Tab, Label, Unit;
    public StatType Type;
    public Mode Mode;
    public float[] Steps;
    public ConfigEntry<float> Entry;
    public float Raw, Final;
    public bool Seen;

    public float Neutral => Mode == Mode.Add ? 0f : 1f;

    public float Apply(float raw)
    {
        Raw = raw;
        Seen = true;
        float v = Entry.Value;
        Final = v == Neutral ? raw : Mode switch
        {
            Mode.Add => raw + v,
            Mode.Divide => raw / v,
            _ => raw * v,
        };
        return Final;
    }

    public string Format(float v) => Mode == Mode.Add ? $"+{Num.Short(v)}{Unit}" : $"x{Num.Short(v)}";
}

static class Num
{
    static readonly string[] Suffix = { "", "K", "M", "B", "T" };

    // 1234 -> 1.23K, 5e20 -> 5e20: keeps huge trainer values readable in the overlay.
    public static string Short(double v)
    {
        double a = Math.Abs(v);
        if (a < 10_000) return v.ToString("0.###");
        int i = 0;
        while (a >= 1000 && i < Suffix.Length - 1) { a /= 1000; v /= 1000; i++; }
        return a < 1000 ? v.ToString("0.##") + Suffix[i] : (v * Math.Pow(1000, i)).ToString("0.##e0");
    }
}

// Step values for the overlay's -/+ buttons: fine steps below 10, then 10, 25, 50, 100, 250, ...
static class Ladder
{
    public static readonly float[] Mult = Build(1, 1.25f, 1.5f, 2, 3, 5);
    public static readonly float[] Add = Build(0, 1, 2, 5);

    public static float[] UpTo(float[] ladder, float max) => System.Array.FindAll(ladder, v => v <= max);

    static float[] Build(params float[] belowTen)
    {
        var steps = new List<float>(belowTen);
        for (double decade = 10; decade <= 1e30; decade *= 10)
            foreach (var m in new[] { 1, 2.5, 5 }) steps.Add((float)(decade * m));
        return steps.ToArray();
    }
}

static class Stats
{
    static readonly float[] Mult = Ladder.Mult;

    public static readonly List<Stat> All = new()
    {
        S("Run", "Base loop score", StatType.BaseLoopScore, Mode.Multiply, Mult),
        S("Run", "Drift tick rate", StatType.DriftTrigger, Mode.Divide, Mult),
        // Over 100% the garage would pay you.
        S("Run", "Garage discount", StatType.UpgradeCostReduction, Mode.Add, Ladder.UpTo(Ladder.Add, 100), "%"),
        S("Car", "Top speed", StatType.TopSpeed, Mode.Multiply, Mult),
        S("Car", "Acceleration", StatType.Acceleration, Mode.Multiply, Mult),
        S("Car", "Drift grip", StatType.DriftGrip, Mode.Multiply, Mult),
        S("Car", "Fuel capacity", StatType.FuelCapacity, Mode.Multiply, Mult),
        S("Car", "Fuel economy", StatType.FuelEfficiency, Mode.Multiply, Mult),
        S("Car", "Refuel efficiency", StatType.RefuelEfficiency, Mode.Multiply, Mult),
        S("Car", "Boost gain", StatType.GlobalBoostGain, Mode.Multiply, Mult),
        S("Car", "Boost cap", StatType.BoostCap, Mode.Multiply, Mult),
        S("Car", "Overdrive boost", StatType.OverdriveBoost, Mode.Multiply, Mult),
        // The game stores durability as an int.
        S("Car", "Max durability", StatType.Durability, Mode.Add, Ladder.UpTo(Ladder.Add, 1_000_000)),
    };

    static readonly Dictionary<StatType, Stat> byType = new();

    static Stat S(string tab, string label, StatType type, Mode mode, float[] steps, string unit = "") =>
        new() { Tab = tab, Label = label, Type = type, Mode = mode, Steps = steps, Unit = unit };

    public static void Bind(ConfigFile config)
    {
        foreach (var s in All)
        {
            s.Entry = config.Bind(s.Tab, s.Type.ToString(), s.Neutral, $"{s.Label}: {s.Mode} the game's value");
            byType[s.Type] = s;
        }
    }

    public static Stat Get(StatType type) => byType.TryGetValue(type, out var s) ? s : null;
}

using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using Il2CppInterop.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LooplerToolBelt;

public class TrainerUI : MonoBehaviour
{
    public TrainerUI(IntPtr ptr) : base(ptr) { }

    static readonly float[] ScoreSteps = { 1, 1.5f, 2, 3, 5, 10, 25, 100, 1000 };
    static readonly float[] LuckSteps = { 0, 5, 10, 25, 50, 100 };
    static readonly float[] DriftSteps = { 1, 2, 5, 10, 50 };
    static readonly float[] GoldSteps = { 1, 1.5f, 2, 3, 5, 10, 100 };

    GameObject root;
    TMP_FontAsset font;
    Sprite panelSprite;
    Color primary = new(0.12f, 0.1f, 0.2f, 0.95f), secondary = new(1f, 0.8f, 0.3f, 1f);
    readonly List<Action> refreshers = new();
    float nextRefresh;

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.f1Key.wasPressedThisFrame)
        {
            if (root == null) Build();
            root.SetActive(!root.activeSelf);
        }
        if (root != null && root.activeSelf && Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + 0.25f;
            foreach (var r in refreshers) r();
        }
    }

    void Build()
    {
        GrabGameStyle();
        refreshers.Clear();

        root = new GameObject("ToolBeltCanvas");
        DontDestroyOnLoad(root);
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        root.AddComponent<GraphicRaycaster>();

        var panel = Child(root, "Panel");
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(20, -20);
        Img(panel, primary);
        var v = panel.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(18, 18, 14, 16);
        v.spacing = 8;
        v.childControlWidth = v.childControlHeight = true;
        v.childForceExpandWidth = v.childForceExpandHeight = false;
        var fit = panel.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Text(panel, "TOOL BELT  <size=60%>[F1]</size>", 28, secondary);

        StepRow(panel, "Score mult", Plugin.ScoreMult, ScoreSteps, x => $"x{x:0.##}",
            () => $"game x{Patches.RawScoreMult:0.##} > x{Patches.RawScoreMult * Plugin.ScoreMult.Value:0.##}");
        StepRow(panel, "Lucky chance", Plugin.LuckBonus, LuckSteps, x => $"+{x:0}%",
            () => $"game {Patches.RawLuck:0.#}% > {Patches.RawLuck + Plugin.LuckBonus.Value:0.#}%");
        StepRow(panel, "Drift fill", Plugin.DriftFill, DriftSteps, x => $"x{x:0.##}",
            () => CardManager.instance != null ? $"meter x{CardManager.instance.DriftMeterMultiplier}" : "");
        ToggleRow(panel, "Drift meter", Plugin.ForceDriftMeter);
        StepRow(panel, "Money mult", Plugin.GoldMult, GoldSteps, x => $"x{x:0.##}", () => "");

        Text(panel, "Leaderboard uploads are off while the tool belt is installed", 14, new Color(1, 1, 1, 0.6f));

        foreach (var r in refreshers) r();
    }

    void GrabGameStyle()
    {
        var tmp = FindFirstObjectByType<TextMeshProUGUI>();
        if (tmp != null) font = tmp.font;

        // First button sprite with 9-slice borders works as a panel frame.
        foreach (var b in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var s = b.image != null ? b.image.sprite : null;
            if (s != null && s.border != Vector4.zero) { panelSprite = s; break; }
        }

        var cm = UIColorManager.instance;
        if (cm != null && cm.uiColors != null && Patches.UiColorIndex < cm.uiColors.Length)
        {
            var c = cm.uiColors[Patches.UiColorIndex];
            primary = c.primaryColor;
            secondary = c.secondaryColor;
        }
        Plugin.Logger.LogInfo($"Overlay style: font={(font != null ? font.name : "none")} sprite={(panelSprite != null ? panelSprite.name : "none")} color#{Patches.UiColorIndex}");
    }

    void StepRow(GameObject parent, string label, ConfigEntry<float> entry, float[] steps, Func<float, string> fmt, Func<string> info)
    {
        var row = Row(parent);
        Text(row, label, 20, Color.white, 150);
        Btn(row, "-", () => entry.Value = Step(steps, entry.Value, -1));
        var val = Text(row, "", 20, secondary, 80, TextAlignmentOptions.Center);
        Btn(row, "+", () => entry.Value = Step(steps, entry.Value, +1));
        var inf = Text(row, "", 16, new Color(1, 1, 1, 0.6f), 210);
        refreshers.Add(() => { val.text = fmt(entry.Value); inf.text = info(); });
    }

    void ToggleRow(GameObject parent, string label, ConfigEntry<bool> entry)
    {
        var row = Row(parent);
        Text(row, label, 20, Color.white, 150);
        var b = Btn(row, "", () => entry.Value = !entry.Value, 80 + 36 * 2 + 16);
        var t = b.GetComponentInChildren<TextMeshProUGUI>();
        refreshers.Add(() => t.text = entry.Value ? "FORCED ON" : "CHARM ONLY");
    }

    static float Step(float[] steps, float cur, int dir)
    {
        int i = 0;
        for (int k = 1; k < steps.Length; k++)
            if (Math.Abs(steps[k] - cur) < Math.Abs(steps[i] - cur)) i = k;
        return steps[Math.Clamp(i + dir, 0, steps.Length - 1)];
    }

    // --- tiny uGUI helpers

    static GameObject Child(GameObject parent, string name)
    {
        var go = new GameObject(name);
        go.AddComponent<RectTransform>();
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    GameObject Row(GameObject parent)
    {
        var row = Child(parent, "Row");
        var h = row.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 8;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = h.childControlHeight = true;
        h.childForceExpandWidth = h.childForceExpandHeight = false;
        return row;
    }

    Image Img(GameObject go, Color color)
    {
        var img = go.AddComponent<Image>();
        img.color = color;
        if (panelSprite != null) { img.sprite = panelSprite; img.type = Image.Type.Sliced; }
        return img;
    }

    TextMeshProUGUI Text(GameObject parent, string s, float size, Color color, float width = -1, TextAlignmentOptions align = TextAlignmentOptions.Left)
    {
        var go = Child(parent, "Text");
        var t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.text = s;
        t.raycastTarget = false;
        if (width > 0) go.AddComponent<LayoutElement>().preferredWidth = width;
        return t;
    }

    GameObject Btn(GameObject parent, string label, Action onClick, float width = 36)
    {
        var go = Child(parent, "Button");
        var img = Img(go, secondary);
        var b = go.AddComponent<Button>();
        b.targetGraphic = img;
        b.onClick.AddListener(DelegateSupport.ConvertDelegate<UnityAction>(onClick));
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = width;
        le.preferredHeight = 36;
        var t = Text(go, label, 20, primary, -1, TextAlignmentOptions.Center);
        var trt = t.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        return go;
    }
}

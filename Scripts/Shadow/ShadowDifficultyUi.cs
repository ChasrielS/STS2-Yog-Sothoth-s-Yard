using System.Globalization;
using System.Text.Json.Nodes;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.sts2.Core.Nodes.TopBar;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.RunData;

namespace YogSothothsYard.Scripts.Shadow;

internal static class ShadowDifficultyUi
{
    internal static readonly Color TextColor = new("796770");

    internal static HoverTip HoverTip() => new(NShadowSelector.Loc("ONE_TOOLTIP_TITLE"), NShadowSelector.Loc("ONE_TOOLTIP"));

    internal static (VFlowContainer Flow, Control Box) FitHoverTip(NHoverTipSet tip, int index = 0)
    {
        var flow = tip.GetNode<VFlowContainer>("textHoverTipContainer");
        var box = flow.GetChild<Control>(index);
        var label = box.GetNode<MegaRichTextLabel>("%Description");
        var width = label.GetParsedText().Split('\n').Max(line =>
            MeasureLine(label, line, label.GetThemeFontSize("normal_font_size")));
        label.AutowrapMode = TextServer.AutowrapMode.Off;
        label.CustomMinimumSize = new Vector2(Math.Max(label.CustomMinimumSize.X, width + 12f), 0f);
        box.ResetSize();
        flow.CustomMinimumSize = new Vector2(box.GetCombinedMinimumSize().X, 0f);
        flow.Size = flow.Size.Max(new Vector2(flow.CustomMinimumSize.X, box.Size.Y));
        return (flow, box);
    }

    internal static float MeasureLine(MegaRichTextLabel label, string line, int fontSize) =>
        Mathf.Ceil(label.GetThemeFont("normal_font").GetStringSize(line, HorizontalAlignment.Left, -1, fontSize).X);
}

public sealed class ShadowTopBarPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_top_bar";
    public static ModPatchTarget[] GetTargets() => [new(typeof(NTopBar), nameof(NTopBar.Initialize), [typeof(IRunState)])];

    public static void Postfix(NTopBar __instance, IRunState runState)
    {
        var level = ShadowRun.Level(runState as RunState);
        if (level == 0 || LocalContext.GetMe(runState) is not { } player) return;
        var portrait = __instance.PortraitTip;
        var ascension = portrait.GetNode<TextureRect>("AscensionIcon");
        var badge = (TextureRect)ascension.Duplicate((int)Node.DuplicateFlags.Scripts);
        badge.Name = "ShadowIcon";
        badge.UniqueNameInOwner = false;
        badge.MouseFilter = Control.MouseFilterEnum.Ignore;
        var levelLabel = badge.GetNode<MegaLabel>("AscensionLabel");
        levelLabel.UniqueNameInOwner = false;
        levelLabel.Show();
        levelLabel.AddThemeColorOverride("font_outline_color", new Color("24181f"));
        NShadowSelector.BlackFlame(badge);
        portrait.AddChild(badge);
        badge.AnchorLeft = badge.AnchorRight = 0f;
        badge.OffsetLeft = -ascension.OffsetRight;
        badge.OffsetRight = -ascension.OffsetLeft + portrait.CustomMinimumSize.X;
        badge.Show();
        levelLabel.SetTextAutoSize(level.ToString());
        portrait.FocusMode = Control.FocusModeEnum.All;

        var hpLabel = __instance.Hp.GetNode<MegaLabel>("HpLabel");
        hpLabel.CustomMinimumSize = new Vector2(0f, hpLabel.CustomMinimumSize.Y);
        __instance.Gold.GetNode<Control>("Spacer").Hide();
        var row = new NShadowTopBarErosion { Player = player, Health = __instance.Hp };
        var parent = __instance.Hp.GetParent();
        parent.AddChild(row);
        parent.MoveChild(row, __instance.Hp.GetIndex() + 1);
    }
}

public sealed class ShadowPortraitTipPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_portrait_tip";
    public static ModPatchTarget[] GetTargets() => [new(typeof(NTopBarPortraitTip), "OnFocus", Type.EmptyTypes)];

    public static bool Prefix(NTopBarPortraitTip __instance, IHoverTip? ____hoverTip)
    {
        if (!__instance.HasNode("ShadowIcon")) return true;
        var shadow = ShadowDifficultyUi.HoverTip();
        IHoverTip[] tips = ____hoverTip == null ? [shadow] : [____hoverTip, shadow];
        var tip = NHoverTipSet.CreateAndShow(__instance, tips);
        if (tip != null)
        {
            ShadowDifficultyUi.FitHoverTip(tip, tips.Length - 1);
            tip.SetGlobalPosition(__instance.GlobalPosition + new Vector2(0f, __instance.Size.Y + 20f));
        }
        return false;
    }
}

public partial class NShadowTopBarErosion : HBoxContainer
{
    internal Player Player { get; init; } = null!;
    internal Control Health { get; init; } = null!;
    private TextureRect _icon = null!;
    private MegaLabel _number = null!;
    private int _lastErosion = -1;
    private ShadowSoulstoneKind? _lastStone;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Pass;
        AddThemeConstantOverride("separation", Health.GetThemeConstant("separation"));
        _icon = (TextureRect)Health.GetNode("HpIcon").Duplicate(0);
        _icon.MouseFilter = MouseFilterEnum.Ignore;
        _number = (MegaLabel)Health.GetNode("HpLabel").Duplicate((int)DuplicateFlags.Scripts);
        _number.UniqueNameInOwner = false;
        _number.MouseFilter = MouseFilterEnum.Ignore;
        _number.AddThemeColorOverride("font_color", ShadowDifficultyUi.TextColor);
        _number.AddThemeColorOverride("font_outline_color", new Color("1b151d"));
        AddChild(_icon);
        AddChild(_number);
        MouseEntered += ShowTip;
        MouseExited += HideTip;
        Hidden += HideTip;
        UpdateDisplay();
        Player.RelicObtained += RelicChanged;
        Player.RelicRemoved += RelicChanged;
    }

    public override void _ExitTree()
    {
        HideTip();
        Player.RelicObtained -= RelicChanged;
        Player.RelicRemoved -= RelicChanged;
    }

    private void ShowTip() => NHoverTipSet.CreateAndShow(this, ShadowErosionTips.Create())
        ?.SetGlobalPosition(GlobalPosition + new Vector2(0f, Size.Y + 20f));

    private void HideTip() => NHoverTipSet.Remove(this);

    public override void _Process(double delta)
    {
        if (ShadowRun.Erosion(Player) != _lastErosion) UpdateDisplay();
    }

    private void RelicChanged(RelicModel relic)
    {
        if (relic is IShadowSoulstone) UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        var erosion = ShadowRun.Erosion(Player);
        var kind = (ShadowSoulstoneIcons.Find(Player) as IShadowSoulstone)?.SoulstoneKind;
        _icon.Visible = kind.HasValue;
        if (kind is { } stone && (kind != _lastStone || ShadowSoulstoneIcons.Stage(erosion) != ShadowSoulstoneIcons.Stage(_lastErosion)))
            _icon.Texture = ShadowSoulstoneIcons.Get(stone, erosion);
        _lastStone = kind;
        _lastErosion = erosion;
        _number.SetTextAutoSize($"{erosion}/{ShadowRun.ErosionLimit}");
    }
}

public sealed class ShadowContinueRunPatch : IPatchMethod
{
    private const string RowName = "ShadowErosion";
    private const float StatusGap = 26f;
    private static readonly Type? RunDataRuntime = typeof(RunSavedDataStore).Assembly.GetType("STS2RitsuLib.RunData.RunSavedDataRuntime");
    private static readonly System.Reflection.MethodInfo? TryGetDocument = RunDataRuntime == null
        ? null : AccessTools.Method(RunDataRuntime, "TryGetDocument");
    private static readonly System.Reflection.MethodInfo? TryGetRaw = TryGetDocument?.GetParameters()[1].ParameterType
        .GetElementType() is { } documentType ? AccessTools.Method(documentType, "TryGetRaw") : null;

    public static string PatchId => "yog_sothoths_yard_shadow_continue_run";
    public static ModPatchTarget[] GetTargets() => [new(typeof(NContinueRunInfo), "ShowInfo", [typeof(SerializableRun)])];

    public static void Postfix(NContinueRunInfo __instance, SerializableRun save)
    {
        var health = __instance.GetNode<MegaRichTextLabel>("%HealthLabel");
        var row = health.GetParent<HBoxContainer>();
        var status = row.GetNodeOrNull<HBoxContainer>(RowName);
        if (status != null)
        {
            status.Hide();
            health.CustomMinimumSize = new Vector2(140f, health.CustomMinimumSize.Y);
        }
        if (TryGetDocument == null || TryGetRaw == null || save.Players.Count == 0) return;
        object?[] args = [save, null];
        if (TryGetDocument.Invoke(null, args) is not true || args[1] is not { } document) return;
        var level = ReadSlot(document, "shadow")?["data"]?["Level"]?.GetValue<int>() ?? 0;
        if (level == 0) return;

        var player = save.Players[0];
        var playerKey = player.NetId.ToString(CultureInfo.InvariantCulture);
        var erosion = ReadSlot(document, "shadow_player")?["players"]?[playerKey]?["Erosion"]?.GetValue<int>() ?? 0;
        var stone = player.Relics.Select(relic => relic.Id is { } id ? ModelDb.GetByIdOrNull<RelicModel>(id) : null)
            .OfType<IShadowSoulstone>().FirstOrDefault();
        var difficulty = __instance.GetNode<MegaRichTextLabel>("%AscensionLabel");
        var loc = NShadowSelector.Loc("SAVED_LEVEL");
        loc.Add("Level", level);
        var ascension = save.Ascension > 0 ? difficulty.Text + "    " : "";
        difficulty.SetTextAutoSize(ascension + loc.GetFormattedText());
        difficulty.Show();

        if (status == null)
        {
            status = new HBoxContainer { Name = RowName, MouseFilter = Control.MouseFilterEnum.Ignore };
            status.AddThemeConstantOverride("separation", row.GetThemeConstant("separation"));
            var icon = (TextureRect)row.GetNode("HeartIcon").Duplicate(0);
            icon.Name = "Icon";
            status.AddChild(icon);
            var label = (MegaRichTextLabel)health.Duplicate((int)Node.DuplicateFlags.Scripts);
            label.Name = "Value";
            label.UniqueNameInOwner = false;
            status.AddChild(label);
            row.AddChild(status);
            row.MoveChild(status, health.GetIndex() + 1);
        }
        var stoneIcon = status.GetNode<TextureRect>("Icon");
        stoneIcon.Visible = stone != null;
        if (stone != null) stoneIcon.Texture = ShadowSoulstoneIcons.Get(stone.SoulstoneKind, erosion);
        var value = status.GetNode<MegaRichTextLabel>("Value");
        value.SetTextAutoSize($"[color=#796770]{erosion}/{ShadowRun.ErosionLimit}[/color]");
        SetStatusWidth(health);
        SetStatusWidth(value);
        status.Show();
    }

    private static void SetStatusWidth(MegaRichTextLabel label)
    {
        var width = ShadowDifficultyUi.MeasureLine(label, label.GetParsedText(), label.MaxFontSize);
        label.CustomMinimumSize = new Vector2(width + StatusGap, label.CustomMinimumSize.Y);
    }

    private static JsonObject? ReadSlot(object document, string key)
    {
        object?[] args = [Entry.ModId, key, null];
        return TryGetRaw!.Invoke(document, args) is true ? args[2] as JsonObject : null;
    }
}

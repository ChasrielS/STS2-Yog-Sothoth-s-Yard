using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;

namespace YogSothothsYard.Scripts.Shadow;

public sealed class ShadowCardInspectPatch : IPatchMethod
{
    private static readonly ConditionalWeakTable<NInspectCardScreen, Preview> Previews = new();
    private static readonly MethodInfo Refresh = AccessTools.Method(typeof(NInspectCardScreen), "UpdateCardDisplay");
    public static string PatchId => "yog_sothoths_yard_shadow_card_inspect";
    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(NInspectCardScreen), nameof(NInspectCardScreen.Open), [typeof(List<CardModel>), typeof(int), typeof(bool)]),
        new(typeof(NInspectCardScreen), "SetCard", [typeof(int)]),
        new(typeof(NInspectCardScreen), nameof(NInspectCardScreen.Close), Type.EmptyTypes)
    ];

    public static void Prefix(NInspectCardScreen __instance, MethodBase __originalMethod)
    {
        if (__originalMethod.Name == nameof(NInspectCardScreen.Open))
        {
            var context = ActiveScreenContext.Instance.GetCurrentScreen();
            var allowed = context is NCardLibrary ||
                context is NCapstoneSubmenuStack { Stack: var stack } && stack.Peek() is NCardLibrary ||
                ShadowRun.Level(RunManager.Instance.DebugOnlyGetState()) > 0;
            if (allowed) Previews.GetValue(__instance, Create).Allowed = true;
            else if (Previews.TryGetValue(__instance, out var preview)) preview.Allowed = false;
        }
        if (Previews.TryGetValue(__instance, out var existing)) existing.Rank = null;
    }

    public static void Postfix(NInspectCardScreen __instance, MethodBase __originalMethod, List<CardModel>? ____cards, int ____index)
    {
        if (!Previews.TryGetValue(__instance, out var preview)) return;
        if (__originalMethod.Name == nameof(NInspectCardScreen.Close))
        {
            preview.Row.Hide();
            foreach (var (button, _) in preview.Buttons) NHoverTipSet.Remove(button);
            __instance.FocusNeighborBottom = new NodePath(".");
            return;
        }
        if (__originalMethod.Name != "SetCard" || ____cards == null) return;

        var card = ____cards[____index];
        preview.Rank = ShadowRanks.Of(card);
        preview.Row.Visible = preview.Allowed;
        NTickbox? previous = null;
        __instance.FocusNeighborBottom = new NodePath(".");
        for (var i = 0; i < preview.Buttons.Count; i++)
        {
            var (button, disabledReason) = preview.Buttons[i];
            var supported = ShadowCardRules.TryGet(card, i + 1, out _);
            button.IsTicked = preview.Rank == i + 1;
            var seen = supported && (button.IsTicked || ShadowCardDiscovery.HasSeen(card, i + 1));
            button.SetEnabled(preview.Allowed && seen);
            NHoverTipSet.Remove(button);
            disabledReason.Key = supported ? (seen ? null : "PREVIEW_UNSEEN") : "PREVIEW_UNSUPPORTED";
            if (!button.IsEnabled) continue;
            if (previous == null) __instance.FocusNeighborBottom = __instance.GetPathTo(button);
            else previous.FocusNeighborBottom = previous.GetPathTo(button);
            button.FocusNeighborTop = button.GetPathTo(previous == null ? (Control)__instance : previous);
            button.FocusNeighborBottom = new NodePath(".");
            previous = button;
        }
    }

    internal static List<CardModel>? CreatePreviewList(NInspectCardScreen screen, List<CardModel>? cards, int index)
    {
        if (cards == null || !Previews.TryGetValue(screen, out var preview) || !preview.Allowed ||
            preview.Rank is not { } rank)
            return null;
        var original = cards[index];
        if (rank == ShadowRanks.Of(original)) return null;
        var copy = (CardModel)original.MutableClone();
        ShadowRanks.Set(copy, rank);
        copy.DowngradeInternal();
        for (var i = 0; i < original.CurrentUpgradeLevel; i++) copy.UpgradeInternal();
        copy.FinalizeUpgradeInternal();
        return new List<CardModel>(cards) { [index] = copy };
    }

    private static Preview Create(NInspectCardScreen screen)
    {
        var original = screen.GetNode<NTickbox>("%Upgrade");
        var tooltip = screen.GetNode<Control>("HoverTipRect");
        var rows = new VBoxContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            GrowHorizontal = Control.GrowDirection.End,
            GrowVertical = Control.GrowDirection.Begin,
            Visible = false
        };
        rows.AddThemeConstantOverride("separation", 0);
        screen.AddChild(rows);
        rows.SetAnchorAndOffset(Side.Right, tooltip.AnchorRight, tooltip.OffsetRight + 8f);
        rows.SetAnchorAndOffset(Side.Left, tooltip.AnchorRight, tooltip.OffsetRight + 8f);
        rows.SetAnchorAndOffset(Side.Bottom, original.AnchorBottom, original.OffsetBottom - 8f);
        rows.SetAnchorAndOffset(Side.Top, original.AnchorBottom,
            original.OffsetBottom - 8f - 3 * original.CustomMinimumSize.Y);
        var preview = new Preview(rows);
        for (var i = 1; i <= 3; i++)
        {
            var rank = i;
            var button = CreateTickbox(original, rank);
            rows.AddChild(button);
            var disabledReason = new DisabledReason();
            preview.Buttons.Add((button, disabledReason));
            button.IsTicked = false;
            button.MouseEntered += () =>
            {
                if (disabledReason.Key is not { } key) return;
                var content = new HoverTip(NShadowSelector.Loc(key)) { ShouldOverrideTextOverflow = true };
                if (NHoverTipSet.CreateAndShow(button, content) is not { } tip) return;
                ShadowDifficultyUi.FitHoverTip(tip);
                tip.SetAlignment(button, HoverTip.GetHoverTipAlignment(button));
            };
            button.MouseExited += () => NHoverTipSet.Remove(button);
            button.Toggled += _ =>
            {
                preview.Rank = button.IsTicked ? rank : 0;
                foreach (var (other, _) in preview.Buttons)
                    if (other != button) other.IsTicked = false;
                Refresh.Invoke(screen, null);
            };
        }
        return preview;
    }

    private static NTickbox CreateTickbox(NTickbox original, int rank)
    {
        var tickbox = new NTickbox
        {
            FocusMode = Control.FocusModeEnum.All,
            MouseFilter = Control.MouseFilterEnum.Stop,
            SizeFlagsHorizontal = Control.SizeFlags.Fill
        };
        var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation", original.GetThemeConstant("separation"));
        tickbox.AddChild(row);
        row.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        var visuals = (Control)original.GetNode("TickboxVisuals").Duplicate(0);
        visuals.Material = (Material)visuals.Material.Duplicate();
        row.AddChild(visuals);
        visuals.Owner = tickbox;
        visuals.UniqueNameInOwner = true;

        var label = (MegaLabel)original.GetNode("ShowUpgradeLabel").Duplicate((int)Node.DuplicateFlags.Scripts);
        label.UniqueNameInOwner = false;
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        label.Text = NShadowSelector.Loc("PREVIEW_" + new string('I', rank)).GetFormattedText();
        row.AddChild(label);
        void SizeToContents() => tickbox.CustomMinimumSize = row.GetCombinedMinimumSize().Max(original.CustomMinimumSize);
        row.MinimumSizeChanged += SizeToContents;
        SizeToContents();
        return tickbox;
    }

    private sealed class Preview(VBoxContainer row)
    {
        internal VBoxContainer Row { get; } = row;
        internal List<(NTickbox Button, DisabledReason Reason)> Buttons { get; } = [];
        internal bool Allowed { get; set; }
        internal int? Rank { get; set; }
    }

    private sealed class DisabledReason { internal string? Key { get; set; } }
}

public sealed class ShadowCardInspectDisplayPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_card_inspect_display";
    public static ModPatchTarget[] GetTargets() => [new(typeof(NInspectCardScreen), "UpdateCardDisplay", Type.EmptyTypes)];

    public static void Prefix(NInspectCardScreen __instance, ref List<CardModel>? ____cards, int ____index, out List<CardModel>? __state)
    {
        __state = null;
        if (ShadowCardInspectPatch.CreatePreviewList(__instance, ____cards, ____index) is not { } preview) return;
        __state = ____cards;
        ____cards = preview;
    }

    public static void Finalizer(ref List<CardModel>? ____cards, List<CardModel>? __state)
    {
        if (__state != null) ____cards = __state;
    }
}

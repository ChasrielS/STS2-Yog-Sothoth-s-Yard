using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens;
using STS2RitsuLib.Patching.Models;

namespace YogSothothsYard.Scripts.Patches;

public sealed class CardInspectPreviewLayoutPatch : IPatchMethod
{
    private const float Margin = 14f;
    private const float Spacing = 10f;

    public static string PatchId => "yog_sothoths_yard_card_inspect_preview_layout";

    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(NHoverTipSet), nameof(NHoverTipSet.SetAlignment), [typeof(Control), typeof(HoverTipAlignment)])];

    public static void Postfix(NHoverTipSet __instance, Control node)
    {
        if (node.Name != "HoverTipRect" || node.GetParent() is not NInspectCardScreen screen ||
            screen.GetNode<NCard>("Card").Model is not (QiChengZhuanHe or IHaaLouLingDishCard)) return;

        var container = __instance.GetNode<NHoverTipCardContainer>("cardHoverTipContainer");
        var tips = container.GetChildren().OfType<Control>().ToArray();
        if (tips.Length == 0) return;

        var viewport = __instance.GetViewportRect();
        var card = node.GetGlobalRect();
        var canvasScale = __instance.GetGlobalTransform().Scale.Abs();
        var cell = new Vector2(tips.Max(tip => tip.Size.X), tips.Max(tip => tip.Size.Y));
        var columns = tips.Length > 3 ? 2 : 1;
        var rows = (tips.Length + columns - 1) / columns;
        var size = new Vector2(columns * (cell.X + Spacing) - Spacing, rows * (cell.Y + Spacing) - Spacing);
        var right = card.Position.X - Margin;
        var arrow = screen.GetNode<Control>("LeftArrow");
        if (arrow.Visible) right = Math.Min(right, arrow.GetGlobalRect().Position.X - Margin);
        var top = viewport.Position.Y + Margin;
        var bottom = viewport.End.Y - Margin;
        var scale = Math.Min(1f, Math.Min(
            (right - viewport.Position.X - Margin) / (size.X * canvasScale.X),
            (bottom - top) / (size.Y * canvasScale.Y)));
        if (scale <= 0f) return;

        for (var i = 0; i < tips.Length; i++)
            tips[i].Position = new Vector2((i % columns) * (cell.X + Spacing), (i / columns) * (cell.Y + Spacing));
        container.Size = size;
        container.Scale = Vector2.One * scale;
        var visibleSize = size * canvasScale * scale;
        container.GlobalPosition = new Vector2(right - visibleSize.X,
            Mathf.Clamp(card.Position.Y, top, Math.Max(top, bottom - visibleSize.Y)));
    }
}

using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens;
using STS2RitsuLib.Patching.Models;

namespace YogSothothsYard.Scripts.Patches;

public sealed class KnifeFormInspectLayoutPatch : IPatchMethod
{
    private const float Margin = 14f;
    private const float Spacing = 10f;

    public static string PatchId => "yog_sothoths_yard_knife_form_inspect_layout";

    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(NHoverTipSet), nameof(NHoverTipSet.SetAlignment), [typeof(Control), typeof(HoverTipAlignment)])];

    public static void Postfix(NHoverTipSet __instance, Control node)
    {
        if (node.Name != "HoverTipRect" || node.GetParent() is not NInspectCardScreen screen ||
            screen.GetNode<NCard>("Card").Model is not QiChengZhuanHe) return;

        var container = __instance.GetNode<NHoverTipCardContainer>("cardHoverTipContainer");
        var tips = container.GetChildren().OfType<Control>().ToArray();
        if (tips.Length != 4) return;

        var viewport = __instance.GetViewportRect();
        var card = node.GetGlobalRect();
        var canvasScale = __instance.GetGlobalTransform().Scale.Abs();
        var cell = new Vector2(tips.Max(tip => tip.Size.X), tips.Max(tip => tip.Size.Y));
        var width = 2 * cell.X + Spacing;
        var right = card.Position.X - Margin;
        var centerY = card.GetCenter().Y;
        // Split the two rows around the native left arrow.
        var arrowGap = screen.GetNode<Control>("LeftArrow").GetGlobalRect().Size.Y + Margin * 2;
        var top = Math.Max(card.Position.Y, viewport.Position.Y + Margin);
        var bottom = Math.Min(card.End.Y, viewport.End.Y - Margin);
        var rowRoom = Math.Min(centerY - top, bottom - centerY) - arrowGap * 0.5f;
        var scale = Math.Min(1f, Math.Min(
            (right - viewport.Position.X - Margin) / (width * canvasScale.X),
            rowRoom / (cell.Y * canvasScale.Y)));
        if (scale <= 0f) return;

        var lowerRow = cell.Y + arrowGap / (scale * canvasScale.Y);
        for (var i = 0; i < tips.Length; i++)
            tips[i].Position = new Vector2((i % 2) * (cell.X + Spacing), (i / 2) * lowerRow);

        container.Size = new Vector2(width, cell.Y + lowerRow);
        container.Scale = Vector2.One * scale;
        container.GlobalPosition = new Vector2(right - width * scale * canvasScale.X,
            centerY - arrowGap * 0.5f - cell.Y * scale * canvasScale.Y);
    }
}

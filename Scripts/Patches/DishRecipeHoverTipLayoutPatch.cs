using Godot;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using STS2RitsuLib.Patching.Models;

namespace YogSothothsYard.Scripts.Patches;

public sealed class DishRecipeHoverTipLayoutPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_dish_recipe_hover_tip_layout";

    private const int MaxRowsPerColumn = 3;
    private const float Spacing = 4f;
    private const float Margin = 12f;

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<NHoverTipSet>(nameof(NHoverTipSet.SetAlignmentForCardHolder))
    ];

    public static void Postfix(NHoverTipSet __instance, NCardHolder holder)
    {
        if (holder.CardModel is not (HaaLouLingCard or IHaaLouLingDishCard or IHaaLouLingIngredientCard)) return;

        // 等文字提示的容器完成排版后，再为卡牌预览分配剩余空间。
        Callable.From(() =>
        {
            if (GodotObject.IsInstanceValid(__instance) && !__instance.IsQueuedForDeletion() &&
                GodotObject.IsInstanceValid(holder) && !holder.IsQueuedForDeletion())
                Layout(__instance, holder);
        }).CallDeferred();
    }

    private static void Layout(NHoverTipSet tipSet, NCardHolder holder)
    {
        var container = tipSet.GetNode<NHoverTipCardContainer>("cardHoverTipContainer");
        var tips = container.GetChildren().OfType<Control>().ToArray();
        if (tips.Length == 0) return;

        var viewport = tipSet.GetViewportRect().Size;
        var rows = Math.Min(MaxRowsPerColumn, tips.Length);
        var columnCount = Mathf.CeilToInt((float)tips.Length / rows);
        var columnWidth = tips.Max(tip => tip.Size.X);
        var rowHeight = tips.Max(tip => tip.Size.Y);
        var newSize = new Vector2(
            columnCount * columnWidth + (columnCount - 1) * Spacing,
            rows * rowHeight + (rows - 1) * Spacing
        );
        var canvasScale = tipSet.GetGlobalTransform().Scale.Abs();
        var left = Margin;
        var right = viewport.X - Margin;
        var initialRect = container.GetGlobalRect();
        var cardsOnLeft = initialRect.GetCenter().X < holder.Hitbox.GetGlobalRect().GetCenter().X;
        var text = tipSet.GetNode<VFlowContainer>("textHoverTipContainer");
        var textTips = text.GetChildren().OfType<Control>().Where(tip => tip.Visible).ToArray();
        if (textTips.Length > 0)
        {
            var textRect = text.GetGlobalRect();
            foreach (var tip in textTips) textRect = textRect.Merge(tip.GetGlobalRect());
            var textPosition = new Vector2(
                Mathf.Clamp(textRect.Position.X, Margin, Math.Max(Margin, viewport.X - Margin - textRect.Size.X)),
                Mathf.Clamp(textRect.Position.Y, Margin, Math.Max(Margin, viewport.Y - Margin - textRect.Size.Y))
            );
            text.GlobalPosition += textPosition - textRect.Position;
            textRect.Position = textPosition;

            var leftSpace = Math.Max(0f, textRect.Position.X - Margin * 2);
            var rightSpace = Math.Max(0f, viewport.X - textRect.End.X - Margin * 2);
            cardsOnLeft = initialRect.GetCenter().X < textRect.GetCenter().X;
            if ((cardsOnLeft ? leftSpace : rightSpace) < newSize.X * canvasScale.X)
                cardsOnLeft = leftSpace > rightSpace;
            if (cardsOnLeft) right = textRect.Position.X - Margin;
            else left = textRect.End.X + Margin;
        }

        for (var index = 0; index < tips.Length; index++)
        {
            var column = index / rows;
            if (cardsOnLeft) column = columnCount - 1 - column;
            tips[index].Position = new Vector2(
                column * (columnWidth + Spacing),
                (index % rows) * (rowHeight + Spacing)
            );
        }

        var scale = Math.Min(1f, Math.Min(
            Math.Max(1f, right - left) / (newSize.X * canvasScale.X),
            (viewport.Y - Margin * 2) / (newSize.Y * canvasScale.Y)
        ));
        var visibleSize = newSize * canvasScale * scale;
        container.Size = newSize;
        container.Scale = Vector2.One * scale;
        container.GlobalPosition = new Vector2(
            Mathf.Clamp(initialRect.Position.X, left, Math.Max(left, right - visibleSize.X)),
            Mathf.Clamp(holder.Hitbox.GlobalPosition.Y, Margin, Math.Max(Margin, viewport.Y - Margin - visibleSize.Y))
        );
    }
}

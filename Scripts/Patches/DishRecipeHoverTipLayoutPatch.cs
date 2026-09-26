using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using STS2RitsuLib.Patching.Models;

namespace YogSothothsYard.Scripts.Patches;

public sealed class DishRecipeHoverTipLayoutPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_dish_recipe_hover_tip_layout";

    private const int MaxRowsPerColumn = 3;
    private const float Spacing = 4f;

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<NHoverTipSet>(nameof(NHoverTipSet.SetAlignmentForCardHolder))
    ];

    public static void Postfix(NHoverTipSet __instance, NCardHolder holder)
    {
        if (holder.CardModel is not IHaaLouLingDishCard)
        {
            return;
        }

        var container = __instance.GetNodeOrNull<NHoverTipCardContainer>("cardHoverTipContainer");
        if (container == null)
        {
            return;
        }

        var tips = container.GetChildren().OfType<Control>().ToArray();
        if (tips.Length <= MaxRowsPerColumn)
        {
            return;
        }

        var columnCount = Mathf.CeilToInt((float)tips.Length / MaxRowsPerColumn);
        var columnWidth = tips.Max(tip => tip.Size.X);
        var rowHeight = tips.Max(tip => tip.Size.Y);
        var newSize = new Vector2(
            columnCount * columnWidth + (columnCount - 1) * Spacing,
            MaxRowsPerColumn * rowHeight + (MaxRowsPerColumn - 1) * Spacing
        );

        var holderCenterX = holder.Hitbox.GlobalPosition.X + holder.Hitbox.Size.X * holder.Hitbox.Scale.X * 0.5f;
        var tipsAreLeftOfCard = container.GlobalPosition.X + container.Size.X * 0.5f < holderCenterX;

        for (var index = 0; index < tips.Length; index++)
        {
            var logicalColumn = index / MaxRowsPerColumn;
            var row = index % MaxRowsPerColumn;
            var visualColumn = tipsAreLeftOfCard
                ? columnCount - 1 - logicalColumn
                : logicalColumn;

            tips[index].Position = new Vector2(
                visualColumn * (columnWidth + Spacing),
                row * (rowHeight + Spacing)
            );
        }

        var oldNearEdgeX = tipsAreLeftOfCard
            ? container.GlobalPosition.X + container.Size.X
            : container.GlobalPosition.X;
        var viewportSize = __instance.GetViewportRect().Size;
        var newPosition = new Vector2(
            tipsAreLeftOfCard ? oldNearEdgeX - newSize.X : oldNearEdgeX,
            holder.Hitbox.GlobalPosition.Y
        );

        newPosition.X = Mathf.Clamp(newPosition.X, 0f, Mathf.Max(0f, viewportSize.X - newSize.X));
        newPosition.Y = Mathf.Clamp(newPosition.Y, 0f, Mathf.Max(0f, viewportSize.Y - newSize.Y));
        container.Size = newSize;
        container.GlobalPosition = newPosition;
    }
}

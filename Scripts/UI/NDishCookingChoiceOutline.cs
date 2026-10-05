using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using STS2RitsuLib.Scaffolding.Godot.NodeAttachments;
using YogSothothsYard.Scripts.Card.HaaLouLingDish;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts.UI;

public partial class NDishCookingChoiceOutline : Node
{
    private NSimpleCardSelectScreen _screen = null!;
    private NCardGrid _grid = null!;
    private readonly Dictionary<NCard, TextureRect> _outlines = [];

    public static void Register() =>
        ModNodeAttachmentRegistry.For(Entry.ModId)
            .RegisterReadyChild<NSimpleCardSelectScreen, NDishCookingChoiceOutline>(
                "dish_cooking_choice_outline",
                static screen => new NDishCookingChoiceOutline { _screen = screen });

    public override void _Ready() => _grid = _screen.GetNode<NCardGrid>("%CardGrid");

    public override void _Process(double delta)
    {
        foreach (var outline in _outlines.Values)
            if (GodotObject.IsInstanceValid(outline)) outline.Visible = false;

        foreach (var holder in _grid.CurrentlyDisplayedCardHolders)
        {
            if (!holder.Visible || holder.CardNode is not { } node ||
                holder.CardModel is not HaaLouLingDishCard { IsCookingChoice: true } card) continue;

            var upgraded = HaaLouLingCookingService.HasPendingDishUpgrade(card.Owner);
            if (card.IsUpgraded != upgraded)
            {
                if (upgraded) card.UpgradeInternal();
                else card.DowngradeInternal();
                node.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
            }

            var state = HaaLouLingCookingService.GetState(card.Owner);
            var recipe = HaaLouLingCookingRecipes.Get(card.Id)!;
            if (state.Slots.Count < state.Capacity || recipe.GetCookingTurns(upgraded) <= 0) continue;

            var highlight = node.CardHighlight;
            // 原版高亮显示已选中的卡牌，红边只提示尚未选中的菜肴。
            if (((ShaderMaterial)highlight.Material).GetShaderParameter("width").AsSingle() > 0.001f) continue;

            if (!_outlines.TryGetValue(node, out var warning))
            {
                warning = (TextureRect)highlight.Duplicate(0);
                warning.Name = "CookingUnavailableOutline";
                warning.UniqueNameInOwner = false;
                warning.Modulate = NCardHighlight.red;
                warning.Material = (ShaderMaterial)highlight.Material.Duplicate();
                ((ShaderMaterial)warning.Material).SetShaderParameter("width", 0.075f);
                warning.MouseFilter = Control.MouseFilterEnum.Ignore;
                var parent = highlight.GetParent();
                parent.AddChild(warning);
                parent.MoveChild(warning, highlight.GetIndex());
                _outlines.Add(node, warning);
            }
            warning.Visible = true;
        }
    }

    public override void _ExitTree()
    {
        foreach (var outline in _outlines.Values)
            if (GodotObject.IsInstanceValid(outline)) outline.QueueFree();
        _outlines.Clear();
    }
}

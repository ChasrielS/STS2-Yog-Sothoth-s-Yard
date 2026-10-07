using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using STS2RitsuLib.Scaffolding.Godot.NodeAttachments;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts.UI;

public partial class NIngredientCountDisplay : Control
{
    private NCardGrid _grid = null!;
    private CardPile? _pile;
    private bool _groupCards = true;
    private NPeekButton? _peekButton;
    private Dictionary<ModelId, int> _quantities = [];
    private readonly List<Label> _labels = [];

    public static void Register() =>
        ModNodeAttachmentRegistry.For(Entry.ModId)
            .RegisterReadyChild<NSimpleCardSelectScreen, NIngredientCountDisplay>(
                "ingredient_kind_choice_counts",
                static screen => new NIngredientCountDisplay
                {
                    _grid = screen.GetNode<NCardGrid>("%CardGrid"),
                    _peekButton = screen.GetNode<NPeekButton>("%PeekButton"),
                    _groupCards = false,
                    MouseFilter = MouseFilterEnum.Ignore
                }, new NodeAttachmentOptions
                {
                    AttachParentSelector = parent => parent.GetNode<NCardGrid>("%CardGrid")
                        .GetNode<Control>("ScrollContainer")
                });

    public static NIngredientCountDisplay Create(NCardGrid grid, CardPile pile) => new()
    {
        _grid = grid,
        _pile = pile,
        MouseFilter = MouseFilterEnum.Ignore
    };

    public override void _Ready()
    {
        if (_pile != null)
        {
            _pile.ContentsChanged += Refresh;
            Refresh();
        }
        else _peekButton!.AddTargets(this);
    }

    public override void _ExitTree()
    {
        if (_pile != null) _pile.ContentsChanged -= Refresh;
    }

    private void Refresh()
    {
        var groups = _pile!.Cards.GroupBy(card => card.Id).ToList();
        _quantities = groups.ToDictionary(group => group.Key, group => group.Count());
        if (_groupCards)
            _grid.SetCards(HaaLouLingCardSortOrder.SortIngredients(groups.Select(group => group.First())),
                _pile.Type, [SortingOrders.Ascending]);
    }

    public override void _Process(double delta)
    {
        if (_pile == null)
        {
            var choice = _grid.CurrentlyDisplayedCards.OfType<HaaLouLingIngredientCard>()
                .FirstOrDefault(card => card.IsKindChoice);
            if (choice == null) return;
            _pile = Entry.FoodPile.GetPile(choice.Owner);
            _pile.ContentsChanged += Refresh;
            Refresh();
        }
        var index = 0;
        var toLocal = GetGlobalTransform().AffineInverse();
        foreach (var holder in _grid.CurrentlyDisplayedCardHolders)
        {
            if (!holder.IsVisibleInTree()) continue;
            if (index == _labels.Count)
            {
                var label = new Label
                {
                    Size = new Vector2(200f, 40f),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    MouseFilter = MouseFilterEnum.Ignore
                };
                label.AddThemeColorOverride("font_color", Colors.White);
                label.AddThemeColorOverride("font_outline_color", Colors.Black);
                label.AddThemeConstantOverride("outline_size", 4);
                label.AddThemeFontSizeOverride("font_size", 30);
                AddChild(label);
                _labels.Add(label);
            }
            var countLabel = _labels[index++];
            countLabel.Visible = true;
            countLabel.Text = $"x{_quantities.GetValueOrDefault(holder.CardModel.Id)}";
            var holderTransform = toLocal * holder.GetGlobalTransform();
            countLabel.Scale = holderTransform.Scale;
            countLabel.Position = holderTransform * new Vector2(-100f, 214f);
        }
        for (; index < _labels.Count; index++) _labels[index].Visible = false;
    }
}

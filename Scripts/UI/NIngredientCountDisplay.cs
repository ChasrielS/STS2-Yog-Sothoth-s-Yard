using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts.UI;

public partial class NIngredientCountDisplay : Control
{
    private NCardGrid _grid = null!;
    private CardPile _pile = null!;
    private Dictionary<ModelId, int> _quantities = [];
    private readonly List<Label> _labels = [];

    public static NIngredientCountDisplay Create(NCardGrid grid, CardPile pile) => new()
    {
        _grid = grid,
        _pile = pile,
        MouseFilter = MouseFilterEnum.Ignore
    };

    public override void _Ready()
    {
        _pile.ContentsChanged += Refresh;
        Refresh();
    }

    public override void _ExitTree() => _pile.ContentsChanged -= Refresh;

    private void Refresh()
    {
        var groups = _pile.Cards.GroupBy(card => card.Id).ToList();
        _quantities = groups.ToDictionary(group => group.Key, group => group.Count());
        _grid.SetCards(HaaLouLingCardSortOrder.SortIngredients(groups.Select(group => group.First())),
            _pile.Type, [SortingOrders.Ascending]);
    }

    public override void _Process(double delta)
    {
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
            countLabel.Text = $"x{_quantities[holder.CardModel.Id]}";
            var holderTransform = toLocal * holder.GetGlobalTransform();
            countLabel.Scale = holderTransform.Scale;
            countLabel.Position = holderTransform * new Vector2(-100f, 214f);
        }
        for (; index < _labels.Count; index++) _labels[index].Visible = false;
    }
}

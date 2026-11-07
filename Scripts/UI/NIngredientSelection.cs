using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using STS2RitsuLib.Patching;
using STS2RitsuLib.Scaffolding.Godot.NodeAttachments;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts.UI;

public partial class NIngredientSelection : Node
{
    private const string AttachmentId = "ingredient_quantity_selection";
    private static readonly System.Reflection.FieldInfo CardsField =
        PrivateAccess.Field<NCardGridSelectionScreen>("_cards");
    private NSimpleCardSelectScreen _screen = null!;
    private NCardGrid _grid = null!;
    private NConfirmButton _confirm = null!;
    private Label _totalLabel = null!;
    private Dictionary<ModelId, List<CardModel>> _groups = [];
    private readonly Dictionary<ModelId, int> _selectedCounts = [];

    public static void Register() => ModNodeAttachmentRegistry.For(Entry.ModId)
        .RegisterReadyChild<NSimpleCardSelectScreen, NIngredientSelection>(AttachmentId,
            static screen => new NIngredientSelection { _screen = screen });

    public static bool TryGet(NSimpleCardSelectScreen screen, out NIngredientSelection selection) =>
        ModNodeAttachmentRegistry.For(Entry.ModId)
            .TryGetAttached<NSimpleCardSelectScreen, NIngredientSelection>(screen, AttachmentId, out selection!) &&
        selection._groups.Count > 0;

    public override void _Ready()
    {
        var cards = (IReadOnlyList<CardModel>)CardsField.GetValue(_screen)!;
        if (cards.FirstOrDefault() is not HaaLouLingIngredientCard first) return;
        if (cards.Any(card => card is not HaaLouLingIngredientCard)) return;
        var pile = Entry.FoodPile.GetPile(first.Owner);
        var owned = pile.Cards.ToHashSet();
        _grid = _screen.GetNode<NCardGrid>("%CardGrid");
        if (cards.Any(card => !owned.Contains(card)))
        {
            var ownedCounts = NIngredientCountDisplay.Create(_grid, pile, groupCards: false);
            _grid.GetNode<Control>("ScrollContainer").AddChild(ownedCounts);
            _screen.GetNode<NPeekButton>("%PeekButton").AddTargets(ownedCounts);
            return;
        }
        _groups = cards.GroupBy(card => card.Id).ToDictionary(group => group.Key, group => group.ToList());
        _confirm = _screen.GetNode<NConfirmButton>("%Confirm");
        _grid.SetCards(HaaLouLingCardSortOrder.SortIngredients(_groups.Values.Select(group => group[0])),
            PileType.None, [SortingOrders.Ascending]);
        var counts = NIngredientCountDisplay.Create(_grid, pile, id => _selectedCounts.GetValueOrDefault(id));
        _grid.GetNode<Control>("ScrollContainer").AddChild(counts);

        var help = CreateHint(left: true, out var helpLabel);
        helpLabel.Text = new LocString("gameplay_ui", "YOG_SOTHOTH_S_YARD_INGREDIENT_SELECTION_CONTROLS").GetFormattedText();
        var total = CreateHint(left: false, out _totalLabel);
        _screen.GetNode<NPeekButton>("%PeekButton").AddTargets(help, total, counts);
        RefreshTotal(0);
    }

    public void Select(CardModel card, HashSet<CardModel> selected, CardSelectorPrefs prefs)
    {
        var group = _groups[card.Id];
        var current = _selectedCounts.GetValueOrDefault(card.Id);
        var maximum = Math.Min(group.Count, prefs.MaxSelect - selected.Count + current);
        var wanted = Input.IsKeyPressed(Key.Ctrl) ? current + 1 :
            Input.IsKeyPressed(Key.Shift) ? (group.Count + 1) / 2 : current == maximum ? 0 : maximum;
        var quantity = Math.Clamp(wanted, 0, maximum);
        selected.ExceptWith(group);
        selected.UnionWith(group.Take(quantity));
        _selectedCounts[card.Id] = quantity;
        if (quantity > 0) _grid.HighlightCard(group[0]);
        else _grid.UnhighlightCard(group[0]);
        if (selected.Count >= prefs.MinSelect && selected.Count <= prefs.MaxSelect) _confirm.Enable();
        else _confirm.Disable();
        RefreshTotal(selected.Count);
    }

    private void RefreshTotal(int count)
    {
        var text = new LocString("gameplay_ui", "YOG_SOTHOTH_S_YARD_INGREDIENT_SELECTION_TOTAL");
        text.Add("Amount", count);
        _totalLabel.Text = text.GetFormattedText();
    }

    private Control CreateHint(bool left, out Label label)
    {
        var panel = new MarginContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        _screen.AddChild(panel);
        panel.SetAnchorsAndOffsetsPreset(left ? Control.LayoutPreset.BottomLeft : Control.LayoutPreset.BottomRight);
        panel.OffsetLeft = left ? 30f : -24f;
        panel.OffsetRight = left ? 350f : -24f;
        panel.OffsetTop = left ? -105f : -20f;
        panel.OffsetBottom = -20f;
        if (!left)
        {
            panel.GrowHorizontal = Control.GrowDirection.Begin;
            panel.GrowVertical = Control.GrowDirection.Begin;
        }
        panel.AddChild(new ColorRect { Color = new Color(0f, 0f, 0f, 0.753f), MouseFilter = Control.MouseFilterEnum.Ignore });
        var margin = new MarginContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        foreach (var edge in new[] { "left", "right" }) margin.AddThemeConstantOverride("margin_" + edge, 16);
        foreach (var edge in new[] { "top", "bottom" }) margin.AddThemeConstantOverride("margin_" + edge, 6);
        panel.AddChild(margin);
        label = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.Off,
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter, MouseFilter = Control.MouseFilterEnum.Ignore
        };
        label.AddThemeFontOverride("font", GD.Load<Font>("res://themes/kreon_regular_shared.tres"));
        label.AddThemeFontSizeOverride("font_size", 24);
        label.AddThemeColorOverride("font_color", new Color(1f, 0.965f, 0.886f));
        margin.AddChild(label);
        if (!left) panel.MinimumSizeChanged += () =>
        {
            var size = panel.GetCombinedMinimumSize();
            panel.OffsetLeft = -24f - size.X;
            panel.OffsetTop = -20f - size.Y;
        };
        return panel;
    }
}

using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using STS2RitsuLib.Scaffolding.Godot.NodeAttachments;

namespace YogSothothsYard.Scripts.UI;

public partial class NMultiplayerIngredientCount : Control
{
    private NMultiplayerPlayerState _state = null!;
    private Control _cardCount = null!;
    private Label _count = null!;

    public static void Register() =>
        ModNodeAttachmentRegistry.For(Entry.ModId)
            .RegisterReadyChild<NMultiplayerPlayerState, NMultiplayerIngredientCount>(
                "ingredient_count",
                static state => new NMultiplayerIngredientCount { _state = state },
                new NodeAttachmentOptions
                {
                    AttachParentSelector = state => state.GetNode<HBoxContainer>("TopInfoContainer"),
                    InsertAfterName = "CardCountContainer"
                });

    public override void _Ready()
    {
        Visible = false;
        MouseFilter = MouseFilterEnum.Ignore;
        if (_state.Player.Character is not HaaLouLing) { SetProcess(false); return; }
        CustomMinimumSize = new Vector2(32f, 28f);
        _cardCount = _state.GetNode<Control>("%CardCountContainer");
        var sourceLabel = _cardCount.GetNode<Label>("CardCount");
        var image = new TextureRect
        {
            Texture = GD.Load<Texture2D>(Entry.FoodPileIconPath),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(image);
        image.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _count = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _count.AddThemeFontOverride("font", sourceLabel.GetThemeFont("font"));
        _count.AddThemeFontSizeOverride("font_size", sourceLabel.GetThemeFontSize("font_size"));
        _count.AddThemeColorOverride("font_color", sourceLabel.GetThemeColor("font_color"));
        _count.AddThemeColorOverride("font_outline_color", sourceLabel.GetThemeColor("font_outline_color"));
        _count.AddThemeConstantOverride("outline_size", 4);
        AddChild(_count);
        _count.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
    }

    public override void _Process(double delta)
    {
        Visible = _cardCount.Visible && _state.Player.PlayerCombatState != null;
        if (Visible) _count.Text = Entry.FoodPile.GetPile(_state.Player).Cards.Count.ToString();
    }
}

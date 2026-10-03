using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using STS2RitsuLib.Scaffolding.Godot.NodeAttachments;
using YogSothothsYard.Scripts.Card.HaaLouLingDish;

namespace YogSothothsYard.Scripts.UI;

public partial class NDishEffectChoicePrompt : Label
{
    private NChooseACardSelectionScreen _screen = null!;

    public static void Register() =>
        ModNodeAttachmentRegistry.For(Entry.ModId)
            .RegisterReadyChild<NChooseACardSelectionScreen, NDishEffectChoicePrompt>(
                "dish_effect_choice_prompt",
                static screen => new NDishEffectChoicePrompt { _screen = screen });

    public override void _Ready()
    {
        Visible = _screen.GetNode<Control>("CardRow").GetChildren().OfType<NGridCardHolder>()
            .Any(holder => holder.CardModel is HaaLouLingDishCard { IsEffectChoice: true });
        if (!Visible) return;

        Text = new LocString("gameplay_ui", "YOG_SOTHOTH_S_YARD_DISH_EFFECT_CHOICE_PROMPT").GetFormattedText();
        SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide);
        OffsetTop = -100f;
        OffsetBottom = -60f;
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;
        MouseFilter = MouseFilterEnum.Ignore;
        AddThemeFontOverride("font", _screen.GetNode<Label>("InspectPrompt/HBoxContainer/Label").GetThemeFont("font"));
        AddThemeFontSizeOverride("font_size", 24);
        AddThemeColorOverride("font_color", new Color(0.992f, 0.957f, 0.89f));
        AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.5f));
        AddThemeConstantOverride("outline_size", 8);
        _screen.GetNode<NPeekButton>("%PeekButton").AddTargets(this);
    }
}

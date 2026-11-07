using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace YogSothothsYard.Scripts;

[RegisterOwnedCardKeyword(
    nameof(Ingredient),
    CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription
)]
[RegisterOwnedCardKeyword(nameof(AncientGlimpse), IconPath = "res://images/powers/gu_shen_de_yi_pie_power_icon.png")]
[RegisterOwnedCardKeyword(nameof(SpecialIngredient), CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
[RegisterOwnedCardKeyword("DaoShi", IconPath = "res://images/powers/dao_shi_power_icon.png")]
[RegisterOwnedCardKeyword("GuiYuan", IconPath = "res://images/powers/gui_yuan_power_icon.png")]
[RegisterOwnedCardKeyword("GuiYuanPlus", IconPath = "res://images/powers/gui_yuan_power_icon.png")]
public sealed class HaaLouLingKeywords
{
    public static readonly CardKeyword Ingredient = ModContentRegistry
        .GetQualifiedKeywordId(Entry.ModId, nameof(Ingredient))
        .GetModCardKeyword();

    public static readonly CardKeyword AncientGlimpse = ModContentRegistry
        .GetQualifiedKeywordId(Entry.ModId, nameof(AncientGlimpse))
        .GetModCardKeyword();

    public static string KeywordId(string name) => ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, name);
    public static readonly CardKeyword SpecialIngredient = KeywordId(nameof(SpecialIngredient)).GetModCardKeyword();

    public static IHoverTip ImmediateCookingTip => new HoverTip(
        new LocString("static_hover_tips", "YOG_SOTHOTH_S_YARD_IMMEDIATE_COOKING.title"),
        new LocString("static_hover_tips", "YOG_SOTHOTH_S_YARD_IMMEDIATE_COOKING.description"));
}

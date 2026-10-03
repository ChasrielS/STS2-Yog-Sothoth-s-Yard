using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace YogSothothsYard.Scripts;

[RegisterOwnedCardKeyword(
    nameof(Ingredient),
    CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription
)]
[RegisterOwnedCardKeyword(nameof(AncientGlimpse), IconPath = "res://images/powers/gu_shen_de_yi_pie_power_icon.png")]
[RegisterOwnedCardKeyword(nameof(QiShi), CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
[RegisterOwnedCardKeyword(nameof(ChengShi), CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
[RegisterOwnedCardKeyword(nameof(ZhuanShi), CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
[RegisterOwnedCardKeyword(nameof(HeShi), CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
[RegisterOwnedCardKeyword("DaoShi", IconPath = "res://images/powers/dao_shi_power_icon.png")]
[RegisterOwnedCardKeyword("DaoShu")]
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
    public static readonly CardKeyword QiShi = KeywordId(nameof(QiShi)).GetModCardKeyword();
    public static readonly CardKeyword ChengShi = KeywordId(nameof(ChengShi)).GetModCardKeyword();
    public static readonly CardKeyword ZhuanShi = KeywordId(nameof(ZhuanShi)).GetModCardKeyword();
    public static readonly CardKeyword HeShi = KeywordId(nameof(HeShi)).GetModCardKeyword();
}

using MegaCrit.Sts2.Core.Entities.Relics;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterRelic(typeof(HaaLouLingRelicPool))]
public sealed class DaNuoGuiMian : HaaLouLingRelic
{
    protected override string ImageName => "da_nuo_gui_mian";
    protected override IEnumerable<string> RegisteredKeywordIds =>
        [HaaLouLingKeywords.KeywordId("GuiYuan"), HaaLouLingKeywords.KeywordId("GuiYuanPlus")];
    public override RelicRarity Rarity => RelicRarity.Shop;
}

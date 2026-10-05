using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterRelic(typeof(HaaLouLingRelicPool))]
public sealed class JiuShiZhenLingFu : HaaLouLingRelic
{
    protected override string ImageName => "jiu_shi_zhen_ling_fu";
    protected override IEnumerable<string> RegisteredKeywordIds => [HaaLouLingKeywords.KeywordId("GuiYuan")];
    public override RelicRarity Rarity => RelicRarity.Common;
    public override async Task BeforeCombatStart()
    {
        Flash();
        await HaaLouLingCombatRules.GiveGuiYuanToEnemies(new ThrowingPlayerChoiceContext(), Owner, 10);
    }
}

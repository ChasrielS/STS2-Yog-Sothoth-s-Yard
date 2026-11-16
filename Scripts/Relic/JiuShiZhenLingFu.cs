using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterRelic(typeof(HaaLouLingRelicPool))]
public sealed class JiuShiZhenLingFu : HaaLouLingRelic
{
    protected override string ImageName => "jiu_shi_zhen_ling_fu";
    protected override IEnumerable<string> RegisteredKeywordIds => [HaaLouLingKeywords.KeywordId("GuiYuan")];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amount", 10)];
    public override RelicRarity Rarity => RelicRarity.Common;
    public override async Task BeforeCombatStart()
    {
        Flash();
        await HaaLouLingCombatRules.GiveGuiYuanToEnemies(new ThrowingPlayerChoiceContext(), Owner,
            DynamicVars["Amount"].IntValue);
    }
}

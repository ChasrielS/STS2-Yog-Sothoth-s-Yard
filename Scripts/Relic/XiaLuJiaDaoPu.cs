using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts.Shadow;

namespace YogSothothsYard.Scripts;

[RegisterRelic(typeof(HaaLouLingRelicPool))]
public sealed class XiaLuJiaDaoPu : HaaLouLingRelic
{
    protected override string ImageName => "xia_lu_jia_dao_pu";
    protected override IEnumerable<string> RegisteredKeywordIds => [HaaLouLingKeywords.KeywordId("DaoShi")];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Threshold", 4), new IntVar("Amount", 1)];
    public override RelicRarity Rarity => RelicRarity.Rare;
    [SavedProperty]
    public int Progress { get; set; }
    public override bool ShowCounter => true;
    public override int DisplayAmount => Progress;

    public async Task KnifeSpent(PlayerChoiceContext context, int count, CardModel source)
    {
        var threshold = DynamicVars["Threshold"].IntValue;
        var amount = DynamicVars["Amount"].IntValue;
        var erosion = ShadowRelicRules.TryGet(this, out var rule) ? rule.Amount : 0;
        var total = Progress + count;
        Progress = total % threshold;
        InvokeDisplayAmountChanged();
        for (var i = 0; i < total / threshold; i++)
        {
            Flash();
            await HaaLouLingCombatRules.GainKnife(context, Owner, amount, source);
            await ShadowRelicRules.Charge(this, erosion, context);
        }
    }
}

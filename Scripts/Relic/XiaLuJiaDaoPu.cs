using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterRelic(typeof(HaaLouLingRelicPool))]
public sealed class XiaLuJiaDaoPu : HaaLouLingRelic
{
    protected override string ImageName => "xia_lu_jia_dao_pu";
    protected override IEnumerable<string> RegisteredKeywordIds => [HaaLouLingKeywords.KeywordId("DaoShi")];
    public override RelicRarity Rarity => RelicRarity.Rare;
    [SavedProperty]
    public int Progress { get; set; }
    public override bool ShowCounter => true;
    public override int DisplayAmount => Progress;

    public async Task KnifeSpent(PlayerChoiceContext context, int count, CardModel source)
    {
        var total = Progress + count;
        Progress = total % 3;
        InvokeDisplayAmountChanged();
        for (var i = 0; i < total / 3; i++)
        {
            Flash();
            await HaaLouLingCombatRules.GainKnife(context, Owner, 1, source);
        }
    }
}

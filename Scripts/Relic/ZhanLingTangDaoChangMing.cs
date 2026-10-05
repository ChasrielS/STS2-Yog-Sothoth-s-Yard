using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterRelic(typeof(HaaLouLingRelicPool))]
public sealed class ZhanLingTangDaoChangMing : HaaLouLingRelic
{
    protected override string ImageName => "zhan_ling_tang_dao_chang_ming";
    protected override IEnumerable<string> RegisteredKeywordIds => [HaaLouLingKeywords.KeywordId("DaoShi")];
    public override RelicRarity Rarity => RelicRarity.Starter;
    public override async Task BeforeCombatStart()
    {
        Flash();
        var context = new ThrowingPlayerChoiceContext();
        await PowerCmd.Apply<DaoFaJingJinPower>(context, Owner.Creature, 2, Owner.Creature, null);
        await HaaLouLingCombatRules.GainKnife(context, Owner, 5);
    }
}

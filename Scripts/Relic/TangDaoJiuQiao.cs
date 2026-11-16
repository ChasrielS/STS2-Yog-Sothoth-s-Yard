using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterRelic(typeof(HaaLouLingRelicPool))]
public sealed class TangDaoJiuQiao : HaaLouLingRelic
{
    protected override string ImageName => "tang_dao_jiu_qiao";
    protected override IEnumerable<string> RegisteredKeywordIds => [HaaLouLingKeywords.KeywordId("DaoShi")];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amount", 1)];
    public override RelicRarity Rarity => RelicRarity.Rare;
    public override async Task BeforeCombatStart()
    {
        Flash();
        await PowerCmd.Apply<DaoFaJingJinPower>(new ThrowingPlayerChoiceContext(), Owner.Creature,
            DynamicVars["Amount"].IntValue, Owner.Creature, null);
    }
}

using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterRelic(typeof(HaaLouLingRelicPool))]
[RegisterTouchOfOrobasRefinement(typeof(ZhanLingTangDaoChangMing))]
public sealed class ZhanLingTangDao : HaaLouLingRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;
    public override async Task BeforeCombatStart()
    {
        Flash();
        await HaaLouLingCombatRules.GainKnife(new ThrowingPlayerChoiceContext(), Owner, 1);
    }
}

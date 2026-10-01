using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterRelic(typeof(HaaLouLingRelicPool))]
public sealed class TangDaoJiuQiao : HaaLouLingRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;
    public override async Task BeforeCombatStart()
    {
        Flash();
        await PowerCmd.Apply<DaoFaJingJinPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, 1, Owner.Creature, null);
    }
}

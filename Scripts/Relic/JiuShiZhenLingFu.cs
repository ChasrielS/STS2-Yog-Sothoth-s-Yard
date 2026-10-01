using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterRelic(typeof(HaaLouLingRelicPool))]
public sealed class JiuShiZhenLingFu : HaaLouLingRelic
{
    public override RelicRarity Rarity => RelicRarity.Common;
    public override async Task BeforeCombatStart()
    {
        Flash();
        await HaaLouLingCombatRules.GiveGuiYuanToEnemies(new ThrowingPlayerChoiceContext(), Owner, 5);
    }
}

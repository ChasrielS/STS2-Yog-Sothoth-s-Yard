using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterRelic(typeof(HaaLouLingRelicPool))]
public sealed class ZhanLingTangDaoChangMing : HaaLouLingRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext context, ICombatState state)
    {
        if (player == Owner && player.PlayerCombatState!.TurnNumber <= 3)
        {
            Flash();
            await HaaLouLingCombatRules.GainKnife(context, Owner, 1);
        }
    }
}

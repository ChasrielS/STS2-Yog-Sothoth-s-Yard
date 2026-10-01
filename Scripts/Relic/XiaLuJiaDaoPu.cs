using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterRelic(typeof(HaaLouLingRelicPool))]
public sealed class XiaLuJiaDaoPu : HaaLouLingRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Player == Owner && play.Card is HaaLouLingCard { Form: KnifeForm.Follow })
        {
            Flash();
            await HaaLouLingCombatRules.GainKnife(context, Owner, 1, play.Card);
        }
    }
}

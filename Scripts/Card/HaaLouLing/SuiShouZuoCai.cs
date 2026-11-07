using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class SuiShouZuoCai : HaaLouLingCard
{
    public SuiShouZuoCai() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await HaaLouLingCookingService.StartRandomDish(context, Owner, IsUpgraded ? CardRarity.Uncommon : CardRarity.Common);
    }
}

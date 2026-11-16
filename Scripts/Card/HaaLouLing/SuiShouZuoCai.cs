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
        CardRarity? rarity = ErosionRank switch
        {
            0 => IsUpgraded ? null : CardRarity.Common,
            1 => IsUpgraded ? CardRarity.Uncommon : null,
            _ => CardRarity.Uncommon
        };
        var upgraded = ErosionRank == 3 || ErosionRank == 2 && IsUpgraded;
        var count = ErosionRank == 3 && IsUpgraded ? 2 : 1;
        for (var i = 0; i < count; i++)
            await HaaLouLingCookingService.StartRandomDish(context, Owner, rarity, upgraded);
    }
}

using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class HuaFeiWeiCai : HaaLouLingCard
{
    public HuaFeiWeiCai() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await HaaLouLingCardChoices.TransformCombatCards(context, this, PileType.Hand.GetPile(Owner).Cards.Where(card => card.Type == CardType.Status),
            IsUpgraded ? CardRarity.Uncommon : null, nonRare: !IsUpgraded);
    }
}

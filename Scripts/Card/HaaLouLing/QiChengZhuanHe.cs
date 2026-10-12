using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class QiChengZhuanHe : HaaLouLingCard
{
    public QiChengZhuanHe() : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    protected override bool UsesKnifeForms => true;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await HaaLouLingCardChoices.AddFormCards(context, this);
    }
}

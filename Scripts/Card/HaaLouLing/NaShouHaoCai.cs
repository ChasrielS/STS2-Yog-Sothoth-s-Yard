using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class NaShouHaoCai : HaaLouLingCard
{
    public NaShouHaoCai() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        await HaaLouLingCardChoices.CookDishesChoice(context, this, DynamicVars.Cards.IntValue, CardRarity.Uncommon);

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}

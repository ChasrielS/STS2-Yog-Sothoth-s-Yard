using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class ChaLouBuQue : HaaLouLingCard
{
    public ChaLouBuQue() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(0)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Apply<ChaLouBuQuePower>(context, 1);
        if (DynamicVars.Cards.IntValue > 0)
            await HaaLouLingCookingService.AddRandomIngredients(context, Owner, DynamicVars.Cards.IntValue, nonRare: true);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

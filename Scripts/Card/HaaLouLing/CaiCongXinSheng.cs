using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
[RegisterDustyTomeCard(typeof(HaaLouLing))]
public sealed class CaiCongXinSheng : HaaLouLingCard
{
    public CaiCongXinSheng() : base(3, CardType.Power, CardRarity.Ancient, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amount", 1), new CardsVar(0)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (DynamicVars.Cards.IntValue > 0)
            await HaaLouLingCookingService.AddRandomIngredients(context, Owner, DynamicVars.Cards.IntValue, nonRare: true);
        await Apply<CaiCongXinShengPower>(context, DynamicVars["Amount"].IntValue);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

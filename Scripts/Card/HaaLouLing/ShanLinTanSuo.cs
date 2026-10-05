using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class ShanLinTanSuo : HaaLouLingCard
{
    public ShanLinTanSuo() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("UncommonFoods", 2), new CardsVar(3)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await HaaLouLingCookingService.AddRandomIngredients(context, Owner, DynamicVars["UncommonFoods"].IntValue, CardRarity.Uncommon);
        await HaaLouLingCookingService.AddRandomIngredients(context, Owner, DynamicVars.Cards.IntValue, CardRarity.Common);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["UncommonFoods"].UpgradeValueBy(2); DynamicVars.Cards.UpgradeValueBy(2);
    }
}

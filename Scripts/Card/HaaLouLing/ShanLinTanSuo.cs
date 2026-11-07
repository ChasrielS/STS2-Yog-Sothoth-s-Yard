using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class ShanLinTanSuo : HaaLouLingCard
{
    public ShanLinTanSuo() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(4), new IntVar("Increase", 1)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await HaaLouLingCookingService.AddRandomIngredients(context, Owner, DynamicVars.Cards.IntValue);
        DynamicVars.Cards.BaseValue += DynamicVars["Increase"].IntValue;
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Increase"].UpgradeValueBy(1);
    }
}

using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class DaoXiaQuCai : HaaLouLingCard
{
    public DaoXiaQuCai() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(5), new IntVar("ExtraCards", 3)];

    protected override bool UsesKnife => true;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await HaaLouLingCookingService.AddRandomIngredients(context, Owner, DynamicVars.Cards.IntValue, CardRarity.Common);
        var spent = await SpendAllKnife(context);
        await HaaLouLingCookingService.AddRandomIngredients(context, Owner, spent * DynamicVars["ExtraCards"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(3);
        DynamicVars["ExtraCards"].UpgradeValueBy(1);
    }
}

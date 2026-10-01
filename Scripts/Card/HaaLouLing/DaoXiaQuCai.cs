using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class DaoXiaQuCai : HaaLouLingCard
{
    public DaoXiaQuCai() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];
    protected override bool UsesKnife => true;
    public override KnifeForm Form => KnifeForm.Finish;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var spent = await FinishKnife(context);
        await HaaLouLingCookingService.AddRandomIngredients(context, Owner, spent * DynamicVars.Cards.IntValue, nonRare: true);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1);
    }
}

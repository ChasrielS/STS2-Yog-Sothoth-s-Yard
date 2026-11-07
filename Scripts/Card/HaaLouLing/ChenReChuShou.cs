using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts.Cooking;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class ChenReChuShou : HaaLouLingCard
{
    public ChenReChuShou() : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CalculationBaseVar(6), new ExtraDamageVar(3),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(static (card, _) =>
            card.Owner?.PlayerCombatState != null && HaaLouLingCookingService.GetState(card.Owner).DishesCompletedThisTurn > 0 ? 1 : 0)];
    public override bool WantsConditionalHighlight => Owner?.PlayerCombatState != null &&
        HaaLouLingCookingService.GetState(Owner).DishesCompletedThisTurn > 0;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Attack(context, play, calculated: true);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(2);
        DynamicVars.ExtraDamage.UpgradeValueBy(1);
    }
}

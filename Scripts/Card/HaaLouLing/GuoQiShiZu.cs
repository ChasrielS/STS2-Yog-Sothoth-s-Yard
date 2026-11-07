using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class GuoQiShiZu : HaaLouLingCard
{
    public GuoQiShiZu() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CalculationBaseVar(10), new ExtraDamageVar(5),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(static (card, _) => card.Owner?.PlayerCombatState == null
            ? 0 : HaaLouLingCookingService.GetState(card.Owner).Slots.Count)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Attack(context, play, calculated: true);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(2);
        DynamicVars.ExtraDamage.UpgradeValueBy(2);
    }
}

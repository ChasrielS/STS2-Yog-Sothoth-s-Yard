using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.ValueProps;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class WuWeiJuQuan : HaaLouLingCard
{
    public WuWeiJuQuan() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CalculationBaseVar(12), new ExtraDamageVar(3),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(static (card, _) => card.Owner?.PlayerCombatState == null
            ? 0 : card.Owner.Creature.Powers.OfType<DishPower>().Select(power => power.Id).Distinct().Count())];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        await Attack(context, play, calculated: true);

    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(3);
        DynamicVars.ExtraDamage.UpgradeValueBy(2);
    }
}

using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.ValueProps;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class WuWeiJuQuan : HaaLouLingCard
{
    public WuWeiJuQuan() : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CalculationBaseVar(2), new ExtraDamageVar(1),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(static (card, _) => card.Owner?.PlayerCombatState == null
            ? 0 : Entry.FoodPile.GetPile(card.Owner).Cards.Select(ingredient => ingredient.Id).Distinct().Count())];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        await Attack(context, play, calculated: true);

    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(1);
        DynamicVars.ExtraDamage.UpgradeValueBy(1);
    }
}

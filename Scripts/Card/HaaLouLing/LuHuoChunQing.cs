using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class LuHuoChunQing : HaaLouLingCard
{
    public LuHuoChunQing() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CalculationBaseVar(1), new ExtraDamageVar(2),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(static (card, _) => HaaLouLingCookingService.GetState(card.Owner).DishesCooked)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) => await Attack(context, play, calculated: true);

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

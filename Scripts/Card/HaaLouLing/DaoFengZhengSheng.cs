using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class DaoFengZhengSheng : HaaLouLingCard
{
    public DaoFengZhengSheng() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    protected override bool UsesKnife => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CalculationBaseVar(4), new ExtraDamageVar(6),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(static (card, _) => HaaLouLingCombatRules.KnifeForCard(card))];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Attack(context, play, calculated: true);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(1);
        DynamicVars.ExtraDamage.UpgradeValueBy(3);
    }
}

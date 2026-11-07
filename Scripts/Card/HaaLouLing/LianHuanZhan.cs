using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class LianHuanZhan : HaaLouLingCard
{
    public LianHuanZhan() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CalculationBaseVar(8), new ExtraDamageVar(2),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(static (card, _) => HaaLouLingCombatRules.KnifeForCard(card) > 0 ? 1 : 0)];

    protected override bool UsesKnife => true;
    public override bool WantsConditionalHighlight => Knife > 0;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Attack(context, play, hits: 2, calculated: true);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(4);
        DynamicVars.ExtraDamage.UpgradeValueBy(1);
    }
}

using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class SaoTangZhan : HaaLouLingCard
{
    public SaoTangZhan() : base(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CalculationBaseVar(8), new ExtraDamageVar(4),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(static (card, _) => HaaLouLingCombatRules.KnifeForCard(card) > 0 ? 1 : 0)];

    protected override bool UsesKnife => true;
    public override bool WantsConditionalHighlight => Knife > 0;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (Knife > 0) await SpendKnife(context, 1);
        await Attack(context, play, calculated: true);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(3);
        DynamicVars.ExtraDamage.UpgradeValueBy(2);
    }
}

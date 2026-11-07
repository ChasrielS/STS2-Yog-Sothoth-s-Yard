using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Commands;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class WenZhuZhenJiao : HaaLouLingCard
{
    public WenZhuZhenJiao() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CalculationBaseVar(8), new CalculationExtraVar(5),
        new CalculatedBlockVar(ValueProp.Move).WithMultiplier(static (card, _) => HaaLouLingCombatRules.KnifeForCard(card) > 0 ? 1 : 0)];

    protected override bool UsesKnife => true;
    public override bool GainsBlock => true;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (Knife > 0) await SpendKnife(context, 1);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.CalculatedBlock.Calculate(play.Target), DynamicVars.CalculatedBlock.Props, play);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(3);
        DynamicVars.CalculationExtra.UpgradeValueBy(2);
    }
}

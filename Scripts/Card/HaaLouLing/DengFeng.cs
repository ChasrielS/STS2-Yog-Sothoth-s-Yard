using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class DengFeng : HaaLouLingCard
{
    public DengFeng() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amount", 1)];
    protected override bool UsesKnife => true;
    protected override bool ShouldGlowRedInternal => Owner?.PlayerCombatState != null && Knife < HaaLouLingCombatRules.KnifeLimit(Owner);

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (Knife == HaaLouLingCombatRules.KnifeLimit(Owner)) await Apply<DaoFaJingJinPower>(context, DynamicVars["Amount"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Amount"].UpgradeValueBy(1);
    }
}

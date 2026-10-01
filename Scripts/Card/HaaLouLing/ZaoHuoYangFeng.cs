using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class ZaoHuoYangFeng : HaaLouLingCard
{
    public ZaoHuoYangFeng() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amount", 1)];
    protected override bool UsesKnife => true;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (IsUpgraded) await Apply<ZaoHuoYangFengPlusPower>(context, DynamicVars["Amount"].IntValue);
        else await Apply<ZaoHuoYangFengPower>(context, DynamicVars["Amount"].IntValue);
    }
}

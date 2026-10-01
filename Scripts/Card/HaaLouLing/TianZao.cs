using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class TianZao : HaaLouLingCard
{
    public TianZao() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amount", 1)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (IsUpgraded) await Apply<TianZaoPlusPower>(context, DynamicVars["Amount"].IntValue);
        else await Apply<TianZaoPower>(context, DynamicVars["Amount"].IntValue);
    }
}

using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class GangRouBingJi : HaaLouLingCard
{
    public GangRouBingJi() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amount", 1), new EnergyVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        await Apply<GangRouBingJiPower>(context, DynamicVars["Amount"].IntValue);

    protected override void OnUpgrade() => DynamicVars["Amount"].UpgradeValueBy(1);
}

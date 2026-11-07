using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class CangFeng : HaaLouLingCard
{
    public CangFeng() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    protected override bool UsesKnife => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amount", 1), new EnergyVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Apply<CangFengPower>(context, DynamicVars["Amount"].IntValue);
        EnergyCost.AddThisCombat(DynamicVars.Energy.IntValue);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class LaoNiDanDai : HaaLouLingCard
{
    public LaoNiDanDai() : base(0, CardType.Skill, CardRarity.Rare, TargetType.AnyAlly) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amount", 5), new CardsVar(1), new EnergyVar(1)];
    protected override bool UsesGuiYuan => true;
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await GiveGuiYuan(context, play, DynamicVars["Amount"].IntValue);
        await CardPileCmd.Draw(context, DynamicVars.Cards.IntValue, Owner);
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1);
    }
}

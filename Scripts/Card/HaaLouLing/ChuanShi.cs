using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class ChuanShi : HaaLouLingCard
{
    public ChuanShi() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amount", 2)];
    protected override bool UsesKnife => true;
    public override KnifeForm Form => KnifeForm.Finish;
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var count = await FinishKnife(context);
        await PowerCmd.Apply<VigorPower>(context, play.Target!, count * DynamicVars["Amount"].IntValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Amount"].UpgradeValueBy(1);
    }
}

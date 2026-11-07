using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class LiuYiShou : HaaLouLingCard
{
    public LiuYiShou() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(13, ValueProp.Move), new IntVar("Amount", 1)];

    public override bool GainsBlock => true;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Block(play);
        await Apply<LiuYiShouPower>(context, DynamicVars["Amount"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3);
    }
}

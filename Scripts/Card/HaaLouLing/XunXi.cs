using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class XunXi : HaaLouLingCard
{
    public XunXi() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(2, ValueProp.Move), new CardsVar(2)];
    public override bool GainsBlock => true;
    protected override bool UsesKnife => true;
    public override KnifeForm Form => KnifeForm.Turn;
    protected override bool ShouldGlowRedInternal => Knife == 0;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Block(play);
        await Block(play);
        if (Knife > 0) { await SpendKnife(context, 1); await CardPileCmd.Draw(context, DynamicVars.Cards.IntValue, Owner); }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(1); DynamicVars.Cards.UpgradeValueBy(1);
    }
}

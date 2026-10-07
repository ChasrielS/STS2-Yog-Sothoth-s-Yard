using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class QiShi : HaaLouLingCard
{
    public QiShi() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1), new IntVar("Amount", 3)];
    protected override bool UsesKnife => true;
    public override KnifeForm Form => KnifeForm.Open;
    public override bool WantsGreenOutline => Knife == 0;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (Knife > 0) return;
        await GainKnife(context, DynamicVars["Amount"].IntValue);
        await CardPileCmd.Draw(context, DynamicVars.Cards.IntValue, Owner);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}

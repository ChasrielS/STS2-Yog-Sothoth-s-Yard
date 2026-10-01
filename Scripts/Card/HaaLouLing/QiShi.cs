using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class QiShi : HaaLouLingCard
{
    public QiShi() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amount", 2)];
    protected override bool UsesKnife => true;
    public override KnifeForm Form => KnifeForm.Open;
    protected override bool ShouldGlowRedInternal => Knife > 0;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (Knife == 0) await GainKnife(context, DynamicVars["Amount"].IntValue);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}

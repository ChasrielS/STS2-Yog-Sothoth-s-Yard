using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class HuiQi : HaaLouLingCard
{
    public HuiQi() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amount", 1)];
    protected override bool UsesKnife => true;
    public override KnifeForm Form => KnifeForm.Turn;
    protected override bool ShouldGlowRedInternal => Knife == 0;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (Knife > 0) { await SpendKnife(context, 1); await PlayerCmd.GainEnergy(1, Owner); }
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}

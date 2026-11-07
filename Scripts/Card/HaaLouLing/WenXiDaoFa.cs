using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class WenXiDaoFa : HaaLouLingCard
{
    public WenXiDaoFa() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
    protected override bool UsesKnife => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amount", 1)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Apply<WenXiDaoFaPower>(context, DynamicVars["Amount"].IntValue);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}

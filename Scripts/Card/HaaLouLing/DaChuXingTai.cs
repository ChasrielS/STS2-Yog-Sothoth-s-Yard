using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class DaChuXingTai : HaaLouLingCard
{
    public DaChuXingTai() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amount", 1)];
    public override IEnumerable<CardKeyword> CanonicalKeywords => FormKeywords.Concat([CardKeyword.Ethereal]);

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Apply<DaChuXingTaiPower>(context, DynamicVars["Amount"].IntValue);
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Ethereal);
    }
}

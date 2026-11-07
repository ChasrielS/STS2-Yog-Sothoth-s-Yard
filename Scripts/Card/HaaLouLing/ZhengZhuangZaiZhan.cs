using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class ZhengZhuangZaiZhan : HaaLouLingCard
{
    public ZhengZhuangZaiZhan() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    protected override bool UsesKnife => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amount", 2)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await GainKnife(context, DynamicVars["Amount"].IntValue);
        await HaaLouLingCardChoices.Retrieve(context, this, PileType.Discard, destination: PileType.Draw, position: CardPilePosition.Top);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Amount"].UpgradeValueBy(1);
    }
}

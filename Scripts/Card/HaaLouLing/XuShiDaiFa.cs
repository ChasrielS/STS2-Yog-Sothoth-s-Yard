using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class XuShiDaiFa : HaaLouLingCard
{
    public XuShiDaiFa() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override bool UsesKnife => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await GainKnife(context, HaaLouLingCombatRules.KnifeLimit(Owner));
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

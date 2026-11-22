using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class ZhenBenShi : HaaLouLingCard
{
    public ZhenBenShi() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    protected override bool UsesKnife => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("BonusStrength", 0)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        base.AdditionalHoverTips.Append(HoverTipFactory.FromPower<StrengthPower>());

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        Apply<StrengthPower>(context, HaaLouLingCombatRules.KnifeLimit(Owner) - Knife + DynamicVars["BonusStrength"].IntValue);

    protected override void OnUpgrade() => DynamicVars["BonusStrength"].UpgradeValueBy(1);
}

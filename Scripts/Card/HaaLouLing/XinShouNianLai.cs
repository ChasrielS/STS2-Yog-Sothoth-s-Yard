using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class XinShouNianLai : HaaLouLingCard
{
    public XinShouNianLai() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amount", 1)];
    protected override bool UsesKnifeForms => true;
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => base.AdditionalHoverTips.Concat([HoverTipFactory.FromKeyword(CardKeyword.Retain)]);

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        await Apply<XinShouNianLaiPower>(context, DynamicVars["Amount"].IntValue);

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

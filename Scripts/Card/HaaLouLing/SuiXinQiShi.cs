using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class SuiXinQiShi : HaaLouLingCard
{
    public SuiXinQiShi() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
    protected override bool UsesKnife => true;
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        base.AdditionalHoverTips.Concat([HoverTipFactory.FromKeyword(HaaLouLingKeywords.QiShi)]);

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Apply<SuiXinQiShiPower>(context, 1);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

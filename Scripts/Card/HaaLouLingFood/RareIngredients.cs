using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class YuanGuXinXiSu : HaaLouLingIngredientCard
{
    protected override string PortraitFileName => "yuan_gu_xin_xi_su";
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromKeyword(HaaLouLingKeywords.AncientGlimpse)
    ];

    public YuanGuXinXiSu() : base(CardRarity.Rare) { }
}

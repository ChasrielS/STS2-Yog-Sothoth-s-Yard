using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts;

namespace YogSothothsYard.Scripts.Card.HaaLouLingDish;

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class YangWangXingKong : HaaLouLingDishCard
{
    protected override string PortraitFileName => "yang_wang_xing_kong";
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        base.CanonicalVars.Append(new DynamicVar("Percent", 10));
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromCard<YuRou>(),
        HoverTipFactory.FromCard<XieRou>(),
        HoverTipFactory.FromCard<YuanGuXinXiSu>(),
        HoverTipFactory.FromKeyword(HaaLouLingKeywords.AncientGlimpse),
        HoverTipFactory.FromPower<PoisonPower>()
    ];

    public YangWangXingKong() : base(CardType.Power, 2, CardRarity.Rare) { }
    protected override void OnUpgrade() => DynamicVars["Percent"].UpgradeValueBy(5);
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class ZhangYuWanZi : HaaLouLingDishCard
{
    protected override string PortraitFileName => "zhang_yu_wan_zi";
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        base.CanonicalVars.Append(new DynamicVar("Cards", 4));
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromCard<YuRou>(),
        HoverTipFactory.FromCard<LuoRou>(),
        HoverTipFactory.FromCard<YuanGuXinXiSu>(),
        HoverTipFactory.FromKeyword(HaaLouLingKeywords.AncientGlimpse)
    ];

    public ZhangYuWanZi() : base(CardType.Power, 3, CardRarity.Rare) { }
    protected override void OnUpgrade() => DynamicVars["CookingTurns"].UpgradeValueBy(-1);
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class BaoZaiFan : HaaLouLingDishCard
{
    protected override string PortraitFileName => "bao_zai_fan";
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        base.CanonicalVars.Append(new DynamicVar("Damage", 2));
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromCard<Mi>(),
        HoverTipFactory.FromCard<ZhuRou>(),
        HoverTipFactory.FromCard<LvYeCai>(),
        HoverTipFactory.FromCard<YuanGuXinXiSu>(),
        HoverTipFactory.FromKeyword(HaaLouLingKeywords.AncientGlimpse)
    ];

    public BaoZaiFan() : base(CardType.Power, 2, CardRarity.Rare) { }
    protected override void OnUpgrade() => DynamicVars["Damage"].UpgradeValueBy(1);
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class ShengYan : HaaLouLingDishCard
{
    protected override string PortraitFileName => "sheng_yan";
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromCard<XieRou>(),
        HoverTipFactory.FromCard<XiaRou>(),
        HoverTipFactory.FromCard<LuoRou>(),
        HoverTipFactory.FromCard<Yan>(),
        HoverTipFactory.FromCard<YuanGuXinXiSu>(),
        HoverTipFactory.FromKeyword(HaaLouLingKeywords.AncientGlimpse),
        HoverTipFactory.FromPower<ArtifactPower>(),
        HoverTipFactory.FromPower<WeakPower>(),
        HoverTipFactory.FromPower<VulnerablePower>(),
        HoverTipFactory.FromPower<ManglePower>()
    ];

    public ShengYan() : base(CardType.Power, 3, CardRarity.Rare) { }
    protected override void OnUpgrade() { }
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class MaoXueWang : HaaLouLingDishCard
{
    protected override string PortraitFileName => "mao_xue_wang";
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromCard<YaRou>(),
        HoverTipFactory.FromCard<QiGuaiDeRou>(),
        HoverTipFactory.FromCard<DouFu>(),
        HoverTipFactory.FromCard<LaJiao>(),
        HoverTipFactory.FromCard<YuanGuXinXiSu>(),
        HoverTipFactory.FromKeyword(HaaLouLingKeywords.AncientGlimpse)
    ];

    public MaoXueWang() : base(CardType.Power, 3, CardRarity.Rare) { }
    protected override void OnUpgrade() => DynamicVars["CookingTurns"].UpgradeValueBy(-1);
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class KaiShuiBaiCai : HaaLouLingDishCard
{
    protected override string PortraitFileName => "kai_shui_bai_cai";
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromCard<LvYeCai>(),
        HoverTipFactory.FromCard<JiRou>(),
        HoverTipFactory.FromCard<ZhuRou>(),
        HoverTipFactory.FromCard<YaRou>(),
        HoverTipFactory.FromCard<YuanGuXinXiSu>(),
        HoverTipFactory.FromKeyword(HaaLouLingKeywords.AncientGlimpse)
    ];

    public KaiShuiBaiCai() : base(CardType.Power, 4, CardRarity.Rare) { }
    protected override void OnUpgrade() => DynamicVars["CookingTurns"].UpgradeValueBy(-1);
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class ShenQianTangFen : HaaLouLingDishCard
{
    protected override string PortraitFileName => "shen_qian_tang_fen";
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        base.CanonicalVars.Append(new DynamicVar("Amount", 2));
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromCard<LuoRou>(),
        HoverTipFactory.FromCard<Mi>(),
        HoverTipFactory.FromCard<Sun>(),
        HoverTipFactory.FromCard<YuRou>(),
        HoverTipFactory.FromCard<YuanGuXinXiSu>(),
        HoverTipFactory.FromKeyword(HaaLouLingKeywords.AncientGlimpse),
        HoverTipFactory.FromPower<PlatingPower>()
    ];

    public ShenQianTangFen() : base(CardType.Power, 3, CardRarity.Rare) { }
    protected override void OnUpgrade() => DynamicVars["Amount"].UpgradeValueBy(1);
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class PenCai : HaaLouLingDishCard
{
    protected override string PortraitFileName => "pen_cai";
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromCard<JiRou>(),
        HoverTipFactory.FromCard<ZhuRou>(),
        HoverTipFactory.FromCard<YuRou>(),
        HoverTipFactory.FromCard<YaRou>(),
        HoverTipFactory.FromCard<YuanGuXinXiSu>(),
        HoverTipFactory.FromKeyword(HaaLouLingKeywords.AncientGlimpse)
    ];

    public PenCai() : base(CardType.Power, 3, CardRarity.Rare) { }
    protected override void OnUpgrade() => DynamicVars["CookingTurns"].UpgradeValueBy(-1);
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class MaLaHuoGuo : HaaLouLingDishCard
{
    protected override string PortraitFileName => "ma_la_huo_guo";
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromCard<QiGuaiDeRou>(),
        HoverTipFactory.FromCard<LaJiao>(),
        HoverTipFactory.FromCard<HuaJiao>(),
        HoverTipFactory.FromCard<Sun>(),
        HoverTipFactory.FromCard<LvYeCai>(),
        HoverTipFactory.FromCard<YuanGuXinXiSu>(),
        HoverTipFactory.FromKeyword(HaaLouLingKeywords.AncientGlimpse)
    ];

    public MaLaHuoGuo() : base(CardType.Skill, 3, CardRarity.Rare) { }
    protected override void OnUpgrade() { }
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class MoYuBao : HaaLouLingDishCard
{
    protected override string PortraitFileName => "mo_yu_bao";
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromCard<QiGuaiDeRou>(),
        HoverTipFactory.FromCard<XieRou>(),
        HoverTipFactory.FromCard<YuRou>(),
        HoverTipFactory.FromCard<LuoRou>(),
        HoverTipFactory.FromCard<XiaRou>(),
        HoverTipFactory.FromCard<YuanGuXinXiSu>(),
        HoverTipFactory.FromKeyword(HaaLouLingKeywords.AncientGlimpse)
    ];

    public MoYuBao() : base(CardType.Power, 3, CardRarity.Rare) { }
    protected override void OnUpgrade() => DynamicVars["CookingTurns"].UpgradeValueBy(-1);
}

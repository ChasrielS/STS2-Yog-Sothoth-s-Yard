using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts;

namespace YogSothothsYard.Scripts.Card.HaaLouLingDish;

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class BaiZhanJi : HaaLouLingDishCard
{
    protected override string PortraitFileName => "bai_zhan_ji";
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromCard<JiRou>()
    ];
    protected override IEnumerable<DynamicVar> CanonicalVars => base.CanonicalVars.Append(new CardsVar(1));
    public BaiZhanJi() : base(CardType.Skill, 1) { }
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class ChangFen : HaaLouLingDishCard
{
    protected override string PortraitFileName => "chang_fen";
    protected override IEnumerable<DynamicVar> CanonicalVars => base.CanonicalVars.Append(new DynamicVar("Amount", 1));
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<Mi>()];
    public ChangFen() : base(CardType.Skill, 0) { }
    protected override void OnUpgrade() => DynamicVars["Amount"].UpgradeValueBy(1);
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class HuoGuo : HaaLouLingDishCard
{
    protected override string PortraitFileName => "huo_guo";
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<QiGuaiDeRou>()];
    public HuoGuo() : base(CardType.Skill, 1) { }
    protected override void OnUpgrade() { }
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class ShengYuPian : HaaLouLingDishCard
{
    protected override string PortraitFileName => "sheng_yu_pian";
    protected override IEnumerable<DynamicVar> CanonicalVars => base.CanonicalVars.Append(new DynamicVar("Amount", 1));
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromCard<YuRou>(),
        HoverTipFactory.FromPower<DexterityPower>()
    ];
    public ShengYuPian() : base(CardType.Power, 2) { }
    protected override void OnUpgrade() => DynamicVars["CookingTurns"].UpgradeValueBy(-1);
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class DongPoZhouZi : HaaLouLingDishCard
{
    protected override string PortraitFileName => "dong_po_zhou_zi";
    protected override IEnumerable<DynamicVar> CanonicalVars => base.CanonicalVars.Append(new DynamicVar("Amount", 1));
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromCard<ZhuRou>(),
        HoverTipFactory.FromPower<StrengthPower>()
    ];
    public DongPoZhouZi() : base(CardType.Power, 2) { }
    protected override void OnUpgrade() => DynamicVars["CookingTurns"].UpgradeValueBy(-1);
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class MaJiangFengWei : HaaLouLingDishCard
{
    protected override string PortraitFileName => "ma_jiang_feng_wei";
    protected override IEnumerable<DynamicVar> CanonicalVars => base.CanonicalVars.Append(new DynamicVar("Amount", 1));
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<LvYeCai>()];
    public MaJiangFengWei() : base(CardType.Skill, 0) { }
    protected override void OnUpgrade() => DynamicVars["Amount"].UpgradeValueBy(1);
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class BoBoJi : HaaLouLingDishCard
{
    protected override string PortraitFileName => "bo_bo_ji";
    protected override IEnumerable<DynamicVar> CanonicalVars => base.CanonicalVars.Append(new DynamicVar("Damage", 2));
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromCard<JiRou>(),
        HoverTipFactory.FromCard<LaJiao>()
    ];
    public BoBoJi() : base(CardType.Power, 1) { }
    protected override void OnUpgrade() => DynamicVars["Damage"].UpgradeValueBy(1);
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class XueHuaJiNao : HaaLouLingDishCard
{
    protected override string PortraitFileName => "xue_hua_ji_nao";
    protected override IEnumerable<DynamicVar> CanonicalVars => base.CanonicalVars.Append(new DynamicVar("Cards", 1));
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromCard<JiRou>(),
        HoverTipFactory.FromCard<QiGuaiDeRou>()
    ];
    public XueHuaJiNao() : base(CardType.Power, 2) { }
    protected override void OnUpgrade() => DynamicVars["Cards"].UpgradeValueBy(1);
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class MaYiShangShu : HaaLouLingDishCard
{
    protected override string PortraitFileName => "ma_yi_shang_shu";
    protected override IEnumerable<DynamicVar> CanonicalVars => base.CanonicalVars.Append(new DynamicVar("Damage", 3));
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromCard<Mi>(),
        HoverTipFactory.FromCard<ZhuRou>()
    ];
    public MaYiShangShu() : base(CardType.Power, 1) { }
    protected override void OnUpgrade() => DynamicVars["Damage"].UpgradeValueBy(1);
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class SuShiLengPan : HaaLouLingDishCard
{
    protected override string PortraitFileName => "su_shi_leng_pan";
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromCard<QiGuaiDeRou>(),
        HoverTipFactory.FromCard<LaJiao>()
    ];
    public SuShiLengPan() : base(CardType.Skill, 1) { }
    protected override void OnUpgrade() => DynamicVars["CookingTurns"].UpgradeValueBy(-1);
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class ChaoHeFen : HaaLouLingDishCard
{
    protected override string PortraitFileName => "chao_he_fen";
    protected override IEnumerable<DynamicVar> CanonicalVars => base.CanonicalVars.Append(new DynamicVar("Amount", 2));
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromCard<Mi>(),
        HoverTipFactory.FromCard<QiGuaiDeRou>()
    ];
    public ChaoHeFen() : base(CardType.Skill, 0) { }
    protected override void OnUpgrade() => DynamicVars["Amount"].UpgradeValueBy(1);
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class MaLaYu : HaaLouLingDishCard
{
    protected override string PortraitFileName => "ma_la_yu";
    protected override IEnumerable<DynamicVar> CanonicalVars => base.CanonicalVars.Append(new DynamicVar("Amount", 2));
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromCard<YuRou>(),
        HoverTipFactory.FromCard<LaJiao>(),
        HoverTipFactory.FromPower<DexterityPower>()
    ];
    public MaLaYu() : base(CardType.Power, 2) { }
    protected override void OnUpgrade() => DynamicVars["Amount"].UpgradeValueBy(1);
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class SuanCaiYu : HaaLouLingDishCard
{
    protected override string PortraitFileName => "suan_cai_yu";
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromCard<YuRou>(),
        HoverTipFactory.FromCard<LvYeCai>()
    ];
    public SuanCaiYu() : base(CardType.Skill, 1) { }
    protected override void OnUpgrade() { }
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class MeiCaiKouRou : HaaLouLingDishCard
{
    protected override string PortraitFileName => "mei_cai_kou_rou";
    protected override IEnumerable<DynamicVar> CanonicalVars => base.CanonicalVars.Append(new DynamicVar("Amount", 2));
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromCard<ZhuRou>(),
        HoverTipFactory.FromCard<LvYeCai>(),
        HoverTipFactory.FromPower<VigorPower>()
    ];
    public MeiCaiKouRou() : base(CardType.Power, 0) { }
    protected override void OnUpgrade() => DynamicVars["Amount"].UpgradeValueBy(1);
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class HuiGuoRou : HaaLouLingDishCard
{
    protected override string PortraitFileName => "hui_guo_rou";
    protected override IEnumerable<DynamicVar> CanonicalVars => base.CanonicalVars.Append(new DynamicVar("Amount", 2));
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromCard<ZhuRou>(),
        HoverTipFactory.FromCard<QingJiao>(),
        HoverTipFactory.FromPower<StrengthPower>()
    ];
    public HuiGuoRou() : base(CardType.Power, 2) { }
    protected override void OnUpgrade() => DynamicVars["Amount"].UpgradeValueBy(1);
}

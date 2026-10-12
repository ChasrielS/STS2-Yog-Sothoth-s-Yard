using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts;

namespace YogSothothsYard.Scripts.Card.HaaLouLingDish;

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class ChaoZhouDongHongXie : HaaLouLingDishCard
{
    protected override string PortraitFileName => "chao_zhou_dong_hong_xie";
    protected override IEnumerable<DynamicVar> CanonicalVars => base.CanonicalVars.Append(new DynamicVar("Amount", 3));
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<XieRou>(), HoverTipFactory.FromPower<PlatingPower>()];
    public ChaoZhouDongHongXie() : base(CardType.Power, 1, CardRarity.Uncommon) { }
    protected override void OnUpgrade() => DynamicVars["CookingTurns"].UpgradeValueBy(-1);
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class MingLuShaoLuo : HaaLouLingDishCard
{
    protected override string PortraitFileName => "ming_lu_shao_luo";
    protected override IEnumerable<DynamicVar> CanonicalVars => base.CanonicalVars.Append(new DynamicVar("Block", 3));
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<LuoRou>(), HoverTipFactory.Static(StaticHoverTip.Block)];
    public MingLuShaoLuo() : base(CardType.Power, 1, CardRarity.Uncommon) { }
    protected override void OnUpgrade() => DynamicVars["Block"].UpgradeValueBy(2);
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class BaBaoSuCai : HaaLouLingDishCard
{
    protected override string PortraitFileName => "ba_bao_su_cai";
    protected override IEnumerable<DynamicVar> CanonicalVars => base.CanonicalVars.Append(new DynamicVar("Amount", 2));
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<LvYeCai>(), HoverTipFactory.FromCard<Sun>()];
    public BaBaoSuCai() : base(CardType.Skill, 0, CardRarity.Uncommon) { }
    protected override void OnUpgrade() => DynamicVars["Amount"].UpgradeValueBy(1);
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class NiangDouFu : HaaLouLingDishCard
{
    protected override string PortraitFileName => "niang_dou_fu";
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<DouFu>(), HoverTipFactory.FromCard<QiGuaiDeRou>()];
    public NiangDouFu() : base(CardType.Skill, 1, CardRarity.Uncommon) { }
    protected override void OnUpgrade() => DynamicVars["CookingTurns"].UpgradeValueBy(-1);
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class KaoRuZhu : HaaLouLingDishCard
{
    protected override string PortraitFileName => "kao_ru_zhu";
    protected override IEnumerable<DynamicVar> CanonicalVars => base.CanonicalVars.Append(new DynamicVar("Block", 3));
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<ZhuRou>(), HoverTipFactory.FromCard<Tang>(), HoverTipFactory.Static(StaticHoverTip.Block)];
    public KaoRuZhu() : base(CardType.Power, 1, CardRarity.Uncommon) { }
    protected override void OnUpgrade() => DynamicVars["Block"].UpgradeValueBy(1);
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class FenZhengRou : HaaLouLingDishCard
{
    protected override string PortraitFileName => "fen_zheng_rou";
    protected override IEnumerable<DynamicVar> CanonicalVars => base.CanonicalVars.Append(new DynamicVar("Amount", 2));
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<Mi>(), HoverTipFactory.FromCard<QiGuaiDeRou>(), HoverTipFactory.FromCard<ZhuRou>(), HoverTipFactory.FromPower<PoisonPower>()];
    public FenZhengRou() : base(CardType.Power, 2, CardRarity.Uncommon) { }
    protected override void OnUpgrade() => DynamicVars["Amount"].UpgradeValueBy(1);
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class YanJuJi : HaaLouLingDishCard
{
    protected override string PortraitFileName => "yan_ju_ji";
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<JiRou>(), HoverTipFactory.FromCard<Yan>()];
    public YanJuJi() : base(CardType.Power, 2, CardRarity.Uncommon) { }
    protected override void OnUpgrade() => DynamicVars["CookingTurns"].UpgradeValueBy(-1);
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class ZhangYuNingMengZhi : HaaLouLingDishCard
{
    protected override string PortraitFileName => "zhang_yu_ning_meng_zhi";
    protected override IEnumerable<DynamicVar> CanonicalVars => base.CanonicalVars.Concat([new DynamicVar("Threshold", 8), new EnergyVar(1)]);
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<QiGuaiDeRou>(), HoverTipFactory.FromCard<Tang>()];
    public ZhangYuNingMengZhi() : base(CardType.Skill, 0, CardRarity.Uncommon) { }
    protected override void OnUpgrade() => DynamicVars["Threshold"].UpgradeValueBy(-4);
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class KouShuiJi : HaaLouLingDishCard
{
    protected override string PortraitFileName => "kou_shui_ji";
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<JiRou>(), HoverTipFactory.FromCard<LaJiao>(), HoverTipFactory.FromCard<HuaJiao>()];
    public KouShuiJi() : base(CardType.Power, 2, CardRarity.Uncommon) { }
    protected override void OnUpgrade() => DynamicVars["CookingTurns"].UpgradeValueBy(-1);
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class GongBaoJiDing : HaaLouLingDishCard
{
    protected override string PortraitFileName => "gong_bao_ji_ding";
    protected override IEnumerable<DynamicVar> CanonicalVars => base.CanonicalVars.Append(new DynamicVar("Block", 4));
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<JiRou>(), HoverTipFactory.FromCard<QingJiao>(), HoverTipFactory.FromCard<LaJiao>(), HoverTipFactory.Static(StaticHoverTip.Block)];
    public GongBaoJiDing() : base(CardType.Power, 2, CardRarity.Uncommon) { }
    protected override void OnUpgrade() => DynamicVars["Block"].UpgradeValueBy(2);
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class ShuiZhuRouPian : HaaLouLingDishCard
{
    protected override string PortraitFileName => "shui_zhu_rou_pian";
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<QiGuaiDeRou>(), HoverTipFactory.FromCard<LaJiao>(), HoverTipFactory.FromCard<HuaJiao>()];
    public ShuiZhuRouPian() : base(CardType.Skill, 2, CardRarity.Uncommon) { }
    protected override void OnUpgrade() { }
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class MaPoDouFu : HaaLouLingDishCard
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    protected override string PortraitFileName => "ma_po_dou_fu";
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<DouFu>(), HoverTipFactory.FromCard<QiGuaiDeRou>(), HoverTipFactory.FromCard<HuaJiao>()];
    public MaPoDouFu() : base(CardType.Skill, 1, CardRarity.Uncommon) { }
    protected override void OnUpgrade() => DynamicVars["CookingTurns"].UpgradeValueBy(-1);
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class XianHuaDouFu : HaaLouLingDishCard
{
    protected override string PortraitFileName => "xian_hua_dou_fu";
    protected override IEnumerable<DynamicVar> CanonicalVars => base.CanonicalVars.Append(new DynamicVar("Amount", 1));
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<DouFu>(), HoverTipFactory.FromCard<QiGuaiDeRou>(), HoverTipFactory.FromCard<JiRou>()];
    public XianHuaDouFu() : base(CardType.Skill, 1, CardRarity.Uncommon) { }
    protected override void OnUpgrade() => DynamicVars["Amount"].UpgradeValueBy(1);
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class LuoShiFen : HaaLouLingDishCard
{
    protected override string PortraitFileName => "luo_shi_fen";
    protected override IEnumerable<DynamicVar> CanonicalVars => base.CanonicalVars.Append(new DynamicVar("Block", 6));
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<LuoRou>(), HoverTipFactory.FromCard<Sun>(), HoverTipFactory.FromCard<Mi>(), HoverTipFactory.Static(StaticHoverTip.Block)];
    public LuoShiFen() : base(CardType.Power, 2, CardRarity.Uncommon) { }
    protected override void OnUpgrade() => DynamicVars["Block"].UpgradeValueBy(3);
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class HuGuoCai : HaaLouLingDishCard
{
    protected override string PortraitFileName => "hu_guo_cai";
    protected override IEnumerable<DynamicVar> CanonicalVars => base.CanonicalVars.Append(new DynamicVar("Amount", 3));
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<LvYeCai>(), HoverTipFactory.FromCard<ZhuRou>(), HoverTipFactory.FromCard<JiRou>(), HoverTipFactory.FromPower<VigorPower>()];
    public HuGuoCai() : base(CardType.Power, 0, CardRarity.Uncommon) { }
    protected override void OnUpgrade() => DynamicVars["Amount"].UpgradeValueBy(1);
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class GanZhaXieTa : HaaLouLingDishCard
{
    protected override string PortraitFileName => "gan_zha_xie_ta";
    protected override IEnumerable<DynamicVar> CanonicalVars => base.CanonicalVars.Append(new DynamicVar("Amount", 5));
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<XieRou>(), HoverTipFactory.FromCard<QiGuaiDeRou>(), HoverTipFactory.FromCard<XiaRou>(), HoverTipFactory.FromPower<PlatingPower>()];
    public GanZhaXieTa() : base(CardType.Power, 1, CardRarity.Uncommon) { }
    protected override void OnUpgrade() => DynamicVars["Amount"].UpgradeValueBy(2);
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class JiDiZhou : HaaLouLingDishCard
{
    protected override string PortraitFileName => "ji_di_zhou";
    protected override IEnumerable<DynamicVar> CanonicalVars => base.CanonicalVars.Append(new DynamicVar("Damage", 5));
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<Mi>(), HoverTipFactory.FromCard<ZhuRou>(), HoverTipFactory.FromCard<Yan>()];
    public JiDiZhou() : base(CardType.Power, 1, CardRarity.Uncommon) { }
    protected override void OnUpgrade() => DynamicVars["Damage"].UpgradeValueBy(2);
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class SuanLaSaoZiTiJin : HaaLouLingDishCard
{
    protected override string PortraitFileName => "suan_la_sao_zi_ti_jin";
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromCard<ZhuRou>(), HoverTipFactory.FromCard<QingJiao>(), HoverTipFactory.FromCard<LaJiao>(),
        HoverTipFactory.Static(StaticHoverTip.ReplayDynamic, new DynamicVar("Times", 1))
    ];
    public SuanLaSaoZiTiJin() : base(CardType.Skill, 1, CardRarity.Uncommon) { }
    protected override void OnUpgrade() => DynamicVars["CookingTurns"].UpgradeValueBy(-1);
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class ShaoLaShuangPin : HaaLouLingDishCard
{
    protected override string PortraitFileName => "shao_la_shuang_pin";
    protected override IEnumerable<DynamicVar> CanonicalVars => base.CanonicalVars.Append(new DynamicVar("Block", 4));
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<ZhuRou>(), HoverTipFactory.FromCard<YaRou>(), HoverTipFactory.FromCard<Tang>(), HoverTipFactory.Static(StaticHoverTip.Block)];
    public ShaoLaShuangPin() : base(CardType.Power, 1, CardRarity.Uncommon) { }
    protected override void OnUpgrade() => DynamicVars["Block"].UpgradeValueBy(2);
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class TingZaiZhou : HaaLouLingDishCard
{
    protected override string PortraitFileName => "ting_zai_zhou";
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<Mi>(), HoverTipFactory.FromCard<XiaRou>(), HoverTipFactory.FromCard<YuRou>(), HoverTipFactory.FromCard<QiGuaiDeRou>()];
    public TingZaiZhou() : base(CardType.Skill, 1, CardRarity.Uncommon) { }
    protected override void OnUpgrade() => DynamicVars["CookingTurns"].UpgradeValueBy(-1);
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class ZhangChaYa : HaaLouLingDishCard
{
    protected override string PortraitFileName => "zhang_cha_ya";
    protected override IEnumerable<DynamicVar> CanonicalVars => base.CanonicalVars.Append(new DynamicVar("Block", 2));
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<YaRou>(), HoverTipFactory.FromCard<Mi>(), HoverTipFactory.FromCard<HuaJiao>(), HoverTipFactory.FromCard<Yan>(), HoverTipFactory.Static(StaticHoverTip.Block)];
    public ZhangChaYa() : base(CardType.Power, 2, CardRarity.Uncommon) { }
    protected override void OnUpgrade() => DynamicVars["CookingTurns"].UpgradeValueBy(-1);
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class ZhuDuJi : HaaLouLingDishCard
{
    protected override string PortraitFileName => "zhu_du_ji";
    protected override IEnumerable<DynamicVar> CanonicalVars => base.CanonicalVars.Append(new DynamicVar("Cards", 1));
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<ZhuRou>(), HoverTipFactory.FromCard<JiRou>(), HoverTipFactory.FromCard<HuaJiao>(), HoverTipFactory.FromCard<Sun>()];
    public ZhuDuJi() : base(CardType.Power, 2, CardRarity.Uncommon) { }
    protected override void OnUpgrade() => DynamicVars["Cards"].UpgradeValueBy(1);
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class YuXiangRouSi : HaaLouLingDishCard
{
    protected override string PortraitFileName => "yu_xiang_rou_si";
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<QiGuaiDeRou>(), HoverTipFactory.FromCard<QingJiao>(), HoverTipFactory.FromCard<Sun>(), HoverTipFactory.FromCard<Tang>()];
    public YuXiangRouSi() : base(CardType.Skill, 2, CardRarity.Uncommon) { }
    protected override void OnUpgrade() { }
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class HuoBaoYaoHua : HaaLouLingDishCard
{
    protected override string PortraitFileName => "huo_bao_yao_hua";
    protected override IEnumerable<DynamicVar> CanonicalVars => base.CanonicalVars.Append(new DynamicVar("Amount", 3));
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<ZhuRou>(), HoverTipFactory.FromCard<QingJiao>(), HoverTipFactory.FromCard<LaJiao>(), HoverTipFactory.FromCard<Sun>(), HoverTipFactory.FromPower<StrengthPower>()];
    public HuoBaoYaoHua() : base(CardType.Power, 2, CardRarity.Uncommon) { }
    protected override void OnUpgrade() => DynamicVars["Amount"].UpgradeValueBy(2);
}

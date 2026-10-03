using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YogSothothsYard.Scripts.Card.HaaLouLingDish;

namespace YogSothothsYard.Scripts.Cooking;

public static class HaaLouLingCardSortOrder
{
    public static List<CardModel> SortIngredients(IEnumerable<CardModel> cards) =>
        cards
            .OrderBy(card => GetRarityRank(card.Rarity))
            .ThenBy(GetIngredientRank)
            .ToList();

    public static List<CookingDishDefinition> SortRecipes(
        IEnumerable<CookingDishDefinition> recipes
    ) => recipes
        .OrderBy(recipe => GetRarityRank(recipe.CreatePreviewCard().Rarity))
        .ThenBy(recipe => GetDishSystemRank(recipe.CreatePreviewCard()))
        .ToList();

    private static int GetRarityRank(CardRarity rarity) => rarity switch
    {
        CardRarity.Common => 0,
        CardRarity.Uncommon => 1,
        CardRarity.Rare => 2,
        _ => 3
    };

    private static int GetIngredientRank(CardModel card) => card switch
    {
        JiRou => 0,
        Mi => 1,
        QiGuaiDeRou => 2,
        YuRou => 3,
        ZhuRou => 4,
        LaJiao => 5,
        LvYeCai => 6,
        QingJiao => 7,
        LuoRou => 8,
        Sun => 9,
        HuaJiao => 10,
        YaRou => 11,
        XieRou => 12,
        DouFu => 13,
        Tang => 14,
        Yan => 15,
        XiaRou => 16,
        YuanGuXinXiSu => 17,
        _ => int.MaxValue
    };

    private static int GetDishSystemRank(CardModel card) => card switch
    {
        // 强化
        ChangFen or ChaoHeFen or SuShiLengPan or SuanLaSaoZiTiJin or TingZaiZhou or MoYuBao => 0,

        // 数值
        ShengYuPian or DongPoZhouZi or MaLaYu or MeiCaiKouRou or HuiGuoRou or
        ChaoZhouDongHongXie or MingLuShaoLuo or LuoShiFen or HuGuoCai or
        GanZhaXieTa or HuoBaoYaoHua or ShenQianTangFen => 1,

        // 效果
        MaJiangFengWei or BaBaoSuCai or NiangDouFu or
        MaPoDouFu or XianHuaDouFu or ShengYan or
        KaiShuiBaiCai => 2,

        // 鸡肉
        BaiZhanJi or BoBoJi or XueHuaJiNao or YanJuJi or KouShuiJi or
        GongBaoJiDing or ZhuDuJi => 3,

        // 频率
        MaYiShangShu or KaoRuZhu or JiDiZhou or ShaoLaShuangPin or ZhangChaYa or
        ZhangYuWanZi or BaoZaiFan or MaoXueWang or PenCai => 4,

        // 其它
        HuoGuo or SuanCaiYu or ZhangYuNingMengZhi or ShuiZhuRouPian or
        YuXiangRouSi or MaLaHuoGuo => 5,

        // 中毒
        FenZhengRou or YangWangXingKong => 6,
        _ => int.MaxValue
    };
}

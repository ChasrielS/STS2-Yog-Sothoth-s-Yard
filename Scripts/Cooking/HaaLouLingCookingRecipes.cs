using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YogSothothsYard.Scripts.Card.HaaLouLingDish;

namespace YogSothothsYard.Scripts.Cooking;

public static class HaaLouLingCookingRecipes
{
    private const string SlotTextureRoot = "res://images/cooking/dishes";

    private static readonly Lazy<IReadOnlyList<CookingDishDefinition>> _all = new(CreateRecipes);

    public static IReadOnlyList<CookingDishDefinition> All => _all.Value;

    public static CookingDishDefinition? Get(ModelId dishModelId) =>
        All.FirstOrDefault(recipe => recipe.DishModelId == dishModelId);

    public static IEnumerable<CookingDishDefinition> AvailableFor(Player player) => All.Where(recipe =>
        recipe.CreatePreviewCard().MultiplayerConstraint != (player.RunState.Players.Count > 1
            ? CardMultiplayerConstraint.SingleplayerOnly : CardMultiplayerConstraint.MultiplayerOnly));

    private static IReadOnlyList<CookingDishDefinition> CreateRecipes()
    {
        var chicken = ModelDb.Card<JiRou>().Id;
        var rice = ModelDb.Card<Mi>().Id;
        var strangeMeat = ModelDb.Card<QiGuaiDeRou>().Id;
        var fish = ModelDb.Card<YuRou>().Id;
        var pork = ModelDb.Card<ZhuRou>().Id;
        var chili = ModelDb.Card<LaJiao>().Id;
        var leafyVegetable = ModelDb.Card<LvYeCai>().Id;
        var greenPepper = ModelDb.Card<QingJiao>().Id;
        var snailMeat = ModelDb.Card<LuoRou>().Id;
        var bambooShoot = ModelDb.Card<Sun>().Id;
        var sichuanPepper = ModelDb.Card<HuaJiao>().Id;
        var duck = ModelDb.Card<YaRou>().Id;
        var crab = ModelDb.Card<XieRou>().Id;
        var tofu = ModelDb.Card<DouFu>().Id;
        var sugar = ModelDb.Card<Tang>().Id;
        var salt = ModelDb.Card<Yan>().Id;
        var shrimp = ModelDb.Card<XiaRou>().Id;
        var ancientPheromone = ModelDb.Card<YuanGuXinXiSu>().Id;

        return
        [
            Create<BaiZhanJi>(
                1,
                "bai_zhan_ji",
                Ingredients((chicken, 1)),
                static (choiceContext, player, upgraded) => HaaLouLingCookingService.AddSpecificIngredients<JiRou>(
                    choiceContext, player, upgraded ? 2 : 1)
            ),
            Create<ChangFen>(
                0,
                "chang_fen",
                Ingredients((rice, 1)),
                static (choiceContext, player, upgraded) => ApplyPower<ChangFenPower>(
                    choiceContext, player, upgraded ? 2 : 1)
            ),
            Create<HuoGuo>(
                1,
                "huo_guo",
                Ingredients((strangeMeat, 1)),
                static (choiceContext, player, upgraded) =>
                    HaaLouLingCookingService.ChooseCommonDishEffect(
                        choiceContext, player, upgraded)
            ),
            Create<ShengYuPian>(
                2,
                "sheng_yu_pian",
                Ingredients((fish, 1)),
                static (choiceContext, player, upgraded) => ApplyPower<DexterityPower>(
                    choiceContext, player, 1),
                upgradedCookingTurns: 1
            ),
            Create<DongPoZhouZi>(
                2,
                "dong_po_zhou_zi",
                Ingredients((pork, 1)),
                static (choiceContext, player, upgraded) => ApplyPower<StrengthPower>(
                    choiceContext, player, 1),
                upgradedCookingTurns: 1
            ),
            Create<MaJiangFengWei>(
                0,
                "ma_jiang_feng_wei",
                Ingredients((leafyVegetable, 1)),
                static (choiceContext, player, upgraded) =>
                    HaaLouLingCookingService.ModifyRandomCounterPower(
                        choiceContext, player, PowerType.Buff, upgraded ? 2 : 1)
            ),
            Create<BoBoJi>(
                1,
                "bo_bo_ji",
                Ingredients((chicken, 1), (chili, 1)),
                static (choiceContext, player, upgraded) => ApplyPower<BoBoJiPower>(
                    choiceContext, player, upgraded ? 3 : 2)
            ),
            Create<XueHuaJiNao>(
                2,
                "xue_hua_ji_nao",
                Ingredients((chicken, 1), (strangeMeat, 1)),
                static (choiceContext, player, upgraded) => ApplyPower<XueHuaJiNaoPower>(
                    choiceContext, player, upgraded ? 2 : 1)
            ),
            Create<MaYiShangShu>(
                1,
                "ma_yi_shang_shu",
                Ingredients((rice, 1), (pork, 1)),
                static (choiceContext, player, upgraded) => ApplyPower<MaYiShangShuPower>(
                    choiceContext, player, upgraded ? 4 : 3)
            ),
            Create<SuShiLengPan>(
                1,
                "su_shi_leng_pan",
                Ingredients((strangeMeat, 1), (chili, 1)),
                static (choiceContext, player, upgraded) => ApplyPower<SuShiLengPanPower>(
                    choiceContext, player, 1),
                upgradedCookingTurns: 0
            ),
            Create<ChaoHeFen>(
                0,
                "chao_he_fen",
                Ingredients((rice, 1), (strangeMeat, 1)),
                static (choiceContext, player, upgraded) => ApplyPower<ChaoHeFenPower>(
                    choiceContext, player, upgraded ? 3 : 2)
            ),
            Create<MaLaYu>(
                2,
                "ma_la_yu",
                Ingredients((fish, 1), (chili, 1)),
                static (choiceContext, player, upgraded) => ApplyPower<DexterityPower>(
                    choiceContext, player, upgraded ? 3 : 2)
            ),
            Create<SuanCaiYu>(
                2,
                "suan_cai_yu",
                Ingredients((fish, 1), (leafyVegetable, 1)),
                static (choiceContext, player, upgraded) =>
                    HaaLouLingCookingService.CopyRandomPowerToOtherPlayers(
                        choiceContext, player, upgraded)
            ),
            Create<MeiCaiKouRou>(
                0,
                "mei_cai_kou_rou",
                Ingredients((pork, 1), (leafyVegetable, 1)),
                static (choiceContext, player, upgraded) => ApplyPower<VigorPower>(
                    choiceContext, player, upgraded ? 3 : 2)
            ),
            Create<HuiGuoRou>(
                2,
                "hui_guo_rou",
                Ingredients((pork, 1), (greenPepper, 1)),
                static (choiceContext, player, upgraded) => ApplyPower<StrengthPower>(
                    choiceContext, player, upgraded ? 3 : 2)
            ),
            Create<ChaoZhouDongHongXie>(
                1,
                "chao_zhou_dong_hong_xie",
                Ingredients((crab, 1)),
                static (choiceContext, player, upgraded) => HaaLouLingCookingService.ApplyDishPlating(
                    choiceContext, player, 3),
                upgradedCookingTurns: 0
            ),
            Create<MingLuShaoLuo>(
                1,
                "ming_lu_shao_luo",
                Ingredients((snailMeat, 1)),
                static (choiceContext, player, upgraded) => ApplyPower<MingLuShaoLuoPower>(
                    choiceContext, player, upgraded ? 5 : 3)
            ),
            Create<BaBaoSuCai>(
                0,
                "ba_bao_su_cai",
                Ingredients((leafyVegetable, 1), (bambooShoot, 1)),
                static (choiceContext, player, upgraded) =>
                    HaaLouLingCookingService.ModifyRandomCounterPower(
                        choiceContext, player, PowerType.Buff, upgraded ? 3 : 2)
            ),
            Create<NiangDouFu>(
                1,
                "niang_dou_fu",
                Ingredients((tofu, 1), (strangeMeat, 1)),
                static (choiceContext, player, upgraded) =>
                    HaaLouLingCookingService.IncreaseCounterPowers(
                        choiceContext, [player.Creature], PowerType.Buff, 1, player.Creature),
                upgradedCookingTurns: 0
            ),
            Create<KaoRuZhu>(
                1,
                "kao_ru_zhu",
                Ingredients((pork, 1), (sugar, 1)),
                static (choiceContext, player, upgraded) => ApplyPower<KaoRuZhuPower>(
                    choiceContext, player, upgraded ? 4 : 3)
            ),
            Create<FenZhengRou>(
                2,
                "fen_zheng_rou",
                Ingredients((rice, 1), (strangeMeat, 1), (pork, 1)),
                static (choiceContext, player, upgraded) => ApplyPower<FenZhengRouPower>(
                    choiceContext, player, upgraded ? 3 : 2)
            ),
            Create<YanJuJi>(
                2,
                "yan_ju_ji",
                Ingredients((chicken, 1), (salt, 1)),
                static (choiceContext, player, upgraded) => ApplyPower<YanJuJiPower>(
                    choiceContext, player, 1),
                upgradedCookingTurns: 1
            ),
            Create<ZhangYuNingMengZhi>(
                0,
                "zhang_yu_ning_meng_zhi",
                Ingredients((strangeMeat, 1), (sugar, 1)),
                ApplyZhangYuNingMengZhi
            ),
            Create<KouShuiJi>(
                2,
                "kou_shui_ji",
                Ingredients((chicken, 1), (chili, 1), (sichuanPepper, 1)),
                static (choiceContext, player, upgraded) => ApplyPower<KouShuiJiPower>(
                    choiceContext, player, 1),
                upgradedCookingTurns: 1
            ),
            Create<GongBaoJiDing>(
                2,
                "gong_bao_ji_ding",
                Ingredients((chicken, 1), (greenPepper, 1), (chili, 1)),
                static (choiceContext, player, upgraded) => ApplyPower<GongBaoJiDingPower>(
                    choiceContext, player, upgraded ? 6 : 4)
            ),
            Create<ShuiZhuRouPian>(
                2,
                "shui_zhu_rou_pian",
                Ingredients((strangeMeat, 1), (chili, 1), (sichuanPepper, 1)),
                static (choiceContext, player, upgraded) =>
                    HaaLouLingCookingService.ChooseUncommonDishEffect(
                        choiceContext, player, upgraded)
            ),
            Create<MaPoDouFu>(
                1,
                "ma_po_dou_fu",
                Ingredients((tofu, 1), (strangeMeat, 1), (sichuanPepper, 1)),
                static (choiceContext, player, upgraded) =>
                    HaaLouLingCookingService.IncreaseCounterPowers(
                        choiceContext, player.Creature.CombatState!.Players.Where(other => other.Creature.IsAlive)
                            .Select(other => other.Creature), PowerType.Buff, 1, player.Creature),
                upgradedCookingTurns: 0
            ),
            Create<XianHuaDouFu>(
                1,
                "xian_hua_dou_fu",
                Ingredients((tofu, 1), (strangeMeat, 1), (chicken, 1)),
                static (choiceContext, player, upgraded) =>
                    HaaLouLingCookingService.IncreaseCounterPowers(
                        choiceContext, player.Creature.CombatState!.Enemies.Where(enemy => enemy.IsAlive),
                        PowerType.Debuff, upgraded ? 2 : 1, player.Creature)
            ),
            Create<LuoShiFen>(
                2,
                "luo_shi_fen",
                Ingredients((snailMeat, 1), (bambooShoot, 1), (rice, 1)),
                static (choiceContext, player, upgraded) => ApplyPower<LuoShiFenPower>(
                    choiceContext, player, upgraded ? 9 : 6)
            ),
            Create<HuGuoCai>(
                0,
                "hu_guo_cai",
                Ingredients((leafyVegetable, 1), (pork, 1), (chicken, 1)),
                static (choiceContext, player, upgraded) => ApplyPower<VigorPower>(
                    choiceContext, player, upgraded ? 4 : 3)
            ),
            Create<GanZhaXieTa>(
                1,
                "gan_zha_xie_ta",
                Ingredients((crab, 1), (strangeMeat, 1), (shrimp, 1)),
                static (choiceContext, player, upgraded) => HaaLouLingCookingService.ApplyDishPlating(
                    choiceContext, player, upgraded ? 7 : 5)
            ),
            Create<JiDiZhou>(
                1,
                "ji_di_zhou",
                Ingredients((rice, 1), (pork, 1), (salt, 1)),
                static (choiceContext, player, upgraded) => ApplyPower<JiDiZhouPower>(
                    choiceContext, player, upgraded ? 7 : 5)
            ),
            Create<SuanLaSaoZiTiJin>(
                1,
                "suan_la_sao_zi_ti_jin",
                Ingredients((pork, 1), (greenPepper, 1), (chili, 1)),
                static (choiceContext, player, upgraded) => ApplyPower<SuanLaSaoZiTiJinPower>(
                    choiceContext, player, 1),
                upgradedCookingTurns: 0
            ),
            Create<ShaoLaShuangPin>(
                1,
                "shao_la_shuang_pin",
                Ingredients((pork, 1), (duck, 1), (sugar, 1)),
                static (choiceContext, player, upgraded) => ApplyPower<ShaoLaShuangPinPower>(
                    choiceContext, player, upgraded ? 6 : 4)
            ),
            Create<TingZaiZhou>(
                1,
                "ting_zai_zhou",
                Ingredients((rice, 1), (shrimp, 1), (fish, 1), (strangeMeat, 1)),
                static (choiceContext, player, upgraded) =>
                    ApplyPower<TingZaiZhouPower>(choiceContext, player, 1),
                upgradedCookingTurns: 0
            ),
            Create<ZhangChaYa>(
                2,
                "zhang_cha_ya",
                Ingredients((duck, 1), (rice, 1), (sichuanPepper, 1), (salt, 1)),
                static (choiceContext, player, upgraded) => ApplyPower<ZhangChaYaPower>(
                    choiceContext, player, 2),
                upgradedCookingTurns: 1
            ),
            Create<ZhuDuJi>(
                2,
                "zhu_du_ji",
                Ingredients((pork, 1), (chicken, 1), (sichuanPepper, 1), (bambooShoot, 1)),
                static (choiceContext, player, upgraded) => ApplyPower<ZhuDuJiPower>(
                    choiceContext, player, upgraded ? 2 : 1)
            ),
            Create<YuXiangRouSi>(
                2,
                "yu_xiang_rou_si",
                Ingredients((strangeMeat, 1), (greenPepper, 1), (bambooShoot, 1), (sugar, 1)),
                static (choiceContext, player, upgraded) =>
                    HaaLouLingCookingService.ChooseCommonAndUncommonDishEffects(
                        choiceContext, player, upgraded)
            ),
            Create<HuoBaoYaoHua>(
                2,
                "huo_bao_yao_hua",
                Ingredients((pork, 1), (greenPepper, 1), (chili, 1), (bambooShoot, 1)),
                static (choiceContext, player, upgraded) =>
                    ApplyHuoBaoYaoHua(choiceContext, player, upgraded)
            ),
            Create<YangWangXingKong>(
                2,
                "yang_wang_xing_kong",
                Ingredients((fish, 1), (crab, 1), (ancientPheromone, 1)),
                static (choiceContext, player, upgraded) =>
                    ApplyPower<YangWangXingKongPower>(
                        choiceContext,
                        player,
                        upgraded ? 15 : 10)
            ),
            Create<ZhangYuWanZi>(
                3,
                "zhang_yu_wan_zi",
                Ingredients((fish, 1), (snailMeat, 1), (ancientPheromone, 1)),
                static (choiceContext, player, upgraded) =>
                    ApplyPower<ZhangYuWanZiPower>(
                        choiceContext,
                        player,
                        4),
                upgradedCookingTurns: 2
            ),
            Create<BaoZaiFan>(
                2,
                "bao_zai_fan",
                Ingredients(
                    (rice, 1),
                    (pork, 1),
                    (leafyVegetable, 1),
                    (ancientPheromone, 1)),
                static (choiceContext, player, upgraded) =>
                    ApplyPower<BaoZaiFanPower>(
                        choiceContext,
                        player,
                        upgraded ? 3 : 2)
            ),
            Create<ShengYan>(
                3,
                "sheng_yan",
                Ingredients(
                    (crab, 1),
                    (shrimp, 1),
                    (snailMeat, 1),
                    (salt, 1),
                    (ancientPheromone, 1)),
                ApplyShengYan
            ),
            Create<MaoXueWang>(
                3,
                "mao_xue_wang",
                Ingredients(
                    (duck, 1),
                    (strangeMeat, 1),
                    (tofu, 1),
                    (chili, 1),
                    (ancientPheromone, 1)),
                static (choiceContext, player, upgraded) =>
                    ApplyPower<MaoXueWangPower>(choiceContext, player, 1),
                upgradedCookingTurns: 2
            ),
            Create<KaiShuiBaiCai>(
                4,
                "kai_shui_bai_cai",
                Ingredients(
                    (leafyVegetable, 1),
                    (chicken, 1),
                    (pork, 1),
                    (duck, 1),
                    (ancientPheromone, 1)),
                ApplyKaiShuiBaiCai,
                upgradedCookingTurns: 3
            ),
            Create<ShenQianTangFen>(
                3,
                "shen_qian_tang_fen",
                Ingredients(
                    (snailMeat, 1),
                    (rice, 1),
                    (bambooShoot, 1),
                    (fish, 1),
                    (ancientPheromone, 1)),
                static (choiceContext, player, upgraded) =>
                    ApplyPower<ShenQianTangFenPower>(
                        choiceContext,
                        player,
                        upgraded ? 3 : 2)
            ),
            Create<PenCai>(
                3,
                "pen_cai",
                Ingredients(
                    (chicken, 1),
                    (pork, 1),
                    (fish, 1),
                    (duck, 1),
                    (ancientPheromone, 1)),
                static (choiceContext, player, upgraded) =>
                    ApplyPower<PenCaiPower>(choiceContext, player, 1),
                upgradedCookingTurns: 2
            ),
            Create<MaLaHuoGuo>(
                3,
                "ma_la_huo_guo",
                Ingredients(
                    (strangeMeat, 1),
                    (chili, 1),
                    (sichuanPepper, 1),
                    (bambooShoot, 1),
                    (leafyVegetable, 1),
                    (ancientPheromone, 1)),
                static (choiceContext, player, upgraded) =>
                    HaaLouLingCookingService.ChooseCommonUncommonAndRareDishEffects(
                        choiceContext,
                        player,
                        upgraded)
            ),
            Create<MoYuBao>(
                3,
                "mo_yu_bao",
                Ingredients(
                    (strangeMeat, 1),
                    (crab, 1),
                    (fish, 1),
                    (snailMeat, 1),
                    (shrimp, 1),
                    (ancientPheromone, 1)),
                ApplyMoYuBao,
                upgradedCookingTurns: 2
            )
        ];
    }

    private static CookingDishDefinition Create<TDish>(
        int cookingTurns,
        string slotTextureFileName,
        IReadOnlyDictionary<ModelId, int> ingredients,
        Func<PlayerChoiceContext, Player, bool, Task> onComplete,
        int? upgradedCookingTurns = null
    ) where TDish : CardModel
    {
        return new CookingDishDefinition
        {
            DishModelId = ModelDb.Card<TDish>().Id,
            CookingTurns = cookingTurns,
            UpgradedCookingTurns = upgradedCookingTurns,
            SlotTexturePath = $"{SlotTextureRoot}/{slotTextureFileName}.png",
            Ingredients = ingredients,
            CreatePreviewCard = static () => ModelDb.Card<TDish>(),
            OnComplete = onComplete
        };
    }

    private static IReadOnlyDictionary<ModelId, int> Ingredients(
        params (ModelId Id, int Count)[] ingredients
    ) => ingredients.ToDictionary(ingredient => ingredient.Id, ingredient => ingredient.Count);

    private static async Task ApplyPower<TPower>(
        PlayerChoiceContext choiceContext,
        Player player,
        int amount
    ) where TPower : PowerModel
    {
        await PowerCmd.Apply<TPower>(
            choiceContext,
            player.Creature,
            amount,
            player.Creature,
            null
        );
    }

    private static async Task ApplyZhangYuNingMengZhi(
        PlayerChoiceContext choiceContext,
        Player player,
        bool upgraded
    )
    {
        var requiredTurns = upgraded ? 4 : 8;
        if (HaaLouLingCookingService.GetRemainingCookingTurnsTotal(player) != requiredTurns)
        {
            return;
        }

        await ApplyPower<ZhangYuNingMengZhiPower>(choiceContext, player, 1);
    }

    private static async Task ApplyHuoBaoYaoHua(
        PlayerChoiceContext choiceContext,
        Player player,
        bool upgraded
    )
    {
        await ApplyPower<StrengthPower>(choiceContext, player, upgraded ? 5 : 3);
        await ApplyPower<HuoBaoYaoHuaPower>(choiceContext, player, 1);
    }

    private static async Task ApplyShengYan(
        PlayerChoiceContext choiceContext,
        Player player,
        bool upgraded
    )
    {
        await HaaLouLingCookingService.RemoveArtifactFromEnemies(player);
        await ApplyPower<ShengYanPower>(choiceContext, player, 1);
        if (upgraded)
        {
            HaaLouLingCookingService.AddShengYanDebilitate(player, 1);
        }
    }

    private static async Task ApplyKaiShuiBaiCai(
        PlayerChoiceContext choiceContext,
        Player player,
        bool upgraded
    )
    {
        await HaaLouLingCookingService.ClearDebuffs(player);
        await ApplyPower<KaiShuiBaiCaiPower>(choiceContext, player, 1);
    }

    private static async Task ApplyMoYuBao(
        PlayerChoiceContext choiceContext,
        Player player,
        bool upgraded
    )
    {
        await ApplyPower<MoYuBaoPower>(choiceContext, player, 1);
        await HaaLouLingCookingService.UpgradeAllCookingSlots(choiceContext, player);
    }
}

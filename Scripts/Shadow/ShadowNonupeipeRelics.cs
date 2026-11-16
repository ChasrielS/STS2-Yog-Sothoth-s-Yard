using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;

namespace YogSothothsYard.Scripts.Shadow;

public sealed class ShadowBlessedAntlerPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_blessed_antler";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(BlessedAntler), nameof(BlessedAntler.BeforeHandDraw),
            [typeof(Player), typeof(PlayerChoiceContext), typeof(ICombatState)])];

    public static bool Prefix(BlessedAntler __instance, Player player, ICombatState combatState, ref Task __result)
    {
        var rank = ShadowRanks.Of(__instance);
        if (rank < 2) return true;
        __result = player == __instance.Owner && player.PlayerCombatState?.TurnNumber == 1
            ? AddDazed(__instance, player, combatState, rank)
            : Task.CompletedTask;
        return false;
    }

    private static async Task AddDazed(BlessedAntler relic, Player player, ICombatState combatState, int rank)
    {
        relic.Flash();
        var cards = new List<CardModel>();
        for (var i = 0; i < relic.DynamicVars.Cards.IntValue; i++)
        {
            var card = combatState.CreateCard<Dazed>(player);
            ShadowRanks.Set(card, rank);
            cards.Add(card);
        }
        CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Draw, player, CardPilePosition.Random));
        await Cmd.Wait(3f);
    }
}

public sealed class ShadowBrilliantScarfCostPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_brilliant_scarf_cost";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(BrilliantScarf), "ShouldModifyCost", [typeof(CardModel)])];

    public static void Postfix(BrilliantScarf __instance, CardModel card, int ____cardsPlayedThisTurn, ref bool __result)
    {
        if (ShadowRanks.Of(__instance) == 3 && ____cardsPlayedThisTurn == 0 &&
            CombatManager.Instance.IsInProgress && card.Owner == __instance.Owner &&
            card.Pile?.Type is PileType.Hand or PileType.Play)
            __result = true;
    }
}

public sealed class ShadowBrilliantScarfPlayPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_brilliant_scarf_play";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(BrilliantScarf), nameof(BrilliantScarf.AfterCardPlayed), [typeof(PlayerChoiceContext), typeof(CardPlay)])];

    public static bool Prefix(BrilliantScarf __instance, CardPlay cardPlay, int ____cardsPlayedThisTurn,
        out int __state, ref Task __result)
    {
        __state = ____cardsPlayedThisTurn;
        if (cardPlay.IsFirstInSeries || ShadowRanks.Of(__instance) == 0) return true;
        __result = Task.CompletedTask;
        return false;
    }

    public static void Postfix(BrilliantScarf __instance, PlayerChoiceContext choiceContext, int ____cardsPlayedThisTurn,
        int __state, ref Task __result)
    {
        if (__state == ____cardsPlayedThisTurn || ____cardsPlayedThisTurn != __instance.DynamicVars.Cards.IntValue ||
            !ShadowRelicRules.TryGet(__instance, out var rule) || rule.Trigger != "Manual") return;
        __instance.Flash();
        __result = ShadowRelicRules.Charge(__instance, rule.Amount, choiceContext);
    }
}

public sealed class ShadowDelicateFrondPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_delicate_frond";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(DelicateFrond), nameof(DelicateFrond.BeforeCombatStart), Type.EmptyTypes)];

    public static bool Prefix(DelicateFrond __instance, ref Task __result)
    {
        var rank = ShadowRanks.Of(__instance);
        if (rank < 2) return true;
        __result = FillSlots(__instance, rank);
        return false;
    }

    private static async Task FillSlots(DelicateFrond relic, int rank)
    {
        relic.Flash();
        var player = relic.Owner;
        while (player.HasOpenPotionSlots &&
            ShadowPotionRules.RandomHigherPotion(player, player.PlayerRng.NonCombatGeneration, rank) is { } potion)
        {
            if (!(await PotionCmd.TryToProcure(potion, player)).success) break;
        }
    }
}

public sealed class ShadowDiamondDiademPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_diamond_diadem";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(DiamondDiadem), nameof(DiamondDiadem.AfterSideTurnStart),
            [typeof(CombatSide), typeof(IReadOnlyList<Creature>), typeof(ICombatState)])];

    public static bool Prefix(DiamondDiadem __instance, IReadOnlyList<Creature> participants, ref Task __result)
    {
        if (ShadowRanks.Of(__instance) != 3) return true;
        var player = __instance.Owner;
        __result = participants.Contains(player.Creature) && player.PlayerCombatState?.TurnNumber <= 1
            ? GainBlock(__instance)
            : Task.CompletedTask;
        return false;
    }

    private static async Task GainBlock(DiamondDiadem relic)
    {
        var creature = relic.Owner.Creature;
        relic.Flash();
        await CreatureCmd.GainBlock(creature, relic.DynamicVars.Block, null);
        await PowerCmd.Apply<BlurPower>(new ThrowingPlayerChoiceContext(), creature, 2m, creature, null);
    }
}

public sealed class ShadowGlitterPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_glitter";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(Glitter), nameof(Glitter.TryModifyCardRewardOptionsLate),
            [typeof(Player), typeof(List<CardCreationResult>), typeof(CardCreationOptions)])];

    public static void Postfix(Glitter __instance, List<CardCreationResult> cardRewards, bool __result)
    {
        if (!__result || ShadowRanks.Of(__instance) != 3) return;
        foreach (var reward in cardRewards)
            if (reward.Card.Enchantment is Glam glam && reward.ModifyingRelics.Contains(__instance))
                glam.Amount = 2;
    }
}

public sealed class ShadowGlamVarsPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_glam_vars";
    public static ModPatchTarget[] GetTargets() => [new(typeof(EnchantmentModel), "get_DynamicVars", Type.EmptyTypes)];

    public static void Postfix(EnchantmentModel __instance, DynamicVarSet __result)
    {
        if (__instance is Glam { Amount: > 1 } glam) __result["Times"].BaseValue = glam.Amount;
    }
}

public sealed class ShadowGlamDescriptionPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_glam_description";
    public static ModPatchTarget[] GetTargets() => [new(typeof(EnchantmentModel), "get_DynamicDescription", Type.EmptyTypes)];

    public static void Postfix(EnchantmentModel __instance, ref LocString __result)
    {
        if (__instance is not Glam { Amount: > 1 } ||
            LocString.GetIfExists("enchantments", "GLAM.shadowDescription") is not { } description) return;
        description.AddVariablesFrom(__result);
        __result = description;
    }
}

public sealed class ShadowJewelryBoxPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_jewelry_box";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(JewelryBox), nameof(JewelryBox.AfterObtained), Type.EmptyTypes)];

    public static bool Prefix(JewelryBox __instance, ref Task __result)
    {
        if (ShadowRanks.Of(__instance) != 3) return true;
        __result = AddUpgradedApotheosis(__instance.Owner);
        return false;
    }

    private static async Task AddUpgradedApotheosis(Player player)
    {
        var card = player.RunState.CreateCard<Apotheosis>(player);
        CardCmd.Upgrade(card, CardPreviewStyle.None);
        CardCmd.PreviewCardPileAdd([await CardPileCmd.Add(card, PileType.Deck)], 2f);
    }
}

public sealed class ShadowNonupeipeRelicTipsPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_nonupeipe_relic_tips";
    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(BlessedAntler), "get_ExtraHoverTips", Type.EmptyTypes),
        new(typeof(Glitter), "get_ExtraHoverTips", Type.EmptyTypes),
        new(typeof(JewelryBox), "get_ExtraHoverTips", Type.EmptyTypes)
    ];

    public static void Postfix(RelicModel __instance, ref IEnumerable<IHoverTip> __result)
    {
        var rank = ShadowRanks.Of(__instance);
        if (rank < 2) return;
        switch (__instance)
        {
            case BlessedAntler:
                var card = ModelDb.Card<Dazed>().ToMutable();
                ShadowRanks.Set(card, rank);
                __result = [HoverTipFactory.ForEnergy(__instance), .. HoverTipFactory.FromCardWithCardHoverTips(card)];
                break;
            case Glitter when rank == 3:
                __result = HoverTipFactory.FromEnchantment<Glam>(amount: 2);
                break;
            case JewelryBox when rank == 3:
                __result = HoverTipFactory.FromCardWithCardHoverTips<Apotheosis>(upgrade: true);
                break;
        }
    }
}

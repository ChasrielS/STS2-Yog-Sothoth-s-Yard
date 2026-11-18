using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.RunRngs;

namespace YogSothothsYard.Scripts.Shadow;

internal static class ShadowLoot
{
    internal static void RollCard(CardModel card, Player player)
    {
        var level = ShadowRun.Level(player);
        if (level == 0 || ShadowRanks.Of(card) > 0) return;
        (int[]? chances, int rarity) = card.Rarity switch
        {
            CardRarity.Common => (ShadowChances.Common, 0),
            CardRarity.Uncommon => (ShadowChances.Uncommon, 1),
            CardRarity.Rare => (ShadowChances.Rare, 2),
            CardRarity.Ancient => (ShadowChances.ShopAndAncient, 2),
            _ => (null, 0)
        };
        if (chances == null) return;
        var rng = ModRunRngRegistry.Get(player, Entry.ModId, "shadow_card_rewards");
        if (!ShadowChances.IsEroded(rng, level, chances)) return;
        var rank = ShadowChances.RollRank(rng, level, rarity);
        if (ShadowCardRules.TryGet(card, rank, out _)) ShadowRanks.Set(card, rank);
    }

    internal static void RollRelic(RelicModel relic, Player player, string stream = "shadow_relic_rewards")
    {
        var level = ShadowRun.Level(player);
        if (level == 0 || ShadowRanks.Of(relic) > 0) return;
        (int[]? chances, int rarity) = relic.Rarity switch
        {
            RelicRarity.Common => (ShadowChances.Common, 0),
            RelicRarity.Uncommon => (ShadowChances.Uncommon, 1),
            RelicRarity.Rare => (ShadowChances.Rare, 2),
            RelicRarity.Shop or RelicRarity.Ancient => (ShadowChances.ShopAndAncient, 2),
            _ => (null, 0)
        };
        if (chances == null) return;
        var rng = ModRunRngRegistry.Get(player, Entry.ModId, stream);
        if (!ShadowChances.IsEroded(rng, level, chances)) return;
        var rank = ShadowChances.RollRank(rng, level, rarity);
        if (ShadowRelicRules.Supports(relic, rank)) ShadowRanks.Set(relic, rank);
    }

    internal static void RollPotion(PotionModel potion, Player player)
    {
        var level = ShadowRun.Level(player);
        if (level == 0 || ShadowRanks.Of(potion) > 0) return;
        (int[]? chances, int rarity) = potion.Rarity switch
        {
            PotionRarity.Common => (ShadowChances.Common, 0),
            PotionRarity.Uncommon => (ShadowChances.Uncommon, 1),
            PotionRarity.Rare => (ShadowChances.Rare, 2),
            _ => (null, 0)
        };
        if (chances == null) return;
        var rng = ModRunRngRegistry.Get(player, Entry.ModId, "shadow_potion_rewards");
        if (!ShadowChances.IsEroded(rng, level, chances)) return;
        var rank = ShadowChances.RollRank(rng, level, rarity);
        if (ShadowPotionRules.Supports(potion, rank)) ShadowRanks.Set(potion, rank);
    }
}

public sealed class ShadowTreasureRelicsPatch : IPatchMethod
{
    private static readonly AccessTools.FieldRef<TreasureRoomRelicSynchronizer, IPlayerCollection>? PlayerCollection =
        AccessTools.Field(typeof(TreasureRoomRelicSynchronizer), "_playerCollection") is { } field
            ? AccessTools.FieldRefAccess<TreasureRoomRelicSynchronizer, IPlayerCollection>(field)
            : null;
    private static readonly AccessTools.FieldRef<TreasureRoomRelicSynchronizer, IRunState>? RunState =
        PlayerCollection == null
            ? AccessTools.FieldRefAccess<TreasureRoomRelicSynchronizer, IRunState>("_runState")
            : null;

    public static string PatchId => "yog_sothoths_yard_shadow_treasure_relics";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(TreasureRoomRelicSynchronizer), nameof(TreasureRoomRelicSynchronizer.BeginRelicPicking), Type.EmptyTypes)];

    public static void Postfix(TreasureRoomRelicSynchronizer __instance)
    {
        var players = PlayerCollection != null ? PlayerCollection(__instance).Players : RunState!(__instance).Players;
        var player = players[0];
        if (ShadowRun.Level(player) == 0 || __instance.CurrentRelics is not List<RelicModel> relics) return;
        for (var i = 0; i < relics.Count; i++)
        {
            var relic = relics[i];
            if (relic.IsMutable) continue;
            var preview = relic.ToMutable();
            ShadowLoot.RollRelic(preview, player, "shadow_treasure_relics");
            if (ShadowRanks.Of(preview) > 0)
            {
                ShadowTreasureRelicClonePatch.MarkPreview(preview);
                relics[i] = preview;
                ShadowItemDiscovery.Observe(preview);
            }
        }
    }
}

public sealed class ShadowTreasureRelicClonePatch : IPatchMethod
{
    private static readonly ConditionalWeakTable<RelicModel, Preview> Previews = new();

    public static string PatchId => "yog_sothoths_yard_shadow_treasure_relic_clone";
    public static ModPatchTarget[] GetTargets() => [new(typeof(RelicModel), nameof(RelicModel.ToMutable), Type.EmptyTypes)];

    internal static void MarkPreview(RelicModel relic) => Previews.Add(relic, new Preview());

    public static bool Prefix(RelicModel __instance, ref RelicModel __result)
    {
        if (!Previews.TryGetValue(__instance, out _)) return true;
        __result = __instance.CanonicalInstance.ToMutable();
        ShadowRanks.Set(__result, ShadowRanks.Of(__instance));
        return false;
    }

    private sealed class Preview { }
}

public sealed class ShadowMerchantCardsPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_merchant_cards";
    public static ModPatchTarget[] GetTargets()
    {
        const string method = nameof(CardFactory.CreateForMerchant);
        var cardList = typeof(IEnumerable<CardModel>);
        var player = typeof(Player);
        var withBlacklist = typeof(CardFactory).GetMethod(method, [player, cardList, cardList, typeof(CardType)]) != null;
        Type[] commonParameters = withBlacklist ? [player, cardList, cardList] : [player, cardList];
        return
        [
            new(typeof(CardFactory), method, [.. commonParameters, typeof(CardType)]),
            new(typeof(CardFactory), method, [.. commonParameters, typeof(CardRarity)])
        ];
    }
    public static void Postfix(Player player, CardCreationResult __result) => ShadowLoot.RollCard(__result.Card, player);
}

public sealed class ShadowCardRewardsPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_card_rewards";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(CardFactory), nameof(CardFactory.CreateForReward), [typeof(Player), typeof(int), typeof(CardCreationOptions)])];

    public static void Prefix(Player player, CardCreationOptions options) =>
        ShadowEvents.FilterEventRewardCards(player, options);

    public static void Postfix(Player player, CardCreationOptions options, ref IEnumerable<CardCreationResult> __result)
    {
        if (ShadowRun.Level(player) == 0) return;
        if (ShadowEvents.TryErodeRewardCards(player, ref __result)) return;
        if (options.Source is not (CardCreationSource.Encounter or CardCreationSource.Shop)) return;
        var rewards = __result.ToList();
        foreach (var reward in rewards) ShadowLoot.RollCard(reward.Card, player);
        __result = rewards;
    }
}

public sealed class ShadowLootRewardsPatch : IPatchMethod
{
    internal static readonly Action<PotionReward, PotionModel> SetPotion =
        AccessTools.MethodDelegate<Action<PotionReward, PotionModel>>(AccessTools.PropertySetter(typeof(PotionReward), nameof(PotionReward.Potion)));
    public static string PatchId => "yog_sothoths_yard_shadow_loot_rewards";
    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(RelicReward), nameof(RelicReward.Populate), Type.EmptyTypes),
        new(typeof(PotionReward), nameof(PotionReward.Populate), Type.EmptyTypes)
    ];

    public static void Prefix(Reward __instance, out (bool WasEmpty, object? Choice) __state)
    {
        __state = (!__instance.IsPopulated, ShadowEvents.BeginReward(__instance));
        if (__state.WasEmpty && __instance is PotionReward potion) ShadowRelicPotionRewards.Prepare(potion);
    }

    public static void Postfix(Reward __instance, (bool WasEmpty, object? Choice) __state)
    {
        ShadowEvents.RestoreChoice(__state.Choice);
        if (!__state.WasEmpty || ShadowRun.Level(__instance.Player) == 0) return;
        switch (__instance)
        {
            case RelicReward { Relic: { } relic } reward:
                if (!ShadowEvents.TagRelicReward(reward, relic)) ShadowLoot.RollRelic(relic, reward.Player);
                break;
            case PotionReward { Potion: { } potion } reward:
                var selected = potion;
                if (!ShadowEvents.TagPotionReward(reward, ref selected)) ShadowLoot.RollPotion(potion, reward.Player);
                else if (selected != potion) SetPotion(reward, selected);
                break;
        }
    }
}

public sealed class ShadowMerchantStockPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_merchant_stock";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(MerchantInventory), nameof(MerchantInventory.CreateForNormalMerchant), [typeof(Player)])];

    public static void Postfix(Player player, MerchantInventory __result)
    {
        foreach (var entry in __result.RelicEntries)
            if (entry.Model is { } relic) RollStock(relic, player);
        foreach (var entry in __result.PotionEntries)
            if (entry.Model is { } potion) RollStock(potion, player);
    }

    internal static void RollStock(RelicModel relic, Player player)
    {
        ShadowLoot.RollRelic(relic, player);
        if (LocalContext.IsMe(player)) ShadowItemDiscovery.Observe(relic);
    }

    internal static void RollStock(PotionModel potion, Player player)
    {
        ShadowLoot.RollPotion(potion, player);
        if (LocalContext.IsMe(player)) ShadowItemDiscovery.Observe(potion);
    }
}

public sealed class ShadowMerchantRestockPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_merchant_restock";
    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(MerchantRelicEntry), "RestockAfterPurchase", [typeof(MerchantInventory)]),
        new(typeof(MerchantPotionEntry), "RestockAfterPurchase", [typeof(MerchantInventory)])
    ];

    public static void Postfix(MerchantEntry __instance, Player ____player)
    {
        switch (__instance)
        {
            case MerchantRelicEntry { Model: { } relic }: ShadowMerchantStockPatch.RollStock(relic, ____player); break;
            case MerchantPotionEntry { Model: { } potion }: ShadowMerchantStockPatch.RollStock(potion, ____player); break;
        }
    }
}


using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Odds;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;

namespace YogSothothsYard.Scripts.Shadow;

public sealed class ShadowRelicCardRewardsPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_relic_card_rewards";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(WhiteStar), nameof(WhiteStar.TryModifyRewards), [typeof(Player), typeof(List<Reward>), typeof(AbstractRoom)]),
         new(typeof(PrayerWheel), nameof(PrayerWheel.TryModifyRewards), [typeof(Player), typeof(List<Reward>), typeof(AbstractRoom)])];

    public static void Postfix(RelicModel __instance, Player player, List<Reward> rewards, bool __result)
    {
        if (!__result || ShadowRanks.Of(__instance) != 3) return;
        var type = __instance is WhiteStar ? RoomType.Boss : RoomType.Monster;
        rewards.Add(new CardReward(CardCreationOptions.ForRoom(player, type), 3, player));
    }
}

public sealed class ShadowLastingCandyPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_lasting_candy";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(LastingCandy), "get_IsInTriggeringCombat", Type.EmptyTypes),
         new(typeof(LastingCandy), "get_ShowCounter", Type.EmptyTypes)];

    public static void Postfix(LastingCandy __instance, MethodBase __originalMethod, ref bool __result)
    {
        if (ShadowRanks.Of(__instance) == 3) __result = __originalMethod.Name == "get_IsInTriggeringCombat";
    }
}

public sealed class ShadowRelicRewardChargePatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_relic_reward_charge";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(Hook), nameof(Hook.AfterModifyingRewards), [typeof(IRunState), typeof(IEnumerable<AbstractModel>)]),
         new(typeof(Hook), nameof(Hook.AfterModifyingCardRewardOptions), [typeof(IRunState), typeof(IEnumerable<AbstractModel>)])];

    public static void Postfix(MethodBase __originalMethod, IEnumerable<AbstractModel> modifiers, ref Task __result)
    {
        var trigger = __originalMethod.Name == nameof(Hook.AfterModifyingRewards) ? "CombatReward" : "CardReward";
        var relics = modifiers.OfType<RelicModel>().Where(relic =>
            ShadowRelicRules.TryGet(relic, out var rule) && rule.Trigger == trigger).ToArray();
        if (relics.Length > 0) __result = ChargeAfter(__result, relics);
    }

    private static async Task ChargeAfter(Task original, IReadOnlyList<RelicModel> relics)
    {
        await original;
        foreach (var relic in relics)
            if (ShadowRelicRules.TryGet(relic, out var rule))
                await ShadowRelicRules.Charge(relic, rule.Amount);
    }
}

internal static class ShadowRelicPotionRewards
{
    private static readonly ConditionalWeakTable<PotionReward, RelicModel> Sources = new();
    private static readonly AccessTools.FieldRef<Reward, Rng?> OverrideRng =
        AccessTools.FieldRefAccess<Reward, Rng?>("_rngOverride");

    internal static void Mark(IEnumerable<Reward> rewards, RelicModel relic)
    {
        foreach (var reward in rewards.OfType<PotionReward>()) Sources.GetValue(reward, _ => relic);
    }

    internal static void Prepare(PotionReward reward)
    {
        if (!Sources.TryGetValue(reward, out var relic)) return;
        var player = reward.Player;
        var rng = OverrideRng(reward) ?? player.PlayerRng.Rewards;
        if (ShadowPotionRules.RandomHigherPotion(player, rng, ShadowRanks.Of(relic)) is { } potion)
            ShadowLootRewardsPatch.SetPotion(reward, potion);
    }

    internal static async Task ChargeWhiteBeast(Task original, RewardsSet rewards)
    {
        await original;
        foreach (var reward in rewards.Rewards.OfType<PotionReward>())
            if (Sources.TryGetValue(reward, out var relic) && relic is WhiteBeastStatue &&
                ShadowRelicRules.TryGet(relic, out var rule))
            {
                Sources.Remove(reward);
                await ShadowRelicRules.Charge(relic, rule.Amount);
            }
    }
}

public sealed class ShadowTinyMailboxPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_tiny_mailbox";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(TinyMailbox), nameof(TinyMailbox.TryModifyRestSiteHealRewards), [typeof(Player), typeof(List<Reward>), typeof(bool)])];

    public static void Postfix(TinyMailbox __instance, List<Reward> rewards, bool __result)
    {
        if (__result && ShadowRanks.Of(__instance) >= 2) ShadowRelicPotionRewards.Mark(rewards.TakeLast(2), __instance);
    }
}

public sealed class ShadowWhiteBeastRewardPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_white_beast_reward";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(RewardsSet), "RollForPotionAndAddTo", [typeof(ICollection<Reward>), typeof(Player), typeof(RoomType)])];

    public static void Prefix(ICollection<Reward> rewards, out int __state) => __state = rewards.Count;

    public static void Postfix(ICollection<Reward> rewards, Player player, int __state)
    {
        if (rewards.Count > __state && player.GetRelic<WhiteBeastStatue>() is { } relic && ShadowRanks.Of(relic) >= 2)
            ShadowRelicPotionRewards.Mark(rewards.Skip(__state), relic);
    }
}

public sealed class ShadowWhiteBeastChargePatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_white_beast_charge";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(RewardsSet), nameof(RewardsSet.GenerateWithoutOffering), Type.EmptyTypes)];

    public static void Postfix(RewardsSet __instance, ref Task __result)
    {
        if (__instance.Player.GetRelic<WhiteBeastStatue>() is { } relic && ShadowRanks.Of(relic) >= 2)
            __result = ShadowRelicPotionRewards.ChargeWhiteBeast(__result, __instance);
    }
}

public sealed class ShadowDreamCatcherSelectionPatch : IPatchMethod
{
    private static readonly ConditionalWeakTable<CardReward, FirstSelection> FirstSelections = new();

    public static string PatchId => "yog_sothoths_yard_shadow_dream_catcher_selection";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(Hook), nameof(Hook.ShouldAllowSelectingMoreCardRewards), [typeof(IRunState), typeof(Player), typeof(CardReward)])];

    internal static void Mark(CardReward reward) => FirstSelections.Add(reward, new FirstSelection());

    public static void Postfix(CardReward reward, ref bool __result)
    {
        if (FirstSelections.Remove(reward)) __result = reward.Cards.Skip(1).Any();
    }

    private sealed class FirstSelection { }
}

public sealed class ShadowRestSiteRelicsPatch : IPatchMethod
{
    internal static readonly Func<RestSiteOption, Player> Owner =
        AccessTools.MethodDelegate<Func<RestSiteOption, Player>>(AccessTools.PropertyGetter(typeof(RestSiteOption), "Owner"));

    public static string PatchId => "yog_sothoths_yard_shadow_rest_site_relics";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(DigRestSiteOption), nameof(DigRestSiteOption.OnSelect), Type.EmptyTypes),
         new(typeof(LiftRestSiteOption), nameof(LiftRestSiteOption.OnSelect), Type.EmptyTypes)];

    public static bool Prefix(RestSiteOption __instance, ref Task<bool> __result)
    {
        if (__instance is not DigRestSiteOption || Owner(__instance).GetRelic<Shovel>() is not { } shovel ||
            ShadowRanks.Of(shovel) is not (> 1 and var rank)) return true;
        __result = Dig(shovel, rank);
        return false;
    }

    public static void Postfix(RestSiteOption __instance, ref Task<bool> __result)
    {
        if (__instance is LiftRestSiteOption && Owner(__instance).GetRelic<Girya>() is { } relic && ShadowRanks.Of(relic) == 3)
            __result = ChargeLift(__result, relic);
    }

    private static async Task<bool> Dig(Shovel relic, int rank)
    {
        var player = relic.Owner;
        var rarity = rank == 3 || player.PlayerRng.Rewards.NextInt(50) < 17 ? RelicRarity.Rare : RelicRarity.Uncommon;
        var reward = RelicFactory.PullNextRelicFromFront(player, rarity,
            candidate => candidate.Rarity == RelicRarity.Rare || rank == 2 && candidate.Rarity == RelicRarity.Uncommon).ToMutable();
        await RelicCmd.Obtain(reward, player);
        await ShadowRelicRules.Charge(relic, rank == 3 ? 30 : 21);
        return true;
    }

    private static async Task<bool> ChargeLift(Task<bool> original, Girya relic)
    {
        var selected = await original;
        if (selected) await ShadowRelicRules.Charge(relic, 30);
        return selected;
    }
}

public sealed class ShadowGiryaOptionDescriptionPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_girya_option_description";
    public static ModPatchTarget[] GetTargets() => [new(typeof(LiftRestSiteOption), "get_Description", Type.EmptyTypes)];

    public static void Postfix(LiftRestSiteOption __instance, ref LocString __result)
    {
        if (ShadowRestSiteRelicsPatch.Owner(__instance).GetRelic<Girya>() is not { } relic || ShadowRanks.Of(relic) != 3) return;
        var description = NShadowSelector.Loc("GIRYA_LIFT");
        description.AddVariablesFrom(__result);
        __result = description;
    }
}

public sealed class ShadowJuzuTreasureOddsPatch : IPatchMethod
{
    private const float TreasureBase = 0.03f;
    public static string PatchId => "yog_sothoths_yard_shadow_juzu_treasure_odds";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(UnknownMapPointOdds), nameof(UnknownMapPointOdds.Roll), [typeof(IEnumerable<RoomType>), typeof(IRunState)])];

    public static void Prefix(UnknownMapPointOdds __instance, IRunState runState, Dictionary<RoomType, float> ____baseOdds,
        out float __state)
    {
        __state = 0f;
        if (!runState.Players.Any(player => player.GetRelic<JuzuBracelet>() is { } relic && ShadowRanks.Of(relic) == 2)) return;
        var previousBase = ____baseOdds[RoomType.Treasure];
        if (previousBase <= 0f || previousBase == TreasureBase || __instance.TreasureOdds < 0f) return;
        __state = previousBase;
        __instance.TreasureOdds *= TreasureBase / previousBase;
        __instance.SetBaseOdds(RoomType.Treasure, TreasureBase);
    }

    public static void Postfix(UnknownMapPointOdds __instance, float __state)
    {
        if (__state <= 0f) return;
        __instance.TreasureOdds *= __state / TreasureBase;
        __instance.SetBaseOdds(RoomType.Treasure, __state);
    }
}

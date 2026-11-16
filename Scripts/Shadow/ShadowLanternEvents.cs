using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;

namespace YogSothothsYard.Scripts.Shadow;

internal static partial class ShadowEvents
{
    internal static IEnumerable<CardModel> ErodedLanternKeys(Player player) =>
        player.Deck.Cards.Where(card => card is LanternKey && ShadowRanks.Of(card) > 0);

    internal static bool IsHistorianChoice(Player player) =>
        ActiveChoice.Value is { EventId: "WAR_HISTORIAN_REPY", Player: var owner } && owner == player;
}

public sealed class ShadowLanternRewardPatch : IPatchMethod
{
    private static readonly AccessTools.FieldRef<SpecialCardReward, CardModel?> RewardCard =
        AccessTools.FieldRefAccess<SpecialCardReward, CardModel?>("_card");

    public static string PatchId => "yog_sothoths_yard_shadow_lantern_reward";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(Hook), nameof(Hook.AfterCombatVictory), [typeof(IRunState), typeof(ICombatState), typeof(CombatRoom)])];

    public static void Postfix(CombatRoom room, ref Task __result)
    {
        if (room.Encounter is not MysteriousKnightEventEncounter { HaveMonstersBeenGenerated: true } encounter) return;
        var rank = encounter.MonstersWithSlots.Select(slot => ShadowMonsterRules.Rank(slot.Item1)).FirstOrDefault();
        if (rank > 0) __result = GiveKeys(__result, room, rank);
    }

    private static async Task GiveKeys(Task original, CombatRoom room, int rank)
    {
        await original;
        foreach (var rewards in room.ExtraRewards.Values)
        {
            foreach (var reward in rewards.OfType<SpecialCardReward>().ToArray())
            {
                if (RewardCard(reward) is not LanternKey key) continue;
                ShadowRanks.Set(key, rank);
                var result = await CardPileCmd.Add(key, PileType.Deck);
                if (!result.success) continue;
                rewards.Remove(reward);
                CardCmd.PreviewCardPileAdd(result, 2f);
            }
        }
    }
}

public sealed class ShadowHistorianKeysPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_historian_keys";
    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(WarHistorianRepy), "RemoveFirstLanternKey", Type.EmptyTypes),
        new(typeof(WarHistorianRepy), "RemoveLanternKeysForSecondChoice", Type.EmptyTypes)
    ];

    public static bool Prefix(WarHistorianRepy __instance, MethodBase __originalMethod, ref Task __result)
    {
        var player = __instance.Owner;
        if (player == null || !ShadowEvents.IsHistorianChoice(player)) return true;
        var keys = ShadowEvents.ErodedLanternKeys(player);
        __result = Spend(__originalMethod.Name == "RemoveFirstLanternKey" ? keys.Take(1) : keys);
        return false;
    }

    private static async Task Spend(IEnumerable<CardModel> keys)
    {
        foreach (var key in keys.ToArray())
        {
            PlayerCmd.CompleteQuest(key);
            await CardPileCmd.RemoveFromDeck(key);
        }
    }
}

public sealed class ShadowHistorianSecondRewardPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_historian_second_reward";
    public static ModPatchTarget[] GetTargets() => [new(typeof(WarHistorianRepy), "get_ShouldGetSecondReward", Type.EmptyTypes)];

    public static void Postfix(WarHistorianRepy __instance, ref bool __result)
    {
        if (__result && __instance.Owner is { } player && ShadowEvents.IsHistorianChoice(player))
            __result = ShadowEvents.ErodedLanternKeys(player).Any();
    }
}

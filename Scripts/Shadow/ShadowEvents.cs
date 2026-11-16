using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Patching.Models;

namespace YogSothothsYard.Scripts.Shadow;

internal static partial class ShadowEvents
{
    private static readonly AsyncLocal<Choice?> ActiveChoice = new();
    private static readonly ConditionalWeakTable<EventOption, Choice> Choices = new();
    private static readonly ConditionalWeakTable<Reward, Choice> CombatRewards = new();
    private static readonly Action<EventOption, LocString> SetOptionDescription =
        AccessTools.MethodDelegate<Action<EventOption, LocString>>(AccessTools.PropertySetter(typeof(EventOption), nameof(EventOption.Description)));

    internal static bool IsEroded(EventModel model)
    {
        if (model.Owner is not { } player || !EditedEvents.Contains(model.Id.Entry)) return false;
        var level = ShadowRun.Level(player);
        if (level == 0) return false;
        if (model is WarHistorianRepy) return ErodedLanternKeys(player).Any();
        return IsEroded(player.RunState, model.Id.Entry, level);
    }

    private static bool IsEroded(IRunState run, string eventId, int level) =>
        ShadowChances.IsEroded(EventRng(run, eventId), level, ShadowChances.Event);

    private static Rng EventRng(IRunState run, string id) =>
        new(unchecked(run.Rng.Seed + (ulong)run.TotalFloor * 3613 + (ulong)StringHelper.GetDeterministicHashCode(id)));

    internal static int LanternKnightRank(CombatState combat, int level)
    {
        if (combat.Encounter is not MysteriousKnightEventEncounter) return 0;
        const string eventId = "THE_LANTERN_KEY";
        return IsEroded(combat.RunState, eventId, level)
            ? ShadowChances.RollRank(EventRng(combat.RunState, eventId + "_knight"), level, 1)
            : 0;
    }

    internal static bool ForcesMonster(CombatState combat, MonsterModel monster)
    {
        if (monster.Id.Entry == "FAKE_MERCHANT_MONSTER") return true;
        if (monster.Id.Entry != "PUNCH_CONSTRUCT") return false;
        return combat.Encounter is PunchOffEventEncounter &&
            IsEroded(combat.RunState, "PUNCH_OFF", ShadowRun.Level(combat.RunState as RunState));
    }

    internal static void ChangeState(EventModel model, ref LocString description, ref IEnumerable<EventOption> eventOptions)
    {
        if (model.Owner is not { } player || ShadowRun.Level(player) == 0) return;
        if (!IsEroded(model)) return;
        var options = eventOptions.ToArray();
        if (LocString.GetIfExists("events", description.LocEntryKey + ".shadow") is { } narrative)
        {
            narrative.AddVariablesFrom(description);
            description = narrative;
        }
        if (description.LocEntryKey == "TRIAL.trialFormat" && options.FirstOrDefault() is { } verdict)
        {
            var caseKey = verdict.TextKey.Split('.')[2];
            if (LocString.GetIfExists("events", $"TRIAL.pages.{caseKey}.description.shadow") is { } story)
                description.Add("TrialStory", story);
        }
        foreach (var option in options)
        {
            if (option.IsLocked || option.IsProceed) continue;
            var choice = ForOption(model, player, option);
            if (choice == null) continue;
            Decorate(model, option, choice);
        }
        eventOptions = options;
    }

    private static void Decorate(EventModel model, EventOption option, Choice choice)
    {
        if (Choices.TryGetValue(option, out _)) return;
        Choices.Add(option, choice);
        var roundTea = choice is { EventId: "ROUND_TEA_PARTY", OptionKey: "ENJOY_TEA" };
        if (roundTea)
            option.BeforeChosen += _ => CreatureCmd.GainMaxHp(choice.Player.Creature,
                Math.Floor(choice.Player.Creature.MaxHp * 0.33m));
        if (!PrepareRewardPreviews(model, option, choice)) return;
        var key = choice.EventId == "DOLL_ROOM" ? option.Description.LocEntryKey : option.TextKey + ".description";
        if (LocString.GetIfExists("events", key + ".shadow") is not { } text) return;

        text.AddVariablesFrom(option.Description);
        if (roundTea) text.Add("ShadowMaxHp", Math.Floor(choice.Player.Creature.MaxHp * 0.33m));
        var erosion = choice.Amount;
        if (choice.Mode == Effects.Bridge)
        {
            erosion = model.DynamicVars["HpLoss"].IntValue * 5;
            option.ThatWillKillPlayerIf(player => ShadowRun.Erosion(player) >= ShadowRun.ErosionLimit &&
                player.Creature.CurrentHp + player.Creature.Block <= erosion);
        }
        text.Add("Erosion", erosion);
        model.DynamicVars.AddTo(text);
        SetOptionDescription(option, text);
    }

    internal static void BeginChoice(EventOption option, out (object? Previous, object? Chosen) state)
    {
        var previous = ActiveChoice.Value;
        if (option.WasChosen || !Choices.TryGetValue(option, out var choice))
        {
            state = (previous, null);
            return;
        }
        ActiveChoice.Value = choice;
        state = (previous, choice);
    }

    internal static void EndChoice((object? Previous, object? Chosen) state, ref Task result)
    {
        ActiveChoice.Value = (Choice?)state.Previous;
        if (state.Chosen is Choice { Amount: > 0, ChargesAfterDamage: false } choice)
            result = CompleteChoice(result, choice);
    }

    private static async Task CompleteChoice(Task original, Choice choice)
    {
        await original;
        await ShadowRun.AddErosion(choice.Player, choice.Amount);
    }

    private static Choice? ForPlayer(Player player, Effects mode) =>
        ActiveChoice.Value is { } choice && choice.Player == player && choice.Mode.HasFlag(mode) ? choice : null;

    private static int RollRank(Choice choice, int rarity) => choice.Rank > 0
        ? choice.Rank : ShadowChances.RollRank(choice.Rng, ShadowRun.Level(choice.Player), rarity);

    private static int CardTier(CardModel card) => card.Rarity switch
    {
        CardRarity.Basic or CardRarity.Common => 0,
        CardRarity.Rare or CardRarity.Ancient => 2,
        _ => 1
    };

    private static int RelicTier(RelicRarity rarity) => rarity switch
    {
        RelicRarity.Common => 0,
        RelicRarity.Rare or RelicRarity.Shop or RelicRarity.Ancient => 2,
        _ => 1
    };

    internal static void FilterEventRewardCards(Player player, CardCreationOptions options)
    {
        if (ForPlayer(player, Effects.CardReward) is not { } choice) return;
        var original = options.CardPoolFilter;
        options.WithFilter(card => (original?.Invoke(card) ?? true) && (choice.EventId == "THE_FUTURE_OF_POTIONS"
            ? ShadowCardRules.TryGet(card, choice.Rank, out _)
            : ShadowCardRules.Available(card).Any()));
    }

    internal static object? BeginPotionTrade(TheFutureOfPotions model, PotionModel potion)
    {
        var previous = ActiveChoice.Value;
        var rank = ShadowRanks.Of(potion);
        if (rank > 0 && model.Owner is { } player && IsEroded(model))
            ActiveChoice.Value = new Choice(player, model.Id.Entry, Effects.CardReward, rank);
        return previous;
    }

    private static int EventCardRank(CardModel card, Choice choice)
    {
        if (choice.CardRanks.TryGetValue(card.Id, out var stored)) return stored;
        var tier = (choice.EventId, card) switch
        {
            ("ROOM_FULL_OF_CHEESE" or "TRASH_HEAP", _) or
            ("THE_LEGENDS_WERE_TRUE", SpoilsMap) or
            ("BYRDONIS_NEST", ByrdonisEgg) or
            ("WOOD_CARVINGS", Peck or ToricToughness) or
            ("ENDLESS_CONVEYOR", FeedingFrenzy) or
            ("WATERFALL_OF_OBVIOUS_SECRETS", DingyDagger or Guilty) => 0,
            (_, MightyWeapon or MightyShield) => 2,
            _ => CardTier(card)
        };
        return ResolveCardRank(card, RollRank(choice, tier));
    }

    private static int ResolveCardRank(CardModel card, int rank) => ShadowCardRules.TryGet(card, rank, out _) ? rank :
        ShadowCardRules.Available(card).FirstOrDefault();

    internal static bool TryErodeRewardCards(Player player, ref IEnumerable<CardCreationResult> rewards)
    {
        if (ForPlayer(player, Effects.CardReward) is not { } choice) return false;
        var results = rewards.ToList();
        foreach (var result in results)
        {
            var card = result.Card;
            var rank = EventCardRank(card, choice);
            if (rank > 0) ShadowRanks.Set(card, rank);
        }
        rewards = results;
        return true;
    }

    internal static void TagCardAdded(CardModel card, CardPile pile)
    {
        if (pile.Type != PileType.Deck || ShadowRanks.Of(card) > 0 || ActiveChoice.Value is not { } choice ||
            card.Owner != choice.Player) return;
        if (choice.Mode.HasFlag(Effects.Card) || choice.Mode.HasFlag(Effects.Transform))
        {
            var rank = EventCardRank(card, choice);
            if (rank > 0) ShadowRanks.Set(card, rank);
        }
    }

    internal static void TagUpgrade(CardModel card)
    {
        if (card.Pile?.Type == PileType.Deck && ForPlayer(card.Owner, Effects.Upgrade) is { } choice)
            ErodeNext(card, choice);
    }

    internal static void TagEnchant(CardModel card)
    {
        if (card.Pile?.Type == PileType.Deck && ForPlayer(card.Owner, Effects.Enchant) is { } choice)
            ErodeNext(card, choice);
    }

    private static void ErodeNext(CardModel card, Choice choice)
    {
        var current = ShadowRanks.Of(card);
        var rank = current == 0
            ? ResolveCardRank(card, RollRank(choice, CardTier(card)))
            : ShadowCardRules.NextRank(card);
        if (rank > current) ShadowRanks.Set(card, rank);
    }

    internal static void PrepareRelic(RelicModel relic, Player player)
    {
        if (ShadowRanks.Of(relic) > 0 || ForPlayer(player, Effects.Relic) is not { } choice) return;
        TagRelic(relic, choice);
    }

    internal static void PreparePotion(ref PotionModel potion, Player player)
    {
        if (ShadowRanks.Of(potion) > 0 || ForPlayer(player, Effects.Potion) is not { } choice) return;
        TagPotion(ref potion, choice);
    }

    private static void TagRelic(RelicModel relic, Choice choice)
    {
        var rank = choice.RelicRanks.TryGetValue(relic.Id, out var stored) ? stored : RollRank(choice, EventRelicTier(relic, choice));
        var available = ResolveRelicRank(relic, rank);
        if (available > 0) ShadowRanks.Set(relic, available);
    }

    private static int EventRelicTier(RelicModel relic, Choice choice) => choice.EventId switch
    {
        "ROOM_FULL_OF_CHEESE" or "TEA_MASTER" or "SUNKEN_STATUE" or "TRASH_HEAP" or "DROWNING_BEACON" => 0,
        "HUNGRY_FOR_MUSHROOMS" => 2,
        _ when choice.EventId != "WELCOME_TO_WONGOS" && choice.Mode.HasFlag(Effects.RandomRelic) => 1,
        _ => relic is HistoryCourse or RoyalPoison or ForgottenSoul or MightyWings or BathTowel or BathRobes
            ? 2 : RelicTier(relic.Rarity)
    };

    private static int ResolveRelicRank(RelicModel relic, int rank) => ShadowRelicRules.Supports(relic, rank) ? rank :
        Enumerable.Range(1, 3).FirstOrDefault(candidate => ShadowRelicRules.Supports(relic, candidate));

    private static int EventPotionTier(Choice choice) => choice.EventId == "DROWNING_BEACON" ? 0 : 1;

    private static int ResolvePotionRank(PotionModel potion, int rank) => ShadowPotionRules.Supports(potion, rank) ? rank :
        Enumerable.Range(1, 3).FirstOrDefault(candidate => ShadowPotionRules.Supports(potion, candidate));

    private static void TagPotion(ref PotionModel potion, Choice choice)
    {
        if (ShadowRanks.Of(potion) > 0) return;
        if (potion.Rarity != PotionRarity.Event && ResolvePotionRank(potion, 1) == 0)
        {
            var rarity = potion.Rarity;
            var anyRarity = choice.EventId is "THE_LEGENDS_WERE_TRUE" or "WELLSPRING" or "ENDLESS_CONVEYOR" or
                "BATTLEWORN_DUMMY" or "WUPPY_IN_TROUBLE";
            var candidates = PotionFactory.GetPotionOptions(choice.Player).Where(candidate =>
                (anyRarity || candidate.Rarity == rarity) && ResolvePotionRank(candidate, 1) > 0);
            if (choice.Rng.NextItem(candidates) is { } replacement) potion = replacement.ToMutable();
        }
        var rank = ResolvePotionRank(potion, RollRank(choice, EventPotionTier(choice)));
        if (rank > 0) ShadowRanks.Set(potion, rank);
    }

    internal static bool TagRelicReward(RelicReward reward, RelicModel relic)
    {
        var choice = CombatRewards.TryGetValue(reward, out var stored) ? stored : ForPlayer(reward.Player, Effects.Relic);
        if (choice == null) return false;
        if (ShadowRanks.Of(relic) == 0) TagRelic(relic, choice);
        return true;
    }

    internal static bool TagPotionReward(PotionReward reward, ref PotionModel potion)
    {
        var choice = CombatRewards.TryGetValue(reward, out var stored) ? stored : ForPlayer(reward.Player, Effects.Potion);
        if (choice == null) return false;
        TagPotion(ref potion, choice);
        return true;
    }

    internal static void TrackCombatRewards(IReadOnlyList<Reward> rewards)
    {
        if (ActiveChoice.Value is not { } choice) return;
        foreach (var reward in rewards)
        {
            if ((reward is RelicReward && choice.Mode.HasFlag(Effects.Relic)) ||
                (reward is PotionReward && choice.Mode.HasFlag(Effects.Potion)))
                CombatRewards.GetValue(reward, _ => choice);
        }
    }

    internal static void ForceRelicReward(RelicReward reward, Player player, int rank) =>
        CombatRewards.GetValue(reward, _ => new Choice(player, "WONGOS_MYSTERY_TICKET", RandomRelic, rank));

    internal static object? BeginReward(Reward reward)
    {
        var previous = ActiveChoice.Value;
        if (CombatRewards.TryGetValue(reward, out var choice)) ActiveChoice.Value = choice;
        return previous;
    }

    internal static void PrepareAlreadyGeneratedRewards(Player player, List<Reward> rewards)
    {
        if (ActiveChoice.Value?.Player != player) return;
        foreach (var reward in rewards)
        {
            if (reward is RelicReward { Relic: { } relic } relicReward) TagRelicReward(relicReward, relic);
            else if (reward is PotionReward { Potion: { } potion } potionReward)
            {
                var selected = potion;
                if (TagPotionReward(potionReward, ref selected) && selected != potion)
                    ShadowLootRewardsPatch.SetPotion(potionReward, selected);
            }
        }
    }

    internal static int FilterRelics(Player player, RelicRarity rarity, ref Func<RelicModel, bool> filter)
    {
        if (ForPlayer(player, Effects.RandomRelic) is not { } choice) return 0;
        var original = filter;
        if (choice.Mode.HasFlag(Effects.ListedRelic))
        {
            filter = relic => original(relic) && (choice.EventId == "RELIC_TRADER" || relic.Rarity == rarity) &&
                ResolveRelicRank(relic, 1) > 0;
            return 0;
        }
        if (choice.EventId != "WELCOME_TO_WONGOS" && choice.Rank == 0)
        {
            filter = relic => original(relic) && ResolveRelicRank(relic, 1) > 0;
            return 0;
        }
        var rank = RollRank(choice, RelicTier(rarity));
        filter = relic => relic.Rarity == rarity && original(relic) && ShadowRelicRules.Supports(relic, rank);
        return rank;
    }

    internal static object? BeginRelicOffers(EventModel model)
    {
        var previous = ActiveChoice.Value;
        if (model.Owner is { } player && IsEroded(model))
            ActiveChoice.Value = new Choice(player, model.Id.Entry, RandomRelic | Effects.ListedRelic);
        return previous;
    }

    internal static void RecordRelicRank(Player player, RelicModel relic, int rank)
    {
        if (rank > 0 && ForPlayer(player, Effects.RandomRelic) is { } choice)
            choice.RelicRanks[relic.Id] = rank;
    }

    internal static bool ConvertBridgeDamage(Creature creature, decimal damage, out int erosion)
    {
        erosion = 0;
        if (ActiveChoice.Value is not { Mode: Effects.Bridge, Player: { } player } ||
            creature != player.Creature) return false;
        erosion = (int)damage * 5;
        return true;
    }

    internal static async Task ApplyBridgeErosion(Player player, int amount, PlayerChoiceContext context)
    {
        var previous = ActiveChoice.Value;
        ActiveChoice.Value = null;
        try { await ShadowRun.AddErosion(player, amount, context); }
        finally { ActiveChoice.Value = previous; }
    }

    internal static void AddErosionAfterEventDamage(PlayerChoiceContext context, Creature target, ValueProp props,
        ref Task<IEnumerable<DamageResult>> result)
    {
        if (!props.HasFlag(ValueProp.Unblockable) ||
            ActiveChoice.Value is not { ChargesAfterDamage: true, DamageErosionCharged: false } choice ||
            target != choice.Player.Creature) return;
        choice.DamageErosionCharged = true;
        result = FinishEventDamage(result, choice, context);
    }

    private static async Task<IEnumerable<DamageResult>> FinishEventDamage(Task<IEnumerable<DamageResult>> original,
        Choice choice, PlayerChoiceContext context)
    {
        var result = await original;
        await ShadowRun.AddErosion(choice.Player, choice.Amount, context);
        return result;
    }

    internal static void BeginDummyRewards(BattlewornDummy model, AbstractRoom room, out object? previous)
    {
        previous = ActiveChoice.Value;
        if (!IsEroded(model) || model.Owner is not { } player || room is not CombatRoom combat) return;
        var effect = combat.Encounter switch
        {
            BattlewornDummyEventV1Encounter => Effects.Potion,
            BattlewornDummyEventV2Encounter => Effects.Upgrade,
            BattlewornDummyEventV3Encounter => RandomRelic,
            _ => Effects.None
        };
        ActiveChoice.Value = new Choice(player, model.Id.Entry, effect);
    }

    internal static void RestoreChoice(object? previous) => ActiveChoice.Value = (Choice?)previous;

    internal static void AdjustGold(Player player, ref decimal amount)
    {
        if (ActiveChoice.Value is not { } choice || choice.Player != player) return;
        if (choice is { EventId: "TRIAL", OptionKey: "NOBLE.INNOCENT" } && amount == 300) amount = 400;
        if (choice is { EventId: "COLOSSAL_FLOWER", Amount: 6 })
            amount = amount switch { 35 => 48, 75 => 100, 135 => 180, _ => amount };
    }

    internal static void AdjustHeal(Creature creature, ref decimal amount)
    {
        if (ActiveChoice.Value is { EventId: "TRIAL", OptionKey: "NOBLE.GUILTY", Player: { } player } &&
            player.Creature == creature && amount == 10) amount = 13;
    }

    internal static bool IsVegetationRest(Player player) =>
        ActiveChoice.Value is { EventId: "DENSE_VEGETATION", OptionKey: "REST", Player: var owner } && owner == player;

    [Flags]
    private enum Effects
    {
        None = 0, Card = 1, Relic = 2, Potion = 4, Enchant = 8, Upgrade = 16,
        Transform = 32, CardReward = 64, Combat = 128, RandomRelic = 256, Bridge = 512, ListedRelic = 1024
    }

    private sealed record Choice(Player Player, string EventId, Effects Mode, int Rank = 0, int Amount = 0,
        string OptionKey = "")
    {
        internal Rng Rng { get; } = EventRng(Player.RunState, EventId + "_loot_" + Player.RunState.GetPlayerSlotIndex(Player));
        internal Dictionary<ModelId, int> CardRanks { get; } = [];
        internal Dictionary<ModelId, int> RelicRanks { get; } = [];
        internal bool ChargesAfterDamage => (EventId, OptionKey) is
            ("WUPPY_IN_TROUBLE", "BE_CAUTIOUS") or ("THERMAE_NONUPEIPE", "DRINK_WATERS");
        internal bool DamageErosionCharged { get; set; }
    }
}

public sealed class ShadowEventInitialPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_event_initial";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(EventModel), "SetInitialEventState", [typeof(bool)])];
    public static void Prefix(EventModel __instance) => ShadowEvents.ApplyEventVariables(__instance);
}

public sealed class ShadowEventStatePatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_event_state";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(EventModel), "SetEventState", [typeof(LocString), typeof(IEnumerable<EventOption>)])];
    public static void Prefix(EventModel __instance, ref LocString description, ref IEnumerable<EventOption> eventOptions) =>
        ShadowEvents.ChangeState(__instance, ref description, ref eventOptions);
}

public sealed class ShadowEventChoicePatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_event_choice";
    public static ModPatchTarget[] GetTargets() => [new(typeof(EventOption), nameof(EventOption.Chosen), Type.EmptyTypes)];
    public static void Prefix(EventOption __instance, out (object? Previous, object? Chosen) __state) =>
        ShadowEvents.BeginChoice(__instance, out __state);
    public static void Postfix((object? Previous, object? Chosen) __state, ref Task __result) =>
        ShadowEvents.EndChoice(__state, ref __result);
}

public sealed class ShadowEventEnchantPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_event_enchant";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(CardCmd), nameof(CardCmd.Enchant), [typeof(EnchantmentModel), typeof(CardModel), typeof(decimal)])];
    public static void Postfix(CardModel card, EnchantmentModel? __result)
    {
        if (__result != null) ShadowEvents.TagEnchant(card);
    }
}

public sealed class ShadowEventPotionPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_event_potion";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(PotionCmd), nameof(PotionCmd.TryToProcure), [typeof(PotionModel), typeof(Player), typeof(int)])];
    public static void Prefix(ref PotionModel potion, Player player) => ShadowEvents.PreparePotion(ref potion, player);
}

public sealed class ShadowPotionTradePatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_potion_trade";
    public static ModPatchTarget[] GetTargets() => [new(typeof(TheFutureOfPotions), "Trade", [typeof(PotionModel)])];
    public static void Prefix(TheFutureOfPotions __instance, PotionModel potion, out object? __state) =>
        __state = ShadowEvents.BeginPotionTrade(__instance, potion);
    public static void Postfix(object? __state, ref Task __result) =>
        ShadowEvents.EndChoice((__state, null), ref __result);
}

public sealed class ShadowEventRelicPoolPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_event_relic_pool";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(RelicFactory), nameof(RelicFactory.PullNextRelicFromFront),
            [typeof(Player), typeof(RelicRarity), typeof(Func<RelicModel, bool>)])];
    public static void Prefix(Player player, RelicRarity rarity, ref Func<RelicModel, bool> filter, out int __state) =>
        __state = ShadowEvents.FilterRelics(player, rarity, ref filter);
    public static void Postfix(Player player, RelicModel __result, int __state) =>
        ShadowEvents.RecordRelicRank(player, __result, __state);
}

public sealed class ShadowEventRelicOffersPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_event_relic_offers";
    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(RelicTrader), nameof(RelicTrader.CalculateVars), Type.EmptyTypes),
        new(typeof(WelcomeToWongos), "GenerateInitialOptions", Type.EmptyTypes)
    ];

    public static void Prefix(EventModel __instance, out object? __state) =>
        __state = ShadowEvents.BeginRelicOffers(__instance);

    public static void Finalizer(object? __state) => ShadowEvents.RestoreChoice(__state);
}

public sealed class ShadowEventCombatRewardsPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_event_combat_rewards";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(EventModel), "EnterCombatWithoutExitingEvent",
            [typeof(EncounterModel), typeof(IReadOnlyList<Reward>), typeof(bool)])];
    public static void Prefix(IReadOnlyList<Reward> extraRewards) => ShadowEvents.TrackCombatRewards(extraRewards);
}

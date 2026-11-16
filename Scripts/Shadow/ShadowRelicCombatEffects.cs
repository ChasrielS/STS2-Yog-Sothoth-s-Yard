using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Patching.Models;

namespace YogSothothsYard.Scripts.Shadow;

public sealed class ShadowBathRobesPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_bath_robes";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(BathRobes), nameof(BathRobes.BeforeCombatStart), Type.EmptyTypes)];

    public static void Prefix(BathRobes __instance, out bool __state)
    {
        __state = ShadowRanks.Of(__instance) == 3 && !__instance.Owner.Creature.IsDead &&
            __instance.Owner.RunState.CurrentRoom?.RoomType == RoomType.Boss && !__instance.IsUsedUp;
    }

    public static void Postfix(BathRobes __instance, bool __state, ref Task __result)
    {
        if (__state) __result = RecordUse(__result, __instance);
    }

    private static async Task RecordUse(Task original, BathRobes relic)
    {
        await original;
        if (!relic.IsUsedUp) return;
        ShadowRanks.RecordBathRobesUse(relic);
        if (ShadowRanks.BathRobesUses(relic) >= 2) return;
        relic.WasUsed = false;
        relic.Status = RelicStatus.Normal;
    }
}

public sealed class ShadowThumbscrewHitCountPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_thumbscrew_hit_count";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(Thumbscrew), nameof(Thumbscrew.ModifyAttackHitCount), [typeof(AttackCommand), typeof(int)])];

    public static void Postfix(RelicModel __instance, AttackCommand attack, int hitCount, ref int __result)
    {
        if (__result <= hitCount || ShadowRanks.Of(__instance) != 3) return;
        __result++;
        if (attack.IsSingleTargeted || attack.IsMultiTargeted)
            ShadowThumbscrewAttackPatch.Mark(attack, __instance);
    }
}

public sealed class ShadowThumbscrewAttackPatch : IPatchMethod
{
    private static readonly ConditionalWeakTable<AttackCommand, RelicModel> Activations = new();

    public static string PatchId => "yog_sothoths_yard_shadow_thumbscrew_attack";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(Hook), nameof(Hook.AfterAttack), [typeof(ICombatState), typeof(PlayerChoiceContext), typeof(AttackCommand)])];

    internal static void Mark(AttackCommand attack, RelicModel relic) => Activations.GetValue(attack, _ => relic);

    public static void Postfix(PlayerChoiceContext choiceContext, AttackCommand command, ref Task __result)
    {
        if (!Activations.TryGetValue(command, out var relic)) return;
        Activations.Remove(command);
        if (command.Results.Any() && ShadowRelicRules.TryGet(relic, out var rule))
            __result = Charge(__result, relic, rule.Amount, choiceContext);
    }

    private static async Task Charge(Task original, RelicModel relic, int amount, PlayerChoiceContext context)
    {
        await original;
        relic.Flash();
        await ShadowRelicRules.Charge(relic, amount, context);
    }
}

public sealed class ShadowRelicRearmPatch : IPatchMethod
{
    private static readonly ConditionalWeakTable<RelicModel, FirstUse> FirstUses = new();
    private static readonly AccessTools.FieldRef<Vambrace, bool> BlockUsed =
        AccessTools.FieldRefAccess<Vambrace, bool>("_blockGainedThisCombat");
    private static readonly AccessTools.FieldRef<Vambrace, CardModel?> BlockCard =
        AccessTools.FieldRefAccess<Vambrace, CardModel?>("_triggeringCard");
    private static readonly AccessTools.FieldRef<UnsettlingLamp, bool> LampUsed =
        AccessTools.FieldRefAccess<UnsettlingLamp, bool>("_isFinishedTriggering");
    private static readonly AccessTools.FieldRef<UnsettlingLamp, CardModel?> LampCard =
        AccessTools.FieldRefAccess<UnsettlingLamp, CardModel?>("_triggeringCard");
    private static readonly AccessTools.FieldRef<UnsettlingLamp, List<PowerModel>?> DoubledPowers =
        AccessTools.FieldRefAccess<UnsettlingLamp, List<PowerModel>?>("_doubledPowers");

    public static string PatchId => "yog_sothoths_yard_shadow_relic_rearm";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(Vambrace), nameof(Vambrace.AfterCardPlayed), [typeof(PlayerChoiceContext), typeof(CardPlay)]),
         new(typeof(UnsettlingLamp), nameof(UnsettlingLamp.AfterCardPlayed), [typeof(PlayerChoiceContext), typeof(CardPlay)])];

    public static void Postfix(RelicModel __instance, CardPlay cardPlay)
    {
        if (ShadowRanks.Of(__instance) != 3 || cardPlay.Player != __instance.Owner ||
            __instance.Owner.Creature.CombatState is not { } combat) return;
        var firstUse = FirstUses.GetValue(__instance, static _ => new FirstUse());
        if (firstUse.Combat == combat) return;
        switch (__instance)
        {
            case Vambrace vambrace when BlockUsed(vambrace) && BlockCard(vambrace) == cardPlay.Card:
                BlockUsed(vambrace) = false;
                BlockCard(vambrace) = null;
                break;
            case UnsettlingLamp lamp when LampUsed(lamp) && LampCard(lamp) == cardPlay.Card:
                LampUsed(lamp) = false;
                LampCard(lamp) = null;
                DoubledPowers(lamp)?.Clear();
                break;
            default: return;
        }
        firstUse.Combat = combat;
        __instance.Status = RelicStatus.Active;
    }

    private sealed class FirstUse
    {
        internal ICombatState? Combat { get; set; }
    }
}

public sealed class ShadowRelicFirstTurnPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_relic_first_turn";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(GamblingChip), nameof(GamblingChip.AfterPlayerTurnStart), [typeof(PlayerChoiceContext), typeof(Player)]),
         new(typeof(VexingPuzzlebox), nameof(VexingPuzzlebox.AfterPlayerTurnStart), [typeof(PlayerChoiceContext), typeof(Player)])];

    public static bool Prefix(RelicModel __instance, PlayerChoiceContext choiceContext, Player player, ref Task __result)
    {
        if (ShadowRanks.Of(__instance) != 3) return true;
        __result = player == __instance.Owner && player.PlayerCombatState?.TurnNumber == 1
            ? __instance is GamblingChip chip ? DiscardAndDraw(chip, choiceContext) : GenerateCards(__instance)
            : Task.CompletedTask;
        return false;
    }

    private static async Task DiscardAndDraw(GamblingChip relic, PlayerChoiceContext context)
    {
        var player = relic.Owner;
        var prompt = new LocString("relics", relic.Id.Entry + ".selectionScreenPrompt");
        var prefs = new CardSelectorPrefs(prompt, 0, PileType.Hand.GetPile(player).Cards.Count);
        var cards = (await CardSelectCmd.FromHandForDiscard(context, player, prefs, null, relic)).ToList();
        if (cards.Count > 0) await CardCmd.DiscardAndDraw(context, cards, cards.Count * 2);
    }

    private static async Task GenerateCards(RelicModel relic)
    {
        relic.Flash();
        var player = relic.Owner;
        var pool = player.Character.CardPool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint);
        var cards = CardFactory.GetDistinctForCombat(player, pool, 2, player.RunState.Rng.CombatCardGeneration).ToArray();
        foreach (var card in cards) card.SetToFreeThisTurn();
        await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand, player);
    }
}

public sealed class ShadowMummifiedHandPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_mummified_hand";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(MummifiedHand), nameof(MummifiedHand.AfterCardPlayed), [typeof(PlayerChoiceContext), typeof(CardPlay)])];

    public static bool Prefix(MummifiedHand __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
    {
        if (ShadowRanks.Of(__instance) != 3) return true;
        __result = cardPlay.Player == __instance.Owner && cardPlay.Card.Type == CardType.Power && CombatManager.Instance.IsInProgress
            ? MakeFree(__instance, choiceContext) : Task.CompletedTask;
        return false;
    }

    private static Task MakeFree(MummifiedHand relic, PlayerChoiceContext context)
    {
        var hand = PileType.Hand.GetPile(relic.Owner).Cards.ToList();
        if (hand.Count == 0) return Task.CompletedTask;
        var rng = relic.Owner.RunState.Rng.CombatCardSelection;
        for (var i = 0; i < 2 && hand.Count > 0; i++)
        {
            var baseCost = hand.Where(card => card.EnergyCost.GetWithModifiers(CostModifiers.None) > 0 || card.BaseStarCost > 0).ToList();
            var card = rng.NextItem(baseCost.Where(card => card.CostsEnergyOrStars(includeGlobalModifiers: true)))
                ?? rng.NextItem(hand.Where(card => card.CostsEnergyOrStars(includeGlobalModifiers: true)))
                ?? rng.NextItem(baseCost)
                ?? rng.NextItem(hand)!;
            card.SetToFreeThisTurn();
            hand.Remove(card);
        }
        relic.Flash();
        return ShadowRelicRules.Charge(relic, 2, context);
    }
}

public sealed class ShadowUnceasingTopPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_unceasing_top";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(UnceasingTop), nameof(UnceasingTop.AfterHandEmptied), [typeof(PlayerChoiceContext), typeof(Player)])];

    public static void Prefix(UnceasingTop __instance, out int __state) => __state = ShadowRelicRules.FlashCount(__instance);

    public static void Postfix(UnceasingTop __instance, PlayerChoiceContext choiceContext, int __state, ref Task __result)
    {
        if (ShadowRanks.Of(__instance) == 3) __result = AddEnergy(__result, __instance, choiceContext, __state);
    }

    private static async Task AddEnergy(Task original, UnceasingTop relic, PlayerChoiceContext context, int flashes)
    {
        await original;
        if (ShadowRelicRules.FlashCount(relic) <= flashes) return;
        await PlayerCmd.GainEnergy(1, relic.Owner);
        await ShadowRelicRules.Charge(relic, 1, context);
    }
}

public sealed class ShadowHandDrillPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_hand_drill";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(HandDrill), nameof(HandDrill.AfterBlockBroken), [typeof(PlayerChoiceContext), typeof(Creature), typeof(Creature)])];

    public static void Postfix(HandDrill __instance, PlayerChoiceContext choiceContext, Creature target, Creature? breaker, ref Task __result)
    {
        if (ShadowRanks.Of(__instance) == 3 &&
            (breaker == __instance.Owner.Creature || breaker?.PetOwner == __instance.Owner) && !target.IsPlayer)
            __result = Weaken(__result, __instance, choiceContext, target);
    }

    private static async Task Weaken(Task original, HandDrill relic, PlayerChoiceContext context, Creature target)
    {
        await original;
        await PowerCmd.Apply<WeakPower>(context, target, 1m, relic.Owner.Creature, null);
        await ShadowRelicRules.Charge(relic, 3, context);
    }
}

public sealed class ShadowGiryaCombatPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_girya_combat";
    public static ModPatchTarget[] GetTargets() => [new(typeof(Girya), nameof(Girya.AfterRoomEntered), [typeof(AbstractRoom)])];

    public static bool Prefix(Girya __instance, AbstractRoom room, ref Task __result)
    {
        if (ShadowRanks.Of(__instance) != 3) return true;
        __result = room is CombatRoom && __instance.TimesLifted > 0 ? ApplyStrength(__instance) : Task.CompletedTask;
        return false;
    }

    private static async Task ApplyStrength(Girya relic)
    {
        relic.Flash();
        var owner = relic.Owner.Creature;
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), owner, 2m * relic.TimesLifted, owner, null);
    }
}

public sealed class ShadowSneckoDrawPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_snecko_draw";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(Hook), nameof(Hook.ModifyHandDraw),
            [typeof(ICombatState), typeof(Player), typeof(decimal), typeof(IEnumerable<AbstractModel>).MakeByRefType()])];

    public static void Postfix(Player player, ref decimal __result)
    {
        if (player.GetRelic<FakeSneckoEye>() is { } relic && ShadowRanks.Of(relic) is > 1 and var rank)
            __result += rank - 1;
    }
}

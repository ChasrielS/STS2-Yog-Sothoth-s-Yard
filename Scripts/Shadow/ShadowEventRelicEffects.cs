using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Patching.Models;

namespace YogSothothsYard.Scripts.Shadow;

public sealed class ShadowMrStrugglesPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_mr_struggles";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(MrStruggles), nameof(MrStruggles.AfterPlayerTurnStart), [typeof(PlayerChoiceContext), typeof(Player)])];

    public static bool Prefix(MrStruggles __instance, PlayerChoiceContext choiceContext, Player player, ref Task __result)
    {
        var rank = ShadowRanks.Of(__instance);
        if (rank == 0) return true;
        __result = Apply(__instance, choiceContext, player, rank);
        return false;
    }

    private static async Task Apply(MrStruggles relic, PlayerChoiceContext context, Player player, int rank)
    {
        if (player != relic.Owner || player.PlayerCombatState is not { } state ||
            player.Creature.CombatState is not { } combat) return;
        relic.Flash();
        var damage = state.TurnNumber + (rank == 2 ? 2 : 1);
        for (var hit = 0; hit < (rank == 3 ? 2 : 1); hit++)
            await CreatureCmd.Damage(context, combat.HittableEnemies, damage, ValueProp.Unpowered, player.Creature);
        await ShadowRelicRules.Charge(relic, 1, context);
    }
}

public sealed class ShadowBingBongPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_bing_bong";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(BingBong), nameof(BingBong.AfterCardChangedPiles), [typeof(CardModel), typeof(PileType), typeof(AbstractModel)])];

    public static bool Prefix(BingBong __instance, CardModel card, AbstractModel? clonedBy, ref Task __result)
    {
        if (ShadowRanks.Of(__instance) != 3) return true;
        __result = Duplicate(__instance, card, clonedBy);
        return false;
    }

    private static async Task Duplicate(BingBong relic, CardModel card, AbstractModel? clonedBy)
    {
        if (card.Pile?.Type != PileType.Deck || card.Owner != relic.Owner || clonedBy != null) return;
        relic.Flash();
        for (var i = 0; i < 2; i++)
        {
            var copy = relic.Owner.RunState.CloneCard(card);
            CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(copy, PileType.Deck, CardPilePosition.Bottom, relic));
        }
        await ShadowRelicRules.Charge(relic, 10);
    }
}

public sealed class ShadowBootPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_boot";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(TheBoot), nameof(TheBoot.ModifyHpLostAfterOstyLate),
            [typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(Creature), typeof(CardModel)])];

    public static void Postfix(TheBoot __instance, decimal amount, ref decimal __result)
    {
        if (ShadowRanks.Of(__instance) > 0 && amount > __instance.DynamicVars["DamageThreshold"].BaseValue)
            __result = amount;
    }
}

public sealed class ShadowDreamCatcherPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_dream_catcher";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(DreamCatcher), nameof(DreamCatcher.TryModifyRestSiteHealRewards), [typeof(Player), typeof(List<Reward>), typeof(bool)])];

    public static bool Prefix(DreamCatcher __instance, Player player, List<Reward> rewards, ref bool __result)
    {
        var rank = ShadowRanks.Of(__instance);
        if (rank == 0 || player != __instance.Owner) return true;
        var reward = new CardReward(
            CardCreationOptions.ForRoom(player, RoomType.Monster), rank == 1 ? 4 : 5, player);
        if (rank == 3) ShadowDreamCatcherSelectionPatch.Mark(reward);
        rewards.Add(reward);
        __instance.Flash();
        __result = true;
        return false;
    }
}

public sealed class ShadowRazorToothPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_razor_tooth";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(RazorTooth), nameof(RazorTooth.AfterCardPlayed), [typeof(PlayerChoiceContext), typeof(CardPlay)])];

    public static void Prefix(RazorTooth __instance, CardPlay cardPlay)
    {
        var rank = ShadowRanks.Of(__instance);
        var card = cardPlay.Card;
        if (rank > 0 && cardPlay.Player == __instance.Owner && card.Type is CardType.Attack or CardType.Skill &&
            ShadowRanks.Of(card) < rank && ShadowCardRules.TryGet(card, rank, out _)) ShadowRanks.Set(card, rank);
    }
}

public sealed class ShadowSwordOfJadePatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_sword_of_jade";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(SwordOfJade), nameof(SwordOfJade.AfterRoomEntered), [typeof(AbstractRoom)])];

    public static void Postfix(SwordOfJade __instance, AbstractRoom room, ref Task __result)
    {
        if (room is CombatRoom && ShadowRelicRules.TryGet(__instance, out var rule))
            __result = Charge(__result, __instance, rule.Amount);
    }

    private static async Task Charge(Task original, RelicModel relic, int amount)
    {
        await original;
        await ShadowRelicRules.Charge(relic, amount);
    }
}

public sealed class ShadowPenNibPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_pen_nib";
    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(PenNib), "get_DisplayAmount", Type.EmptyTypes),
        new(typeof(PenNib), "set_AttacksPlayed", [typeof(int)]),
        new(typeof(PenNib), "UpdateDisplay", Type.EmptyTypes),
        new(typeof(PenNib), nameof(PenNib.ModifyDamageMultiplicative),
            [typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(Creature), typeof(CardModel), typeof(CardPlay)])
    ];

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var limit = AccessTools.Method(typeof(ShadowPenNibPatch), nameof(Threshold));
        var ready = AccessTools.Method(typeof(ShadowPenNibPatch), nameof(ReadyThreshold));
        foreach (var instruction in instructions)
        {
            var loadTen = instruction.LoadsConstant(10L);
            if (!loadTen && !instruction.LoadsConstant(9L))
            {
                yield return instruction;
                continue;
            }
            var relic = new CodeInstruction(OpCodes.Ldarg_0);
            relic.labels.AddRange(instruction.labels);
            relic.blocks.AddRange(instruction.blocks);
            yield return relic;
            yield return new CodeInstruction(OpCodes.Call, loadTen ? limit : ready);
        }
    }

    private static int Threshold(PenNib relic) => ShadowRanks.Of(relic) == 1 ? 8 : 10;
    private static int ReadyThreshold(PenNib relic) => Threshold(relic) - 1;
}

public sealed class ShadowSturdyClampPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_sturdy_clamp";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(SturdyClamp), nameof(SturdyClamp.AfterPreventingBlockClear), [typeof(AbstractModel), typeof(Creature)])];

    public static bool Prefix(SturdyClamp __instance, AbstractModel preventer, Creature creature, ref Task __result)
    {
        if (ShadowRanks.Of(__instance) == 0) return true;
        __result = Apply(__instance, preventer, creature);
        return false;
    }

    private static async Task Apply(SturdyClamp relic, AbstractModel preventer, Creature creature)
    {
        if (relic != preventer || creature != relic.Owner.Creature || creature.Block == 0) return;
        var limit = relic.DynamicVars.Block.IntValue;
        if (creature.Block > limit)
            await CreatureCmd.LoseBlock(new BlockingPlayerChoiceContext(), creature, creature.Block - limit, null);
        relic.Flash();
    }
}

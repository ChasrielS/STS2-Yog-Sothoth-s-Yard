using System.Runtime.CompilerServices;
using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;

namespace YogSothothsYard.Scripts.Shadow;

public sealed class ShadowQuestHatchPatch : IPatchMethod
{
    private static readonly Func<RestSiteOption, Player> Owner =
        AccessTools.MethodDelegate<Func<RestSiteOption, Player>>(AccessTools.PropertyGetter(typeof(RestSiteOption), "Owner"));

    public static string PatchId => "yog_sothoths_yard_shadow_quest_hatch";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(HatchRestSiteOption), nameof(HatchRestSiteOption.OnSelect), Type.EmptyTypes)];

    public static void Prefix(HatchRestSiteOption __instance, out int[] __state) =>
        __state = Owner(__instance).Deck.Cards.OfType<ByrdonisEgg>().Select(ShadowRanks.Of).Where(rank => rank > 0).ToArray();

    public static void Postfix(HatchRestSiteOption __instance, int[] __state, ref Task<bool> __result)
    {
        if (__state.Length > 0) __result = Charge(__result, Owner(__instance), __state);
    }

    private static async Task<bool> Charge(Task<bool> original, Player player, IEnumerable<int> ranks)
    {
        if (!await original) return false;
        foreach (var rank in ranks) await ShadowRun.AddErosion(player, Erosion(rank));
        return true;
    }

    internal static int Erosion(int rank) => rank switch { 1 => 13, 2 => 20, _ => 27 };
}

public sealed class ShadowByrdpipPatch : IPatchMethod
{
    internal static readonly AsyncLocal<RelicModel?> Current = new();
    internal static int Rank => Current.Value is { } relic ? ShadowRanks.Of(relic) : 0;
    public static string PatchId => "yog_sothoths_yard_shadow_byrdpip";
    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(Byrdpip), nameof(Byrdpip.AfterObtained), Type.EmptyTypes),
        new(typeof(Byrdpip), nameof(Byrdpip.BeforeCombatStart), Type.EmptyTypes),
        new(typeof(Byrdine), nameof(Byrdine.AfterObtained), Type.EmptyTypes),
        new(typeof(Byrdine), nameof(Byrdine.BeforeCombatStart), Type.EmptyTypes)
    ];

    public static void Prefix(RelicModel __instance, out RelicModel? __state)
    {
        __state = Current.Value;
        Current.Value = __instance;
    }

    public static void Postfix(RelicModel? __state) => Current.Value = __state;
}

public sealed class ShadowByrdCardTransformPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_byrd_card_transform";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(CardCmd), nameof(CardCmd.Transform), [typeof(CardModel), typeof(CardModel), typeof(CardPreviewStyle)])];

    public static void Prefix(CardModel original, CardModel replacement)
    {
        if (original is ByrdonisEgg && replacement is ByrdSwoop && ShadowByrdpipPatch.Rank is > 0 and var hatchRank)
            ShadowRanks.Set(replacement, hatchRank);
        else if (original is ByrdSwoop && replacement is GodByrd && ShadowRanks.Of(original) is > 0 and var rank)
            ShadowRanks.Set(replacement, rank);
    }
}

public sealed class ShadowByrdRelicReplacePatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_byrd_relic_replace";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(RelicCmd), "Replace", [typeof(RelicModel), typeof(RelicModel)])];

    public static void Prefix(RelicModel original, RelicModel replace)
    {
        if (original is Byrdpip && replace is Byrdine && ShadowRanks.Of(original) is > 0 and var rank)
            ShadowRanks.Set(replace, rank);
    }
}

public sealed class ShadowByrdEvolutionPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_byrd_evolution";
    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(Byrdpip), "get_DisplayAmount", Type.EmptyTypes),
        new(typeof(Byrdpip), "AfterCardPlayed", [typeof(PlayerChoiceContext), typeof(CardPlay)]),
        new(typeof(Byrdpip), "TryModifyRestSiteOptions", [typeof(Player), typeof(ICollection<RestSiteOption>)]),
        new(typeof(Byrdpip), "UpdateDisplay", Type.EmptyTypes)
    ];

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
        ReplaceThreshold(instructions, 6, nameof(RequiredPlays));

    internal static IEnumerable<CodeInstruction> ReplaceThreshold(IEnumerable<CodeInstruction> instructions, int original, string method)
    {
        var threshold = AccessTools.Method(typeof(ShadowByrdEvolutionPatch), method);
        foreach (var instruction in instructions)
        {
            if (!instruction.LoadsConstant((long)original))
            {
                yield return instruction;
                continue;
            }
            var loadRelic = new CodeInstruction(OpCodes.Ldarg_0);
            loadRelic.labels.AddRange(instruction.labels);
            loadRelic.blocks.AddRange(instruction.blocks);
            yield return loadRelic;
            yield return new CodeInstruction(OpCodes.Call, threshold);
        }
    }

    internal static int RequiredPlays(RelicModel relic) => ShadowRanks.Of(relic) == 3 ? 5 : 6;
    internal static int PlaysBeforeEvolutionCue(Player player) =>
        player.GetRelic<Byrdpip>() is { } relic ? RequiredPlays(relic) - 1 : 5;
}

public sealed class ShadowByrdEvolutionCuePatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_byrd_evolution_cue";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(NByrdpipReadyToEvolveVfx), nameof(NByrdpipReadyToEvolveVfx.Create), [typeof(Player)])];

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
        ShadowByrdEvolutionPatch.ReplaceThreshold(instructions, 5, nameof(ShadowByrdEvolutionPatch.PlaysBeforeEvolutionCue));
}

public sealed class ShadowByrdRelicTipsPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_byrd_relic_tips";
    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(Byrdpip), "get_ExtraHoverTips", Type.EmptyTypes),
        new(typeof(Byrdine), "get_ExtraHoverTips", Type.EmptyTypes)
    ];

    public static void Postfix(RelicModel __instance, ref IEnumerable<IHoverTip> __result)
    {
        var rank = ShadowRanks.Of(__instance);
        if (rank > 0) __result = __result.Select(tip => Preview(tip, rank));
    }

    private static IHoverTip Preview(IHoverTip tip, int rank)
    {
        if (tip is not CardHoverTip { Card: ByrdSwoop or GodByrd } cardTip)
            return tip;
        var preview = cardTip.Card.CanonicalInstance.ToMutable();
        ShadowRanks.Set(preview, rank);
        return HoverTipFactory.FromCard(preview);
    }
}

public sealed class ShadowDeckAdditionPatch : IPatchMethod
{
    private static readonly ConditionalWeakTable<CardModel, FresnelEffect> FresnelEffects = new();
    public static string PatchId => "yog_sothoths_yard_shadow_deck_addition";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(Hook), nameof(Hook.ModifyCardBeingAddedToDeck),
            [typeof(IRunState), typeof(CardModel), typeof(List<AbstractModel>).MakeByRefType()])];

    public static void Postfix(CardModel card, CardModel __result, List<AbstractModel> modifyingModels)
    {
        ShadowEvents.TagCardAdded(__result, __result.Owner.Deck);
        if (FresnelEffects.TryGetValue(card, out var effect) && !effect.Charged) MarkLens(__result);
        var preservesBirdRank = __result is ByrdSwoop && ShadowByrdpipPatch.Rank > 0 ||
            (__result.Id.Entry == "GOD_BYRD" && ShadowByrdpipPatch.Current.Value?.Id.Entry == "BYRDINE");
        foreach (var relic in modifyingModels.OfType<RelicModel>())
        {
            var rank = ShadowRanks.Of(relic);
            if (rank == 0) continue;
            if (!preservesBirdRank && relic is (FrozenEgg or ToxicEgg or MoltenEgg) && ShadowCardRules.TryGet(__result, rank, out _) &&
                ShadowRanks.Of(__result) < rank) ShadowRanks.Set(__result, rank);
            if (relic is FresnelLens) MarkLens(__result);
        }
    }

    internal static void MarkLens(CardModel card) => FresnelEffects.GetValue(card, _ => new FresnelEffect());
    internal static bool WasEnchantedByLens(CardModel card) => FresnelEffects.TryGetValue(card, out _);

    internal static Task ChargeLens(CardModel card)
    {
        if (!FresnelEffects.TryGetValue(card, out var effect) || effect.Charged ||
            card.Owner.GetRelic<FresnelLens>() is not { } relic || !ShadowRelicRules.TryGet(relic, out var rule))
            return Task.CompletedTask;
        effect.Charged = true;
        return ShadowRelicRules.Charge(relic, rule.Amount);
    }

    private sealed class FresnelEffect
    {
        internal bool Charged { get; set; }
    }
}

public sealed class ShadowFresnelRewardPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_fresnel_reward";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(CardCreationResult), nameof(CardCreationResult.ModifyCard), [typeof(CardModel), typeof(RelicModel)])];
    public static void Postfix(CardModel card, RelicModel modifyingRelic)
    {
        if (modifyingRelic is FresnelLens && ShadowRanks.Of(modifyingRelic) > 0)
            ShadowDeckAdditionPatch.MarkLens(card);
    }
}

public sealed class ShadowBoneTeaPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_bone_tea";
    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(BoneTea), nameof(BoneTea.AfterPlayerTurnStart), [typeof(PlayerChoiceContext), typeof(Player)]),
        new(typeof(Bellows), nameof(Bellows.AfterPlayerTurnStart), [typeof(PlayerChoiceContext), typeof(Player)])
    ];

    public static void Prefix(RelicModel __instance, Player player)
    {
        var rank = ShadowRanks.Of(__instance);
        if (rank == 0 || __instance.IsUsedUp || player != __instance.Owner || player.PlayerCombatState?.TurnNumber != 1)
            return;
        foreach (var card in PileType.Hand.GetPile(player).Cards)
            if (ShadowRanks.Of(card) < rank && ShadowCardRules.TryGet(card, rank, out _)) ShadowRanks.Set(card, rank);
    }
}

public sealed class ShadowDiscourtesyPatch : IPatchMethod
{
    internal static readonly AsyncLocal<int> Rank = new();
    public static string PatchId => "yog_sothoths_yard_shadow_discourtesy";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(TeaOfDiscourtesy), nameof(TeaOfDiscourtesy.BeforeCombatStart), Type.EmptyTypes)];

    public static void Prefix(TeaOfDiscourtesy __instance, out int __state)
    {
        __state = Rank.Value;
        Rank.Value = ShadowRanks.Of(__instance);
    }

    public static void Postfix(int __state) => Rank.Value = __state;
}

public sealed class ShadowWongoTicketPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_wongo_ticket";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(WongosMysteryTicket), nameof(WongosMysteryTicket.TryModifyRewards),
            [typeof(Player), typeof(List<Reward>), typeof(AbstractRoom)])];

    public static void Prefix(List<Reward> rewards, out int __state) => __state = rewards.Count;
    public static void Postfix(WongosMysteryTicket __instance, Player player, List<Reward> rewards, bool __result, int __state)
    {
        var rank = ShadowRanks.Of(__instance);
        if (!__result || rank == 0) return;
        foreach (var reward in rewards.Skip(__state).OfType<RelicReward>())
            ShadowEvents.ForceRelicReward(reward, player, rank);
    }
}

public sealed class ShadowSwordOfStonePatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_sword_of_stone";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(SwordOfStone), nameof(SwordOfStone.AfterCombatVictory), [typeof(CombatRoom)])];

    public static bool Prefix(SwordOfStone __instance, CombatRoom room, ref Task __result)
    {
        var rank = ShadowRanks.Of(__instance);
        if (rank == 0 || room.RoomType != RoomType.Elite) return true;
        __instance.ElitesDefeated++;
        __instance.Flash();
        if (__instance.ElitesDefeated < 5 - rank)
        {
            __result = Task.CompletedTask;
            return false;
        }
        var sword = ModelDb.Relic<SwordOfJade>().ToMutable();
        ShadowRanks.Set(sword, rank);
        __result = RelicCmd.Replace(__instance, sword);
        return false;
    }
}

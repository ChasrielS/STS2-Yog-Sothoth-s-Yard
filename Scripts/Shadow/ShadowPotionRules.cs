using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Patching.Models;

namespace YogSothothsYard.Scripts.Shadow;

internal static class ShadowPotionRules
{
    private static readonly ConditionalWeakTable<DynamicVarSet, Applied> Versions = new();
    private static readonly Dictionary<Type, (string Var, int Value)[]> FirstRank = new()
    {
        [typeof(ExplosiveAmpoule)] = [("Damage", 13)],
        [typeof(BlockPotion)] = [("Block", 16)],
        [typeof(AttackPotion)] = [],
        [typeof(FirePotion)] = [("Damage", 25)],
        [typeof(FlexPotion)] = [("StrengthPower", 7)],
        [typeof(SkillPotion)] = [],
        [typeof(PowerPotion)] = [],
        [typeof(SpeedPotion)] = [("DexterityPower", 7)],
        [typeof(ColorlessPotion)] = [],
        [typeof(WeakPotion)] = [("WeakPower", 4)],
        [typeof(SwiftPotion)] = [("Cards", 4)],
        [typeof(VulnerablePotion)] = [("VulnerablePower", 4)],
        [typeof(XingShenTang)] = [("KnifeLimit", 1)],
        [typeof(GuiYuanFuShui)] = [("Amount", 40)],
        [typeof(Fortifier)] = [],
        [typeof(LiquidBronze)] = [("ThornsPower", 4)],
        [typeof(Clarity)] = [("ClarityPower", 4)],
        [typeof(RadiantTincture)] = [("RadiancePower", 4)],
        [typeof(CureAll)] = [("Energy", 2), ("Cards", 2)],
        [typeof(BlessingOfTheForge)] = [],
        [typeof(HeartOfIron)] = [("PlatingPower", 9)],
        [typeof(PowderedDemise)] = [("Demise", 12)],
        [typeof(RegenPotion)] = [("RegenPower", 6)],
        [typeof(GigantificationPotion)] = [],
        [typeof(FruitJuice)] = [("MaxHp", 6)],
        [typeof(BeetleJuice)] = [("DamageDecrease", 40)],
        [typeof(DistilledChaos)] = [("Repeat", 4)],
        [typeof(ShacklingPotion)] = [("StrengthPower", 9)],
        [typeof(OrobicAcid)] = [],
        [typeof(ShipInABottle)] = [("Block", 13)],
        [typeof(FairyInABottle)] = [],
        [typeof(BottledPotential)] = [("Cards", 6)],
        [typeof(SneckoOil)] = [("Cards", 9)],
        [typeof(PotionShapedRock)] = [("Damage", 20)],
        [typeof(GlowwaterPotion)] = [],
        [typeof(FoulPotion)] = [("Damage", 13), ("Gold", 110)]
    };

    private static (string Var, int Value)[]? Values(PotionModel potion, int rank)
    {
        if (rank == 1) return FirstRank.GetValueOrDefault(potion.GetType());
        return (potion, rank) switch
        {
            (XingShenTang, 2) => [("KnifeLimit", 1), ("Amount", 4)],
            (XingShenTang, 3) => [("KnifeLimit", 2), ("Amount", 5)],
            (GaoTangJingHua, 2) => [],
            (GaoTangJingHua, 3) => [("Dishes", 2)],
            (GuiYuanFuShui, 2) => [("Amount", 52)],
            (GuiYuanFuShui, 3) => [("Amount", 65)],
            (StrengthPotion, 2) => [("StrengthPower", 3)],
            (DexterityPotion, 2) => [("DexterityPower", 3)],
            (EnergyPotion, 2) => [("Energy", 3)],
            (PotionOfBinding, 2 or 3) => [("WeakPower", 2), ("VulnerablePower", rank - 1)],
            (StableSerum, 2) => [("Repeat", 3)],
            (FyshOil, 2 or 3) => [("StrengthPower", 2), ("DexterityPower", rank - 1)],
            (CureAll, 2 or 3) => [("Energy", rank), ("Cards", rank + 1)],
            (MazalethsGift, 3) => [("RitualPower", 2)],
            (LuckyTonic, 3) => [("BufferPower", 2)],
            (FoulPotion, 2) => [("Damage", 14), ("Gold", 125)],
            (FoulPotion, 3) => [("Damage", 16), ("Gold", 150)],
            (PotionShapedRock, 2 or 3) => [("Damage", 15 + 5 * rank)],
            (TouchOfInsanity or GamblersBrew or Duplicator or LiquidMemories or DropletOfPrecognition, 3) => [],
            (EntropicBrew or GlowwaterPotion, 2 or 3) => [],
            _ => null
        };
    }

    internal static bool Supports(PotionModel potion, int rank) => Values(potion, rank) != null;

    internal static void Apply(PotionModel potion, DynamicVarSet variables)
    {
        var rank = ShadowRanks.Of(potion);
        if (rank == 0) return;
        if (Versions.TryGetValue(variables, out var version) && version.Rank == rank) return;
        if (Values(potion, rank) is not { } values) return;
        Versions.Remove(variables);
        Versions.Add(variables, new Applied(rank));
        foreach (var (name, value) in values)
            if (variables.TryGetValue(name, out var variable)) variable.BaseValue = value;
    }

    internal static int SelectionCount(PotionModel potion) => ShadowRanks.Of(potion) switch
    {
        1 => 2, 2 => 4, 3 => 7, _ => 0
    };

    internal static string? AlternateDescription(PotionModel potion) => potion switch
    {
        EntropicBrew => ShadowRanks.Of(potion) == 2 ? ".shadowDescription2" : ".shadowDescription3",
        XingShenTang or GaoTangJingHua or AttackPotion or SkillPotion or PowerPotion or ColorlessPotion or Fortifier or
            BlessingOfTheForge or GigantificationPotion or OrobicAcid or FairyInABottle or GlowwaterPotion or
            TouchOfInsanity or GamblersBrew or Duplicator or LiquidMemories or DropletOfPrecognition => ".shadowDescription",
        _ => null
    };

    internal static PotionModel? RandomHigherPotion(Player player, Rng rng, int rank)
    {
        var options = PotionFactory.GetPotionOptions(player).ToArray();
        var rarity = rank == 3 || rng.NextInt(35) < 10 ? PotionRarity.Rare : PotionRarity.Uncommon;
        var candidates = options.Where(potion => potion.Rarity == rarity).ToArray();
        if (candidates.Length == 0 && rank == 2)
            candidates = options.Where(potion => potion.Rarity is PotionRarity.Uncommon or PotionRarity.Rare).ToArray();
        return rng.NextItem(candidates)?.ToMutable();
    }

    private sealed record Applied(int Rank);
}

public sealed class ShadowPotionVariablesPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_potion_variables";
    public static ModPatchTarget[] GetTargets() => [new(typeof(PotionModel), "get_DynamicVars", Type.EmptyTypes)];
    public static void Postfix(PotionModel __instance, DynamicVarSet __result)
    {
        if (__instance.IsMutable) ShadowPotionRules.Apply(__instance, __result);
    }
}

public sealed class ShadowPotionDescriptionPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_potion_description";
    public static ModPatchTarget[] GetTargets() => [new(typeof(PotionModel), "get_DynamicDescription", Type.EmptyTypes)];
    public static void Postfix(PotionModel __instance, ref LocString __result)
    {
        var cost = ShadowRanks.PotionCost(__instance);
        if (cost == 0) return;
        if (ShadowPotionRules.AlternateDescription(__instance) is { } suffix)
        {
            var alternate = new LocString("potions", __instance.Id.Entry + suffix);
            alternate.AddVariablesFrom(__result);
            alternate.Add("SelectionCount", ShadowPotionRules.SelectionCount(__instance));
            __result = alternate;
        }
        var formatted = __result.GetFormattedText();
        __result = NShadowSelector.Loc("POTION_DESCRIPTION");
        __result.Add("Description", formatted);
        __result.Add("Amount", cost);
    }
}

public sealed class ShadowPotionUsePatch : IPatchMethod
{
    private static readonly ConditionalWeakTable<PowerModel, ErodedGiant> GiantPowers = new();
    public static string PatchId => "yog_sothoths_yard_shadow_potion_use";
    public static ModPatchTarget[] GetTargets() =>
        new[] { typeof(AttackPotion), typeof(SkillPotion), typeof(PowerPotion), typeof(ColorlessPotion),
            typeof(Fortifier), typeof(BlessingOfTheForge), typeof(GigantificationPotion), typeof(FairyInABottle),
            typeof(GlowwaterPotion), typeof(OrobicAcid), typeof(PotionOfBinding), typeof(TouchOfInsanity),
            typeof(GamblersBrew), typeof(Duplicator), typeof(EntropicBrew), typeof(LiquidMemories),
            typeof(DropletOfPrecognition) }
            .Select(type => new ModPatchTarget(type, "OnUse", [typeof(PlayerChoiceContext), typeof(Creature)])).ToArray();

    public static bool Prefix(PotionModel __instance, PlayerChoiceContext choiceContext, Creature? target,
        ref Task __result, out ShadowCombatGenerationPatch.Options? __state)
    {
        __state = ShadowCombatGenerationPatch.Current.Value;
        var rank = ShadowRanks.Of(__instance);
        if (rank == 0) return true;
        if (__instance is PotionOfBinding binding)
        {
            __result = UseBinding(choiceContext, binding);
            return false;
        }
        if (target?.Player is not { } player) return true;
        switch (__instance)
        {
            case AttackPotion or SkillPotion or PowerPotion or ColorlessPotion:
                ShadowCombatGenerationPatch.Current.Value = new ShadowCombatGenerationPatch.Options(player, 3, 4,
                    UseNativeChoice: true);
                return true;
            case Fortifier:
                __result = CreatureCmd.GainBlock(target, target.Block * 3, ValueProp.Unpowered, null);
                break;
            case BlessingOfTheForge:
                foreach (var card in PileType.Hand.GetPile(player).Cards.ToArray())
                {
                    if (ShadowCardRules.TryGet(card, 1, out _) && ShadowRanks.Of(card) == 0) ShadowRanks.Set(card, 1);
                    if (card.IsUpgradable) CardCmd.Upgrade(card);
                }
                __result = Task.CompletedTask;
                break;
            case FairyInABottle:
                __result = CreatureCmd.Heal(target, Math.Max(1m, target.MaxHp * 0.4m));
                break;
            case GigantificationPotion:
                __result = ApplyGiantPower(choiceContext, target, __instance);
                break;
            case GlowwaterPotion:
                __result = UseGlowwater(choiceContext, player, __instance);
                break;
            case OrobicAcid:
                __result = UseOrobicAcid(player, __instance);
                break;
            case TouchOfInsanity:
                __result = UseTouchOfInsanity(choiceContext, player);
                break;
            case GamblersBrew:
                __result = UseGamblersBrew(choiceContext, player, __instance);
                break;
            case Duplicator:
                __result = ShadowDuplicationPatch.Apply(choiceContext, target);
                break;
            case EntropicBrew:
                __result = UseEntropicBrew(player, rank);
                break;
            case LiquidMemories:
                __result = RetrieveCards(choiceContext, player, PileType.Discard);
                break;
            case DropletOfPrecognition:
                __result = RetrieveCards(choiceContext, player, PileType.Draw);
                break;
            default: return true;
        }
        return false;
    }

    public static void Postfix(ShadowCombatGenerationPatch.Options? __state) =>
        ShadowCombatGenerationPatch.Current.Value = __state;

    private static async Task UseBinding(PlayerChoiceContext context, PotionOfBinding potion)
    {
        var source = potion.Owner.Creature;
        if (source.CombatState is not { } combat) return;
        var targets = combat.HittableEnemies;
        foreach (var enemy in targets)
            NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(
                NSmokePuffVfx.Create(enemy, NSmokePuffVfx.SmokePuffColor.Green));
        await PowerCmd.Apply<WeakPower>(context, targets, potion.DynamicVars["WeakPower"].BaseValue, source, null);
        await PowerCmd.Apply<VulnerablePower>(context, targets, potion.DynamicVars["VulnerablePower"].BaseValue, source, null);
    }

    private static async Task UseTouchOfInsanity(PlayerChoiceContext context, Player player)
    {
        if (player.PlayerCombatState is not { } combat) return;
        var cards = combat.AllCards.ToArray();
        if (cards.Length == 0) return;
        var prefs = new CardSelectorPrefs(NShadowSelector.Loc("POTION_CHOOSE_TWO"), Math.Min(2, cards.Length));
        foreach (var card in await CardSelectCmd.FromSimpleGrid(context, cards, player, prefs))
            card.SetToFreeThisCombat();
    }

    private static async Task UseGamblersBrew(PlayerChoiceContext context, Player player, PotionModel potion)
    {
        NCombatRoom.Instance?.PlaySplashVfx(player.Creature, new Godot.Color("a19f91"));
        var prefs = new CardSelectorPrefs(potion.SelectionScreenPrompt, 0, PileType.Hand.GetPile(player).Cards.Count);
        var cards = (await CardSelectCmd.FromHandForDiscard(context, player, prefs, null, potion)).ToList();
        await CardCmd.DiscardAndDraw(context, cards, cards.Count * 2);
    }

    private static async Task UseEntropicBrew(Player player, int rank)
    {
        while (player.HasOpenPotionSlots &&
            ShadowPotionRules.RandomHigherPotion(player, player.RunState.Rng.CombatPotionGeneration, rank) is { } potion)
            if (!(await PotionCmd.TryToProcure(potion, player)).success) break;
    }

    private static async Task RetrieveCards(PlayerChoiceContext context, Player player, PileType type)
    {
        var pile = type.GetPile(player);
        if (pile.Cards.Count == 0) return;
        if (type == PileType.Discard) NCombatRoom.Instance?.PlaySplashVfx(player.Creature, Godot.Colors.Blue);
        var prefs = new CardSelectorPrefs(NShadowSelector.Loc("POTION_CHOOSE_TWO"), Math.Min(2, pile.Cards.Count));
        foreach (var card in (await CardSelectCmd.FromCombatPile(context, pile, player, prefs)).ToArray())
        {
            if (type == PileType.Discard) card.SetToFreeThisTurn();
            await CardPileCmd.Add(card, PileType.Hand);
        }
    }

    private static async Task UseGlowwater(PlayerChoiceContext context, Player player, PotionModel potion)
    {
        foreach (var card in PileType.Hand.GetPile(player).Cards.ToArray()) await CardCmd.Exhaust(context, card);
        var count = Math.Min(ShadowPotionRules.SelectionCount(potion), PileType.Draw.GetPile(player).Cards.Count);
        if (count > 0)
        {
            var cards = await CardSelectCmd.FromCombatPile(context, PileType.Draw.GetPile(player), player,
                new CardSelectorPrefs(NShadowSelector.Loc("GLOWWATER_PROMPT"), 0, count) { RequireManualConfirmation = true });
            foreach (var card in cards.ToArray()) await CardCmd.Exhaust(context, card);
        }
        await CardPileCmd.Draw(context, potion.DynamicVars.Cards.BaseValue, player);
    }

    private static async Task UseOrobicAcid(Player player, PotionModel potion)
    {
        var pool = player.Character.CardPool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint).ToList();
        var rng = player.RunState.Rng.CombatCardGeneration;
        var cards = CardFactory.GetDistinctForCombat(player, pool.Where(card => card.Type == CardType.Attack), 2, rng)
            .Concat(CardFactory.GetDistinctForCombat(player, pool.Where(card => card.Type == CardType.Skill), 1, rng))
            .Concat(CardFactory.GetDistinctForCombat(player, pool.Where(card => card.Type == CardType.Power), 1, rng)).ToList();
        foreach (var card in cards) card.SetToFreeThisTurn();
        await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand, potion.Owner);
    }

    private static async Task ApplyGiantPower(PlayerChoiceContext context, Creature target, PotionModel potion)
    {
        var power = await PowerCmd.Apply<GigantificationPower>(context, target, 1, potion.Owner.Creature, null);
        if (power != null) GiantPowers.GetValue(power, static _ => new ErodedGiant());
    }

    internal static bool IsErodedGiant(PowerModel power) => GiantPowers.TryGetValue(power, out _);
    private sealed class ErodedGiant { }
}

public sealed class ShadowDuplicationPatch : IPatchMethod
{
    private static readonly ConditionalWeakTable<DuplicationPower, Charges> ExtraPlays = new();
    public static string PatchId => "yog_sothoths_yard_shadow_duplication";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(DuplicationPower), nameof(DuplicationPower.ModifyCardPlayCount),
            [typeof(CardModel), typeof(Creature), typeof(int)])];

    internal static async Task Apply(PlayerChoiceContext context, Creature target)
    {
        if (await PowerCmd.Apply<DuplicationPower>(context, target, 1, target, null) is { } power)
            ExtraPlays.GetValue(power, static _ => new Charges()).Amount++;
    }

    internal static int Remaining(DuplicationPower power) =>
        ExtraPlays.TryGetValue(power, out var charges) ? charges.Amount : 0;

    internal static void Spend(DuplicationPower power)
    {
        if (ExtraPlays.TryGetValue(power, out var charges) && charges.Amount > 0) charges.Amount--;
    }

    public static void Postfix(DuplicationPower __instance, CardModel card, ref int __result)
    {
        if (card.Owner.Creature == __instance.Owner && Remaining(__instance) > 0) __result++;
    }

    private sealed class Charges
    {
        internal int Amount { get; set; }
    }
}

public sealed class ShadowDuplicationSpendPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_duplication_spend";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(DuplicationPower), nameof(DuplicationPower.AfterModifyingCardPlayCount), [typeof(CardModel)])];

    public static void Prefix(DuplicationPower __instance) => ShadowDuplicationPatch.Spend(__instance);
}

public sealed class ShadowDuplicationDescriptionPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_duplication_description";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(PowerModel), "get_SmartDescription", Type.EmptyTypes)];

    public static void Postfix(PowerModel __instance, ref LocString __result)
    {
        if (__instance is not DuplicationPower power || ShadowDuplicationPatch.Remaining(power) <= 0) return;
        __result = NShadowSelector.Loc("DUPLICATION_POWER");
        var count = Math.Min(ShadowDuplicationPatch.Remaining(power), power.Amount);
        __result.Add("DoubleCount", count);
        __result.Add("SingleCount", power.Amount - count);
    }
}

public sealed class ShadowCombatGenerationPatch : IPatchMethod
{
    internal static readonly AsyncLocal<Options?> Current = new();
    public static string PatchId => "yog_sothoths_yard_shadow_combat_generation";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(CardFactory), nameof(CardFactory.GetDistinctForCombat),
            [typeof(Player), typeof(IEnumerable<CardModel>), typeof(int), typeof(Rng)])];

    public static void Prefix(Player player, ref int count)
    {
        if (Current.Value is { } options && options.Player == player && options.NativeCount == count)
            count = options.Count;
    }

    public sealed record Options(Player Player, int NativeCount, int Count, bool UseNativeChoice = false);
}

public sealed class ShadowCombatChoicePatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_combat_choice";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(CardSelectCmd), nameof(CardSelectCmd.FromChooseACardScreen),
            [typeof(PlayerChoiceContext), typeof(IReadOnlyList<CardModel>), typeof(Player), typeof(bool)])];

    public static bool Prefix(PlayerChoiceContext context, IReadOnlyList<CardModel> cards, Player player,
        bool canSkip, ref Task<CardModel?> __result)
    {
        if (cards.Count <= 3 || ShadowCombatGenerationPatch.Current.Value is not { UseNativeChoice: false } options ||
            options.Player != player) return true;
        __result = ChooseFromGrid(context, cards, player, canSkip);
        return false;
    }

    private static async Task<CardModel?> ChooseFromGrid(PlayerChoiceContext context, IReadOnlyList<CardModel> cards,
        Player player, bool canSkip)
    {
        if (LocalContext.IsMe(player))
            foreach (var card in cards) SaveManager.Instance.MarkCardAsSeen(card);
        var prefs = new CardSelectorPrefs(NShadowSelector.Loc("CHOOSE_GENERATED_CARD"), canSkip ? 0 : 1, 1);
        return (await CardSelectCmd.FromSimpleGrid(context, cards, player, prefs)).FirstOrDefault();
    }
}

public sealed class ShadowPotionChoiceLimitPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_potion_choice_limit";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(CardSelectCmd), nameof(CardSelectCmd.FromChooseACardScreen),
            [typeof(PlayerChoiceContext), typeof(IReadOnlyList<CardModel>), typeof(Player), typeof(bool)], MethodType.Async)];

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var count = AccessTools.PropertyGetter(typeof(IReadOnlyCollection<CardModel>), nameof(IReadOnlyCollection<CardModel>.Count));
        return new CodeMatcher(instructions)
            .MatchStartForward(new CodeMatch(OpCodes.Callvirt, count), new CodeMatch(OpCodes.Ldc_I4_3))
            .ThrowIfInvalid("Could not find the card selection size limit.")
            .Advance(1)
            .Set(OpCodes.Call, AccessTools.DeclaredMethod(typeof(ShadowPotionChoiceLimitPatch), nameof(ChoiceLimit)))
            .InstructionEnumeration();
    }

    private static int ChoiceLimit() =>
        ShadowCombatGenerationPatch.Current.Value is { UseNativeChoice: true } options ? options.Count : 3;
}

public sealed class ShadowGigantificationPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_gigantification";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(GigantificationPower), nameof(GigantificationPower.ModifyDamageMultiplicative),
            [typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(Creature), typeof(CardModel), typeof(CardPlay)])];
    public static void Postfix(GigantificationPower __instance, ref decimal __result)
    {
        if (__result == 3 && ShadowPotionUsePatch.IsErodedGiant(__instance)) __result = 4;
    }
}

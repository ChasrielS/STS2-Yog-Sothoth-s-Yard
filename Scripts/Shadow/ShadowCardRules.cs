using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Patching.Models;

namespace YogSothothsYard.Scripts.Shadow;

internal static partial class ShadowCardRules
{
    private static readonly ConditionalWeakTable<DynamicVarSet, AppliedRule> Applied = new();
    private static readonly Dictionary<(Type Card, int Rank), Rule> Rules = CreateRules();
    private static readonly AccessTools.FieldRef<CalculatedVar, Func<CardModel, Creature?, decimal>> Multiplier =
        AccessTools.FieldRefAccess<CalculatedVar, Func<CardModel, Creature?, decimal>>("_multiplierCalc");
    private static readonly AccessTools.FieldRef<TheBall, decimal> BallDamage =
        AccessTools.FieldRefAccess<TheBall, decimal>("_extraDamageFromPlays");

    private static Dictionary<(Type Card, int Rank), Rule> CreateRules()
    {
        var rules = new Dictionary<(Type Card, int Rank), Rule>();
        AddCharacterRules(rules);
        AddBaseRules(rules);
        return rules;
    }

    internal static bool TryGet(CardModel card, int rank, out Rule rule) =>
        Rules.TryGetValue((card.GetType(), rank), out rule);

    internal static IEnumerable<int> Available(CardModel card) =>
        Enumerable.Range(1, 3).Where(rank => TryGet(card, rank, out _));

    internal static bool IsEroded(CardModel card) => ShadowRanks.Of(card) > 0;

    internal static int NextRank(CardModel card)
    {
        var current = ShadowRanks.Of(card);
        return Available(card).FirstOrDefault(rank => rank > current, current);
    }

    internal static void Apply(CardModel card, DynamicVarSet variables, Dictionary<string, decimal>? beforeUpgrade = null)
    {
        var rank = ShadowRanks.Of(card);
        if (rank == 0) return;
        if (card is Guilty guilty)
        {
            variables["Combats"].BaseValue = Math.Max(0, Guilty.maxCombats + rank - guilty.CombatsSeen);
            return;
        }
        if (!TryGet(card, rank, out var rule) || rule.Values is not { Length: > 0 } values) return;
        if (beforeUpgrade == null && Applied.TryGetValue(variables, out var previous) &&
            previous.Rank == rank && previous.Upgrade == card.CurrentUpgradeLevel)
            return;
        Applied.Remove(variables);
        Applied.Add(variables, new AppliedRule(rank, card.CurrentUpgradeLevel));
        foreach (var value in values)
        {
            if (!variables.TryGetValue(value.Name, out var variable) &&
                (value.Name != "HitCount" || !variables.TryGetValue("Repeat", out variable))) continue;
            decimal desired = card.IsUpgraded ? value.Upgraded : value.Normal;
            if (card.Id.Entry == "DINGY_DAGGER")
            {
                var upgrades = (decimal)card.CurrentUpgradeLevel;
                desired = value.Normal + upgrades * (value.Upgraded - value.Normal) + upgrades * (upgrades - 1) / 2;
            }
            if (card is TheBall ball && value.Name == "Damage") desired += BallDamage(ball);
            if (card is YuanYangFengHuan reflection && value.Name == "RequiredKnife") desired += reflection.ExtraRequiredKnife;
            variable.BaseValue = desired;
            if (beforeUpgrade != null && beforeUpgrade.TryGetValue(variable.Name, out var before))
            {
                if (before != desired) variable.UpgradeValueBy(0);
                else variable.FinalizeUpgrade();
            }
        }
        if (card is Stack)
            Multiplier(variables.CalculatedBlock) = static (model, _) =>
                PileType.Hand.GetPile(model.Owner).Cards.Count + PileType.Discard.GetPile(model.Owner).Cards.Count +
                (ShadowRanks.Of(model) == 3 ? PileType.Draw.GetPile(model.Owner).Cards.Count : 0);
        else if (card is MindBlast)
            Multiplier(variables.CalculatedDamage) = static (model, _) =>
                PileType.Hand.GetPile(model.Owner).Cards.Count + PileType.Draw.GetPile(model.Owner).Cards.Count;
        variables.RecalculateForUpgradeOrEnchant();
    }

    private static void Add<T>(Dictionary<(Type, int), Rule> rules, int rank, params Value[] values)
        where T : CardModel => rules.Add((typeof(T), rank), new Rule(Values: values));

    private static Value V(string name, int value) => new(name, value, value);
    private static Value V(string name, int value, int upgraded) => new(name, value, upgraded);
    private static Value D(int value, int upgraded) => V("Damage", value, upgraded);
    private static Value B(int value, int upgraded) => V("Block", value, upgraded);
    private static Value C(int value, int upgraded) => V("Cards", value, upgraded);
    private static Value A(int value, int upgraded) => V("Amount", value, upgraded);
    private static Value Base(int value, int upgraded) => V("CalculationBase", value, upgraded);
    private static Value Extra(int value, int upgraded) => V("ExtraDamage", value, upgraded);

    internal readonly record struct Rule(
        Value[]? Values = null, int? Cost = null, int DrawErosion = 0, int ExhaustErosion = 0, int EndErosion = 0);
    internal readonly record struct Value(string Name, int Normal, int Upgraded);
    private sealed record AppliedRule(int Rank, int Upgrade);
}

public sealed class ShadowCardVariablesPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_card_variables";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(CardModel), "get_DynamicVars", Type.EmptyTypes)];

    public static void Postfix(CardModel __instance, DynamicVarSet __result)
    {
        if (__instance.IsMutable) ShadowCardRules.Apply(__instance, __result);
    }
}

public sealed class ShadowCardUpgradePatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_card_upgrade";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(CardModel), nameof(CardModel.UpgradeInternal), Type.EmptyTypes)];

    public static void Prefix(CardModel __instance, out Dictionary<string, decimal>? __state)
    {
        __state = ShadowCardRules.TryGet(__instance, ShadowRanks.Of(__instance), out var rule) && rule.Values is { Length: > 0 }
            ? __instance.DynamicVars.ToDictionary(entry => entry.Key, entry => entry.Value.BaseValue)
            : null;
    }

    public static void Postfix(CardModel __instance, Dictionary<string, decimal>? __state)
    {
        ShadowEvents.TagUpgrade(__instance);
        if (ShadowCardRules.IsEroded(__instance))
            ShadowCardRules.Apply(__instance, __instance.DynamicVars, __state);
    }
}

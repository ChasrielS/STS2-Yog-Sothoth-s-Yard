using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Patching.Models;

namespace YogSothothsYard.Scripts.Shadow;

public sealed class ShadowCardDescriptionPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_card_description";
    public static ModPatchTarget[] GetTargets() => [new(typeof(CardModel), "get_Description", Type.EmptyTypes)];

    public static void Postfix(CardModel __instance, ref LocString __result)
    {
        var rank = ShadowRanks.Of(__instance);
        if (rank == 0 || !ShadowCardRules.TryGet(__instance, rank, out _)) return;
        var descriptionKey = __instance.Id.Entry + ".shadowDescription";
        var alternate = LocString.GetIfExists("cards", descriptionKey + rank) ?? LocString.GetIfExists("cards", descriptionKey);
        if (alternate == null) return;
        alternate.Add("Rank", rank);
        alternate.Add("Roman", rank == 1 ? "I" : rank == 2 ? "II" : "III");
        alternate.Add("Options", 3 + rank);
        alternate.Add("DrawLimit", __instance is Normality && rank == 3 ? 2 : 3);
        alternate.Add("CardInterval", 10 - rank);
        alternate.Add("IsRankThree", rank == 3);
        alternate.Add("QuestErosion", __instance is SpoilsMap ? ShadowQuestMapPatch.Erosion(rank) : ShadowQuestHatchPatch.Erosion(rank));
        __result = alternate;
    }
}

public sealed class ShadowCardTurnEndPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_card_turn_end";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(CardModel), nameof(CardModel.OnTurnEndInHandWrapper), [typeof(PlayerChoiceContext)])];

    public static void Postfix(CardModel __instance, PlayerChoiceContext choiceContext, ref Task __result)
    {
        if (ShadowCardRules.TryGet(__instance, ShadowRanks.Of(__instance), out var rule) && rule.EndErosion > 0)
            __result = Apply(__result, __instance, choiceContext, rule.EndErosion);
    }

    private static async Task Apply(Task original, CardModel card, PlayerChoiceContext context, int amount)
    {
        await original;
        await ShadowRun.AddErosion(card.Owner, amount, context);
    }
}

public sealed class ShadowRegretDamagePatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_regret_damage";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(CreatureCmd), nameof(CreatureCmd.Damage), [typeof(PlayerChoiceContext), typeof(Creature),
            typeof(decimal), typeof(ValueProp), typeof(Creature), typeof(CardModel), typeof(CardPlay)])];

    public static void Prefix(CardModel? cardSource, ref decimal amount)
    {
        if (cardSource is Regret) amount += ShadowRanks.Of(cardSource);
    }
}

public sealed class ShadowNormalityPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_normality";
    public static ModPatchTarget[] GetTargets() => [new(typeof(Normality), "get_ShouldPreventCardPlay", Type.EmptyTypes)];

    public static void Postfix(Normality __instance, ref bool __result)
    {
        if (__result || ShadowRanks.Of(__instance) != 3 || __instance.CombatState is not { } combat) return;
        __result = CombatManager.Instance.History.CardPlaysStarted.Count(entry =>
            entry.HappenedThisTurn(combat) && entry.CardPlay.Player == __instance.Owner) >= 2;
    }
}

public sealed class ShadowGuiltyPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_guilty";
    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(Guilty), nameof(Guilty.AfterCombatEnd), [typeof(CombatRoom)])
    ];

    public static bool Prefix(Guilty __instance, ref Task __result)
    {
        var rank = ShadowRanks.Of(__instance);
        if (rank == 0) return true;
        __result = Advance(__instance, rank);
        return false;
    }

    private static async Task Advance(Guilty card, int rank)
    {
        if (card.Pile?.Type != PileType.Deck) return;
        card.CombatsSeen++;
        if (card.CombatsSeen >= Guilty.maxCombats + rank) await CardPileCmd.RemoveFromDeck(card);
    }
}

public sealed class ShadowNativeCardPlayPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_native_card_play";
    public static ModPatchTarget[] GetTargets() =>
        new[] { typeof(Discovery), typeof(Splash), typeof(Automation), typeof(RipAndTear),
            typeof(Entrench), typeof(Outmaneuver), typeof(Distraction), typeof(HelloWorld), typeof(Metamorphosis) }
            .Select(type => new ModPatchTarget(type, "OnPlay", [typeof(PlayerChoiceContext), typeof(CardPlay)])).ToArray();

    public static bool Prefix(CardModel __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay,
        ref Task __result, out ShadowCombatGenerationPatch.Options? __state)
    {
        __state = ShadowCombatGenerationPatch.Current.Value;
        var rank = ShadowRanks.Of(__instance);
        if (rank == 0) return true;
        if (__instance is Discovery or Splash)
        {
            ShadowCombatGenerationPatch.Current.Value = new ShadowCombatGenerationPatch.Options(__instance.Owner, 3, 3 + rank);
            return true;
        }
        if (__instance is Automation)
        {
            __result = PowerCmd.Apply<ShadowAutomationPower>(choiceContext, __instance.Owner.Creature,
                10 - rank, __instance.Owner.Creature, __instance);
            return false;
        }
        if (__instance is RipAndTear && rank == 3 && __instance.CombatState is { } combat)
        {
            __result = DamageCmd.Attack(__instance.DynamicVars.Damage.BaseValue).FromCard(__instance, cardPlay)
                .TargetingRandomOpponents(combat).WithHitCount(3).WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
            return false;
        }
        if (rank != 3) return true;
        switch (__instance)
        {
            case Entrench:
                __result = CreatureCmd.GainBlock(__instance.Owner.Creature, __instance.Owner.Creature.Block * 2,
                    ValueProp.Unpowered | ValueProp.Move, cardPlay);
                return false;
            case Outmaneuver:
                __result = PowerCmd.Apply<ShadowTwoTurnEnergyPower>(choiceContext, __instance.Owner.Creature,
                    __instance.DynamicVars.Energy.IntValue, __instance.Owner.Creature, __instance);
                return false;
            case Distraction:
                __result = GenerateSkills(__instance);
                return false;
            case HelloWorld:
                __result = ApplyHelloWorld(__instance, choiceContext);
                return false;
            case Metamorphosis:
                __result = GenerateUpgradedAttacks(__instance);
                return false;
        }
        return true;
    }

    public static void Postfix(ShadowCombatGenerationPatch.Options? __state) =>
        ShadowCombatGenerationPatch.Current.Value = __state;

    private static async Task GenerateSkills(CardModel card)
    {
        var player = card.Owner;
        var pool = player.Character.CardPool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Where(candidate => candidate.Type == CardType.Skill);
        var cards = CardFactory.GetDistinctForCombat(player, pool, 2, player.RunState.Rng.CombatCardGeneration).ToList();
        foreach (var generated in cards) generated.SetToFreeThisTurn();
        await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand, player);
    }

    private static async Task GenerateUpgradedAttacks(CardModel card)
    {
        var player = card.Owner;
        var pool = player.Character.CardPool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Where(candidate => candidate.Type == CardType.Attack && candidate.IsUpgradable);
        var cards = CardFactory.GetForCombat(player, pool, card.DynamicVars.Cards.IntValue, player.RunState.Rng.CombatCardGeneration);
        foreach (var generated in cards)
        {
            CardCmd.Upgrade(generated, CardPreviewStyle.None);
            generated.SetToFreeThisCombat();
            var added = await CardPileCmd.AddGeneratedCardToCombat(generated, PileType.Draw, player, CardPilePosition.Random);
            CardCmd.PreviewCardPileAdd(added);
        }
    }

    private static async Task ApplyHelloWorld(CardModel card, PlayerChoiceContext context)
    {
        var player = card.Owner;
        await CreatureCmd.TriggerAnim(player.Creature, "PowerUp", player.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<HelloWorldPower>(context, player.Creature, 2, player.Creature, card);
    }
}

public sealed class ShadowWarlordCardPlayPatch : IPatchMethod
{
    private static readonly AsyncLocal<CardModel?> Source = new();

    public static string PatchId => "yog_sothoths_yard_shadow_warlord_card_play";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(MightyWeapon), "OnPlay", [typeof(PlayerChoiceContext), typeof(CardPlay)]),
         new(typeof(MightyShield), "OnPlay", [typeof(PlayerChoiceContext), typeof(CardPlay)])];

    public static void Prefix(CardModel __instance, out CardModel? __state)
    {
        __state = Source.Value;
        Source.Value = ShadowRanks.Of(__instance) > 0 ? __instance : null;
    }

    public static void Postfix(CardModel? __state) => Source.Value = __state;

    internal static void TagDazed(CardModel card, PileType pile, Player? creator)
    {
        if (card is Dazed && pile == PileType.Draw && Source.Value is { } source &&
            source.Owner == creator && creator == card.Owner)
            ShadowRanks.Set(card, ShadowRanks.Of(source));
    }
}

public sealed class ShadowGeneratedDazedPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_generated_dazed";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(CardPileCmd), nameof(CardPileCmd.AddGeneratedCardToCombat),
            [typeof(CardModel), typeof(PileType), typeof(Player), typeof(CardPilePosition)])];

    public static void Prefix(CardModel card, PileType newPileType, Player? creator) =>
        ShadowWarlordCardPlayPatch.TagDazed(card, newPileType, creator);
}

public sealed class ShadowWarlordCardTipsPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_warlord_card_tips";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(CardModel), "get_HoverTips", Type.EmptyTypes)];

    public static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
    {
        if (__instance is not (MightyWeapon or MightyShield) ||
            ShadowRanks.Of(__instance) is not (> 0 and var rank)) return;
        var preview = ModelDb.Card<Dazed>().ToMutable();
        ShadowRanks.Set(preview, rank);
        __result = __result.Where(tip => tip.CanonicalModel is not Dazed)
            .Append(HoverTipFactory.FromCard(preview)).Concat(preview.HoverTips);
    }
}

[RegisterPower]
public sealed class ShadowTwoTurnEnergyPower : HaaLouLingPower
{
    private int _turns = 2;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    public override string? CustomIconPath => "res://images/atlases/power_atlas.sprites/energy_next_turn_power.tres";
    public override string? CustomBigIconPath => "res://images/powers/energy_next_turn_power.png";

    public override async Task AfterEnergyReset(Player player)
    {
        if (player.Creature != Owner || _turns <= 0) return;
        _turns--;
        Flash();
        await PlayerCmd.GainEnergy(Amount, player);
        if (_turns == 0) await PowerCmd.Remove(this);
    }
}

[RegisterPower]
public sealed class ShadowAutomationPower : HaaLouLingPower
{
    private int _cardsDrawn;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    public override int DisplayAmount => Math.Max(0, Amount - _cardsDrawn);
    public override string? CustomIconPath => "res://images/atlases/power_atlas.sprites/automation_power.tres";
    public override string? CustomBigIconPath => "res://images/powers/automation_power.png";

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card.Owner.Creature != Owner) return;
        if (++_cardsDrawn >= Amount)
        {
            _cardsDrawn = 0;
            Flash();
            await PlayerCmd.GainEnergy(1, card.Owner);
        }
        InvokeDisplayAmountChanged();
    }
}

public sealed class ShadowQuestMapPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_quest_map";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(SpoilsMap), nameof(SpoilsMap.OnQuestComplete), Type.EmptyTypes)];

    public static void Postfix(SpoilsMap __instance, ref Task<int> __result)
    {
        var rank = ShadowRanks.Of(__instance);
        if (rank > 0) __result = Charge(__result, __instance, rank);
    }

    private static async Task<int> Charge(Task<int> result, SpoilsMap card, int rank)
    {
        var gold = await result;
        await ShadowRun.AddErosion(card.Owner, Erosion(rank));
        return gold;
    }

    internal static int Erosion(int rank) => rank switch { 1 => 33, 2 => 83, _ => 150 };
}

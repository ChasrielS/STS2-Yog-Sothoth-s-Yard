using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.RunRngs;

namespace YogSothothsYard.Scripts.Shadow;

internal static partial class ShadowMonsterRules
{
    private static readonly ConditionalWeakTable<MonsterModel, State> Monsters = new();
    private static readonly AsyncLocal<MoveExecution?> Acting = new();
    private static readonly AccessTools.FieldRef<MoveState, IReadOnlyList<AbstractIntent>> Intents =
        AccessTools.FieldRefAccess<MoveState, IReadOnlyList<AbstractIntent>>("<Intents>k__BackingField");

    private static readonly Dictionary<string, Rule> Rules = new()
    {
        ["NIBBIT"] = new(10, 8, 12, "BUTT_MOVE"),
        ["SHRINKER_BEETLE"] = new(0, 5, 10, "STOMP_MOVE"),
        ["LEAF_SLIME_S"] = new(5, 1, 2, "TACKLE_MOVE"),
        ["LEAF_SLIME_M"] = new(5, 2, 4, "CLUMP_SHOT"),
        ["TWIG_SLIME_S"] = new(5, 1, 2, "TACKLE_MOVE"),
        ["TWIG_SLIME_M"] = new(5, 2, 4, "POKEY_POUNCE_MOVE"),
        ["FLYCONID"] = new(10, 8, 12, "SMASH_MOVE"),
        ["BRUTE_RUBY_RAIDER"] = new(10, 3, 6, "BEAT_MOVE"),
        ["ASSASSIN_RUBY_RAIDER"] = new(10, 2, 4, "KILLSHOT_MOVE"),
        ["AXE_RUBY_RAIDER"] = new(10, 5, 8, "BIG_SWING"),
        ["CROSSBOW_RUBY_RAIDER"] = new(10, 3, 6, "FIRE_MOVE"),
        ["TRACKER_RUBY_RAIDER"] = new(10, 2, 4, "HOUNDS_MOVE"),
        ["FOGMOG"] = new(10, 8, 12, "HEADBUTT_MOVE"),
        ["EYE_WITH_TEETH"] = new(10, 0, 0),
        ["MAWLER"] = new(10, 10, 20, "RIP_AND_TEAR_MOVE"),
        ["FUZZY_WURM_CRAWLER"] = new(10, 8, 16),
        ["INKLET"] = new(10, 5, 10, "PIERCING_GAZE_MOVE"),
        ["SNAPPING_JAXFRUIT"] = new(10, 3, 6, "ENERGY_ORB_MOVE"),
        ["SLITHERING_STRANGLER"] = new(0, 5, 8, "LASH"),
        ["VINE_SHAMBLER"] = new(10, 15, 22, "CHOMP_MOVE"),
        ["CUBEX_CONSTRUCT"] = new(10, 15, 22, "EXPEL_MOVE"),
        ["TOADPOLE"] = new(5, 4, 8, "SPIKE_SPIT_MOVE"),
        ["DAMP_CULTIST"] = new(0, 3, 5, "DARK_STRIKE_MOVE"),
        ["CALCIFIED_CULTIST"] = new(10, 3, 5, "DARK_STRIKE_MOVE"),
        ["GREMLIN_MERC"] = new(5, 15, 22, "HEHE_MOVE"),
        ["FAT_GREMLIN"] = new(10, 0, 5),
        ["SNEAKY_GREMLIN"] = new(10, 5, 10, "TACKLE_MOVE"),
        ["SEAPUNK"] = new(10, 8, 12, "SEA_KICK_MOVE"),
        ["FOSSIL_STALKER"] = new(0, 15, 22, "LATCH_MOVE"),
        ["LIVING_FOG"] = new(10, 6, 9, "SUPER_GAS_BLAST_MOVE"),
        ["GAS_BOMB"] = new(10, 10, 15, "EXPLODE_MOVE"),
        ["CORPSE_SLUG"] = new(0, 8, 12, "GLOMP_MOVE"),
        ["TWO_TAILED_RAT"] = new(10, 5, 10, "SCRATCH_MOVE"),
        ["SEWER_CLAM"] = new(5, 10, 20, "JET_MOVE"),
        ["HAUNTED_SHIP"] = new(5, 6, 9, "SWIPE_MOVE"),
        ["SLUDGE_SPINNER"] = new(10, 8, 12, "SLAM_MOVE"),
        ["PUNCH_CONSTRUCT"] = new(10, 15, 22, "STRONG_PUNCH_MOVE"),
        ["TUNNELER"] = new(10, 8, 16, "BELOW_MOVE", "FROM_BELOW_MOVE"),
        ["SPINY_TOAD"] = new(5, 15, 30, "SPIKE_EXPLOSION_MOVE"),
        ["CHOMPER"] = new(5, 5, 10, "CLAMP_MOVE"),
        ["HUNTER_KILLER"] = new(10, 15, 30),
        ["THE_OBSCURA"] = new(10, 10, 15),
        ["PARAFRIGHT"] = new(10, 2, 4, "SLAM_MOVE"),
        ["BOWLBUG_EGG"] = new(10, 2, 3, "BITE_MOVE"),
        ["BOWLBUG_NECTAR"] = new(10, 2, 4, "THRASH_MOVE", "THRASH2_MOVE"),
        ["BOWLBUG_ROCK"] = new(10, 2, 4, "HEADBUTT_MOVE"),
        ["BOWLBUG_SILK"] = new(10, 3, 6, "THRASH_MOVE"),
        ["LOUSE_PROGENITOR"] = new(0, 15, 22, "POUNCE_MOVE"),
        ["SLUMBERING_BEETLE"] = new(5, 2, 3, "ROLL_OUT_MOVE"),
        ["THIEVING_HOPPER"] = new(0, 8, 16, "THIEVERY_MOVE"),
        ["EXOSKELETON"] = new(0, 3, 6, "MANDIBLES_MOVE"),
        ["MYTE"] = new(5, 5, 8, "BITE_MOVE"),
        ["OVICOPTER"] = new(10, 10, 15, "SMASH_MOVE"),
        ["TOUGH_EGG"] = new(10, 0, 0),
        ["HATCHLING"] = new(10, 0, 2),
        ["GLOBE_HEAD"] = new(0, 15, 22, "THUNDER_STRIKE"),
        ["TURRET_OPERATOR"] = new(10, 2, 4, "UNLOAD_MOVE", "UNLOAD_MOVE_2"),
        ["LIVING_SHIELD"] = new(0, 2, 4, "SHIELD_SLAM_MOVE", "SMASH_MOVE"),
        ["AXEBOT"] = new(10, 10, 15, "ONE_TWO_MOVE"),
        ["OWL_MAGISTRATE"] = new(10, 20, 30, "VERDICT"),
        ["DEVOTED_SCULPTOR"] = new(0, 3, 6, "SAVAGE_MOVE"),
        ["FROG_KNIGHT"] = new(5, 15, 22, "STRIKE_DOWN_EVIL"),
        ["SLIMED_BERSERKER"] = new(5, 20, 30, "SMOTHER_MOVE"),
        ["THE_LOST"] = new(10, 5, 10, "EYE_LASERS"),
        ["THE_FORGOTTEN"] = new(10, 5, 10, "DREAD"),
        ["SCROLL_OF_BITING"] = new(10, 3, 6, "CHOMP"),
        ["FABRICATOR"] = new(10, 10, 20),
        ["GUARDBOT"] = new(10, 0, 0),
        ["NOISEBOT"] = new(10, 0, 0),
        ["STABBOT"] = new(10, 1, 2, "STAB_MOVE"),
        ["ZAPBOT"] = new(10, 1, 2, "ZAP"),
        ["FAKE_MERCHANT_MONSTER"] = new(10, 12, 18, "SWIPE_MOVE"),
        ["MYSTERIOUS_KNIGHT"] = new(5, 20, 40, "RAM_MOVE"),
    };

    internal static int Rank(MonsterModel? monster) =>
        monster != null && Monsters.TryGetValue(monster, out var state) ? state.Rank : 0;

    internal static void OnCreated(CombatState combat, Creature creature, MonsterModel monster, CombatSide side)
    {
        if (side == CombatSide.Player && monster.Id.Entry is ("BYRDPIP" or "BYRDINE_MONSTER") &&
            ShadowByrdpipPatch.Rank is > 0 and var petRank)
        {
            Monsters.Add(monster, new State(new Rule(0, 0, 0), petRank));
            return;
        }
        if (side != CombatSide.Enemy || combat.RunState is not RunState run || ShadowRun.Level(run) == 0) return;
        var id = monster.Id.Entry;
        var parent = Acting.Value?.Monster;
        var inherited = parent != null && Rank(parent) > 0 && ((parent.Id.Entry, id) switch
        {
            ("FOGMOG", "EYE_WITH_TEETH") or ("THE_OBSCURA", "PARAFRIGHT") or ("LIVING_FOG", "GAS_BOMB") or
            ("OVICOPTER", "TOUGH_EGG") or ("AXEBOT", "AXEBOT") or
            ("FABRICATOR", "GUARDBOT" or "NOISEBOT" or "STABBOT" or "ZAPBOT") or
            ("GREMLIN_MERC", "FAT_GREMLIN" or "SNEAKY_GREMLIN") => true,
            _ => false
        });
        var level = ShadowRun.Level(run);
        var rng = ModRunRngRegistry.Get(run, Entry.ModId, "shadow_monsters");
        var rank = inherited ? Rank(parent) : 0;
        if (id == "MYSTERIOUS_KNIGHT") rank = ShadowEvents.LanternKnightRank(combat, level);
        else if (rank == 0 && ShadowEvents.ForcesMonster(combat, monster))
            rank = ShadowChances.RollRank(rng, level, 0);
        if (rank == 0 && id != "MYSTERIOUS_KNIGHT" && parent == null &&
            combat.Encounter is { RoomType: var room } and not PunchOffEventEncounter)
        {
            var (chance, rarity) = room switch
            {
                RoomType.Monster => (ShadowChances.Common, 0),
                RoomType.Elite => (ShadowChances.Uncommon, 1),
                RoomType.Boss => (ShadowChances.Rare, 2),
                _ => (null, 0)
            };
            if (chance != null && ShadowChances.IsEroded(rng, level, chance))
                rank = ShadowChances.RollRank(rng, level, rarity);
        }
        if (rank == 0 || !Rules.TryGetValue(id, out var rule)) return;
        Monsters.Add(monster, new State(rule, rank));
        var hpPercent = id is "EYE_WITH_TEETH" or "FAT_GREMLIN" or "TOUGH_EGG" or "GUARDBOT" or "NOISEBOT"
            ? 10 * rank : rule.HpPercent;
        if (hpPercent == 0) return;
        var hp = creature.MaxHp + Math.Max(1, (int)Math.Ceiling(creature.MaxHp * hpPercent / 100m));
        creature.SetMaxHpInternal(hp);
        creature.SetCurrentHpInternal(hp);
    }

    internal static void PrepareMove(MonsterModel monster, MoveState move)
    {
        if (!Monsters.TryGetValue(monster, out var state) || monster.Creature.Side != CombatSide.Enemy || move.Id == "UNSET_MOVE") return;
        if (state.Plan is { } oldPlan && oldPlan.NativeMove == move && oldPlan.Turn == state.Turns) return;
        var plan = PlanMove(monster, state, move);
        state.Plan = plan;
        Intents(move) = plan.Intents;
    }

    internal static void BeginAction(MonsterModel? monster, out object? previous)
    {
        previous = Acting.Value;
        Acting.Value = monster == null ? null : new MoveExecution(monster, monster.NextMove);
    }

    internal static void EndAction(object? previous) => Acting.Value = (MoveExecution?)previous;

    internal static async Task FinishMove(Task original, MonsterModel monster, MoveState move, ICombatState? combat, object? plan)
    {
        await original;
        if (!Monsters.TryGetValue(monster, out var state)) return;
        if (combat == null) return;
        state.Turns++;
        if (!GetNativeMove(move).Intents.Any(intent => intent.IntentType == IntentType.Stun)) state.CompletedMoves++;
        state.ConsecutiveReplacements = plan is MovePlan { Replaced: true } ? state.ConsecutiveReplacements + 1 : 0;
        if (plan is not MovePlan completed || completed.NativeMove != move) return;
        foreach (var action in completed.Actions)
            state.UsedMoves.Add(action.Move.Id);
        state.PreviousBuff = completed.Actions.FirstOrDefault(action =>
            (action.Kind & MoveKind.Buff) != 0 && action.Move.Id is
                "SPIKEN_MOVE" or "PROTRUDING_SPIKES_MOVE" or "JUDICIAL_FLIGHT")?.Move.Id;
        if (completed.Erosion > 0)
            foreach (var player in combat.Players)
                await ShadowRun.AddErosion(player, completed.Erosion);
    }

    internal static void TagGeneratedCards(ref IEnumerable<CardModel> cards)
    {
        if (ShadowDiscourtesyPatch.Rank.Value > 0)
        {
            var list = cards.ToList();
            foreach (var card in list.OfType<Dazed>()) ShadowRanks.Set(card, ShadowDiscourtesyPatch.Rank.Value);
            cards = list;
        }
    }

    internal static async Task ApplyInitialEffects(Task original, Creature creature)
    {
        await original;
        if (creature.Monster is not { } monster || Rank(monster) is not (> 0 and var rank)) return;
        if (creature.IsEnemy) ShadowErosionRankPower.AddTo(creature, rank);
        var stronger = creature.CombatState?.RunState.AscensionLevel >= 8;
        var context = new ThrowingPlayerChoiceContext();
        switch (monster.Id.Entry)
        {
            case "CUBEX_CONSTRUCT":
                await CreatureCmd.GainBlock(creature, rank * 3, ValueProp.Move, null);
                if (rank == 3) await Add<ArtifactPower>(1);
                break;
            case "FOSSIL_STALKER": await Add<SuckPower>(rank); break;
            case "CORPSE_SLUG": await Add<RavenousPower>(1 << (rank - 1)); break;
            case "SEWER_CLAM": await Add<PlatingPower>((stronger ? 3 : 2) * rank); break;
            case "LOUSE_PROGENITOR": await Add<CurlUpPower>((stronger ? 4 : 3) << (rank - 1)); break;
            case "SLUMBERING_BEETLE" or "FROG_KNIGHT": await Add<PlatingPower>((stronger ? 4 : 3) * rank); break;
            case "EXOSKELETON": await Add<HardToKillPower>(-(1 << (rank - 1))); break;
            case "GLOBE_HEAD": await Add<GalvanicPower>(1 << (rank - 1)); break;
            case "LIVING_SHIELD": await Add<RampartPower>(5 << (rank - 1)); break;
            case "MYSTERIOUS_KNIGHT":
                await Add<PlatingPower>(rank == 3 ? 2 : 1);
                if (rank > 1) await Add<StrengthPower>(rank - 1);
                break;
            case "GREMLIN_MERC":
                foreach (var power in creature.GetPowerInstances<ThieveryPower>().ToArray())
                    await PowerCmd.ModifyAmount(context, power, rank * 5, creature, null);
                break;
            case "INKLET" when rank == 3: await Add<SlipperyPower>(1); break;
            case "PUNCH_CONSTRUCT" when rank == 3: await Add<ArtifactPower>(1); break;
            case "AXEBOT" when rank == 3 && monster is Axebot { ShouldPlaySpawnAnimation: false }:
                await Add<StockPower>(1);
                break;
            case "SCROLL_OF_BITING" when rank == 3: await Add<PaperCutsPower>(1); break;
            case "ZAPBOT" when rank == 3: await Add<HighVoltagePower>(1); break;
        }

        Task Add<T>(int amount) where T : PowerModel => PowerCmd.Apply<T>(context, creature, amount, creature, null);
    }

    internal static decimal BonusBlock(Creature creature)
    {
        var action = Acting.Value;
        if (action == null || Rank(action.Monster) is not (> 0 and var rank)) return 0;
        var source = action.Monster;
        var high = source.Creature.CombatState?.RunState.AscensionLevel >= 8;
        if (source.Id.Entry == "GUARDBOT" && action.Move.Id == "GUARD_MOVE" &&
            creature.Monster?.Id.Entry == "FABRICATOR") return rank * 3;
        if (creature != source.Creature) return 0;
        return rank * ((source.Id.Entry, action.Move.Id) switch
        {
            ("AXE_RUBY_RAIDER", "SWING_1" or "SWING_2") or ("SLITHERING_STRANGLER", "THWACK") or
            ("THE_OBSCURA", "HARDENING_STRIKE_MOVE") => 1,
            ("SEAPUNK", "BUBBLE_BURP_MOVE") => high ? 2 : 1,
            ("BOWLBUG_EGG", "BITE_MOVE") => source.Creature.CombatState?.RunState.AscensionLevel >= 9 ? 2 : 1,
            ("PUNCH_CONSTRUCT", "READY_MOVE") or ("THE_FORGOTTEN", "MIASMA") => 2,
            ("TUNNELER", "BURROW_MOVE") => high ? 8 : 7,
            _ => 0
        });
    }

    internal static void ModifyPower(PowerModel power, Creature? applier, ref decimal amount)
    {
        var action = Acting.Value;
        if (action == null || Rank(action.Monster) is not (> 0 and var rank)) return;
        var source = action.Monster;
        if (source.Creature != applier) return;
        amount += (source.Id.Entry, action.Move.Id, power) switch
        {
            ("SLITHERING_STRANGLER", "CONSTRICT", ConstrictPower) or
            ("TOADPOLE", "SPIKEN_MOVE", ThornsPower) or
            ("SPINY_TOAD", "PROTRUDING_SPIKES_MOVE", ThornsPower) => rank,
            ("DAMP_CULTIST", "INCANTATION_MOVE", RitualPower) or
            ("THIEVING_HOPPER", "FLUTTER_MOVE", FlutterPower) => 1 << (rank - 1),
            ("DEVOTED_SCULPTOR", "FORBIDDEN_INCANTATION_MOVE", RitualPower) => rank == 3 ? 7 : 2 * rank,
            ("TOADPOLE", "SPIKE_SPIT_MOVE", ThornsPower) or
            ("SPINY_TOAD", "SPIKE_EXPLOSION_MOVE", ThornsPower) => -rank,
            ("CALCIFIED_CULTIST", "INCANTATION_MOVE", RitualPower) when rank == 3 => 1,
            ("HUNTER_KILLER", "TENDERIZING_GOOP_MOVE", TenderPower) when rank == 3 => 1,
            ("NIBBIT", "HISS_MOVE", StrengthPower) or ("SEAPUNK", "BUBBLE_BURP_MOVE", StrengthPower) or
            ("OVICOPTER", "NUTRITIONAL_PASTE_MOVE", StrengthPower) when rank >= 2 => 1,
            ("BOWLBUG_NECTAR", "BUFF_MOVE", StrengthPower) or ("MYTE", "SUCK_MOVE", StrengthPower) when rank == 2 => 1,
            ("AXEBOT", "BOOT_UP_MOVE", StrengthPower) when rank >= 2 && source is Axebot axebot =>
                2 - axebot.StockAmount + (rank == 3 && axebot.ShouldPlaySpawnAnimation
                    ? (axebot.Creature.CombatState?.RunState.AscensionLevel >= 9 ? 5 : 4) : 0),
            ("THE_LOST", "DEBILITATING_SMOG", StrengthPower) or
            ("THE_FORGOTTEN", "MIASMA", DexterityPower) when rank == 3 => Math.Sign(amount),
            _ => 0
        };
        if (source is MegaCrit.Sts2.Core.Models.Monsters.ShrinkerBeetle && power is ShrinkPower)
            power.DynamicVars["DamageDecrease"].BaseValue = 30 + 10 * rank;
    }

    private sealed record Rule(int HpPercent, int Erosion, int Erosion2, string? Move = null, string? Alternate = null);
    private sealed record State(Rule Rule, int Rank)
    {
        internal int CompletedMoves { get; set; }
        internal int Turns { get; set; }
        internal int ConsecutiveReplacements { get; set; }
        internal string? PreviousBuff { get; set; }
        internal HashSet<string> UsedMoves { get; } = [];
        internal MovePlan? Plan { get; set; }
    }

    private sealed record MoveExecution(MonsterModel Monster, MoveState Move, MoveKind Kinds = MoveKind.All)
    {
        internal int[]? StatusRanks { get; set; }
        internal Dictionary<Player, int> Recipients { get; } = [];
    }
}

public sealed class ShadowMonsterCreatePatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_monster_create";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(CombatState), nameof(CombatState.CreateCreature), [typeof(MonsterModel), typeof(CombatSide), typeof(string)])];
    public static void Postfix(CombatState __instance, Creature __result, MonsterModel monster, CombatSide side) =>
        ShadowMonsterRules.OnCreated(__instance, __result, monster, side);
}

public sealed class ShadowMonsterInitialPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_monster_initial";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(Creature), nameof(Creature.AfterAddedToRoom), Type.EmptyTypes)];
    public static void Postfix(Creature __instance, ref Task __result)
    {
        if (__instance.IsMonster && ShadowMonsterRules.Rank(__instance.Monster) > 0)
            __result = ShadowMonsterRules.ApplyInitialEffects(__result, __instance);
    }
}

public sealed class ShadowMonsterRollMovePatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_monster_roll_move";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(MonsterModel), nameof(MonsterModel.RollMove), [typeof(IEnumerable<Creature>)])];
    public static void Postfix(MonsterModel __instance) => ShadowMonsterRules.PrepareMove(__instance, __instance.NextMove);
}

public sealed class ShadowMonsterImmediateMovePatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_monster_immediate_move";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(MonsterModel), nameof(MonsterModel.SetMoveImmediate), [typeof(MoveState), typeof(bool)])];
    public static void Prefix(MonsterModel __instance, MoveState state, bool forceTransition)
    {
        if (forceTransition || __instance.NextMove.CanTransitionAway) ShadowMonsterRules.PrepareMove(__instance, state);
    }
}

public sealed class ShadowMonsterPerformMovePatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_monster_perform_move";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(MonsterModel), nameof(MonsterModel.PerformMove), Type.EmptyTypes)];

    public static void Prefix(MonsterModel __instance,
        out (object? Previous, MoveState Move, ICombatState? Combat, object? Plan) __state)
    {
        ShadowMonsterRules.BeginAction(__instance, out var previous);
        __state = (previous, __instance.NextMove, __instance.Creature.CombatState, ShadowMonsterRules.CapturePlan(__instance));
    }

    public static void Postfix(MonsterModel __instance,
        (object? Previous, MoveState Move, ICombatState? Combat, object? Plan) __state, ref Task __result)
    {
        ShadowMonsterRules.EndAction(__state.Previous);
        if (ShadowMonsterRules.Rank(__instance) > 0)
            __result = ShadowMonsterRules.FinishMove(__result, __instance, __state.Move, __state.Combat, __state.Plan);
    }
}

public sealed class ShadowMonsterSpawnPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_monster_spawn";
    public static ModPatchTarget[] GetTargets()
    {
        const string method = nameof(SurprisePower.AfterDeath);
        var arguments = typeof(SurprisePower).GetMethod(method)!.GetParameters()
            .Select(parameter => parameter.ParameterType).ToArray();
        return [new(typeof(SurprisePower), method, arguments), new(typeof(StockPower), method, arguments)];
    }
    public static void Prefix(PowerModel __instance, Creature target, out object? __state) =>
        ShadowMonsterRules.BeginAction(target == __instance.Owner ? __instance.Owner.Monster : null, out __state);
    public static void Postfix(object? __state) => ShadowMonsterRules.EndAction(__state);
}

public sealed class ShadowMonsterCardsPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_monster_cards";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(CardPileCmd), nameof(CardPileCmd.AddGeneratedCardsToCombat),
            [typeof(IEnumerable<CardModel>), typeof(PileType), typeof(Player), typeof(CardPilePosition)])];
    public static void Prefix(ref IEnumerable<CardModel> cards) => ShadowMonsterRules.TagGeneratedCards(ref cards);
}

public sealed class ShadowMonsterBlockPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_monster_block";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(CreatureCmd), nameof(CreatureCmd.GainBlock), [typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(CardPlay), typeof(bool)])];
    public static bool Prefix(Creature creature, ref decimal amount, ValueProp props, CardPlay? cardPlay, ref Task<decimal> __result)
    {
        if (!ShadowMonsterRules.AllowBlock(creature, props, cardPlay))
        {
            __result = Task.FromResult(0m);
            return false;
        }
        if (cardPlay == null) amount += ShadowMonsterRules.BonusBlock(creature);
        return true;
    }
}

public sealed class ShadowMonsterPowerPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_monster_power";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(PowerCmd), nameof(PowerCmd.Apply),
            [typeof(PlayerChoiceContext), typeof(PowerModel), typeof(Creature), typeof(decimal), typeof(Creature), typeof(CardModel), typeof(bool)])];
    public static bool Prefix(PowerModel power, Creature target, Creature? applier, CardModel? cardSource, ref decimal amount,
        ref Task __result)
    {
        if (!ShadowMonsterRules.AllowPower(power, target, applier, cardSource, amount))
        {
            __result = Task.CompletedTask;
            return false;
        }
        if (cardSource == null && PowerCmd.FindExistingInstanceForStacking(power, target, applier) == null)
            ShadowMonsterRules.ModifyPower(power, applier, ref amount);
        return true;
    }
}

public sealed class ShadowMonsterPowerReductionPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_monster_power_reduction";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(PowerCmd), nameof(PowerCmd.ModifyAmount),
            [typeof(PlayerChoiceContext), typeof(PowerModel), typeof(decimal), typeof(Creature), typeof(CardModel), typeof(bool)])];
    public static bool Prefix(PowerModel power, Creature? applier, CardModel? cardSource, ref decimal offset, ref Task<int> __result)
    {
        if (!ShadowMonsterRules.AllowPower(power, power.Owner, applier, cardSource, offset))
        {
            __result = Task.FromResult(power.Amount);
            return false;
        }
        if (cardSource == null) ShadowMonsterRules.ModifyPower(power, applier, ref offset);
        return true;
    }
}

public sealed class ShadowMonsterMoveActionPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_monster_move_action";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(MoveState), nameof(MoveState.PerformMove), [typeof(IEnumerable<Creature>)])];

    public static bool Prefix(MoveState __instance, IEnumerable<Creature> targets, ref bool ____performedAtLeastOnce,
        ref Task __result)
    {
        if (!ShadowMonsterRules.TryRunMove(__instance, targets, ref ____performedAtLeastOnce, out var action)) return true;
        __result = action;
        return false;
    }
}

public sealed class ShadowMonsterAttackPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_monster_attack";
    public static ModPatchTarget[] GetTargets() => [new(typeof(AttackCommand), nameof(AttackCommand.Execute), [typeof(PlayerChoiceContext)])];

    public static bool Prefix(AttackCommand __instance, ref decimal ____damagePerHit, ref int ____hitCount,
        ref Task<AttackCommand> __result, out object? __state)
    {
        __state = null;
        if (ShadowMonsterRules.PrepareAttack(__instance.Attacker, ref ____damagePerHit, ref ____hitCount))
        {
            __state = ShadowMonsterRules.EnterAttack(__instance.Attacker);
            return true;
        }
        __result = Task.FromResult(__instance);
        return false;
    }

    public static void Postfix(object? __state)
    {
        if (__state != null) ShadowMonsterRules.EndAction(__state);
    }
}

public sealed class ShadowMonsterStatusPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_monster_status";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(CardPileCmd), nameof(CardPileCmd.AddGeneratedCardToCombat),
            [typeof(CardModel), typeof(PileType), typeof(Player), typeof(CardPilePosition)])];

    public static bool Prefix(CardModel card, Player? creator, ref Task<CardPileAddResult> __result)
    {
        if (creator != null || ShadowMonsterRules.PrepareStatus(card)) return true;
        card.Owner.Creature.CombatState?.RemoveCard(card);
        __result = Task.FromResult(default(CardPileAddResult));
        return false;
    }
}

public sealed class ShadowMonsterSummonPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_monster_summon";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(Fabricator), "SpawnAggroBot", Type.EmptyTypes),
         new(typeof(Fabricator), "SpawnDefensiveBot", Type.EmptyTypes)];

    public static bool Prefix(ref Task __result)
    {
        if (ShadowMonsterRules.AllowSummon()) return true;
        __result = Task.CompletedTask;
        return false;
    }
}

public sealed class ShadowHatchlingHpPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_hatchling_hp";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(CreatureCmd), nameof(CreatureCmd.SetMaxAndCurrentHp), [typeof(Creature), typeof(decimal)])];

    public static void Prefix(Creature creature, ref decimal amount) => amount = ShadowMonsterRules.HatchlingHp(creature, amount);
}

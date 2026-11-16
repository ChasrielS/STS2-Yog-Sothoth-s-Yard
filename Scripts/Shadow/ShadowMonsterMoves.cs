using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.RunRngs;

namespace YogSothothsYard.Scripts.Shadow;

internal static partial class ShadowMonsterRules
{
    private static readonly ConditionalWeakTable<MoveState, NativeMove> NativeMoves = new();
    private static readonly AccessTools.FieldRef<MoveState, Func<IReadOnlyList<Creature>, Task>> PerformNative =
        AccessTools.FieldRefAccess<MoveState, Func<IReadOnlyList<Creature>, Task>>("_onPerform");
    private static readonly Func<TwoTailedRat, bool> CanCallForBackup = AccessTools.MethodDelegate<Func<TwoTailedRat, bool>>(
        AccessTools.DeclaredMethod(typeof(TwoTailedRat), "CanSummon"));
    private static readonly AccessTools.FieldRef<TwoTailedRat, int> TurnsUntilBackup =
        AccessTools.FieldRefAccess<TwoTailedRat, int>("_turnsUntilSummonable");

    private static readonly Dictionary<(string Monster, string Move), (int Damage, int Hits)> AttackBonuses = new()
    {
        [("NIBBIT", "BUTT_MOVE")] = (1, 0),
        [("NIBBIT", "SLICE_MOVE")] = (1, 0),
        [("LEAF_SLIME_S", "TACKLE_MOVE")] = (1, 0),
        [("LEAF_SLIME_M", "CLUMP_SHOT")] = (1, 0),
        [("TWIG_SLIME_S", "TACKLE_MOVE")] = (1, 0),
        [("TWIG_SLIME_M", "POKEY_POUNCE_MOVE")] = (1, 0),
        [("FLYCONID", "FRAIL_SPORES_MOVE")] = (1, 0),
        [("FLYCONID", "SMASH_MOVE")] = (1, 0),
        [("BRUTE_RUBY_RAIDER", "BEAT_MOVE")] = (1, 0),
        [("ASSASSIN_RUBY_RAIDER", "KILLSHOT_MOVE")] = (1, 0),
        [("AXE_RUBY_RAIDER", "SWING_1")] = (1, 0),
        [("AXE_RUBY_RAIDER", "SWING_2")] = (1, 0),
        [("AXE_RUBY_RAIDER", "BIG_SWING")] = (1, 0),
        [("CROSSBOW_RUBY_RAIDER", "FIRE_MOVE")] = (2, 0),
        [("TRACKER_RUBY_RAIDER", "HOUNDS_MOVE")] = (0, 1),
        [("FOGMOG", "SWIPE_MOVE")] = (1, 0),
        [("FOGMOG", "SWIPE_RANDOM_MOVE")] = (1, 0),
        [("FOGMOG", "HEADBUTT_MOVE")] = (2, 0),
        [("MAWLER", "RIP_AND_TEAR_MOVE")] = (2, 0),
        [("MAWLER", "CLAW_MOVE")] = (1, 0),
        [("FUZZY_WURM_CRAWLER", "FIRST_ACID_GOOP")] = (2, 0),
        [("FUZZY_WURM_CRAWLER", "ACID_GOOP")] = (2, 0),
        [("INKLET", "JAB_MOVE")] = (1, 0),
        [("INKLET", "WHIRLWIND_MOVE")] = (1, 0),
        [("INKLET", "PIERCING_GAZE_MOVE")] = (1, 0),
        [("SNAPPING_JAXFRUIT", "ENERGY_ORB_MOVE")] = (1, 0),
        [("VINE_SHAMBLER", "GRASPING_VINES_MOVE")] = (1, 0),
        [("VINE_SHAMBLER", "SWIPE_MOVE")] = (1, 0),
        [("VINE_SHAMBLER", "CHOMP_MOVE")] = (2, 0),
        [("CUBEX_CONSTRUCT", "REPEATER_BLAST_MOVE")] = (1, 0),
        [("CUBEX_CONSTRUCT", "REPEATER_BLAST_MOVE_2")] = (1, 0),
        [("CUBEX_CONSTRUCT", "EXPEL_MOVE")] = (1, 0),
        [("TOADPOLE", "SPIKE_SPIT_MOVE")] = (1, 0),
        [("TOADPOLE", "WHIRL_MOVE")] = (1, 0),
        [("CALCIFIED_CULTIST", "DARK_STRIKE_MOVE")] = (2, 0),
        [("GREMLIN_MERC", "GIMME_MOVE")] = (1, 0),
        [("GREMLIN_MERC", "DOUBLE_SMASH_MOVE")] = (1, 0),
        [("GREMLIN_MERC", "HEHE_MOVE")] = (1, 0),
        [("SNEAKY_GREMLIN", "TACKLE_MOVE")] = (1, 0),
        [("SEAPUNK", "SEA_KICK_MOVE")] = (2, 0),
        [("LIVING_FOG", "ADVANCED_GAS_MOVE")] = (1, 0),
        [("LIVING_FOG", "BLOAT_MOVE")] = (1, 0),
        [("LIVING_FOG", "SUPER_GAS_BLAST_MOVE")] = (1, 0),
        [("TWO_TAILED_RAT", "SCRATCH_MOVE")] = (1, 0),
        [("TWO_TAILED_RAT", "DISEASE_BITE_MOVE")] = (1, 0),
        [("SEWER_CLAM", "JET_MOVE")] = (1, 0),
        [("HAUNTED_SHIP", "SWIPE_MOVE")] = (1, 0),
        [("HAUNTED_SHIP", "STOMP_MOVE")] = (1, 0),
        [("SLUDGE_SPINNER", "OIL_SPRAY_MOVE")] = (1, 0),
        [("SLUDGE_SPINNER", "SLAM_MOVE")] = (1, 0),
        [("SLUDGE_SPINNER", "RAGE_MOVE")] = (1, 0),
        [("PUNCH_CONSTRUCT", "STRONG_PUNCH_MOVE")] = (2, 0),
        [("PUNCH_CONSTRUCT", "FAST_PUNCH_MOVE")] = (1, 0),
        [("TUNNELER", "BITE_MOVE")] = (2, 0),
        [("TUNNELER", "BELOW_MOVE")] = (3, 0),
        [("TUNNELER", "FROM_BELOW_MOVE")] = (4, 0),
        [("SPINY_TOAD", "SPIKE_EXPLOSION_MOVE")] = (2, 0),
        [("SPINY_TOAD", "TONGUE_LASH_MOVE")] = (2, 0),
        [("CHOMPER", "CLAMP_MOVE")] = (1, 0),
        [("HUNTER_KILLER", "BITE_MOVE")] = (2, 0),
        [("HUNTER_KILLER", "PUNCTURE_MOVE")] = (1, 0),
        [("THE_OBSCURA", "PIERCING_GAZE_MOVE")] = (1, 0),
        [("THE_OBSCURA", "HARDENING_STRIKE_MOVE")] = (1, 0),
        [("PARAFRIGHT", "SLAM_MOVE")] = (1, 0),
        [("BOWLBUG_EGG", "BITE_MOVE")] = (1, 0),
        [("BOWLBUG_ROCK", "HEADBUTT_MOVE")] = (1, 0),
        [("BOWLBUG_SILK", "THRASH_MOVE")] = (1, 0),
        [("SLUMBERING_BEETLE", "ROLL_OUT_MOVE")] = (2, 0),
        [("MYTE", "BITE_MOVE")] = (2, 0),
        [("MYTE", "SUCK_MOVE")] = (2, 0),
        [("OVICOPTER", "SMASH_MOVE")] = (1, 0),
        [("OVICOPTER", "TENDERIZER_MOVE")] = (1, 0),
        [("TURRET_OPERATOR", "UNLOAD_MOVE")] = (1, 0),
        [("TURRET_OPERATOR", "UNLOAD_MOVE_2")] = (1, 0),
        [("AXEBOT", "ONE_TWO_MOVE")] = (1, 0),
        [("AXEBOT", "HAMMER_UPPERCUT_MOVE")] = (4, 0),
        [("OWL_MAGISTRATE", "MAGISTRATE_SCRUTINY")] = (1, 0),
        [("OWL_MAGISTRATE", "VERDICT")] = (3, 0),
        [("FROG_KNIGHT", "STRIKE_DOWN_EVIL")] = (2, 0),
        [("FROG_KNIGHT", "TONGUE_LASH")] = (1, 0),
        [("FROG_KNIGHT", "BEETLE_CHARGE")] = (5, 0),
        [("SLIMED_BERSERKER", "SMOTHER_MOVE")] = (3, 0),
        [("SLIMED_BERSERKER", "FURIOUS_PUMMELING_MOVE")] = (1, 0),
        [("THE_LOST", "EYE_LASERS")] = (1, 0),
        [("SCROLL_OF_BITING", "CHOMP")] = (2, 0),
        [("SCROLL_OF_BITING", "CHEW")] = (1, 0),
        [("FABRICATOR", "FABRICATING_STRIKE_MOVE")] = (3, 0),
        [("FABRICATOR", "DISINTEGRATE_MOVE")] = (2, 0),
        [("STABBOT", "STAB_MOVE")] = (1, 0),
        [("ZAPBOT", "ZAP")] = (1, 0),
        [("FAKE_MERCHANT_MONSTER", "SWIPE_MOVE")] = (2, 0),
        [("FAKE_MERCHANT_MONSTER", "THROW_RELIC_MOVE")] = (1, 0),
        [("GAS_BOMB", "EXPLODE_MOVE")] = (1, 0),
        [("THE_FORGOTTEN", "DREAD")] = (2, 0),
        [("MYSTERIOUS_KNIGHT", "FLAIL_MOVE")] = (1, 0),
        [("MYSTERIOUS_KNIGHT", "RAM_MOVE")] = (2, 0),
        [("HATCHLING", "NIBBLE_MOVE")] = (1, 0),
    };

    private static NativeMove GetNativeMove(MoveState move) =>
        NativeMoves.GetValue(move, static state => new NativeMove(state.Intents.ToArray(), PerformNative(state)));

    private static string Species(MonsterModel monster) =>
        monster is ToughEgg { IsHatched: true } ? "HATCHLING" : monster.Id.Entry;

    private static MovePlan PlanMove(MonsterModel monster, State state, MoveState move)
    {
        var id = Species(monster);
        var native = GetNativeMove(move);
        var fullMove = Part(move, native.Intents, MoveKind.All);
        if (state.Rank == 1)
        {
            var rule = state.Rule;
            var applies = rule.Move == null
                ? (state.CompletedMoves + 1) % 3 == 0 && native.Intents.All(intent => intent.IntentType != IntentType.Stun)
                : move.Id == rule.Move || move.Id == rule.Alternate;
            return Plan(monster, state, move, [fullMove], applies ? rule.Erosion : 0);
        }

        if (id == "GAS_BOMB")
            return Plan(monster, state, move, [fullMove], state.Rank == 2 ? state.Rule.Erosion2 : 20);

        var amount = id == "HATCHLING" ? Rules[id].Erosion2 : state.Rule.Erosion2;
        if (amount == 0 || native.Intents.Any(intent => intent.IntentType is IntentType.Stun or IntentType.Sleep))
            return Plan(monster, state, move, [fullMove], 0);

        var turn = state.Turns + 1;
        if (state.Rank == 2)
        {
            if (!CanReplace(id, move.Id, turn) || native.Intents.Length == 0 ||
                MonsterRng(monster).NextInt(100) >= Math.Max(0, 33 - 16 * state.ConsecutiveReplacements))
                return Plan(monster, state, move, [fullMove], 0);
            var replaceable = native.Intents.Select((intent, index) => (intent, index))
                .Where(item => id != "FABRICATOR" || item.intent.IntentType != IntentType.Summon).ToArray();
            if (replaceable.Length == 0) return Plan(monster, state, move, [fullMove], 0);
            var removed = replaceable[MonsterRng(monster).NextInt(replaceable.Length)].index;
            var kept = native.Intents.Where((_, index) => index != removed).ToArray();
            return Plan(monster, state, move, [Part(move, kept, KindOf(kept))], amount, replaced: true);
        }

        if (id is "DAMP_CULTIST" or "CALCIFIED_CULTIST" or "FAT_GREMLIN" or "SNEAKY_GREMLIN")
            return MonsterRng(monster).NextInt(2) == 0
                ? Plan(monster, state, move, [], amount)
                : Plan(monster, state, move, [fullMove], 0);

        if (ProtectedRankThreeMove(monster, id, move, turn) is { } protectedMove)
            return Plan(monster, state, move, [Part(protectedMove, GetNativeMove(protectedMove).Intents, MoveKind.All)], 0);
        if (monster.MoveStateMachine is not { } machine)
            return Plan(monster, state, move, [fullMove], 0);

        var (copies, chance, comboSize) = RankThreePool(id, monster);
        var pool = machine.States.Values.OfType<MoveState>()
            .Where(candidate => CanPool(monster, state, id, candidate.Id))
            .SelectMany(candidate => SplitMove(id, candidate)).Cast<MovePart?>().ToList();
        for (var i = 0; i < copies; i++) pool.Add(null);

        var rng = MonsterRng(monster);
        var count = chance > 0 && rng.NextInt(100) < chance ? comboSize : 1;
        MovePart?[] selected = count == 1 ? [pool[rng.NextInt(pool.Count)]] : ChooseDifferentKinds(pool, count, rng);
        var actions = selected.OfType<MovePart>().ToList();
        if (id == "THIEVING_HOPPER" && turn == 1 && machine.States["THIEVERY_MOVE"] is MoveState theft)
            actions.Add(Part(theft, GetNativeMove(theft).Intents.Where(intent => intent.IntentType == IntentType.CardDebuff).ToArray(),
                MoveKind.Debuff));

        var release = state.PreviousBuff switch
        {
            "SPIKEN_MOVE" => "SPIKE_SPIT_MOVE",
            "PROTRUDING_SPIKES_MOVE" => "SPIKE_EXPLOSION_MOVE",
            "JUDICIAL_FLIGHT" => "VERDICT",
            _ => null
        };
        if (release != null && machine.States[release] is MoveState releaseMove)
        {
            var index = actions.FindIndex(action => action.Move == releaseMove);
            if (index >= 0) actions[index] = actions[index] with { Kind = actions[index].Kind | MoveKind.Release };
            else actions.Add(Part(releaseMove, [], MoveKind.Release));
        }
        return Plan(monster, state, move, actions, selected.Contains(null) ? amount : 0);
    }

    private static Rng MonsterRng(MonsterModel monster) =>
        ModRunRngRegistry.Get((MegaCrit.Sts2.Core.Runs.RunState)monster.Creature.CombatState!.RunState,
            Entry.ModId, "shadow_monster_intents");

    private static MovePlan Plan(MonsterModel monster, State state, MoveState move, IEnumerable<MovePart> actions,
        int erosion, bool replaced = false)
    {
        var parts = actions.GroupBy(action => action.Move).Select(group =>
            Part(group.Key, group.SelectMany(action => action.Intents).ToArray(),
                group.Aggregate(MoveKind.None, (kinds, action) => kinds | action.Kind)))
            .OrderByDescending(part => part.Intents.Any(intent => KindOf(intent) == MoveKind.Attack)).ToArray();
        var intents = new List<AbstractIntent>();
        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            var id = Species(monster);
            var (damage, hits) = state.Rank >= 2 ? AttackBonuses.GetValueOrDefault((id, part.Move.Id)) : (0, 0);
            var status = (part.Kind & MoveKind.Status) != 0 ? StatusRanks(id, part.Move.Id, state.Rank, monster) : null;
            parts[i] = part with { StatusRanks = status };
            foreach (var intent in part.Intents)
                intents.Add(intent switch
                {
                    DeathBlowIntent attack when damage != 0 => new DeathBlowIntent(() => attack.DamageCalc!() + damage),
                    MultiAttackIntent attack when damage != 0 || hits != 0 => new ErodedMultiAttackIntent(attack, damage, hits),
                    AttackIntent attack when damage != 0 => new SingleAttackIntent(() => attack.DamageCalc!() + damage),
                    StatusIntent when status != null => new StatusIntent(status.Length),
                    _ => intent
                });
        }
        if (erosion > 0) intents.Add(new ShadowErosionIntent(erosion));
        return new MovePlan(move, state.Turns, parts, intents.ToArray(), erosion, replaced);
    }

    private static bool CanReplace(string monster, string move, int turn) => (monster, move) switch
    {
        ("SHRINKER_BEETLE" or "TRACKER_RUBY_RAIDER" or "FOGMOG" or "CUBEX_CONSTRUCT" or "DAMP_CULTIST" or
            "CALCIFIED_CULTIST" or "LIVING_FOG" or "HAUNTED_SHIP" or "HUNTER_KILLER" or "THE_OBSCURA" or
            "AXEBOT" or "DEVOTED_SCULPTOR", _) => turn > 1,
        ("MAWLER", "ROAR_MOVE") or ("TOADPOLE", "SPIKE_SPIT_MOVE") or ("SPINY_TOAD", "SPIKE_EXPLOSION_MOVE") or
        ("TWO_TAILED_RAT", "CALL_FOR_BACKUP_MOVE") or ("TUNNELER", "BURROW_MOVE" or "DIZZY_MOVE") or
        ("OVICOPTER", "LAY_EGGS_MOVE") or ("OWL_MAGISTRATE", "JUDICIAL_FLIGHT") or
        ("FROG_KNIGHT", "BEETLE_CHARGE") or ("FABRICATOR", "FABRICATE_MOVE") => false,
        ("SLUMBERING_BEETLE", _) => turn > 3,
        ("THIEVING_HOPPER", _) => turn is 3 or 4,
        ("BOWLBUG_NECTAR", _) => turn != 2,
        _ => true
    };

    private static MoveState? ProtectedRankThreeMove(MonsterModel monster, string id, MoveState move, int turn)
    {
        var keep = id switch
        {
            "SHRINKER_BEETLE" or "TRACKER_RUBY_RAIDER" or "FOGMOG" or "CUBEX_CONSTRUCT" or "LIVING_FOG" or
                "HAUNTED_SHIP" or "HUNTER_KILLER" or "THE_OBSCURA" or "AXEBOT" or "DEVOTED_SCULPTOR" => turn == 1,
            "SLUMBERING_BEETLE" => turn <= 3,
            "THIEVING_HOPPER" => turn is 2 or >= 5,
            _ => false
        };
        if (keep) return move;
        var machine = monster.MoveStateMachine;
        if (machine == null) return null;
        if (monster is TwoTailedRat rat && CanCallForBackup(rat) &&
            monster.Creature.CombatState!.Enemies.All(enemy => enemy == monster.Creature ||
                enemy.Monster is not TwoTailedRat other || !Monsters.TryGetValue(other, out var state) ||
                state.Plan?.Actions.All(action => action.Move.Id != "CALL_FOR_BACKUP_MOVE") != false))
            return machine.States["CALL_FOR_BACKUP_MOVE"] as MoveState;
        if (monster is Ovicopter && monster.Creature.CombatState!.GetTeammatesOf(monster.Creature).Count(enemy => enemy.IsAlive) <= 3)
            return machine.States["LAY_EGGS_MOVE"] as MoveState;
        return null;
    }

    private static (int Copies, int ComboChance, int ComboSize) RankThreePool(string id, MonsterModel monster) => id switch
    {
        "NIBBIT" or "FLYCONID" or "SLITHERING_STRANGLER" or "VINE_SHAMBLER" or "CUBEX_CONSTRUCT" or
            "SEAPUNK" or "FOSSIL_STALKER" or "PUNCH_CONSTRUCT" or "THE_OBSCURA" or "MYTE" or "OVICOPTER" => (2, 33, 2),
        "AXE_RUBY_RAIDER" or "FOGMOG" or "LIVING_FOG" => (1, 50, 2),
        "GREMLIN_MERC" or "SLUDGE_SPINNER" or "LOUSE_PROGENITOR" or "GLOBE_HEAD" => (2, 66, 2),
        "OWL_MAGISTRATE" or "FROG_KNIGHT" or "SLIMED_BERSERKER" => (2, 25, 2),
        "FAKE_MERCHANT_MONSTER" => (3, 25, 2),
        "INKLET" or "TOADPOLE" or "SPINY_TOAD" or "CORPSE_SLUG" or "TWO_TAILED_RAT" or
            "THIEVING_HOPPER" or "EXOSKELETON" or "TURRET_OPERATOR" or "THE_FORGOTTEN" or
            "SCROLL_OF_BITING" or "MYSTERIOUS_KNIGHT" => (2, 0, 1),
        "SNAPPING_JAXFRUIT" or "BOWLBUG_EGG" or "SLUMBERING_BEETLE" or "STABBOT" => (1, 100, 2),
        "AXEBOT" => (1, 50, 2),
        "FABRICATOR" when CanFabricate(monster) => (1, 50, 2),
        _ => (1, 0, 1)
    };

    private static bool CanFabricate(MonsterModel monster) =>
        monster.Creature.CombatState!.GetTeammatesOf(monster.Creature).Count(enemy => enemy.IsAlive && enemy != monster.Creature) <= 2;

    private static bool CanPool(MonsterModel monster, State state, string id, string move) => (id, move) switch
    {
        (_, "STUNNED" or "DIZZY_MOVE" or "SPAWNED_MOVE" or "SNORE_MOVE") => false,
        ("SHRINKER_BEETLE", "SHRINKER_MOVE") or ("TRACKER_RUBY_RAIDER", "TRACK_MOVE") or
        ("FOGMOG" or "THE_OBSCURA", "ILLUSION_MOVE") or ("FOGMOG", "SWIPE_RANDOM_MOVE") or
        ("FUZZY_WURM_CRAWLER", "FIRST_ACID_GOOP") or ("AXE_RUBY_RAIDER", "SWING_2") or
        ("CUBEX_CONSTRUCT", "REPEATER_BLAST_MOVE_2") or ("LIVING_FOG", "BLOAT_MOVE") or
        ("HAUNTED_SHIP", "HAUNT_MOVE") or ("TWO_TAILED_RAT", "CALL_FOR_BACKUP_MOVE") or
        ("HUNTER_KILLER", "TENDERIZING_GOOP_MOVE") or ("BOWLBUG_NECTAR", "THRASH2_MOVE") or
        ("THIEVING_HOPPER", "FLUTTER_MOVE" or "ESCAPE_MOVE") or ("OVICOPTER", "LAY_EGGS_MOVE") or
        ("HATCHLING", "HATCH_MOVE") or ("DEVOTED_SCULPTOR", "FORBIDDEN_INCANTATION_MOVE") or
        ("AXEBOT", "BOOT_UP_MOVE") => false,
        ("MAWLER", "ROAR_MOVE") or ("BOWLBUG_NECTAR", "BUFF_MOVE") or ("FROG_KNIGHT", "BEETLE_CHARGE") =>
            !state.UsedMoves.Contains(move),
        ("TOADPOLE", "SPIKEN_MOVE") or ("SPINY_TOAD", "PROTRUDING_SPIKES_MOVE") or ("OWL_MAGISTRATE", "JUDICIAL_FLIGHT") =>
            state.PreviousBuff != move,
        ("TUNNELER", _) => (move is "BELOW_MOVE" or "FROM_BELOW_MOVE") == monster.Creature.HasPower<BurrowedPower>(),
        ("LIVING_SHIELD", _) => (move == "SHIELD_SLAM_MOVE") ==
            monster.Creature.CombatState!.GetTeammatesOf(monster.Creature).Any(enemy => enemy.IsAlive && enemy != monster.Creature),
        ("FABRICATOR", _) => (move != "DISINTEGRATE_MOVE") == CanFabricate(monster),
        _ => true
    };

    private static IEnumerable<MovePart> SplitMove(string id, MoveState move)
    {
        var intents = GetNativeMove(move).Intents;
        if (id is "LIVING_SHIELD" or "TUNNELER")
        {
            yield return Part(move, intents, KindOf(intents));
            yield break;
        }
        if ((id, move.Id) is ("THE_LOST", "DEBILITATING_SMOG") or ("THE_FORGOTTEN", "MIASMA"))
        {
            yield return Part(move, intents.Where(intent => intent.IntentType == IntentType.Debuff).ToArray(),
                MoveKind.Debuff | MoveKind.Buff);
            if (id == "THE_FORGOTTEN")
                yield return Part(move, intents.Where(intent => intent.IntentType == IntentType.Defend).ToArray(), MoveKind.Defend);
            yield break;
        }
        foreach (var intent in intents)
            if ((id, intent.IntentType) != ("THIEVING_HOPPER", IntentType.CardDebuff))
                yield return Part(move, [intent], KindOf(intent));
    }

    private static MovePart?[] ChooseDifferentKinds(List<MovePart?> pool, int count, Rng rng)
    {
        var result = new List<MovePart?>(count);
        for (var i = 0; i < count && pool.Count > 0; i++)
        {
            var groups = pool.GroupBy(PoolKind).ToArray();
            var group = groups[rng.NextInt(groups.Length)].ToArray();
            var picked = group[rng.NextInt(group.Length)];
            result.Add(picked);
            var kinds = PoolKind(picked);
            pool.RemoveAll(part => (PoolKind(part) & kinds) != 0);
        }
        return result.ToArray();
    }

    private static MoveKind PoolKind(MovePart? part) => part?.Kind switch
    {
        MoveKind.Status => MoveKind.Debuff,
        MoveKind.Summon => MoveKind.Buff,
        { } kind => kind,
        _ => MoveKind.Erosion
    };

    private static MovePart Part(MoveState move, AbstractIntent[] intents, MoveKind kind) => new(move, intents, kind);

    private static MoveKind KindOf(IEnumerable<AbstractIntent> intents) =>
        intents.Aggregate(MoveKind.None, (kinds, intent) => kinds | KindOf(intent));

    private static MoveKind KindOf(AbstractIntent intent) => intent.IntentType switch
    {
        IntentType.Attack or IntentType.DeathBlow => MoveKind.Attack,
        IntentType.Buff or IntentType.Heal => MoveKind.Buff,
        IntentType.Defend => MoveKind.Defend,
        IntentType.Debuff or IntentType.DebuffStrong or IntentType.CardDebuff => MoveKind.Debuff,
        IntentType.StatusCard => MoveKind.Status,
        IntentType.Summon => MoveKind.Summon,
        IntentType.Escape => MoveKind.Escape,
        _ => MoveKind.Other
    };

    internal static object? CapturePlan(MonsterModel monster) =>
        Monsters.TryGetValue(monster, out var state) ? state.Plan : null;

    internal static bool TryRunMove(MoveState move, IEnumerable<Creature> targets, ref bool performed, out Task result)
    {
        if (Acting.Value is { } action && Monsters.TryGetValue(action.Monster, out var state) &&
            state.Plan is { } plan && plan.NativeMove == move)
        {
            performed = true;
            result = RunMove(action.Monster, plan, targets as IReadOnlyList<Creature> ?? targets.ToArray());
            return true;
        }
        result = Task.CompletedTask;
        return false;
    }

    private static async Task RunMove(MonsterModel monster, MovePlan plan, IReadOnlyList<Creature> targets)
    {
        if (plan.Actions.Length == 0 && plan.Erosion > 0 && monster is TwoTailedRat rat)
            TurnsUntilBackup(rat)--;
        var previous = Acting.Value;
        try
        {
            foreach (var part in plan.Actions)
            {
                if (!monster.Creature.IsAlive) break;
                if (part.Kind == MoveKind.None && monster is FatGremlin) continue;
                Acting.Value = new MoveExecution(monster, part.Move, part.Kind) { StatusRanks = part.StatusRanks };
                var native = GetNativeMove(part.Move);
                if (monster is ThievingHopper && part.Move.Id == "THIEVERY_MOVE" && (part.Kind & MoveKind.Debuff) == 0)
                {
                    if ((part.Kind & MoveKind.Attack) != 0)
                        await DamageCmd.Attack(native.Intents.OfType<AttackIntent>().First().DamageCalc!()).FromMonster(monster)
                            .WithAttackerAnim("Steal", 0.25f).WithHitFx("vfx/vfx_attack_blunt").Execute(null);
                    continue;
                }
                await native.Perform(targets);
            }
        }
        finally { Acting.Value = previous; }
    }

    internal static bool PrepareAttack(Creature? attacker, ref decimal damage, ref int hits)
    {
        if (Acting.Value is not { } action || action.Monster.Creature != attacker || Rank(action.Monster) == 0) return true;
        if ((action.Kinds & MoveKind.Attack) == 0) return false;
        if (Rank(action.Monster) >= 2)
        {
            var bonus = AttackBonuses.GetValueOrDefault((Species(action.Monster), action.Move.Id));
            damage += bonus.Damage;
            hits += bonus.Hits;
        }
        return true;
    }

    internal static object? EnterAttack(Creature? attacker)
    {
        if (Acting.Value is not { } action || action.Monster.Creature != attacker) return null;
        Acting.Value = null;
        return action;
    }

    internal static bool AllowBlock(Creature creature, ValueProp props, CardPlay? cardPlay) =>
        Acting.Value is not { } action || Rank(action.Monster) == 0 || cardPlay != null ||
        !props.HasFlag(ValueProp.Move) || creature.Side != action.Monster.Creature.Side ||
        (action.Kinds & MoveKind.Defend) != 0;

    internal static bool AllowPower(PowerModel power, Creature target, Creature? applier, CardModel? card, decimal amount)
    {
        if (Acting.Value is not { } action || Rank(action.Monster) is not (> 0 and var rank) || card != null) return true;
        var owner = action.Monster.Creature;
        if (applier != owner && (applier != null || action.Move.Id != "LEECHING_HUG_MOVE")) return true;
        if (power is ThornsPower && amount < 0 && rank == 3 && action.Monster is Toadpole or SpinyToad)
            return (action.Kinds & MoveKind.Release) != 0;
        if (power is ThornsPower && amount < 0) return true;
        if (power is MinionPower && target != owner) return true;
        var kind = target.Side != owner.Side || power is SwipePower ? MoveKind.Debuff : MoveKind.Buff;
        return (action.Kinds & kind) != 0;
    }

    internal static bool AllowSummon() =>
        Acting.Value is not { } action || Rank(action.Monster) == 0 || (action.Kinds & MoveKind.Summon) != 0;

    internal static decimal HatchlingHp(Creature creature, decimal amount) =>
        creature.Monster is ToughEgg { IsHatched: true } egg && Rank(egg) > 0
            ? amount + Math.Ceiling(amount / 10m) : amount;

    [Flags]
    private enum MoveKind
    {
        None = 0, Attack = 1, Defend = 2, Buff = 4, Debuff = 8, Status = 16, Summon = 32, Escape = 64,
        Other = 128, Erosion = 256, Release = 512, All = Attack | Defend | Buff | Debuff | Status | Summon | Escape | Other
    }

    private sealed record NativeMove(AbstractIntent[] Intents, Func<IReadOnlyList<Creature>, Task> Perform);
    private sealed record MovePart(MoveState Move, AbstractIntent[] Intents, MoveKind Kind, int[]? StatusRanks = null);
    private sealed record MovePlan(MoveState NativeMove, int Turn, MovePart[] Actions, AbstractIntent[] Intents, int Erosion, bool Replaced);

    private sealed class ErodedMultiAttackIntent : MultiAttackIntent
    {
        internal ErodedMultiAttackIntent(MultiAttackIntent original, int damage, int hits)
            : base(0, () => original.Repeats + hits) => DamageCalc = () => original.DamageCalc!() + damage;
    }
}

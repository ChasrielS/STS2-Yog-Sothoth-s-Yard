using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Patching.Models;

namespace YogSothothsYard.Scripts.Shadow;

internal static partial class ShadowEvents
{
    private static readonly HashSet<string> EditedEvents =
    [
        "SLIPPERY_BRIDGE", "SELF_HELP_BOOK", "THIS_OR_THAT", "ROOM_FULL_OF_CHEESE", "BRAIN_LEECH", "TEA_MASTER",
        "THE_LEGENDS_WERE_TRUE", "SUNKEN_STATUE", "JUNGLE_MAZE_ADVENTURE", "WHISPERING_HOLLOW",
        "LUMINOUS_CHOIR", "MORPHIC_GROVE", "BYRDONIS_NEST", "UNREST_SITE", "WOOD_CARVINGS", "WELLSPRING",
        "AROMA_OF_CHAOS", "TABLET_OF_TRUTH", "DENSE_VEGETATION", "SAPPHIRE_SEED", "TRASH_HEAP", "ENDLESS_CONVEYOR",
        "WATERLOGGED_SCRIPTORIUM", "ABYSSAL_BATHS", "DROWNING_BEACON", "SUNKEN_TREASURY",
        "SPIRALING_WHIRLPOOL", "PUNCH_OFF", "SYMBIOTE", "POTION_COURIER", "RELIC_TRADER", "RANWID_THE_ELDER",
        "STONE_OF_ALL_TIME", "DOLL_ROOM", "WELCOME_TO_WONGOS", "FIELD_OF_MAN_SIZED_HOLES", "BUGSLAYER",
        "COLOSSAL_FLOWER", "THE_LANTERN_KEY", "SPIRIT_GRAFTER", "AMALGAMATOR", "INFESTED_AUTOMATON",
        "LOST_WISP", "WAR_HISTORIAN_REPY", "ROUND_TEA_PARTY", "TRIAL", "BATTLEWORN_DUMMY",
        "HUNGRY_FOR_MUSHROOMS", "GRAVE_OF_THE_FORGOTTEN",
        "WATERFALL_OF_OBVIOUS_SECRETS", "WUPPY_IN_TROUBLE", "MUSEUM_OF_TORTURE", "SHRINE_OF_THE_WARLORD",
        "THERMAE_NONUPEIPE", "THE_FUTURE_OF_POTIONS"
    ];

    private const Effects RandomRelic = Effects.Relic | Effects.RandomRelic;

    private static Choice? ForOption(EventModel model, Player player, EventOption option)
    {
        var eventId = model.Id.Entry;
        var key = option.TextKey[(option.TextKey.LastIndexOf('.') + 1)..];
        var mode = Effects.None;
        var amount = 0;
        switch (eventId)
        {
            case "SLIPPERY_BRIDGE" when key.StartsWith("HOLD_ON_", StringComparison.Ordinal):
                if (EventRng(player.RunState, option.TextKey).NextInt(2) == 0) mode = Effects.Bridge;
                break;
            case "SELF_HELP_BOOK" when key != "NO_OPTIONS": mode = Effects.Enchant; break;
            case "THIS_OR_THAT":
                if (key == "PLAIN") amount = 10;
                else if (key == "ORNATE") mode = Effects.Card | RandomRelic;
                break;
            case "ROOM_FULL_OF_CHEESE":
                mode = key == "GORGE" ? Effects.CardReward : Effects.Relic;
                break;
            case "BRAIN_LEECH": mode = Effects.CardReward | Effects.Card; break;
            case "TEA_MASTER": mode = Effects.Relic; break;
            case "THE_LEGENDS_WERE_TRUE":
                mode = key == "NAB_THE_MAP" ? Effects.Card : Effects.Potion;
                break;
            case "SUNKEN_STATUE":
                if (key == "DIVE_INTO_WATER") amount = 20;
                else mode = Effects.Relic;
                break;
            case "JUNGLE_MAZE_ADVENTURE": amount = key == "SOLO_QUEST" ? 28 : 10; break;
            case "WHISPERING_HOLLOW": mode = key == "GOLD" ? Effects.Potion : Effects.Transform; break;
            case "LUMINOUS_CHOIR": mode = key == "OFFER_TRIBUTE" ? RandomRelic : Effects.Card; break;
            case "MORPHIC_GROVE":
                if (key == "GROUP") mode = Effects.Transform;
                else amount = 10;
                break;
            case "BYRDONIS_NEST":
                if (key == "TAKE") mode = Effects.Card;
                else amount = 20;
                break;
            case "UNREST_SITE": mode = key == "REST" ? Effects.Card : RandomRelic; break;
            case "WOOD_CARVINGS": mode = key == "SNAKE" ? Effects.Enchant : Effects.Transform; break;
            case "WELLSPRING": mode = key == "BOTTLE" ? Effects.Potion : Effects.Card; break;
            case "AROMA_OF_CHAOS": mode = key == "LET_GO" ? Effects.Transform : Effects.Upgrade; break;
            case "TABLET_OF_TRUTH":
                if (key == "SMASH") amount = 25;
                else if (key.StartsWith("DECIPHER", StringComparison.Ordinal)) mode = Effects.Upgrade;
                break;
            case "DENSE_VEGETATION":
                if (key == "TRUDGE_ON") amount = 15;
                else if (key == "REST") amount = HalfMaxHp(player);
                break;
            case "SAPPHIRE_SEED":
                if (key == "EAT")
                {
                    mode = Effects.Upgrade;
                    amount = 15;
                }
                else mode = Effects.Enchant;
                break;
            case "TRASH_HEAP":
                mode = key == "DIVE_IN" ? Effects.Relic : Effects.Card;
                break;
            case "ENDLESS_CONVEYOR":
                mode = key switch
                {
                    "OBSERVE_CHEF" or "SPICY_SNAPPY" => Effects.Upgrade,
                    "JELLY_LIVER" => Effects.Transform,
                    "SEAPUNK_SALAD" or "FRIED_EEL" => Effects.Card,
                    "SUSPICIOUS_CONDIMENT" => Effects.Potion,
                    _ => Effects.None
                };
                amount = key switch { "CAVIAR" => 10, "GOLDEN_FYSH" => 12, "CLAM_ROLL" => 15, _ => 0 };
                break;
            case "WATERLOGGED_SCRIPTORIUM":
                if (key == "BLOODY_INK") amount = 20;
                else mode = Effects.Enchant;
                break;
            case "ABYSSAL_BATHS":
                if (key is "IMMERSE" or "LINGER") amount = 10;
                else if (key == "ABSTAIN") amount = 15;
                break;
            case "DROWNING_BEACON": mode = key == "BOTTLE" ? Effects.Potion : Effects.Relic; break;
            case "SUNKEN_TREASURY":
                amount = key == "SECOND_CHEST" ? 62 : 10;
                if (key == "SECOND_CHEST") mode = Effects.Card;
                break;
            case "SPIRALING_WHIRLPOOL":
                if (key == "DRINK") amount = HalfMaxHp(player);
                else mode = Effects.Enchant;
                break;
            case "PUNCH_OFF":
                mode = key == "NAB" ? Effects.Card | RandomRelic : Effects.Combat;
                break;
            case "SYMBIOTE": mode = key == "APPROACH" ? Effects.Enchant : Effects.Transform; break;
            case "POTION_COURIER": mode = Effects.Potion; break;
            case "RELIC_TRADER": mode = Effects.Relic; break;
            case "RANWID_THE_ELDER": mode = RandomRelic; break;
            case "STONE_OF_ALL_TIME":
                if (key == "LIFT") amount = 30;
                else mode = Effects.Enchant;
                break;
            case "DOLL_ROOM": mode = Effects.Relic; break;
            case "WELCOME_TO_WONGOS":
                if (key is "MYSTERY_BOX" or "FEATURED_ITEM") mode = Effects.Relic;
                else if (key == "BARGAIN_BIN") mode = RandomRelic;
                break;
            case "FIELD_OF_MAN_SIZED_HOLES": mode = key == "RESIST" ? Effects.Card : Effects.Enchant; break;
            case "BUGSLAYER": mode = Effects.Card; break;
            case "COLOSSAL_FLOWER":
                if (key.StartsWith("EXTRACT_", StringComparison.Ordinal)) amount = 6;
                else if (key == "POLLINOUS_CORE") mode = Effects.Relic;
                break;
            case "THE_LANTERN_KEY":
                if (key == "RETURN_THE_KEY") amount = 15;
                else mode = Effects.Combat;
                break;
            case "SPIRIT_GRAFTER":
                if (key == "LET_IT_IN")
                {
                    mode = Effects.Card;
                    amount = 40;
                }
                else mode = Effects.Upgrade;
                break;
            case "AMALGAMATOR": mode = Effects.Card; break;
            case "INFESTED_AUTOMATON": mode = Effects.CardReward; break;
            case "LOST_WISP":
                if (key == "SEARCH") amount = 10;
                else mode = Effects.Card | Effects.Relic;
                break;
            case "WAR_HISTORIAN_REPY":
                if (key == "UNLOCK_CAGE")
                {
                    mode = Effects.Relic;
                }
                else if (key == "UNLOCK_CHEST") mode = RandomRelic | Effects.Potion;
                break;
            case "ROUND_TEA_PARTY":
                if (key == "ENJOY_TEA") mode = Effects.Relic;
                else if (key is "PICK_FIGHT" or "CONTINUE_FIGHT") mode = RandomRelic;
                break;
            case "TRIAL":
                var path = option.TextKey.Split('.');
                var defendant = path.Length > 2 ? path[2] : "";
                (mode, amount) = (defendant, key) switch
                {
                    ("MERCHANT", "GUILTY") => (Effects.Card | RandomRelic, 0),
                    ("MERCHANT", "INNOCENT") => (Effects.Card | Effects.Upgrade, 0),
                    ("NOBLE", "GUILTY") => (Effects.None, 15),
                    ("NOBLE", "INNOCENT") => (Effects.Card, 50),
                    ("NONDESCRIPT", "GUILTY") => (Effects.Card | Effects.CardReward, 0),
                    ("NONDESCRIPT", "INNOCENT") => (Effects.Card | Effects.Transform, 0),
                    _ => (Effects.None, 0)
                };
                key = defendant + "." + key;
                break;
            case "BATTLEWORN_DUMMY":
                mode = key switch
                {
                    "SETTING_1" => Effects.Potion,
                    "SETTING_2" => Effects.Upgrade,
                    "SETTING_3" => RandomRelic,
                    _ => Effects.None
                };
                break;
            case "HUNGRY_FOR_MUSHROOMS": mode = Effects.Relic; break;
            case "GRAVE_OF_THE_FORGOTTEN":
                mode = key == "CONFRONT" ? Effects.Card | Effects.Enchant : Effects.Relic;
                break;
            case "WATERFALL_OF_OBVIOUS_SECRETS":
                mode = key switch
                {
                    "BE_NICE" => Effects.Card,
                    "BEAT_UP" => Effects.Card | RandomRelic,
                    "NEGOTIATE" => Effects.CardReward,
                    _ => Effects.None
                };
                break;
            case "WUPPY_IN_TROUBLE":
                if (key == "HELP_IMMEDIATELY") mode = Effects.Card;
                else if (key == "BE_CAUTIOUS") amount = 50;
                else if (key == "IGNORE") mode = Effects.Potion;
                break;
            case "MUSEUM_OF_TORTURE":
                if (key == "TAKE") mode = Effects.Relic | Effects.Card;
                else if (key == "BREAK_OUT") amount = 20;
                break;
            case "SHRINE_OF_THE_WARLORD" when key is "TAKE_WEAPON" or "TAKE_SHIELD" or "TAKE_WINGS":
                mode = Effects.Card;
                if (key == "TAKE_WINGS") mode |= Effects.Relic;
                break;
            case "THERMAE_NONUPEIPE" when key is "THERMAL_BATHS" or "PRIVATE_OASIS" or "DRINK_WATERS":
                mode = Effects.Relic;
                if (key == "DRINK_WATERS")
                {
                    mode |= Effects.Card;
                    amount = 50;
                }
                break;
        }
        return mode == Effects.None && amount == 0 ? null : new Choice(player, eventId, mode, Amount: amount, OptionKey: key);
    }

    internal static void ApplyEventVariables(EventModel model)
    {
        if (!IsEroded(model) || model.Owner is not { } player) return;
        var rng = EventRng(player.RunState, model.Id.Entry + "_values");
        var maxHp = player.Creature.MaxHp;
        switch (model.Id.Entry)
        {
            case "THIS_OR_THAT": Set("Gold", rng.NextInt(55, 91)); break;
            case "SUNKEN_STATUE": Set("Gold", rng.NextInt(135, 162)); break;
            case "JUNGLE_MAZE_ADVENTURE":
                Set("JoinForcesGold", rng.NextInt(47, 86));
                Set("SoloGold", rng.NextInt(180, 220));
                break;
            case "MORPHIC_GROVE": Set("MaxHp", 6); break;
            case "BYRDONIS_NEST": Set("MaxHp", 9); break;
            case "TABLET_OF_TRUTH": Set("SmashHPGain", 25); break;
            case "DENSE_VEGETATION":
                Set("Heal", Math.Floor(maxHp * 0.4m));
                Set("Gold", rng.NextInt(81, 133));
                break;
            case "SAPPHIRE_SEED": Set("Heal", 12); break;
            case "ENDLESS_CONVEYOR":
                Set("CaviarMaxHp", 5);
                Set("ClamRollHeal", 13);
                Set("GoldenFyshGold", 100);
                break;
            case "WATERLOGGED_SCRIPTORIUM": Set("MaxHp", 8); break;
            case "ABYSSAL_BATHS":
                Set("MaxHp", 3);
                Set("Heal", 13);
                break;
            case "SUNKEN_TREASURY":
                Set("SmallChestGold", rng.NextInt(69, 90));
                Set("LargeChestGold", rng.NextInt(405, 485));
                break;
            case "SPIRALING_WHIRLPOOL": Set("Heal", Math.Floor(maxHp * 0.44m)); break;
            case "STONE_OF_ALL_TIME": Set("DrinkMaxHpGain", 13); break;
            case "COLOSSAL_FLOWER":
                Set("Prize1", 48);
                Set("Prize2", 100);
                Set("Prize3", 180);
                break;
            case "THE_LANTERN_KEY": Set("Gold", 133); break;
            case "SPIRIT_GRAFTER": Set("LetItInHealAmount", 33); break;
            case "LOST_WISP": Set("Gold", rng.NextInt(60, 101)); break;
            case "WATERFALL_OF_OBVIOUS_SECRETS": Set("NegotiateCostKey", 70); break;
            case "WUPPY_IN_TROUBLE":
                Set("ImmediateWuppyHp", 80);
                Set("CautiousWuppyHp", 40);
                Set("HpLoss", 3);
                break;
            case "MUSEUM_OF_TORTURE": Set("MaxHp", 8); break;
            case "THERMAE_NONUPEIPE":
                Set("DrinkHpLoss", 21);
                break;
        }

        void Set(string name, decimal value)
        {
            if (model.DynamicVars.TryGetValue(name, out var variable)) variable.BaseValue = value;
        }
    }

    private static int HalfMaxHp(Player player) => (int)Math.Floor(player.Creature.MaxHp * 0.10m) * 5;
}

public sealed class ShadowBridgeDamagePatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_bridge_damage";
    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(CreatureCmd), nameof(CreatureCmd.Damage),
            [typeof(PlayerChoiceContext), typeof(Creature), typeof(decimal), typeof(ValueProp),
             typeof(CardModel), typeof(CardPlay)]),
        new(typeof(CreatureCmd), nameof(CreatureCmd.Damage),
            [typeof(PlayerChoiceContext), typeof(Creature), typeof(decimal), typeof(ValueProp),
             typeof(Creature), typeof(CardModel), typeof(CardPlay)])
    ];

    public static bool Prefix(PlayerChoiceContext choiceContext, Creature target, decimal amount,
        ref Task<IEnumerable<DamageResult>> __result)
    {
        if (target.Player is not { } player || !ShadowEvents.ConvertBridgeDamage(target, amount, out var erosion))
            return true;
        __result = ReplaceDamage(player, erosion, choiceContext);
        return false;
    }

    public static void Postfix(PlayerChoiceContext choiceContext, Creature target, ValueProp props,
        ref Task<IEnumerable<DamageResult>> __result) =>
        ShadowEvents.AddErosionAfterEventDamage(choiceContext, target, props, ref __result);

    private static async Task<IEnumerable<DamageResult>> ReplaceDamage(Player player, int amount, PlayerChoiceContext context)
    {
        await ShadowEvents.ApplyBridgeErosion(player, amount, context);
        return [];
    }
}

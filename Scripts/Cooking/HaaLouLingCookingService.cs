using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Utils;
using YogSothothsYard.Scripts;
using YogSothothsYard.Scripts.UI;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace YogSothothsYard.Scripts.Cooking;

public static class HaaLouLingCookingService
{
    private const string QueueFullKey = "YOG_SOTHOTH_S_YARD_COOKING_QUEUE_FULL";
    private const string MissingIngredientsKey = "YOG_SOTHOTH_S_YARD_COOKING_MISSING_INGREDIENTS";

    private static readonly AttachedState<PlayerCombatState, HaaLouLingCookingState> States =
        new(() => new HaaLouLingCookingState());

    public static HaaLouLingCookingState GetState(Player player) =>
        States.GetOrCreate(player.PlayerCombatState!);

    public static IReadOnlyList<CookingDishDefinition> GetCookableRecipes(Player player)
    {
        var pile = Entry.FoodPile.GetPile(player);
        var counts = pile.Cards
            .GroupBy(card => card.Id)
            .ToDictionary(group => group.Key, group => group.Count());

        return HaaLouLingCardSortOrder.SortRecipes(
            HaaLouLingCookingRecipes.All.Where(recipe => HasIngredients(counts, recipe))
        );
    }

    public static bool HasPendingDishUpgrade(Player player) =>
        player.Creature.GetPower<MoYuBaoPower>() != null ||
        player.Creature.GetPowerAmount<ChangFenPower>() > 0 ||
        player.Creature.GetPowerAmount<ChaoHeFenPower>() > 0;

    public static bool CanCookNow(Player player)
    {
        var combatManager = CombatManager.Instance;
        return combatManager.IsInProgress &&
               combatManager.IsPartOfPlayerTurn(player) &&
               player.PlayerCombatState?.Phase == PlayerTurnPhase.Play &&
               !combatManager.PlayerActionsDisabled &&
               !combatManager.IsPlayerReadyToEndTurn(player);
    }

    public static async Task AddGeneratedIngredients(
        PlayerChoiceContext choiceContext,
        Player player,
        IEnumerable<CardModel> cards
    )
    {
        var results = await CardPileCmd.AddGeneratedCardsToCombat(cards, Entry.FoodPile, player);
        if (results.Count == 0)
        {
            return;
        }

        CardCmd.PreviewCardPileAdd(results);

        var chickenCount = results.Count(result => result.success && result.cardAdded is JiRou);
        if (chickenCount > 0)
        {
            await TriggerChickenGained(choiceContext, player, chickenCount);
        }

    }

    public static async Task StartCooking(
        PlayerChoiceContext choiceContext,
        Player player,
        ModelId dishModelId,
        bool isUpgraded
    )
    {
        var recipe = HaaLouLingCookingRecipes.Get(dishModelId);
        if (recipe == null)
        {
            return;
        }

        var state = GetState(player);
        var autoUpgrade = player.Creature.GetPower<MoYuBaoPower>() != null;
        var consumePendingUpgrade =
            !autoUpgrade &&
            isUpgraded &&
            HasPendingDishUpgrade(player);
        isUpgraded = autoUpgrade || consumePendingUpgrade;
        var cookingTurns = recipe.GetCookingTurns(isUpgraded);
        var needsCookingSlot = cookingTurns > 0;
        if (needsCookingSlot && state.Slots.Count >= state.Capacity)
        {
            ShowThought(player, QueueFullKey);
            return;
        }

        var pile = Entry.FoodPile.GetPile(player);
        var ingredientCards = FindIngredientCards(pile, recipe);
        if (ingredientCards == null)
        {
            ShowThought(player, MissingIngredientsKey);
            return;
        }

        await TriggerIngredientsUsed(choiceContext, player, ingredientCards);

        var preserveChicken = player.Creature.GetPower<YanJuJiPower>() != null;
        foreach (var card in ingredientCards)
        {
            if (preserveChicken && card is JiRou)
            {
                continue;
            }

            await CardPileCmd.RemoveFromCombat(card, false);
        }

        if (consumePendingUpgrade)
        {
            await ConsumeDishUpgrade(choiceContext, player);
        }

        if (!needsCookingSlot)
        {
            await CompleteDish(
                choiceContext,
                player,
                recipe,
                isUpgraded,
                isCopy: false
            );
            NCookingQueueDisplay.RefreshFor(player);
            return;
        }

        state.Slots.Add(new CookingSlot
        {
            DishModelId = recipe.DishModelId,
            RemainingTurns = cookingTurns,
            IsUpgraded = isUpgraded,
            IsCopy = false
        });

        NCookingQueueDisplay.EnsureFor(player);
        NCookingQueueDisplay.RefreshFor(player);
    }

    public static async Task Advance(PlayerChoiceContext choiceContext, Player player)
    {
        var state = GetState(player);
        if (state.Slots.Count == 0)
        {
            NCookingQueueDisplay.RefreshFor(player);
            return;
        }

        foreach (var slot in state.Slots)
        {
            slot.RemainingTurns--;
        }

        var completedSlots = state.Slots
            .Where(slot => slot.RemainingTurns <= 0)
            .ToList();

        foreach (var slot in completedSlots)
        {
            if (!state.Slots.Remove(slot))
            {
                continue;
            }

            var recipe = HaaLouLingCookingRecipes.Get(slot.DishModelId);
            if (recipe != null)
            {
                await CompleteDish(
                    choiceContext,
                    player,
                    recipe,
                    slot.IsUpgraded,
                    slot.IsCopy
                );
            }
        }

        NCookingQueueDisplay.RefreshFor(player);
    }

    public static async Task ChooseCommonDishEffect(
        PlayerChoiceContext choiceContext,
        Player player,
        bool isUpgraded
    ) => await ChooseDishEffect(choiceContext, player, CardRarity.Common, isUpgraded);

    public static async Task ChooseUncommonDishEffect(
        PlayerChoiceContext choiceContext,
        Player player,
        bool isUpgraded
    ) => await ChooseDishEffect(choiceContext, player, CardRarity.Uncommon, isUpgraded);

    public static async Task ChooseRareDishEffect(
        PlayerChoiceContext choiceContext,
        Player player,
        bool isUpgraded
    ) => await ChooseDishEffect(choiceContext, player, CardRarity.Rare, isUpgraded);

    public static async Task ChooseCommonAndUncommonDishEffects(
        PlayerChoiceContext choiceContext,
        Player player,
        bool isUpgraded
    )
    {
        await ChooseCommonDishEffect(choiceContext, player, isUpgraded);
        await ChooseUncommonDishEffect(choiceContext, player, isUpgraded);
    }

    public static async Task ChooseCommonUncommonAndRareDishEffects(
        PlayerChoiceContext choiceContext,
        Player player,
        bool isUpgraded
    )
    {
        await ChooseCommonDishEffect(choiceContext, player, isUpgraded);
        await ChooseUncommonDishEffect(choiceContext, player, isUpgraded);
        await ChooseRareDishEffect(choiceContext, player, isUpgraded);
    }

    private static async Task ChooseDishEffect(
        PlayerChoiceContext choiceContext,
        Player player,
        CardRarity rarity,
        bool isUpgraded
    )
    {
        var recipeCards = HaaLouLingCookingRecipes.All
            .Where(recipe => recipe.CreatePreviewCard().Rarity == rarity)
            .Select(recipe => (Recipe: recipe, Card: recipe.CreatePreviewCard()))
            .ToList();

        if (recipeCards.Count == 0)
        {
            return;
        }

        var choices = CardFactory.GetDistinctForCombat(
            player,
            recipeCards.Select(entry => entry.Card),
            3,
            player.RunState.Rng.CombatCardGeneration
        ).ToList();

        if (isUpgraded)
        {
            foreach (var choice in choices)
            {
                CardCmd.Upgrade(choice);
            }
        }

        var selected = await CardSelectCmd.FromChooseACardScreen(
            choiceContext,
            choices,
            player,
            canSkip: true
        );
        if (selected == null)
        {
            return;
        }

        var selectedRecipe = HaaLouLingCookingRecipes.Get(selected.Id);
        if (selectedRecipe == null)
        {
            return;
        }

        await selectedRecipe.OnComplete(choiceContext, player, selected.IsUpgraded);
    }

    public static async Task ModifyRandomCounterPower(
        PlayerChoiceContext choiceContext,
        Player player,
        PowerType type,
        int amount
    )
    {
        var powers = player.Creature.Powers
            .Where(power =>
                power is not HaaLouLingCookingPower &&
                power is not GuShenDeYiPiePower &&
                power.StackType == PowerStackType.Counter &&
                power.Type == type &&
                power.Amount != 0)
            .ToList();

        var selected = player.RunState.Rng.CombatCardGeneration.NextItem(powers);
        if (selected == null)
        {
            return;
        }

        await ModifyCounterPower(choiceContext, selected, type, amount, player.Creature);
    }

    public static async Task IncreaseRandomEnemyDebuff(
        PlayerChoiceContext choiceContext,
        Player player,
        int amount
    )
    {
        var enemies = player.Creature.CombatState?.HittableEnemies
            .Where(enemy => EligibleCounterPowers(enemy, PowerType.Debuff).Any())
            .ToList() ?? [];
        var enemy = player.RunState.Rng.CombatTargets.NextItem(enemies);
        var selected = enemy == null
            ? null
            : player.RunState.Rng.CombatCardGeneration.NextItem(
                EligibleCounterPowers(enemy, PowerType.Debuff).ToList()
            );
        if (selected != null)
        {
            await PowerCmd.ModifyAmount(
                choiceContext,
                selected,
                amount,
                player.Creature,
                null
            );
        }
    }

    public static async Task IncreaseRandomPlayerBuff(
        PlayerChoiceContext choiceContext,
        Player player,
        int amount
    )
    {
        var players = player.Creature.CombatState?.Players
            .Where(candidate => candidate.Creature.IsAlive)
            .Where(candidate => EligibleCounterPowers(candidate.Creature, PowerType.Buff).Any())
            .ToList() ?? [];
        var target = player.RunState.Rng.CombatCardGeneration.NextItem(players);
        var selected = target == null
            ? null
            : player.RunState.Rng.CombatCardGeneration.NextItem(
                EligibleCounterPowers(target.Creature, PowerType.Buff).ToList()
            );
        if (selected != null)
        {
            await ModifyCounterPower(
                choiceContext,
                selected,
                PowerType.Buff,
                amount,
                player.Creature
            );
        }
    }

    public static async Task IncreaseTwoRandomEnemyDebuffs(
        PlayerChoiceContext choiceContext,
        Player player,
        int amount
    )
    {
        var enemies = player.Creature.CombatState?.HittableEnemies
            .Where(enemy => EligibleCounterPowers(enemy, PowerType.Debuff).Any())
            .ToList() ?? [];
        var enemy = player.RunState.Rng.CombatTargets.NextItem(enemies);
        if (enemy == null)
        {
            return;
        }

        var powers = EligibleCounterPowers(enemy, PowerType.Debuff).ToList();
        for (var i = 0; i < 2 && powers.Count > 0; i++)
        {
            var selected = player.RunState.Rng.CombatCardGeneration.NextItem(powers);
            if (selected == null)
            {
                break;
            }

            powers.Remove(selected);
            await PowerCmd.ModifyAmount(
                choiceContext,
                selected,
                amount,
                player.Creature,
                null
            );
        }
    }

    public static async Task CopyRandomPowerToOtherPlayers(
        PlayerChoiceContext choiceContext,
        Player player,
        bool allPlayers
    )
    {
        var combatState = player.Creature.CombatState;
        if (combatState == null)
        {
            return;
        }

        var otherPlayers = combatState.Players
            .Where(other => other != player && other.Creature.IsAlive)
            .ToList();
        if (otherPlayers.Count == 0)
        {
            return;
        }

        var powers = player.Creature.Powers
            .Where(power => power is not HaaLouLingCookingPower && power.Amount != 0)
            .ToList();
        var selectedPower = player.RunState.Rng.CombatCardGeneration.NextItem(powers);
        if (selectedPower == null)
        {
            return;
        }

        var targets = allPlayers
            ? otherPlayers
            : [player.RunState.Rng.CombatTargets.NextItem(otherPlayers)!];
        var amount = selectedPower.StackType == PowerStackType.Counter
            ? selectedPower.Amount
            : 1;
        var canonicalPower = ModelDb.GetById<PowerModel>(selectedPower.Id);

        foreach (var target in targets)
        {
            await PowerCmd.Apply(
                choiceContext,
                canonicalPower.ToMutable(),
                target.Creature,
                amount,
                player.Creature,
                null
            );
        }
    }

    private static async Task CompleteDish(
        PlayerChoiceContext choiceContext,
        Player player,
        CookingDishDefinition recipe,
        bool isUpgraded,
        bool isCopy
    )
    {
        var state = GetState(player);
        await TriggerDishCompleted(choiceContext, player, recipe, isUpgraded, isCopy);

        var failed = DidAncientGlimpseCauseFailure(player);
        if (!failed)
        {
            var triggerBonusEffect = state.BonusDishEffectCharges > 0;
            if (triggerBonusEffect)
            {
                state.BonusDishEffectCharges--;
            }

            await recipe.OnComplete(choiceContext, player, isUpgraded);

            if (triggerBonusEffect)
            {
                await recipe.OnComplete(choiceContext, player, isUpgraded);
            }
        }

        if (!isCopy && UsesAncientPheromone(recipe))
        {
            await PowerCmd.Apply<GuShenDeYiPiePower>(
                choiceContext,
                player.Creature,
                1,
                player.Creature,
                null
            );
        }
    }

    private static bool DidAncientGlimpseCauseFailure(Player player)
    {
        var power = player.Creature.GetPower<GuShenDeYiPiePower>();
        if (power is not { Amount: > 0 })
        {
            return false;
        }

        var failureChance = Math.Clamp(
            power.Amount * GuShenDeYiPiePower.FailureChancePerStack,
            0,
            100
        );
        if (player.RunState.Rng.Niche.NextInt(100) >= failureChance)
        {
            return false;
        }

        power.FlashEffect();
        return true;
    }

    private static bool UsesAncientPheromone(CookingDishDefinition recipe) =>
        recipe.Ingredients.ContainsKey(ModelDb.Card<YuanGuXinXiSu>().Id);

    private static async Task TriggerDishCompleted(
        PlayerChoiceContext choiceContext,
        Player player,
        CookingDishDefinition completedRecipe,
        bool isUpgraded,
        bool isCopy
    )
    {
        foreach (var power in player.Creature.Powers.ToList())
        {
            switch (power)
            {
                case XueHuaJiNaoPower { Amount: > 0 } snowChicken:
                    snowChicken.FlashEffect();
                    await AddSpecificIngredients<JiRou>(
                        choiceContext,
                        player,
                        snowChicken.Amount
                    );
                    break;
                case MaYiShangShuPower { Amount: > 0 } antsClimbingATree:
                    antsClimbingATree.FlashEffect();
                    await DamageRandomEnemy(
                        choiceContext,
                        player,
                        antsClimbingATree.Amount
                    );
                    break;
                case KaoRuZhuPower { Amount: > 0 } roastPig:
                    roastPig.FlashEffect();
                    await GainBlock(player, roastPig.Amount);
                    break;
                case JiDiZhouPower { Amount: > 0 } congee:
                    congee.FlashEffect();
                    await DamageRandomEnemy(choiceContext, player, congee.Amount);
                    break;
                case ShaoLaShuangPinPower { Amount: > 0 } roastPlatter:
                    roastPlatter.FlashEffect();
                    await GainBlock(player, roastPlatter.Amount);
                    break;
                case PenCaiPower { Amount: > 0 } feast when !isCopy:
                    feast.FlashEffect();
                    for (var i = 0; i < feast.Amount; i++)
                    {
                        if (!await BeginDishCopy(
                                choiceContext,
                                player,
                                completedRecipe,
                                isUpgraded))
                        {
                            break;
                        }
                    }
                    break;
            }
        }
    }

    private static async Task TriggerChickenGained(
        PlayerChoiceContext choiceContext,
        Player player,
        int chickenCount
    )
    {
        foreach (var power in player.Creature.Powers.ToList())
        {
            switch (power)
            {
                case BoBoJiPower { Amount: > 0 } skewers:
                    skewers.FlashEffect();
                    for (var i = 0; i < chickenCount; i++)
                    {
                        await DamageAllEnemies(choiceContext, player, skewers.Amount);
                    }
                    break;
                case ZhuDuJiPower { Amount: > 0 } porkTripeChicken:
                    porkTripeChicken.FlashEffect();
                    await AddRandomIngredients(
                        choiceContext,
                        player,
                        chickenCount * porkTripeChicken.Amount
                    );
                    break;
            }
        }
    }

    private static async Task TriggerIngredientsUsed(
        PlayerChoiceContext choiceContext,
        Player player,
        IReadOnlyCollection<CardModel> ingredients
    )
    {
        var chickenCount = ingredients.Count(card => card is JiRou);
        var strangeMeatCount = ingredients.Count(card => card is QiGuaiDeRou);
        var state = GetState(player);

        foreach (var power in player.Creature.Powers.ToList())
        {
            switch (power)
            {
                case FenZhengRouPower { Amount: > 0 } steamedPork when strangeMeatCount > 0:
                    steamedPork.FlashEffect();
                    await ApplyPowerToEnemies<PoisonPower>(
                        choiceContext,
                        player,
                        steamedPork.Amount * strangeMeatCount
                    );
                    break;
                case GongBaoJiDingPower { Amount: > 0 } kungPaoChicken when chickenCount > 0:
                    kungPaoChicken.FlashEffect();
                    await GainBlock(player, kungPaoChicken.Amount * chickenCount);
                    break;
                case ZhangChaYaPower { Amount: > 0 } smokedDuck:
                    smokedDuck.FlashEffect();
                    await GainBlock(player, smokedDuck.Amount * ingredients.Count);
                    break;
                case BaoZaiFanPower { Amount: > 0 } clayPotRice:
                    clayPotRice.FlashEffect();
                    await DamageAllEnemies(
                        choiceContext,
                        player,
                        clayPotRice.Amount * ingredients.Count
                    );
                    break;
                case ZhangYuWanZiPower { Amount: > 0 } octopusBalls:
                    var total = state.OctopusBallIngredientProgress + ingredients.Count;
                    var thresholds = total / 8;
                    state.OctopusBallIngredientProgress = total % 8;
                    if (thresholds > 0)
                    {
                        octopusBalls.FlashEffect();
                        state.BonusDishEffectCharges += thresholds * octopusBalls.Amount;
                    }
                    break;
            }
        }
    }

    public static async Task AddRandomIngredients(
        PlayerChoiceContext choiceContext,
        Player player,
        int count
    )
    {
        if (count <= 0)
        {
            return;
        }

        var ingredients = ModelDb.CardPool<HaaLouLingFoodPool>()
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Where(card => card is IHaaLouLingIngredientCard)
            .ToList();
        if (ingredients.Count == 0)
        {
            return;
        }

        var generated = CardFactory.GetForCombat(
            player,
            ingredients,
            count,
            player.RunState.Rng.CombatCardGeneration
        ).ToList();
        await AddGeneratedIngredients(choiceContext, player, generated);
    }

    public static int CountIngredients<TIngredient>(Player player)
        where TIngredient : CardModel => Entry.FoodPile.GetPile(player).Cards.Count(
            card => card is TIngredient
        );

    public static int GetRemainingCookingTurnsTotal(Player player) =>
        GetState(player).Slots.Sum(slot => Math.Max(0, slot.RemainingTurns));

    public static async Task UpgradeCookingSlots(
        PlayerChoiceContext choiceContext,
        Player player,
        bool allSlots
    )
    {
        var state = GetState(player);
        var candidates = state.Slots
            .Where(slot =>
            {
                var recipe = HaaLouLingCookingRecipes.Get(slot.DishModelId);
                return slot.RemainingTurns > 0 &&
                       !slot.IsUpgraded &&
                       recipe != null;
            })
            .ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        if (!allSlots)
        {
            var minimum = candidates.Min(slot => slot.RemainingTurns);
            candidates = candidates.Where(slot => slot.RemainingTurns == minimum).ToList();
        }

        foreach (var slot in candidates)
        {
            var recipe = HaaLouLingCookingRecipes.Get(slot.DishModelId);
            if (recipe == null)
            {
                continue;
            }

            var cookingTurnDelta = recipe.GetCookingTurns(true) - recipe.GetCookingTurns(false);
            slot.IsUpgraded = true;
            slot.RemainingTurns = Math.Max(0, slot.RemainingTurns + cookingTurnDelta);
        }

        var completedSlots = state.Slots.Where(slot => slot.RemainingTurns <= 0).ToList();
        foreach (var slot in completedSlots)
        {
            if (!state.Slots.Remove(slot))
            {
                continue;
            }

            var recipe = HaaLouLingCookingRecipes.Get(slot.DishModelId);
            if (recipe != null)
            {
                await CompleteDish(
                    choiceContext,
                    player,
                    recipe,
                    slot.IsUpgraded,
                    slot.IsCopy
                );
            }
        }

        NCookingQueueDisplay.RefreshFor(player);
    }

    private static async Task AddSpecificIngredients<TIngredient>(
        PlayerChoiceContext choiceContext,
        Player player,
        int count
    ) where TIngredient : CardModel
    {
        if (count <= 0)
        {
            return;
        }

        var cards = CardFactory.GetForCombat(
            player,
            [ModelDb.Card<TIngredient>()],
            count,
            player.RunState.Rng.CombatCardGeneration
        ).ToList();
        await AddGeneratedIngredients(choiceContext, player, cards);
    }

    private static Task GainBlock(Player player, int amount) =>
        CreatureCmd.GainBlock(player.Creature, amount, ValueProp.Unpowered, null);

    public static async Task DamageRandomEnemy(
        PlayerChoiceContext choiceContext,
        Player player,
        int amount
    )
    {
        var enemies = player.Creature.CombatState?.HittableEnemies;
        if (enemies is not { Count: > 0 })
        {
            return;
        }

        var target = player.RunState.Rng.CombatTargets.NextItem(enemies);
        if (target != null)
        {
            await CreatureCmd.Damage(
                choiceContext,
                target,
                amount,
                ValueProp.Unpowered,
                player.Creature
            );
        }
    }

    public static async Task DamageAllEnemies(
        PlayerChoiceContext choiceContext,
        Player player,
        int amount
    )
    {
        var enemies = player.Creature.CombatState?.HittableEnemies;
        if (enemies is { Count: > 0 })
        {
            await CreatureCmd.Damage(
                choiceContext,
                enemies,
                amount,
                ValueProp.Unpowered,
                player.Creature
            );
        }
    }

    public static async Task ApplyPowerToEnemies<TPower>(
        PlayerChoiceContext choiceContext,
        Player player,
        int amount
    ) where TPower : PowerModel
    {
        var enemies = player.Creature.CombatState?.HittableEnemies;
        if (enemies is { Count: > 0 })
        {
            await PowerCmd.Apply<TPower>(
                choiceContext,
                enemies,
                amount,
                player.Creature,
                null
            );
        }
    }

    public static async Task ClearDebuffs(Player player)
    {
        foreach (var power in player.Creature.Powers
                     .Where(power =>
                         power.Type == PowerType.Debuff &&
                         power is not GuShenDeYiPiePower)
                     .ToList())
        {
            await PowerCmd.Remove(power);
        }
    }

    public static async Task RemoveArtifactFromEnemies(Player player)
    {
        var enemies = player.Creature.CombatState?.Enemies
            .Where(enemy => enemy.IsAlive)
            .ToList() ?? [];
        foreach (var enemy in enemies)
        {
            await PowerCmd.Remove<ArtifactPower>(enemy);
        }
    }

    public static void AddShengYanMangle(Player player, int amount)
    {
        if (amount > 0)
        {
            GetState(player).ShengYanManglePerTurn += amount;
        }
    }

    public static int GetShengYanMangle(Player player) =>
        GetState(player).ShengYanManglePerTurn;

    public static async Task UpgradeAllCookingSlots(
        PlayerChoiceContext choiceContext,
        Player player
    ) => await UpgradeCookingSlots(choiceContext, player, allSlots: true);

    private static async Task<bool> BeginDishCopy(
        PlayerChoiceContext choiceContext,
        Player player,
        CookingDishDefinition recipe,
        bool isUpgraded
    )
    {
        var state = GetState(player);
        var cookingTurns = recipe.GetCookingTurns(isUpgraded);
        if (cookingTurns <= 0)
        {
            await CompleteDish(
                choiceContext,
                player,
                recipe,
                isUpgraded,
                isCopy: true
            );
            return true;
        }

        if (state.Slots.Count >= state.Capacity)
        {
            return false;
        }

        state.Slots.Add(new CookingSlot
        {
            DishModelId = recipe.DishModelId,
            RemainingTurns = cookingTurns,
            IsUpgraded = isUpgraded,
            IsCopy = true
        });
        NCookingQueueDisplay.EnsureFor(player);
        NCookingQueueDisplay.RefreshFor(player);
        return true;
    }

    private static IEnumerable<PowerModel> EligibleCounterPowers(
        Creature creature,
        PowerType type
    ) => creature.Powers.Where(power =>
        power is not HaaLouLingCookingPower &&
        power.StackType == PowerStackType.Counter &&
        power.Type == type &&
        power.Amount != 0);

    private static Task ModifyCounterPower(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        PowerType type,
        int amount,
        Creature applier
    )
    {
        var offset = type == PowerType.Debuff
            ? power.Amount > 0
                ? -Math.Min(amount, power.Amount)
                : Math.Min(amount, -power.Amount)
            : power.Amount < 0
                ? Math.Min(amount, -power.Amount)
                : amount;

        return PowerCmd.ModifyAmount(choiceContext, power, offset, applier, null);
    }

    private static async Task ConsumeDishUpgrade(
        PlayerChoiceContext choiceContext,
        Player player
    )
    {
        var power = player.Creature.Powers.FirstOrDefault(candidate =>
            (candidate is ChangFenPower or ChaoHeFenPower) && candidate.Amount > 0);
        if (power != null)
        {
            await PowerCmd.ModifyAmount(
                choiceContext,
                power,
                -1,
                player.Creature,
                null
            );
        }
    }

    private static bool HasIngredients(
        IReadOnlyDictionary<ModelId, int> ingredientCounts,
        CookingDishDefinition recipe
    )
    {
        foreach (var (ingredientId, count) in recipe.Ingredients)
        {
            if (!ingredientCounts.TryGetValue(ingredientId, out var owned) || owned < count)
            {
                return false;
            }
        }

        return true;
    }

    private static List<CardModel>? FindIngredientCards(CardPile pile, CookingDishDefinition recipe)
    {
        var result = new List<CardModel>();
        foreach (var (ingredientId, count) in recipe.Ingredients)
        {
            var cards = pile.Cards
                .Where(card => card.Id == ingredientId)
                .Take(count)
                .ToList();

            if (cards.Count < count)
            {
                return null;
            }

            result.AddRange(cards);
        }

        return result;
    }

    private static void ShowThought(Player player, string key)
    {
        if (!LocalContext.IsMe(player))
        {
            return;
        }

        var text = new LocString("gameplay_ui", key).GetFormattedText();
        NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(
            NThoughtBubbleVfx.Create(text, player.Creature, 2.0)
        );
    }
}

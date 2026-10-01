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
using YogSothothsYard.Scripts.Card.HaaLouLingDish;
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
        var counts = pile.Cards.GroupBy(card => card.Id).ToDictionary(group => group.Key, group => group.Count());
        var chef = player.Creature.HasPower<DaChuXingTaiPower>();
        return HaaLouLingCardSortOrder.SortRecipes(HaaLouLingCookingRecipes.All.Where(recipe =>
            CanStartRecipe(player, recipe) && (chef ? pile.Cards.Count >= recipe.Ingredients.Values.Sum() : HasIngredients(counts, recipe))));
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

    public static async Task AddGeneratedIngredients(PlayerChoiceContext choiceContext, Player player, IEnumerable<CardModel> cards)
    {
        if (player.Character is not HaaLouLing || player.Creature.HasPower<CaiCongXinShengPower>()) return;
        var results = await CardPileCmd.AddGeneratedCardsToCombat(cards, Entry.FoodPile, player);
        var gained = results.Where(result => result.success).ToList();
        if (gained.Count == 0) return;
        CardCmd.PreviewCardPileAdd(results);
        if (player.GetRelic<TongBingChaoShao>() is { } spoon) await spoon.IngredientsGained(choiceContext, gained.Count);
        var chickens = gained.Count(result => result.cardAdded is JiRou);
        if (chickens > 0) await TriggerChickenGained(choiceContext, player, chickens);
    }

    public static async Task StartCooking(PlayerChoiceContext choiceContext, Player player, ModelId dishModelId, bool isUpgraded)
    {
        var recipe = HaaLouLingCookingRecipes.Get(dishModelId);
        if (recipe == null || !CanStartRecipe(player, recipe)) return;
        isUpgraded = HasPendingDishUpgrade(player);
        if (!HasRoom(player, recipe, isUpgraded)) return;
        var pile = Entry.FoodPile.GetPile(player);
        var chef = player.Creature.HasPower<DaChuXingTaiPower>();
        var used = chef ? pile.Cards.Take(recipe.Ingredients.Values.Sum()).ToList() : FindIngredientCards(pile, recipe);
        if (used == null || used.Count != recipe.Ingredients.Values.Sum())
        {
            ShowThought(player, MissingIngredientsKey);
            return;
        }
        GetState(player).CookedThisTurn.Add(recipe.DishModelId);
        var counted = chef ? recipe.Ingredients.SelectMany(item =>
            Enumerable.Repeat(ModelDb.GetById<CardModel>(item.Key), item.Value)).ToList() : used;
        await TriggerIngredientsUsed(choiceContext, player, counted);
        if (!player.Creature.HasPower<CaiCongXinShengPower>())
            foreach (var card in used)
                if (!(card is JiRou && player.Creature.HasPower<YanJuJiPower>())) await CardPileCmd.RemoveFromCombat(card, false);
        if (isUpgraded && !player.Creature.HasPower<MoYuBaoPower>()) await ConsumeDishUpgrade(choiceContext, player);
        await Enqueue(choiceContext, player, recipe, isUpgraded, false);
    }

    public static Task Advance(PlayerChoiceContext choiceContext, Player player) => ChangeCookingTurns(choiceContext, player, -1);

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

        await GainDishEffect(choiceContext, player, selectedRecipe, selected.IsUpgraded);
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
                power is not DaoShiGaugePower &&
                power.StackType == PowerStackType.Counter &&
                power.TypeForCurrentAmount == type &&
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
                Math.Sign(selected.Amount) * amount,
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
                Math.Sign(selected.Amount) * amount,
                player.Creature,
                null
            );
        }
    }

    public static async Task CopyRandomPowerToOtherPlayers(PlayerChoiceContext choiceContext, Player player, bool allPlayers)
    {
        var others = player.Creature.CombatState!.Players.Where(other => other != player && other.Creature.IsAlive).ToList();
        var candidates = player.Creature.Powers.Where(power => power.GetType().Assembly == typeof(PowerModel).Assembly && power.Amount != 0).ToList();
        var selected = player.RunState.Rng.CombatCardGeneration.NextItem(candidates);
        if (selected == null || others.Count == 0) return;
        var targets = allPlayers ? others : [player.RunState.Rng.CombatTargets.NextItem(others)!];
        foreach (var target in targets)
        {
            var existing = PowerCmd.FindExistingInstanceForStacking(selected, target.Creature, selected.Applier);
            if (existing != null) await PowerCmd.ModifyAmount(choiceContext, existing, selected.Amount, selected.Applier, null);
            else await PowerCmd.Apply(choiceContext, (PowerModel)selected.ClonePreservingMutability(), target.Creature, selected.Amount, selected.Applier, null);
        }
    }

    private static async Task CompleteDish(PlayerChoiceContext choiceContext, Player player, CookingDishDefinition recipe, bool isUpgraded, bool isCopy)
    {
        var state = GetState(player);
        state.CompletionDepth++;
        var shares = player.Creature.Powers.OfType<FenNiYiFenPower>().Where(power => power.Amount > 0).ToList();
        var recipients = shares.Select(share => share.Recipient).ToList();
        foreach (var share in shares) await PowerCmd.Decrement(share);
        await TriggerDishCompleted(choiceContext, player, recipe, isUpgraded, isCopy);
        var repeats = state.BonusDishEffectCharges > 0 ? 2 : 1;
        if (repeats == 2) state.BonusDishEffectCharges--;
        for (var i = 0; i < repeats; i++)
        {
            await GainDishEffect(choiceContext, player, recipe, isUpgraded);
            foreach (var recipient in recipients)
                if (recipient?.Creature.IsAlive == true) await GainDishEffect(choiceContext, recipient, recipe, isUpgraded);
        }
        if (UsesAncientPheromone(recipe)) HaaLouLingCombatRules.GainAncientGlimpse(player);
        state.CompletionDepth--;
        await DrainCopies(choiceContext, player);
    }

    public static Task GainDishEffect(PlayerChoiceContext context, Player player, CookingDishDefinition recipe, bool upgraded)
    {
        if (!player.Creature.IsAlive || (player.Creature.GetPowerAmount<GuShenDeYiPiePower>() >= 4 &&
            recipe.CreatePreviewCard().Rarity == CardRarity.Rare) || (player.Character is not HaaLouLing && IsCookingEffect(recipe)))
            return Task.CompletedTask;
        return recipe.OnComplete(context, player, upgraded);
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
                        GetState(player).PendingCopies.Add((completedRecipe, isUpgraded));
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
                    for (var i = 0; i < strangeMeatCount; i++)
                        await ApplyPowerToEnemies<PoisonPower>(choiceContext, player, steamedPork.Amount);
                    break;
                case GongBaoJiDingPower { Amount: > 0 } kungPaoChicken when chickenCount > 0:
                    kungPaoChicken.FlashEffect();
                    for (var i = 0; i < chickenCount; i++) await GainBlock(player, kungPaoChicken.Amount);
                    break;
                case ZhangChaYaPower { Amount: > 0 } smokedDuck:
                    smokedDuck.FlashEffect();
                    for (var i = 0; i < ingredients.Count; i++) await GainBlock(player, smokedDuck.Amount);
                    break;
                case BaoZaiFanPower { Amount: > 0 } clayPotRice:
                    clayPotRice.FlashEffect();
                    for (var i = 0; i < ingredients.Count; i++)
                        await DamageAllEnemies(choiceContext, player, clayPotRice.Amount);
                    break;
                case TianZaoBasePower stove:
                    await stove.IngredientsUsed(choiceContext, ingredients.Count);
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

    public static async Task AddRandomIngredients(PlayerChoiceContext choiceContext, Player player, int count, CardRarity? rarity = null, bool nonRare = false)
    {
        if (count <= 0 || player.Character is not HaaLouLing || player.Creature.HasPower<CaiCongXinShengPower>()) return;
        var ingredients = Ingredients(player, rarity, nonRare);
        await AddGeneratedIngredients(choiceContext, player,
            CardFactory.GetForCombat(player, ingredients, count, player.RunState.Rng.CombatCardGeneration));
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

        await FinishReady(choiceContext, player);

        NCookingQueueDisplay.RefreshFor(player);
    }

    public static async Task AddSpecificIngredients<TIngredient>(
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
                         power.TypeForCurrentAmount == PowerType.Debuff &&
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

    private static async Task DrainCopies(PlayerChoiceContext context, Player player)
    {
        var state = GetState(player);
        if (state.CompletionDepth > 0) return;
        var copies = state.PendingCopies.ToList();
        state.PendingCopies.Clear();
        foreach (var (recipe, upgraded) in copies) await StartWithoutIngredients(context, player, recipe, upgraded, true);
    }

    private static IEnumerable<PowerModel> EligibleCounterPowers(
        Creature creature,
        PowerType type
    ) => creature.Powers.Where(power =>
        power is not HaaLouLingCookingPower &&
        power is not GuShenDeYiPiePower &&
        power is not DaoShiGaugePower &&
        power.StackType == PowerStackType.Counter &&
        power.TypeForCurrentAmount == type &&
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
            : Math.Sign(power.Amount) * amount;

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

    public static void RefreshCapacity(Player player)
    {
        GetState(player).Capacity = HaaLouLingCookingState.DefaultCapacity + player.Creature.GetPowerAmount<GuoWanPiaoPenPower>();
        NCookingQueueDisplay.RefreshFor(player);
    }

    public static IReadOnlyList<CardModel> Ingredients(Player player, CardRarity? rarity = null, bool nonRare = false) =>
        ModelDb.CardPool<HaaLouLingFoodPool>().GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Where(card => card is IHaaLouLingIngredientCard && (rarity == null || card.Rarity == rarity) &&
                (!nonRare || card.Rarity != CardRarity.Rare)).ToList();

    private static bool CanStartRecipe(Player player, CookingDishDefinition recipe) =>
        !player.Creature.HasPower<CaiCongXinShengPower>() || !GetState(player).CookedThisTurn.Contains(recipe.DishModelId);

    private static bool HasRoom(Player player, CookingDishDefinition recipe, bool upgraded)
    {
        var room = recipe.GetCookingTurns(upgraded) <= 0 || GetState(player).Slots.Count < GetState(player).Capacity;
        if (!room) ShowThought(player, QueueFullKey);
        return room;
    }

    public static async Task<bool> StartWithoutIngredients(PlayerChoiceContext context, Player player, CookingDishDefinition recipe,
        bool upgraded = false, bool isCopy = false)
    {
        if (player.Character is not HaaLouLing || !CanStartRecipe(player, recipe)) return false;
        upgraded |= HasPendingDishUpgrade(player);
        if (!HasRoom(player, recipe, upgraded)) return false;
        GetState(player).CookedThisTurn.Add(recipe.DishModelId);
        if (upgraded && !player.Creature.HasPower<MoYuBaoPower>()) await ConsumeDishUpgrade(context, player);
        await Enqueue(context, player, recipe, upgraded, isCopy);
        return true;
    }

    private static async Task Enqueue(PlayerChoiceContext context, Player player, CookingDishDefinition recipe, bool upgraded, bool copy)
    {
        var turns = recipe.GetCookingTurns(upgraded);
        if (turns <= 0) await CompleteDish(context, player, recipe, upgraded, copy);
        else GetState(player).Slots.Add(new CookingSlot { DishModelId = recipe.DishModelId, RemainingTurns = turns, IsUpgraded = upgraded, IsCopy = copy });
        NCookingQueueDisplay.EnsureFor(player);
        NCookingQueueDisplay.RefreshFor(player);
    }

    public static async Task ChangeCookingTurns(PlayerChoiceContext context, Player player, int delta, bool left = false, bool right = false)
    {
        var slots = GetState(player).Slots;
        var selected = left ? slots.Take(1) : right ? slots.TakeLast(1) : slots;
        foreach (var slot in selected) slot.RemainingTurns = Math.Max(0, slot.RemainingTurns + delta);
        await FinishReady(context, player);
    }

    private static async Task FinishReady(PlayerChoiceContext context, Player player)
    {
        var state = GetState(player);
        var ready = state.Slots.Where(slot => slot.RemainingTurns <= 0).ToList();
        foreach (var slot in ready) state.Slots.Remove(slot);
        state.CompletionDepth++;
        foreach (var slot in ready)
            if (HaaLouLingCookingRecipes.Get(slot.DishModelId) is { } recipe)
            {
                if (state.ProcessingTurnStart) state.CompletedAtTurnStart++;
                await CompleteDish(context, player, recipe, slot.IsUpgraded, slot.IsCopy);
            }
        state.CompletionDepth--;
        await DrainCopies(context, player);
        NCookingQueueDisplay.RefreshFor(player);
    }

    public static int RemoveCookingSlots(Player player)
    {
        var state = GetState(player);
        var count = state.Slots.Count;
        state.Slots.Clear();
        NCookingQueueDisplay.RefreshFor(player);
        return count;
    }

    public static async Task CookRandomDishes(PlayerChoiceContext context, Player player, int count, bool rare)
    {
        var recipes = HaaLouLingCookingRecipes.All.Where(recipe =>
            rare ? recipe.CreatePreviewCard().Rarity == CardRarity.Rare : recipe.CreatePreviewCard().Rarity != CardRarity.Rare).ToList();
        for (var i = 0; i < count; i++)
            await StartWithoutIngredients(context, player, player.RunState.Rng.CombatCardGeneration.NextItem(recipes)!);
    }

    public static async Task RandomDishEffects(PlayerChoiceContext context, Player player, int count, bool uncommonOnly)
    {
        var recipes = HaaLouLingCookingRecipes.All.Where(recipe => uncommonOnly ?
            recipe.CreatePreviewCard().Rarity == CardRarity.Uncommon : recipe.CreatePreviewCard().Rarity != CardRarity.Rare).ToList();
        for (var i = 0; i < count; i++)
            await GainDishEffect(context, player, player.RunState.Rng.CombatCardGeneration.NextItem(recipes)!, false);
    }

    private static bool IsCookingEffect(CookingDishDefinition recipe) => recipe.CreatePreviewCard() is
        BaiZhanJi or XueHuaJiNao or ChangFen or ChaoHeFen or KouShuiJi or YanJuJi or ZhangYuNingMengZhi or
        ZhangYuWanZi or BaoZaiFan or TingZaiZhou or ZhuDuJi or PenCai or MoYuBao or BoBoJi or MaYiShangShu or
        KaoRuZhu or FenZhengRou or GongBaoJiDing or JiDiZhou or ShaoLaShuangPin or ZhangChaYa;

}

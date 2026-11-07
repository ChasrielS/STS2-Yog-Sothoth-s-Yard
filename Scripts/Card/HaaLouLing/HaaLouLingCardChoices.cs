using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YogSothothsYard.Scripts.Card.HaaLouLingDish;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

public static class HaaLouLingCardChoices
{
    public static async Task AddCurses(Player player, int count, PileType pile)
    {
        var cards = Enumerable.Range(0, count).Select(_ => player.Creature.CombatState!.CreateCard(ModelDb.Card<CanZhou>(), player));
        var results = await CardPileCmd.AddGeneratedCardsToCombat(cards, pile, player);
        CardCmd.PreviewCardPileAdd(results);
    }

    private static LocString Prompt(CardModel source)
    {
        var prompt = new LocString("cards", source.Id.Entry + ".selectionScreenPrompt");
        source.DynamicVars.AddTo(prompt);
        return prompt;
    }

    public static async Task RetainFromHand(PlayerChoiceContext context, CardModel source, int count)
    {
        var selected = await CardSelectCmd.FromHand(context, source.Owner, new CardSelectorPrefs(Prompt(source), count),
            card => !card.Keywords.Contains(CardKeyword.Retain), source);
        foreach (var card in selected) CardCmd.ApplyKeyword(card, CardKeyword.Retain);
    }
    public static Task<IEnumerable<CardModel>> SelectOwnedIngredients(PlayerChoiceContext context, CardModel source,
        int maximum)
    {
        var pile = Entry.FoodPile.GetPile(source.Owner);
        return CardSelectCmd.FromSimpleGrid(context, pile.Cards, source.Owner,
            new CardSelectorPrefs(Prompt(source), 0, maximum) { RequireManualConfirmation = true });
    }

    public static async Task TransformIngredients(PlayerChoiceContext context, CardModel source, IEnumerable<CardModel> cards)
    {
        foreach (var card in cards.ToList())
        {
            var choices = HaaLouLingCookingService.Ingredients(source.Owner, card.Rarity)
                .Where(candidate => candidate.Id != card.Id).ToList();
            if (choices.Count == 0) continue;
            await CardPileCmd.RemoveFromCombat(card, false);
            await HaaLouLingCookingService.AddGeneratedIngredients(context, source.Owner,
                HaaLouLingCookingService.GenerateRandomIngredients(source.Owner, choices, 1));
        }
    }
    public static async Task Retrieve(PlayerChoiceContext context, CardModel source, PileType pile,
        bool skillsOnly = false, int count = 1, PileType destination = PileType.Hand, CardPilePosition position = CardPilePosition.Bottom)
    {
        var selected = await CardSelectCmd.FromCombatPile(context, pile.GetPile(source.Owner), source.Owner,
            new CardSelectorPrefs(Prompt(source), count),
            skillsOnly ? card => card.Type == CardType.Skill : null);
        await CardPileCmd.Add(selected, destination, position);
    }
    public static async Task CookDishesChoice(PlayerChoiceContext context, CardModel source, int count)
    {
        var choices = HaaLouLingCardSortOrder.SortRecipes(HaaLouLingCookingRecipes.AvailableFor(source.Owner))
            .Select(recipe => source.Owner.Creature.CombatState!.CreateCard(recipe.CreatePreviewCard(), source.Owner)).ToList();
        var upgraded = HaaLouLingCookingService.HasPendingDishUpgrade(source.Owner);
        var selection = new CookingDishSelection();
        foreach (var card in choices.Cast<HaaLouLingDishCard>())
        {
            card.CookingSelection = selection;
            if (upgraded) card.UpgradeInternal();
            card.BaseReplayCount = HaaLouLingCookingService.PendingDishReplayCount(source.Owner,
                HaaLouLingCookingRecipes.Get(card.Id)!);
        }
        var selected = await CardSelectCmd.FromSimpleGrid(context, choices, source.Owner,
            new CardSelectorPrefs(Prompt(source), count));
        foreach (var card in selected)
            await HaaLouLingCookingService.StartWithoutIngredients(context, source.Owner, HaaLouLingCookingRecipes.Get(card.Id)!);
    }
    public static async Task AddKnifeCards(PlayerChoiceContext context, CardModel source)
    {
        var pool = ModelDb.CardPool<HaaLouLingCardPool>().GetUnlockedCards(source.Owner.UnlockState, source.Owner.RunState.CardMultiplayerConstraint)
            .OfType<HaaLouLingCard>().Where(card => card.HasKnifeEffect && card.CanBeGeneratedInCombat);
        var cards = CardFactory.GetForCombat(source.Owner, pool, 4, source.Owner.RunState.Rng.CombatCardGeneration).ToList();
        foreach (var card in cards)
        {
            if (source.IsUpgraded) CardCmd.Upgrade(card);
            card.EnergyCost.SetThisTurn(0);
        }
        var results = await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand, source.Owner);
        CardCmd.PreviewCardPileAdd(results);
    }
}

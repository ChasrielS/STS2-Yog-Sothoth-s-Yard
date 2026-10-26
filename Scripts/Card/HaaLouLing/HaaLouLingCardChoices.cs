using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YogSothothsYard.Scripts.Card.HaaLouLingDish;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

public static class HaaLouLingCardChoices
{
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
        int maximum, int minimum = 0, Func<CardModel, bool>? filter = null)
    {
        var pile = Entry.FoodPile.GetPile(source.Owner);
        var cards = filter == null ? pile.Cards : pile.Cards.Where(filter).ToList();
        return CardSelectCmd.FromSimpleGrid(context, cards, source.Owner,
            new CardSelectorPrefs(Prompt(source), minimum, maximum) { RequireManualConfirmation = true });
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
        bool skillsOnly = false, int count = 1)
    {
        var selected = await CardSelectCmd.FromCombatPile(context, pile.GetPile(source.Owner), source.Owner,
            new CardSelectorPrefs(Prompt(source), count),
            skillsOnly ? card => card.Type == CardType.Skill : null);
        await CardPileCmd.Add(selected, PileType.Hand);
    }
    public static async Task CookDishesChoice(PlayerChoiceContext context, CardModel source, int count, CardRarity? rarity = null)
    {
        var choices = HaaLouLingCardSortOrder.SortRecipes(HaaLouLingCookingRecipes.AvailableFor(source.Owner)
            .Where(recipe => rarity == null || recipe.CreatePreviewCard().Rarity == rarity))
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
    public static async Task AddFormCards(PlayerChoiceContext context, CardModel source)
    {
        var pool = ModelDb.CardPool<HaaLouLingCardPool>().GetUnlockedCards(source.Owner.UnlockState, source.Owner.RunState.CardMultiplayerConstraint)
            .OfType<HaaLouLingCard>().Where(card => card.CanBeGeneratedInCombat).ToList();
        foreach (var form in new[] { KnifeForm.Open, KnifeForm.Follow, KnifeForm.Turn, KnifeForm.Finish })
        {
            var card = CardFactory.GetForCombat(source.Owner, pool.Where(card => card.Form == form), 1,
                source.Owner.RunState.Rng.CombatCardGeneration).Single();
            if (source.IsUpgraded) CardCmd.Upgrade(card);
            card.EnergyCost.SetThisTurn(0);
            var result = await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, source.Owner);
            CardCmd.PreviewCardPileAdd(result);
        }
    }
}

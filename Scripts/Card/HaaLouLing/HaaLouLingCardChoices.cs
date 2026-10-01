using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
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

    public static async Task ChooseIngredients(PlayerChoiceContext context, CardModel source, int count, CardRarity? rarity = null)
    {
        var owner = source.Owner;
        var choices = HaaLouLingCookingService.Ingredients(owner, rarity).Select(card => owner.Creature.CombatState!.CreateCard(card, owner)).ToList();
        var selected = await CardSelectCmd.FromSimpleGrid(context, choices, owner,
            new CardSelectorPrefs(Prompt(source), count));
        await HaaLouLingCookingService.AddGeneratedIngredients(context, owner, selected);
    }
    public static async Task<IEnumerable<CardModel>> SelectOwnedIngredients(PlayerChoiceContext context, CardModel source, int maximum) =>
        await CardSelectCmd.FromCombatPile(context, Entry.FoodPile.GetPile(source.Owner), source.Owner,
            new CardSelectorPrefs(Prompt(source), 0, maximum));

    public static async Task TransformIngredients(PlayerChoiceContext context, CardModel source, IEnumerable<CardModel> cards, bool sameRarity = false)
    {
        foreach (var card in cards.ToList())
        {
            var choices = HaaLouLingCookingService.Ingredients(source.Owner, sameRarity ? card.Rarity : null);
            await CardPileCmd.RemoveFromCombat(card, false);
            await HaaLouLingCookingService.AddGeneratedIngredients(context, source.Owner,
                CardFactory.GetForCombat(source.Owner, choices, 1, source.Owner.RunState.Rng.CombatCardGeneration));
        }
    }
    public static async Task TransformCombatCards(PlayerChoiceContext context, CardModel source, IEnumerable<CardModel> cards, CardRarity? rarity = null, bool nonRare = false)
    {
        foreach (var card in cards.ToList())
        {
            await CardPileCmd.RemoveFromCombat(card, false);
            await HaaLouLingCookingService.AddRandomIngredients(context, source.Owner, 1, rarity, nonRare);
        }
    }
    public static async Task Retrieve(PlayerChoiceContext context, CardModel source, PileType pile, bool skillsOnly = false)
    {
        var selected = await CardSelectCmd.FromCombatPile(context, pile.GetPile(source.Owner), source.Owner,
            new CardSelectorPrefs(Prompt(source), 1),
            skillsOnly ? card => card.Type == CardType.Skill : null);
        foreach (var card in selected) await CardPileCmd.Add(card, PileType.Hand);
    }
    public static async Task CookRareChoice(PlayerChoiceContext context, CardModel source)
    {
        var recipes = HaaLouLingCookingRecipes.All.Select(recipe => recipe.CreatePreviewCard()).Where(card => card.Rarity == CardRarity.Rare);
        var choices = CardFactory.GetDistinctForCombat(source.Owner, recipes, 3, source.Owner.RunState.Rng.CombatCardGeneration).ToList();
        var selected = await CardSelectCmd.FromChooseACardScreen(context, choices, source.Owner, false);
        if (selected != null) await HaaLouLingCookingService.StartWithoutIngredients(context, source.Owner, HaaLouLingCookingRecipes.Get(selected.Id)!);
    }
    public static async Task AddFormCards(PlayerChoiceContext context, CardModel source)
    {
        var pool = ModelDb.CardPool<HaaLouLingCardPool>().GetUnlockedCards(source.Owner.UnlockState, source.Owner.RunState.CardMultiplayerConstraint)
            .OfType<HaaLouLingCard>().Where(card => card.CanBeGeneratedInCombat).ToList();
        foreach (var form in new[] { KnifeForm.Open, KnifeForm.Follow })
        {
            var card = CardFactory.GetForCombat(source.Owner, pool.Where(card => card.Form == form), 1,
                source.Owner.RunState.Rng.CombatCardGeneration).Single();
            card.EnergyCost.SetThisTurn(0);
            var result = await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, source.Owner);
            CardCmd.PreviewCardPileAdd(result);
        }
    }
}

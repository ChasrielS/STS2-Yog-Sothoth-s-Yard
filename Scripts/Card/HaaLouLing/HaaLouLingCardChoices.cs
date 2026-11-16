using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YogSothothsYard.Scripts.Card.HaaLouLingDish;
using YogSothothsYard.Scripts.Cooking;
using YogSothothsYard.Scripts.Shadow;

namespace YogSothothsYard.Scripts;

public static class HaaLouLingCardChoices
{
    public static async Task AddCurses(Player player, int count, PileType pile, int erosion = 0)
    {
        var cards = Enumerable.Range(0, count).Select(_ => CreateCurse(player, erosion)).ToArray();
        var results = await CardPileCmd.AddGeneratedCardsToCombat(cards, pile, player);
        CardCmd.PreviewCardPileAdd(results);
    }

    internal static CardModel CreateCurse(Player player, int erosion)
    {
        var card = player.Creature.CombatState!.CreateCard(ModelDb.Card<CanZhou>(), player);
        if (erosion > 0) ShadowRanks.Set(card, erosion);
        return card;
    }

    internal static IHoverTip CurseTip(int erosion)
    {
        var curse = ModelDb.Card<CanZhou>();
        if (erosion == 0) return HoverTipFactory.FromCard(curse);
        var preview = curse.ToMutable();
        ShadowRanks.Set(preview, erosion);
        return HoverTipFactory.FromCard(preview);
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
    public static Task<IEnumerable<CardModel>> SelectOwnedNonRareIngredients(PlayerChoiceContext context, CardModel source)
    {
        var cards = Entry.FoodPile.GetPile(source.Owner).Cards.Where(card => card.Rarity != CardRarity.Rare).ToList();
        if (cards.Count == 0) return Task.FromResult<IEnumerable<CardModel>>([]);
        return CardSelectCmd.FromSimpleGrid(context, cards, source.Owner,
            new CardSelectorPrefs(Prompt(source), 0, cards.Count) { RequireManualConfirmation = true });
    }

    public static async Task TransformIngredients(PlayerChoiceContext context, CardModel source, IEnumerable<CardModel> cards)
    {
        foreach (var card in cards.ToList())
        {
            var choices = HaaLouLingCookingService.Ingredients(source.Owner, card.Rarity)
                .Where(candidate => candidate.Id != card.Id).ToList();
            if (choices.Count == 0) continue;
            await CardPileCmd.RemoveFromCombat(card, false);
            var generated = HaaLouLingCookingService.GenerateRandomIngredients(source.Owner, choices, 1).ToList();
            var extra = ShadowRanks.Of(source) switch
            {
                1 => source.Owner.RunState.Rng.CombatCardGeneration.NextInt(100) < 20,
                2 => source.Owner.RunState.Rng.CombatCardGeneration.NextInt(100) < 50,
                3 => true,
                _ => false
            };
            if (extra && generated.Count > 0)
            {
                generated.Add(generated[0].CreateClone());
            }
            await HaaLouLingCookingService.AddGeneratedIngredients(context, source.Owner, generated);
        }
    }
    public static async Task Retrieve(PlayerChoiceContext context, CardModel source, PileType pile,
        bool skillsOnly = false, int count = 1, PileType destination = PileType.Hand,
        CardPilePosition position = CardPilePosition.Bottom, int discountThisTurn = 0)
    {
        var selected = await CardSelectCmd.FromCombatPile(context, pile.GetPile(source.Owner), source.Owner,
            new CardSelectorPrefs(Prompt(source), count),
            skillsOnly ? card => card.Type == CardType.Skill : null);
        await CardPileCmd.Add(selected, destination, position);
        if (discountThisTurn > 0)
            foreach (var card in selected) card.EnergyCost.AddThisTurn(-discountThisTurn);
    }
    public static async Task CookDishesChoice(PlayerChoiceContext context, CardModel source, int count,
        bool forceUpgrade = false, int advance = 0)
    {
        var choices = HaaLouLingCardSortOrder.SortRecipes(HaaLouLingCookingRecipes.AvailableFor(source.Owner))
            .Select(recipe => source.Owner.Creature.CombatState!.CreateCard(recipe.CreatePreviewCard(), source.Owner)).ToList();
        var upgraded = forceUpgrade || HaaLouLingCookingService.HasPendingDishUpgrade(source.Owner);
        var selection = new CookingDishSelection { ForceUpgrade = forceUpgrade };
        foreach (var card in choices.Cast<HaaLouLingDishCard>())
        {
            card.CookingSelection = selection;
            if (upgraded) card.UpgradeInternal();
            if (advance > 0)
            {
                var turns = card.DynamicVars["CookingTurns"];
                turns.BaseValue = Math.Max(0, turns.IntValue - advance);
            }
            card.BaseReplayCount = HaaLouLingCookingService.PendingDishReplayCount(source.Owner,
                HaaLouLingCookingRecipes.Get(card.Id)!);
        }
        var selected = await CardSelectCmd.FromSimpleGrid(context, choices, source.Owner,
            new CardSelectorPrefs(Prompt(source), count));
        foreach (var card in selected)
            await HaaLouLingCookingService.StartWithoutIngredients(context, source.Owner, HaaLouLingCookingRecipes.Get(card.Id)!,
                forceUpgrade, advance);
    }
}

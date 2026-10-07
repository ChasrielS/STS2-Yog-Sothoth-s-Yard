using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts.UI;

public static class CookingRecipeScreen
{
    public static void Show(Player player)
    {
        var capstone = NCapstoneContainer.Instance;
        if (capstone?.CurrentCapstoneScreen is NCardPileScreen { Pile: CookingRecipePile })
        {
            capstone.Close();
            return;
        }

        if (!HaaLouLingCookingService.CanCookNow(player))
        {
            Entry.ShowCannotCookMessage(player);
            return;
        }

        NCookingQueueDisplay.EnsureFor(player);
        NCookingQueueDisplay.RefreshFor(player);

        if (Entry.FoodPile.GetPile(player).IsEmpty)
        {
            Entry.ShowFoodPileEmptyMessage(player);
            return;
        }
        var recipes = HaaLouLingCookingService.GetCookableRecipes(player);
        if (recipes.Count == 0)
        {
            Entry.ShowNoCookableDishesMessage(player);
            return;
        }
        var pile = new CookingRecipePile(
            player,
            recipes,
            HaaLouLingCookingService.HasPendingDishUpgrade(player)
        );
        HaaLouLingPileScreen.Show(pile);
    }

    public static async Task EditPreset(Player player, int slot, bool adding)
    {
        var preset = CookingMenuPreferences.Data.Presets[slot];
        var recipes = adding ? HaaLouLingCookingService.GetCookableRecipes(player) :
            HaaLouLingCardSortOrder.SortRecipes(HaaLouLingCookingRecipes.All
                .Where(recipe => preset.Dishes.Contains(recipe.DishModelId.Entry)));
        if (recipes.Count == 0) return;
        var pile = new CookingRecipePile(player, recipes, HaaLouLingCookingService.HasPendingDishUpgrade(player));
        var prompt = new LocString("gameplay_ui", adding
            ? "YOG_SOTHOTH_S_YARD_COOKING_PRESET_ADD_PROMPT"
            : "YOG_SOTHOTH_S_YARD_COOKING_PRESET_REMOVE_PROMPT");
        var selector = NSimpleCardSelectScreen.Create(pile.Cards.ToList(),
            new CardSelectorPrefs(prompt, 0, pile.Cards.Count) { RequireManualConfirmation = true });
        NCapstoneContainer.Instance!.Close();
        NOverlayStack.Instance!.Push(selector);
        IEnumerable<CardModel> selected;
        try { selected = await selector.CardsSelected(); }
        catch (TaskCanceledException) { return; }
        var ids = selected.Select(card => card.Id.Entry).ToHashSet();
        if (ids.Count > 0)
            CookingMenuPreferences.Update(data =>
            {
                var dishes = data.Presets[slot].Dishes;
                if (adding) dishes.AddRange(ids.Where(id => !dishes.Contains(id)));
                else dishes.RemoveAll(ids.Contains);
            });
        Show(player);
    }

    public static void ShowForLocalPlayer()
    {
        if (!MegaCrit.Sts2.Core.Combat.CombatManager.Instance.IsInProgress)
        {
            return;
        }

        var player = LocalContext.GetMe(MegaCrit.Sts2.Core.Combat.CombatManager.Instance.DebugOnlyGetState());
        if (player?.Character is HaaLouLing)
        {
            Show(player);
        }
    }
}

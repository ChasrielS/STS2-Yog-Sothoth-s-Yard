using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
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

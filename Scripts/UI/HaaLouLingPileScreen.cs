using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Runs;
using YogSothothsYard.Scripts.Cooking;
using YogSothothsYard.Scripts.GameActions;

namespace YogSothothsYard.Scripts.UI;

public static class HaaLouLingPileScreen
{
    private const string FoodPileInfoTextKey = "YOG_SOTHOTH_S_YARD_FOOD_PILE_INFO";
    private const string CookingScreenTitleKey = "YOG_SOTHOTH_S_YARD_COOKING_SCREEN_TITLE";
    private const string CookingScreenEmptyKey = "YOG_SOTHOTH_S_YARD_COOKING_SCREEN_EMPTY";

    public static void Show(CardPile pile)
    {
        var screen = NCardPileScreen.ShowScreen(pile, Array.Empty<string>());
        if (screen.IsNodeReady())
        {
            Initialize(screen);
        }
        else
        {
            screen.Connect(
                Node.SignalName.Ready,
                Callable.From(() => Initialize(screen)),
                (uint)GodotObject.ConnectFlags.OneShot
            );
        }
    }

    private static void Initialize(NCardPileScreen screen)
    {
        if (screen.Pile is CookingRecipePile recipePile)
        {
            InitializeCookingScreen(screen, recipePile);
            return;
        }

        if (screen.Pile.Type != Entry.FoodPile)
        {
            return;
        }

        var grid = screen.GetNode<NCardGrid>("CardGrid");
        grid.SetCards(
            HaaLouLingCardSortOrder.SortIngredients(screen.Pile.Cards),
            screen.Pile.Type,
            [SortingOrders.Ascending]
        );

        var bottomLabel = screen.GetNode<MegaRichTextLabel>("%BottomLabel");
        bottomLabel.Visible = true;
        bottomLabel.Text = "[center]" + new LocString("gameplay_ui", FoodPileInfoTextKey).GetFormattedText();
    }

    private static void InitializeCookingScreen(
        NCardPileScreen screen,
        CookingRecipePile recipePile
    )
    {
        var infoKey = recipePile.IsEmpty ? CookingScreenEmptyKey : CookingScreenTitleKey;
        var bottomLabel = screen.GetNode<MegaRichTextLabel>("%BottomLabel");
        bottomLabel.Visible = true;
        bottomLabel.Text = "[center]" + new LocString("gameplay_ui", infoKey).GetFormattedText();

        var grid = screen.GetNode<NCardGrid>("CardGrid");
        grid.Connect(
            NCardGrid.SignalName.HolderPressed,
            Callable.From<NCardHolder>(holder => OnRecipePressed(recipePile, holder))
        );
    }

    private static void OnRecipePressed(CookingRecipePile recipePile, NCardHolder holder)
    {
        var card = holder.CardModel;
        if (card == null || !recipePile.TryGetRecipe(card, out var recipe))
        {
            return;
        }

        RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(
            new StartCookingDishAction(recipePile.Player, recipe.DishModelId, card.IsUpgraded)
        );
        NCapstoneContainer.Instance?.Close();
    }
}

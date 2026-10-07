using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Runs;
using YogSothothsYard.Scripts.Cooking;
using YogSothothsYard.Scripts.GameActions;

namespace YogSothothsYard.Scripts.UI;

public static class HaaLouLingPileScreen
{
    private const string FoodPileInfoTextKey = "YOG_SOTHOTH_S_YARD_FOOD_PILE_INFO";

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
        grid.GetNode<Control>("ScrollContainer").AddChild(NIngredientCountDisplay.Create(grid, screen.Pile));

        var bottomLabel = screen.GetNode<MegaRichTextLabel>("%BottomLabel");
        bottomLabel.Visible = true;
        bottomLabel.Text = "[center]" + new LocString("gameplay_ui", FoodPileInfoTextKey).GetFormattedText();
    }

    private static void InitializeCookingScreen(
        NCardPileScreen screen,
        CookingRecipePile recipePile
    )
    {
        var grid = screen.GetNode<NCardGrid>("CardGrid");
        grid.Connect(
            NCardGrid.SignalName.HolderPressed,
            Callable.From<NCardHolder>(holder => OnRecipePressed(recipePile, holder))
        );
        grid.Connect(NCardGrid.SignalName.HolderAltPressed, Callable.From<NCardHolder>(holder =>
        {
            var cards = recipePile.Cards.ToList();
            NGame.Instance!.GetInspectCardScreen().Open(cards, cards.IndexOf(holder.CardModel!));
        }));

        screen.AddChild(NCookingRecipeFilter.Create(screen, recipePile));
    }

    private static void OnRecipePressed(CookingRecipePile recipePile, NCardHolder holder)
    {
        if (!HaaLouLingCookingService.CanCookNow(recipePile.Player))
        {
            Entry.ShowCannotCookMessage(recipePile.Player);
            return;
        }
        var card = holder.CardModel;
        if (card == null || !recipePile.TryGetRecipe(card, out var recipe))
        {
            return;
        }

        RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(
            new StartCookingDishAction(recipePile.Player, recipe.DishModelId)
        );
        NCapstoneContainer.Instance?.Close();
    }
}

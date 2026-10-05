using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;

namespace YogSothothsYard.Scripts.Cooking;

public sealed class CookingRecipePile : CardPile
{
    private readonly Dictionary<CardModel, CookingDishDefinition> _recipesByCard = [];

    public Player Player { get; }

    public CookingRecipePile(
        Player player,
        IEnumerable<CookingDishDefinition> recipes,
        bool upgradeNextDish
    ) : base(PileType.None)
    {
        Player = player;

        foreach (var recipe in recipes)
        {
            var previewCard = recipe.CreatePreviewCard().ToMutable();
            if (upgradeNextDish)
            {
                previewCard.UpgradeInternal();
            }

            previewCard.Owner = player;
            previewCard.BaseReplayCount = HaaLouLingCookingService.PendingDishReplayCount(player, recipe);
            SaveManager.Instance.MarkCardAsSeen(previewCard);
            _recipesByCard[previewCard] = recipe;
            AddInternal(previewCard, silent: true);
        }
    }

    public bool TryGetRecipe(CardModel card, out CookingDishDefinition recipe) =>
        _recipesByCard.TryGetValue(card, out recipe!);

    public void RefreshCookingEffects()
    {
        var upgraded = HaaLouLingCookingService.HasPendingDishUpgrade(Player);
        var changed = false;
        foreach (var card in Cards)
        {
            if (card.IsUpgraded != upgraded)
            {
                if (upgraded) card.UpgradeInternal();
                else card.DowngradeInternal();
                changed = true;
            }
            var replays = HaaLouLingCookingService.PendingDishReplayCount(Player, _recipesByCard[card]);
            if (card.BaseReplayCount != replays)
            {
                card.BaseReplayCount = replays;
                changed = true;
            }
        }
        if (changed) InvokeContentsChanged();
    }
}

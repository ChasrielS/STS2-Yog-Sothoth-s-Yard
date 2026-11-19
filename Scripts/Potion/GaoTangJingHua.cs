using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts.Card.HaaLouLingDish;
using YogSothothsYard.Scripts.Cooking;
using YogSothothsYard.Scripts.Shadow;

namespace YogSothothsYard.Scripts;

[RegisterPotion(typeof(HaaLouLingPotionPool))]
public sealed class GaoTangJingHua : HaaLouLingPotion
{
    protected override string ImageName => "gao_tang_jing_hua";
    public override PotionRarity Rarity => PotionRarity.Uncommon;
    public override TargetType TargetType => TargetType.AnyPlayer;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Dishes", 1)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HaaLouLingKeywords.ImmediateCookingTip];

    protected override async Task OnUse(PlayerChoiceContext context, Creature? target)
    {
        AssertValidForTargetedPotion(target);
        var player = target.Player!;
        var includesRare = ShadowRanks.Of(this) is 2 or 3;
        var choices = HaaLouLingCardSortOrder.SortRecipes(HaaLouLingCookingRecipes.AvailableFor(player)
            .Where(recipe => (includesRare || recipe.CreatePreviewCard().Rarity != CardRarity.Rare) &&
                HaaLouLingCookingService.CanStartRecipe(player, recipe)))
            .Select(recipe => player.Creature.CombatState!.CreateCard(recipe.CreatePreviewCard(), player)).ToList();
        if (choices.Count == 0) return;
        var selection = new CookingDishSelection { CompleteImmediately = true };
        foreach (var card in choices.Cast<HaaLouLingDishCard>())
        {
            card.IsCompletionChoice = true;
            card.CookingSelection = selection;
            card.SetCookingUpgrade(HaaLouLingCookingService.HasPendingDishUpgrade(player));
            card.BaseReplayCount = HaaLouLingCookingService.PendingDishReplayCount(player, HaaLouLingCookingRecipes.Get(card.Id)!);
        }

        var count = Math.Min(DynamicVars["Dishes"].IntValue, choices.Count);
        var prompt = new LocString("gameplay_ui", count == 1
            ? "YOG_SOTHOTH_S_YARD_DISH_COMPLETE_CHOICE_PROMPT"
            : "YOG_SOTHOTH_S_YARD_DISH_COMPLETE_TWO_CHOICE_PROMPT");
        var selected = await CardSelectCmd.FromSimpleGrid(context, choices, player, new CardSelectorPrefs(prompt, count));
        foreach (var card in selected)
            await HaaLouLingCookingService.CompleteDishImmediately(context, player,
                HaaLouLingCookingRecipes.Get(card.Id)!);
    }
}

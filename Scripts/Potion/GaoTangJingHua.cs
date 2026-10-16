using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts.Card.HaaLouLingDish;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterPotion(typeof(HaaLouLingPotionPool))]
public sealed class GaoTangJingHua : HaaLouLingPotion
{
    protected override string ImageName => "gao_tang_jing_hua";
    public override PotionRarity Rarity => PotionRarity.Uncommon;
    public override TargetType TargetType => TargetType.AnyPlayer;

    protected override async Task OnUse(PlayerChoiceContext context, Creature? target)
    {
        AssertValidForTargetedPotion(target);
        var player = target.Player!;
        var choices = HaaLouLingCardSortOrder.SortRecipes(HaaLouLingCookingRecipes.AvailableFor(player)
            .Where(recipe => recipe.CreatePreviewCard().Rarity != CardRarity.Rare))
            .Select(recipe => player.Creature.CombatState!.CreateCard(recipe.CreatePreviewCard(), player)).ToList();
        foreach (var card in choices.Cast<HaaLouLingDishCard>()) card.IsEffectChoice = true;

        var selected = (await CardSelectCmd.FromSimpleGrid(context, choices, player,
            new CardSelectorPrefs(new LocString("gameplay_ui", "YOG_SOTHOTH_S_YARD_DISH_EFFECT_CHOICE_PROMPT"), 1)))
            .FirstOrDefault();
        if (selected != null)
            await HaaLouLingCookingService.GainDishEffect(context, player,
                HaaLouLingCookingRecipes.Get(selected.Id)!, selected.IsUpgraded);
    }
}

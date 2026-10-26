using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Scaffolding.Content;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

public abstract class HaaLouLingIngredientCard : ModCardTemplate, IHaaLouLingIngredientCard
{
    private const string PortraitRoot = "res://images/packed/card_portraits/haa_lou_ling_food";
    protected abstract string PortraitFileName { get; }

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{PortraitRoot}/{PortraitFileName}.png"
    );

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => HaaLouLingCardSortOrder.SortRecipes(
            (IsMutable && Owner != null ? HaaLouLingCookingRecipes.AvailableFor(Owner) : HaaLouLingCookingRecipes.All)
            .Where(recipe => recipe.Ingredients.ContainsKey(Id)))
        .Select(recipe => HoverTipFactory.FromCard(recipe.CreatePreviewCard()));

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        HaaLouLingKeywords.Ingredient
    ];

    protected HaaLouLingIngredientCard(CardRarity rarity)
        : base(-1, CardType.None, rarity, TargetType.None, true)
    {
    }
}

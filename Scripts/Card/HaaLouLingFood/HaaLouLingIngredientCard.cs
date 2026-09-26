using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Scaffolding.Content;

namespace YogSothothsYard.Scripts;

public abstract class HaaLouLingIngredientCard : ModCardTemplate, IHaaLouLingIngredientCard
{
    private const string PortraitRoot = "res://images/packed/card_portraits/haa_lou_ling_food";

    protected abstract string PortraitFileName { get; }

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{PortraitRoot}/{PortraitFileName}.png"
    );

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        HaaLouLingKeywords.Ingredient
    ];

    protected HaaLouLingIngredientCard(CardRarity rarity)
        : base(-1, CardType.None, rarity, TargetType.None, true)
    {
    }
}

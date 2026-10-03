using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Scaffolding.Content;

namespace YogSothothsYard.Scripts.Card.HaaLouLingDish;

public abstract class HaaLouLingDishCard : ModCardTemplate, IHaaLouLingDishCard
{
    private const string PortraitRoot = "res://images/packed/card_portraits/haa_lou_ling_dish";
    private readonly int _cookingTurns;

    public bool IsEffectChoice { get; set; }

    protected abstract string PortraitFileName { get; }

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{PortraitRoot}/{PortraitFileName}.png"
    );

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        ModCardVars.Int("CookingTurns", _cookingTurns)
            .WithSharedTooltip("YOG_SOTHOTH_S_YARD_COOKING_TURNS")
    ];

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        description.Add("CookingTurnsReduced", IsUpgraded && DynamicVars["CookingTurns"].BaseValue < _cookingTurns);
    }

    protected HaaLouLingDishCard(
        CardType type,
        int cookingTurns,
        CardRarity rarity = CardRarity.Common
    )
        : base(
            -1,
            type,
            rarity,
            type == CardType.Power ? TargetType.Self : TargetType.None,
            true
    )
    {
        _cookingTurns = cookingTurns;
    }

}

using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Scaffolding.Content;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts.Card.HaaLouLingDish;

public abstract class HaaLouLingDishCard : ModCardTemplate, IHaaLouLingDishCard
{
    private const string PortraitRoot = "res://images/packed/card_portraits/haa_lou_ling_dish";
    private readonly int _cookingTurns;

    public bool IsEffectChoice { get; set; }
    public CookingDishSelection? CookingSelection { get; set; }
    public bool IsCookingChoice => CookingSelection != null;
    public bool IsCookingCompletion { get; set; }
    public bool IsRecipePreview { get; set; }

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
        description.Add("ShowCookingTurns", !IsCookingCompletion);
    }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        HaaLouLingCookingService.GainDishEffect(context, Owner, HaaLouLingCookingRecipes.Get(Id)!, IsUpgraded);

    protected override CardLocation GetResultLocationForCardPlay() =>
        new(Owner, PileType.None, CardPilePosition.Bottom);

    public override CardLocation ModifyCardPlayResultLocation(CardModel card, bool isAutoPlay, ResourceInfo resources, CardLocation location)
    {
        // 技能和能力菜肴都移出战斗，不接受腐化、回手等普通牌堆去向。
        if (card == this) location.pileType = PileType.None;
        return location;
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

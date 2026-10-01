using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public class FindFood : ModCardTemplate
{
    private const int energyCost = 1;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Basic;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://images/packed/card_portraits/haa_lou_ling/xun_zhao_shi_cai.png"
    );

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Innate
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(2)
    ];

    public FindFood() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var commonFoods = ModelDb.CardPool<HaaLouLingFoodPool>()
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .Where(card => card is IHaaLouLingIngredientCard && card.Rarity == CardRarity.Common)
            .ToList();

        if (commonFoods.Count == 0)
        {
            return;
        }

        var generatedFoods = CardFactory.GetForCombat(
            Owner,
            commonFoods,
            DynamicVars.Cards.IntValue,
            Owner.RunState.Rng.CombatCardGeneration
        ).ToList();

        await HaaLouLingCookingService.AddGeneratedIngredients(choiceContext, Owner, generatedFoods);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1);
    }
}

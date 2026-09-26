using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace YogSothothsYard.Scripts.Cooking;

public sealed class CookingDishDefinition
{
    public required ModelId DishModelId { get; init; }
    public required int CookingTurns { get; init; }
    public int? UpgradedCookingTurns { get; init; }
    public required string SlotTexturePath { get; init; }
    public required IReadOnlyDictionary<ModelId, int> Ingredients { get; init; }
    public required Func<CardModel> CreatePreviewCard { get; init; }
    public required Func<PlayerChoiceContext, Player, bool, Task> OnComplete { get; init; }

    public int GetCookingTurns(bool isUpgraded) =>
        isUpgraded && UpgradedCookingTurns.HasValue
            ? UpgradedCookingTurns.Value
            : CookingTurns;
}

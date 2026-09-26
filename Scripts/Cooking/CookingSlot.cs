using MegaCrit.Sts2.Core.Models;

namespace YogSothothsYard.Scripts.Cooking;

public sealed class CookingSlot
{
    public required ModelId DishModelId { get; init; }
    public required int RemainingTurns { get; set; }
    public required bool IsUpgraded { get; set; }
    public bool IsCopy { get; init; }
}

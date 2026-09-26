namespace YogSothothsYard.Scripts.Cooking;

public sealed class HaaLouLingCookingState
{
    public const int DefaultCapacity = 2;

    public int Capacity { get; set; } = DefaultCapacity;
    public List<CookingSlot> Slots { get; } = [];
    public int OctopusBallIngredientProgress { get; set; }
    public int BonusDishEffectCharges { get; set; }
    public int ShengYanManglePerTurn { get; set; }
}

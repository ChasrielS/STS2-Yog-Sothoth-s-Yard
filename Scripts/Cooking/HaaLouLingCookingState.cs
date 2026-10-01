namespace YogSothothsYard.Scripts.Cooking;

public sealed class HaaLouLingCookingState
{
    public const int DefaultCapacity = 2;

    public int Capacity { get; set; } = DefaultCapacity;
    public List<CookingSlot> Slots { get; } = [];
    public int OctopusBallIngredientProgress { get; set; }
    public int BonusDishEffectCharges { get; set; }
    public int ShengYanManglePerTurn { get; set; }
    public int KnifeSpentThisTurn { get; set; }
    public int CompletedAtTurnStart { get; set; }
    public bool ProcessingTurnStart { get; set; }
    public int CompletionDepth { get; set; }
    public HashSet<MegaCrit.Sts2.Core.Models.ModelId> CookedThisTurn { get; } = [];
    public Dictionary<MegaCrit.Sts2.Core.Models.ModelId, int> DishStartGains { get; } = [];
    public List<(MegaCrit.Sts2.Core.Models.CardModel Card, int Knife)> CardKnifeSnapshots { get; } = [];
    public List<(CookingDishDefinition Recipe, bool Upgraded)> PendingCopies { get; } = [];
}

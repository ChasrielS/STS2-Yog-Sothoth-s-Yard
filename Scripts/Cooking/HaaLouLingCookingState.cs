namespace YogSothothsYard.Scripts.Cooking;

public sealed class HaaLouLingCookingState
{
    public const int DefaultCapacity = 2;

    public int Capacity { get; set; } = DefaultCapacity;
    public List<CookingSlot> Slots { get; } = [];
    public int OctopusBallIngredientProgress { get; set; }
    public int ShengYanDebilitatePerTurn { get; set; }
    public int IngredientsUsedThisTurn { get; set; }
    public int IngredientsUsed { get; set; }
    public int DishesStartedThisTurn { get; set; }
    public event Action? RecipePreviewsChanged;
    public void RefreshRecipePreviews() => RecipePreviewsChanged?.Invoke();
    public bool ProcessingTurnStart { get; set; }
    public bool AwaitingNativeTurnStart { get; set; }
    public bool PlatingGrantedAtTurnStart { get; set; }
    public List<(int Amount, MegaCrit.Sts2.Core.Entities.Creatures.Creature Applier)> PendingTurnStartPoison { get; } = [];
    public HashSet<MegaCrit.Sts2.Core.Models.ModelId> CookedThisTurn { get; } = [];
    public Dictionary<MegaCrit.Sts2.Core.Models.ModelId, int> DishStartGains { get; } = [];
    public List<(MegaCrit.Sts2.Core.Models.CardModel Card, int Knife)> CardKnifeSnapshots { get; } = [];
}

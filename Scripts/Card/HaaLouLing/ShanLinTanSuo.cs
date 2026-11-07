using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class ShanLinTanSuo : HaaLouLingCard
{
    public ShanLinTanSuo() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1), new IntVar("Increase", 2)];

    private int _additionalIngredients;

    [SavedProperty]
    public int AdditionalIngredients
    {
        get => _additionalIngredients;
        set
        {
            AssertMutable();
            _additionalIngredients = value;
            DynamicVars.Cards.BaseValue = 1 + value;
        }
    }

    protected override void AfterDowngraded() => DynamicVars.Cards.BaseValue = 1 + AdditionalIngredients;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await HaaLouLingCookingService.AddRandomIngredients(context, Owner, DynamicVars.Cards.IntValue);
        var increase = DynamicVars["Increase"].IntValue;
        AdditionalIngredients += increase;
        if (DeckVersion is ShanLinTanSuo deck) deck.AdditionalIngredients += increase;
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Increase"].UpgradeValueBy(1);
    }
}

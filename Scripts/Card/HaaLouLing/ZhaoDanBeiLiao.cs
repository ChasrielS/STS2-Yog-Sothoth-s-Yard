using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Localization;
using YogSothothsYard.Scripts.Cooking;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class ZhaoDanBeiLiao : HaaLouLingCard
{
    public ZhaoDanBeiLiao() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amount", 1)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var choices = HaaLouLingCardSortOrder.SortIngredients(HaaLouLingCookingService.Ingredients(Owner))
            .Select(template => CombatState!.CreateCard(template, Owner)).ToList();
        var prompt = new LocString("cards", Id.Entry + ".selectionScreenPrompt");
        DynamicVars.AddTo(prompt);
        var selected = await CardSelectCmd.FromSimpleGrid(context, choices, Owner,
            new CardSelectorPrefs(prompt, DynamicVars["Amount"].IntValue));
        await HaaLouLingCookingService.AddGeneratedIngredients(context, Owner, selected);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Amount"].UpgradeValueBy(1);
    }
}

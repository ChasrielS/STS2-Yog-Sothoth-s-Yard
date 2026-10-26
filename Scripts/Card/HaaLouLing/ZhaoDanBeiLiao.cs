using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Localization;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class ZhaoDanBeiLiao : HaaLouLingCard
{
    public ZhaoDanBeiLiao() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amount", 1)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var firstPlay = !CombatManager.Instance.History.CardPlaysFinished.Any(entry =>
            entry.CardPlay.Card == this && entry.HappenedThisTurn(CombatState));
        var choices = HaaLouLingCardSortOrder.SortIngredients(HaaLouLingCookingService.Ingredients(Owner, nonRare: true))
            .Select(template => CombatState!.CreateCard(template, Owner)).ToList();
        var prompt = new LocString("cards", Id.Entry + ".selectionScreenPrompt");
        var selected = await CardSelectCmd.FromSimpleGrid(context, choices, Owner, new CardSelectorPrefs(prompt, 2));
        var cards = selected.SelectMany(card => Enumerable.Range(0, DynamicVars["Amount"].IntValue)
            .Select(_ => CombatState!.CreateCard(card.CanonicalInstance, Owner)));
        await HaaLouLingCookingService.AddGeneratedIngredients(context, Owner, cards);
        if (firstPlay) await CardPileCmd.Draw(context, 1, Owner);
    }

    protected override void OnUpgrade() => DynamicVars["Amount"].UpgradeValueBy(1);
}

using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class ShuangFen : HaaLouLingCard
{
    public ShuangFen() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var firstPlay = !CombatManager.Instance.History.CardPlaysFinished.Any(entry =>
            entry.CardPlay.Card == this && entry.HappenedThisTurn(CombatState));
        var selected = await HaaLouLingCardChoices.SelectIngredientKinds(context, this, DynamicVars.Cards.IntValue);
        var kinds = selected.Select(card => card.Id).ToHashSet();
        var copies = Entry.FoodPile.GetPile(Owner).Cards.Where(card => kinds.Contains(card.Id))
            .Select(card => card.CreateClone()).ToList();
        await HaaLouLingCookingService.AddGeneratedIngredients(context, Owner, copies);
        if (firstPlay) await CardPileCmd.Draw(context, 1, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1);
    }
}

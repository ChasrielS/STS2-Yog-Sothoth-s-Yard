using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Commands;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class TiaoJian : HaaLouLingCard
{
    public TiaoJian() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Ingredients", 3), new CardsVar(2)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CardPileCmd.Draw(context, DynamicVars.Cards.IntValue, Owner);
        var count = Entry.FoodPile.GetPile(Owner).Cards.Count;
        if (count == 0) return;
        var selected = (await HaaLouLingCardChoices.SelectOwnedIngredients(context, this, count)).ToList();
        await CardPileCmd.RemoveFromCombat(selected, false);
        await CardPileCmd.Draw(context, selected.Count / DynamicVars["Ingredients"].IntValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1);
        DynamicVars["Ingredients"].UpgradeValueBy(-1);
    }
}

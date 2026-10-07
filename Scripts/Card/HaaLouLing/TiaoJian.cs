using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Commands;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class TiaoJian : HaaLouLingCard
{
    public TiaoJian() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Ingredients", 3)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var selected = (await HaaLouLingCardChoices.SelectOwnedIngredients(context, this,
            Entry.FoodPile.GetPile(Owner).Cards.Count)).ToList();
        foreach (var card in selected) await CardPileCmd.RemoveFromCombat(card, false);
        await CardPileCmd.Draw(context, selected.Count / DynamicVars["Ingredients"].IntValue, Owner);
    }

    protected override void OnUpgrade() => DynamicVars["Ingredients"].UpgradeValueBy(-1);
}

using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Commands;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class DianDuZi : HaaLouLingCard
{
    public DianDuZi() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2), new EnergyVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var count = DynamicVars.Cards.IntValue;
        var selected = await HaaLouLingCardChoices.SelectOwnedIngredients(context, this, count, count, sortByQuantity: true);
        foreach (var card in selected) await CardPileCmd.RemoveFromCombat(card, false);
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1);
        DynamicVars.Energy.UpgradeValueBy(1);
    }
}

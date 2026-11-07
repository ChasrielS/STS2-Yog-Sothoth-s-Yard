using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class MangErBuLuan : HaaLouLingCard
{
    public MangErBuLuan() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2), new EnergyVar(1)];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CardPileCmd.Draw(context, DynamicVars.Cards.IntValue, Owner);
        await PlayerCmd.GainEnergy(HaaLouLingCookingService.GetState(Owner).Slots.Count * DynamicVars.Energy.IntValue, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}

using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class KaiYan : HaaLouLingCard
{
    public KaiYan() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amount", 0)];
    public override IEnumerable<CardKeyword> CanonicalKeywords => FormKeywords.Concat([CardKeyword.Innate]);
    protected override bool HasEnergyCostX => true;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (EnergyCost.CapturedXValue == HaaLouLingCookingService.GetState(Owner).Capacity) await Apply<GuoWanPiaoPenPower>(context, 1);
        await HaaLouLingCookingService.CookRandomDishes(context, Owner, ResolveEnergyXValue() + DynamicVars["Amount"].IntValue, rare: false);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Amount"].UpgradeValueBy(1);
    }
}

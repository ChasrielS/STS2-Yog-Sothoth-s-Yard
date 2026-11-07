using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class KaiYan : HaaLouLingCard
{
    public KaiYan() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amount", 0)];
    protected override bool HasEnergyCostX => true;
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HaaLouLingKeywords.ImmediateCookingTip];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var count = ResolveEnergyXValue() + DynamicVars["Amount"].IntValue;
        await HaaLouLingCookingService.CompleteRandomDishes(context, Owner, count, CardRarity.Uncommon);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Amount"].UpgradeValueBy(1);
    }
}

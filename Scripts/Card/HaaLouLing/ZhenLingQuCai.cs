using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class ZhenLingQuCai : HaaLouLingCard
{
    public ZhenLingQuCai() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amount", 5)];
    protected override bool UsesGuiYuan => true;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await GiveGuiYuan(context, play, DynamicVars["Amount"].IntValue);
        await HaaLouLingCookingService.AddRandomIngredients(context, Owner, 1, CardRarity.Uncommon);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Amount"].UpgradeValueBy(5);
    }
}

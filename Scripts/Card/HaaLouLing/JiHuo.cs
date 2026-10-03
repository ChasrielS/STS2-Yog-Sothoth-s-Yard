using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class JiHuo : HaaLouLingCard
{
    public JiHuo() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("CookingAdvance", 1)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await HaaLouLingCookingService.ChangeCookingTurns(context, Owner, -DynamicVars["CookingAdvance"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["CookingAdvance"].UpgradeValueBy(1);
    }
}

using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class LiHun : HaaLouLingCard
{
    public LiHun() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amount", 35)];

    protected override bool UsesGuiYuan => true;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await GiveGuiYuan(context, play, DynamicVars["Amount"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Amount"].UpgradeValueBy(15);
    }
}

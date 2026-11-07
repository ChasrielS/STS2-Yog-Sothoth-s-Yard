using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class WuMiZhiChui : HaaLouLingCard
{
    public WuMiZhiChui() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amount", 1)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Apply<MianFeiPengRenPower>(context, DynamicVars["Amount"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Amount"].UpgradeValueBy(1);
    }
}

using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class YiZhouWeiYin : HaaLouLingCard
{
    public YiZhouWeiYin() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amount", 2)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var power = await PowerCmd.Apply<YiZhouWeiYinPower>(context, Owner.Creature, DynamicVars["Amount"].IntValue, Owner.Creature, this);
        if (power != null && (ErosionRank == 3 || ErosionRank == 2 && IsUpgraded))
            power.DiscardCurse = true;
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Amount"].UpgradeValueBy(1);
    }
}

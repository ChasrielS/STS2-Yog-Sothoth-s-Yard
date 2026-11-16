using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class DaoGong : HaaLouLingCard
{
    public DaoGong() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }
    protected override bool UsesKnife => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amount", 1)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var amount = DynamicVars["Amount"].IntValue;
        var power = await PowerCmd.Apply<DaoGongPower>(context, Owner.Creature, amount, Owner.Creature, this);
        if (power == null) return;
        if (ErosionRank is 1 or 2) power.RareBonus += amount;
        if (ErosionRank == 2) power.UncommonBonus += amount;
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

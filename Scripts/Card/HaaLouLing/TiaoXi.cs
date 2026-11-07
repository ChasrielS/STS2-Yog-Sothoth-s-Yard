using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class TiaoXi : HaaLouLingCard
{
    public TiaoXi() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amount", 10), new EnergyVar(2)];

    protected override bool UsesGuiYuan => true;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await HaaLouLingCombatRules.GiveGuiYuan(context, Owner.Creature, DynamicVars["Amount"].IntValue, Owner.Creature, this);
        await Apply<EnergyNextTurnPower>(context, DynamicVars.Energy.IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Amount"].UpgradeValueBy(-5);
    }
}

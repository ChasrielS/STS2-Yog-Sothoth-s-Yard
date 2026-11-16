using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class GuiYuBenChu : HaaLouLingCard
{
    public GuiYuBenChu() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }
    protected override bool UsesGuiYuan => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amount", 0)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Apply<GuiYuBenChuPower>(context, 1);
        if (DynamicVars["Amount"].IntValue > 0)
            await HaaLouLingCombatRules.GiveGuiYuanToEnemies(context, Owner, DynamicVars["Amount"].IntValue, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

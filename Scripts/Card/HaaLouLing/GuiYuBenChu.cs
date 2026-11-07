using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class GuiYuBenChu : HaaLouLingCard
{
    public GuiYuBenChu() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }
    protected override bool UsesGuiYuan => true;

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) => Apply<GuiYuBenChuPower>(context, 1);

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

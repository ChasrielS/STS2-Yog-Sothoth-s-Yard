using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class ZhouRen : HaaLouLingCard
{
    public ZhouRen() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }
    protected override bool UsesKnife => true;
    protected override bool UsesGuiYuan => true;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (IsUpgraded) await Apply<ZhouRenPlusPower>(context, 1);
        else await Apply<ZhouRenPower>(context, 1);
    }
}

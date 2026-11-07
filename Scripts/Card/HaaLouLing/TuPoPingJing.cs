using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class TuPoPingJing : HaaLouLingCard
{
    public TuPoPingJing() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }
    protected override bool UsesKnife => true;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Apply<TuPoPingJingPower>(context, 1);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

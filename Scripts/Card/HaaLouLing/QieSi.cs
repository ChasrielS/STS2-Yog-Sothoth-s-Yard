using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class QieSi : HaaLouLingCard
{
    public QieSi() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(3, ValueProp.Move), new IntVar("Hits", 3)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Attack(context, play, DynamicVars["Hits"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Hits"].UpgradeValueBy(1);
    }
}

using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class DaoLuZaiXin : HaaLouLingCard
{
    public DaoLuZaiXin() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];
    protected override bool UsesKnifeForms => true;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        await Apply<DaoLuZaiXinPower>(context, DynamicVars.Cards.IntValue);

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}

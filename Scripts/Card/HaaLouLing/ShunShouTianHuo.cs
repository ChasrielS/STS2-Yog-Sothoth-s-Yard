using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class ShunShouTianHuo : HaaLouLingCard
{
    public ShunShouTianHuo() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new IntVar("Amount", 1), new IntVar("CookingAdvance", 1)];

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var amount = DynamicVars["Amount"].IntValue;
        return DynamicVars["CookingAdvance"].IntValue switch
        {
            3 => Apply<ShunShouTianHuoThreeTurnsPower>(context, amount),
            2 => Apply<ShunShouTianHuoTwoTurnsPower>(context, amount),
            _ => Apply<ShunShouTianHuoPower>(context, amount)
        };
    }

    protected override void OnUpgrade() => DynamicVars["Amount"].UpgradeValueBy(1);
}

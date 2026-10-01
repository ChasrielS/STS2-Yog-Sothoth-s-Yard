using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class YuShiZhuiJi : HaaLouLingCard
{
    public YuShiZhuiJi() : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(3, ValueProp.Move), new CalculationBaseVar(0), new CalculationExtraVar(1), new CalculatedVar("CalculatedHits").WithMultiplier(static (card, _) => HaaLouLingCookingService.GetState(card.Owner).KnifeSpentThisTurn)];
    protected override bool UsesKnife => true;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Attack(context, play, (int)((CalculatedVar)DynamicVars["CalculatedHits"]).Calculate(play.Target));
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1);
    }
}

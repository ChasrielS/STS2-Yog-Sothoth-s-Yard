using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class JiFeiSiSan : HaaLouLingCard
{
    public JiFeiSiSan() : base(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CalculationBaseVar(0), new ExtraDamageVar(1), new CalculatedDamageVar(ValueProp.Move).WithMultiplier(static (card, _) => HaaLouLingCookingService.CountIngredients<JiRou>(card.Owner))];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Attack(context, play, calculated: true);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(4);
    }
}

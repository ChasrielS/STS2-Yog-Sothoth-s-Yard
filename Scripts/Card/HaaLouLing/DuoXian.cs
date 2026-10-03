using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class DuoXian : HaaLouLingCard
{
    public DuoXian() : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(4, ValueProp.Move), new ExtraDamageVar(1),
        new CalculationBaseVar(0), new CalculationExtraVar(1),
        new CalculatedVar("CalculatedHits").WithMultiplier(static (card, _) => HaaLouLingCookingService.GetState(card.Owner).IngredientsUsedThisTurn)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var hits = (int)((CalculatedVar)DynamicVars["CalculatedHits"]).Calculate(play.Target);
        await Attack(context, play);
        if (hits > 0)
            await DamageCmd.Attack(DynamicVars.ExtraDamage.BaseValue).FromCard(this, play).Targeting(play.Target!)
                .WithHitCount(hits).WithHitVfxNode(HaaLouLingStrikeSlashVfx.Create).Execute(context);
    }

    protected override void OnUpgrade() => DynamicVars.ExtraDamage.UpgradeValueBy(1);
}

using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class ZhanLingGuiYuanPoWang : HaaLouLingCard
{
    public ZhanLingGuiYuanPoWang() : base(1, CardType.Attack, CardRarity.Ancient, TargetType.AnyEnemy) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CalculationBaseVar(12), new ExtraDamageVar(8),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(static (card, _) => HaaLouLingCombatRules.KnifeForCard(card))];
    protected override bool UsesKnife => true;
    protected override bool UsesGuiYuan => true;
    public override KnifeForm Form => KnifeForm.Finish;

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource, CardPlay? cardPlay) =>
        cardSource == this && props.IsPoweredAttack() && cardPlay?.Target != null && target != null && target != cardPlay.Target ? 0.5m : 1m;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var knife = await FinishKnife(context);
        await HaaLouLingFinisherVfx.Anticipate(Owner.Creature, knife, true);
        var attack = await DamageCmd.Attack(DynamicVars.CalculatedDamage).FromCard(this, play)
            .TargetingAllOpponents(CombatState!)
            .WithHitVfxNode(target => HaaLouLingFinisherVfx.CreateImpact(target, knife, true, target == play.Target))
            .WithHitFx(null, null, "blunt_attack.mp3").Execute(context);
        foreach (var results in attack.Results.SelectMany(hit => hit).GroupBy(result => result.Receiver))
            await HaaLouLingCombatRules.GiveGuiYuan(context, results.Key, (int)results.Sum(result => result.TotalDamage), Owner.Creature, this);
        await HaaLouLingFinisherVfx.ImpactPause(true);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.ExtraDamage.UpgradeValueBy(4);
    }
}

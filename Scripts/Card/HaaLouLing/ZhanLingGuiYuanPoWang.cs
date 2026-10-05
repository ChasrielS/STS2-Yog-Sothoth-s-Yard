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
[RegisterDustyTomeCard(typeof(HaaLouLing))]
public sealed class ZhanLingGuiYuanPoWang : HaaLouLingCard
{
    public ZhanLingGuiYuanPoWang() : base(1, CardType.Attack, CardRarity.Ancient, TargetType.AllEnemies) { }
    private Creature? _highestHpTarget;
    private Creature? HighestHpTarget => _highestHpTarget ?? CombatState?.HittableEnemies.OrderByDescending(enemy => enemy.CurrentHp).FirstOrDefault();
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CalculationBaseVar(7), new ExtraDamageVar(5),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(static (card, _) => HaaLouLingCombatRules.KnifeForCard(card)),
        new IntVar("ExtraHit", 6)];
    protected override bool UsesKnife => true;
    protected override bool UsesGuiYuan => true;
    public override KnifeForm Form => KnifeForm.Finish;

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource, CardPlay? cardPlay) =>
        cardSource == this && target != null && target == HighestHpTarget
            ? HaaLouLingCombatRules.KnifeForCard(this) * DynamicVars["ExtraHit"].BaseValue
            : 0m;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        _highestHpTarget = CombatState!.HittableEnemies.OrderByDescending(enemy => enemy.CurrentHp).FirstOrDefault();
        await FinishKnife(context);
        var attack = await Attack(context, play, calculated: true);
        _highestHpTarget = null;
        foreach (var results in attack.Results.SelectMany(hit => hit).GroupBy(result => result.Receiver))
            await HaaLouLingCombatRules.GiveGuiYuan(context, results.Key, (int)results.Sum(result => result.TotalDamage), Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(3);
        DynamicVars.ExtraDamage.UpgradeValueBy(2);
        DynamicVars["ExtraHit"].UpgradeValueBy(2);
    }
}

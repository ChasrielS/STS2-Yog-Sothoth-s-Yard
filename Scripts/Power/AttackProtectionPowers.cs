using System.Reflection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

public abstract class EnemyTurnProtectionPower : HaaLouLingPower
{
    protected bool IsEnemyAttack(Creature? target, ValueProp props, Creature? dealer) =>
        target == Owner && dealer?.Side == CombatSide.Enemy && props.IsPoweredAttack();
    public override async Task AfterSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Enemy) await PowerCmd.Remove(this);
    }
}

[RegisterPower]
public sealed class MianYiPower : EnemyTurnProtectionPower
{
    public override PowerStackType StackType => PowerStackType.Single;
    public override decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay) =>
        IsEnemyAttack(target, props, dealer) ? 0 : decimal.MaxValue;
}

[RegisterPower]
public sealed class YuanYangFengHuanPower : EnemyTurnProtectionPower
{
    private static readonly FieldInfo BaseDamage = typeof(AttackCommand).GetField("_damagePerHit", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private decimal _baseDamage;

    public override Task BeforeAttack(AttackCommand command)
    {
        if (command.Attacker?.Side == CombatSide.Enemy)
            _baseDamage = Math.Max(0, (decimal)BaseDamage.GetValue(command)!);
        return Task.CompletedTask;
    }

    public override decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay) =>
        IsEnemyAttack(target, props, dealer) ? 0 : decimal.MaxValue;

    public override async Task BeforeDamageReceived(PlayerChoiceContext context, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (!IsEnemyAttack(target, props, dealer) || Owner.HasPower<MianYiPower>()) return;
        Flash();
        await PowerCmd.Decrement(this);
        // 不带施加者，保留目标的易伤等受伤修正，不套用玩家的力量与攻击减伤。
        await CreatureCmd.Damage(context, dealer!, _baseDamage, ValueProp.Move, null, null, null);
    }
}

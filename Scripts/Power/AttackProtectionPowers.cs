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
using STS2RitsuLib.Patching.Models;

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
    public override string? CustomIconPath => "res://images/powers/mian_yi_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/mian_yi_power.png";
    public override PowerStackType StackType => PowerStackType.Single;
    public override decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay) =>
        IsEnemyAttack(target, props, dealer) ? 0 : decimal.MaxValue;
}

public abstract class YuanYangFengHuanBasePower : EnemyTurnProtectionPower
{
    public override string? CustomIconPath => "res://images/powers/yuan_yang_feng_huan_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/yuan_yang_feng_huan_power.png";
    private static readonly FieldInfo BaseDamage = typeof(AttackCommand).GetField("_damagePerHit", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private decimal _baseDamage;

    public override Task BeforeAttack(AttackCommand command)
    {
        if (command.Attacker?.Side == CombatSide.Enemy)
            _baseDamage = Math.Max(0, (decimal)BaseDamage.GetValue(command)!);
        return Task.CompletedTask;
    }

    // Keep the enemy's damage per hit, without applying the player's attack modifiers.
    protected Task Reflect(PlayerChoiceContext context, Creature attacker) =>
        CreatureCmd.Damage(context, attacker, _baseDamage, ValueProp.Move, null, null, null);
}

[RegisterPower]
public sealed class YuanYangFengHuanPower : YuanYangFengHuanBasePower
{
    public override decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay) =>
        IsEnemyAttack(target, props, dealer) ? 0 : decimal.MaxValue;

    public override async Task BeforeDamageReceived(PlayerChoiceContext context, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (!IsEnemyAttack(target, props, dealer) || Owner.HasPower<MianYiPower>() ||
            Owner.GetPower<YuanYangFengHuanIntentPower>()?.Covers(dealer) == true) return;
        Flash();
        await PowerCmd.Decrement(this);
        await Reflect(context, dealer!);
    }
}

[RegisterPower]
public sealed class YuanYangFengHuanIntentPower : YuanYangFengHuanBasePower
{
    private Creature? _attacker;

    internal bool Covers(Creature? dealer) => Amount > 0 && dealer?.Monster?.IsPerformingMove == true &&
        (_attacker == null || _attacker == dealer);

    public override decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay) =>
        IsEnemyAttack(target, props, dealer) && Covers(dealer) ? 0 : decimal.MaxValue;

    public override async Task BeforeDamageReceived(PlayerChoiceContext context, Creature target, decimal amount,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (!IsEnemyAttack(target, props, dealer) || !Covers(dealer) || Owner.HasPower<MianYiPower>()) return;
        if (_attacker == null)
        {
            _attacker = dealer;
            Flash();
        }
        await Reflect(context, dealer!);
    }

    internal async Task EndMove(Creature attacker)
    {
        if (_attacker != attacker) return;
        _attacker = null;
        await PowerCmd.Decrement(this);
    }
}

public sealed class YuanYangFengHuanIntentPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_yuan_yang_feng_huan_intent";
    public static ModPatchTarget[] GetTargets() => [PatchTarget.Method<MonsterModel>(nameof(MonsterModel.PerformMove))];

    public static void Postfix(MonsterModel __instance, ref Task __result)
    {
        var attacker = __instance.Creature;
        var protections = attacker.CombatState?.PlayerCreatures
            .Select(player => player.GetPower<YuanYangFengHuanIntentPower>()).OfType<YuanYangFengHuanIntentPower>().ToList();
        if (protections is { Count: > 0 }) __result = Complete(__result, attacker, protections);
    }

    private static async Task Complete(Task move, Creature attacker, IReadOnlyList<YuanYangFengHuanIntentPower> protections)
    {
        await move;
        foreach (var protection in protections)
            if (protection.Owner.Powers.Contains(protection)) await protection.EndMove(attacker);
    }
}

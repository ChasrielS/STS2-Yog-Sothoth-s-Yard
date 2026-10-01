using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

public abstract class GuiYuanBasePower : HaaLouLingPower
{
    protected abstract int AttackPenalty { get; }
    protected abstract int Vulnerability { get; }
    protected abstract int DamageLimit { get; }
    public override PowerType Type => PowerType.Debuff;
    public override LocString Description
    {
        get
        {
            var text = base.Description;
            text.Add("Amount", Amount);
            text.Add("AttackPenalty", AttackPenalty * ((Amount >= 10 ? 1 : 0) + (Amount >= 50 ? 1 : 0)));
            text.Add("Vulnerability", Vulnerability * ((Amount >= 20 ? 1 : 0) + (Amount >= 70 ? 1 : 0)));
            text.Add("Amplified", Amount >= 30);
            text.Add("AtMaximum", Amount >= 100);
            text.Add("DamageLimit", DamageLimit);
            return text;
        }
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (!props.IsPoweredAttack()) return 1;
        var modifier = 1m;
        if (dealer == Owner)
            modifier *= 1m - AttackPenalty * ((Amount >= 10 ? 1 : 0) + (Amount >= 50 ? 1 : 0)) / 100m;
        if (target == Owner)
            modifier *= 1m + Vulnerability * ((Amount >= 20 ? 1 : 0) + (Amount >= 70 ? 1 : 0)) / 100m;
        return modifier;
    }

    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount, Creature? applier, out decimal modifiedAmount)
    {
        modifiedAmount = amount;
        if (target != Owner || canonicalPower is not GuiYuanBasePower) return false;
        if (amount > 0)
        {
            if (Amount >= 30) amount = decimal.Floor(amount * 1.5m);
            modifiedAmount = Math.Min(amount, Math.Max(0, 100 - Amount));
        }
        else modifiedAmount = Math.Max(amount, -Amount);
        return modifiedAmount != amount || Amount >= 30 && amount > 0;
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner) || AmountOnTurnStart < 100) return;
        Flash();
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner,
            Math.Min(DamageLimit, (Owner.MaxHp + 3) / 4), ValueProp.Unpowered, Owner);
    }
}

[RegisterPower]
public sealed class GuiYuanPower : GuiYuanBasePower
{
    protected override int AttackPenalty => 10;
    protected override int Vulnerability => 20;
    protected override int DamageLimit => 100;
}

[RegisterPower]
public sealed class GuiYuanPlusPower : GuiYuanBasePower
{
    protected override int AttackPenalty => 15;
    protected override int Vulnerability => 30;
    protected override int DamageLimit => 120;
}

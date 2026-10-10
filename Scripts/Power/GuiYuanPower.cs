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
    public override string? CustomIconPath => "res://images/powers/gui_yuan_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/gui_yuan_power.png";
    protected abstract int AttackPenalty { get; }
    protected abstract int Vulnerability { get; }
    protected abstract decimal ExcessHpLossPerLayer { get; }
    protected abstract int HpLossLimitPercent { get; }
    private decimal _pendingHpLoss;
    private int EffectMultiplier => Amount >= 100 ? 2 : 1;
    private decimal HpLossPercent => 50m + Math.Max(0, Amount - 100) * ExcessHpLossPerLayer;
    private int CurrentAttackPenalty => AttackPenalty * ((Amount >= 10 ? 1 : 0) + (Amount >= 50 ? 1 : 0)) * EffectMultiplier;
    private int CurrentVulnerability => Vulnerability * ((Amount >= 20 ? 1 : 0) + (Amount >= 70 ? 1 : 0)) * EffectMultiplier;
    public override PowerType Type => PowerType.Debuff;
    public override LocString Description
    {
        get
        {
            var text = base.Description;
            text.Add("Amount", Amount);
            text.Add("AttackPenalty", CurrentAttackPenalty);
            text.Add("Vulnerability", CurrentVulnerability);
            text.Add("Amplified", Amount >= 30);
            text.Add("Amplification", 25 * EffectMultiplier);
            text.Add("AtMaximum", Amount >= 100);
            text.Add("HpLossPercent", HpLossPercent);
            text.Add("HpLossLimitPercent", HpLossLimitPercent);
            return text;
        }
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (!props.IsPoweredAttack()) return 1;
        var modifier = 1m;
        if (dealer == Owner)
            modifier *= 1m - CurrentAttackPenalty / 100m;
        if (target == Owner)
            modifier *= 1m + CurrentVulnerability / 100m;
        return modifier;
    }

    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount, Creature? applier, out decimal modifiedAmount)
    {
        modifiedAmount = amount;
        if (target != Owner || canonicalPower is not GuiYuanBasePower) return false;
        if (amount > 0)
        {
            var gained = Amount >= 30 ? decimal.Floor(amount * (1m + 0.25m * EffectMultiplier)) : amount;
            if (Amount >= 100)
                _pendingHpLoss = decimal.Floor(Math.Min(gained * HpLossPercent / 100m,
                    Owner.MaxHp * (HpLossLimitPercent / 100m)));
            modifiedAmount = HaaLouLingCombatRules.CanExceedGuiYuanLimit(applier)
                ? gained : Math.Min(gained, Math.Max(0, 100 - Amount));
        }
        else modifiedAmount = Math.Max(amount, -Amount);
        return modifiedAmount != amount || _pendingHpLoss > 0;
    }

    public override async Task AfterModifyingPowerAmountReceived(PowerModel power)
    {
        if (_pendingHpLoss <= 0) return;
        var loss = _pendingHpLoss;
        _pendingHpLoss = 0;
        Flash();
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner, loss,
            ValueProp.Unblockable | ValueProp.Unpowered, null, null);
    }
}

[RegisterPower]
public sealed class GuiYuanPower : GuiYuanBasePower
{
    protected override int AttackPenalty => 10;
    protected override int Vulnerability => 20;
    protected override decimal ExcessHpLossPerLayer => 0.5m;
    protected override int HpLossLimitPercent => 10;
}

[RegisterPower]
public sealed class GuiYuanPlusPower : GuiYuanBasePower
{
    protected override int AttackPenalty => 15;
    protected override int Vulnerability => 30;
    protected override decimal ExcessHpLossPerLayer => 1m;
    protected override int HpLossLimitPercent => 20;
}

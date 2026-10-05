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
    private decimal _pendingDamage;
    private Creature? _pendingDealer;
    private int EffectMultiplier => Amount >= 100 ? 2 : 1;
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
            text.Add("Amplification", 50 * EffectMultiplier);
            text.Add("AtMaximum", Amount >= 100);
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
            if (Amount >= 100)
            {
                _pendingDamage = decimal.Floor(amount * 2 * 0.5m);
                _pendingDealer = applier;
                modifiedAmount = 0;
                return true;
            }
            if (Amount >= 30) amount = decimal.Floor(amount * 1.5m);
            modifiedAmount = Math.Min(amount, Math.Max(0, 100 - Amount));
        }
        else modifiedAmount = Math.Max(amount, -Amount);
        return modifiedAmount != amount || Amount >= 30 && amount > 0;
    }

    public override async Task AfterModifyingPowerAmountReceived(PowerModel power)
    {
        if (_pendingDamage <= 0) return;
        var damage = _pendingDamage;
        var dealer = _pendingDealer;
        _pendingDamage = 0;
        _pendingDealer = null;
        Flash();
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner, damage, ValueProp.Move, dealer, null, null);
    }
}

[RegisterPower]
public sealed class GuiYuanPower : GuiYuanBasePower
{
    protected override int AttackPenalty => 10;
    protected override int Vulnerability => 20;
}

[RegisterPower]
public sealed class GuiYuanPlusPower : GuiYuanBasePower
{
    protected override int AttackPenalty => 15;
    protected override int Vulnerability => 30;
}

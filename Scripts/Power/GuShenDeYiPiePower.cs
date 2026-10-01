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

[RegisterPower]
public sealed class GuShenDeYiPiePower : HaaLouLingPower
{
    public override PowerType Type => PowerType.Debuff;
    public override string? CustomIconPath => "res://images/powers/gu_shen_de_yi_pie_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/gu_shen_de_yi_pie_power.png";
    private int AttackPenalty => new[] { 0, 10, 25, 50, 90 }[Math.Clamp(Amount, 0, 4)];
    private int IncomingPenalty => new[] { 0, 5, 15, 30, 50 }[Math.Clamp(Amount, 0, 4)];
    public override LocString Description
    {
        get
        {
            var text = base.Description;
            text.Add("AttackPenalty", AttackPenalty);
            text.Add("IncomingPenalty", IncomingPenalty);
            text.Add("AtMaximum", Amount >= 4);
            return text;
        }
    }
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (!props.IsPoweredAttack()) return 1;
        return (dealer == Owner ? 1 - AttackPenalty / 100m : 1) * (target == Owner ? 1 + IncomingPenalty / 100m : 1);
    }
    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount, Creature? applier, out decimal modifiedAmount)
    {
        modifiedAmount = amount;
        if (target != Owner || canonicalPower is not GuShenDeYiPiePower) return false;
        modifiedAmount = Math.Clamp(amount, 0, Math.Max(0, 4 - Amount));
        return modifiedAmount != amount;
    }
}

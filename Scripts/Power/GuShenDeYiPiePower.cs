using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace YogSothothsYard.Scripts;

[RegisterPower]
public sealed class GuShenDeYiPiePower : ModPowerTemplate
{
    public const int FailureChancePerStack = 20;

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string? CustomIconPath =>
        "res://images/powers/gu_shen_de_yi_pie_power_icon.png";
    public override string? CustomBigIconPath =>
        "res://images/powers/gu_shen_de_yi_pie_power.png";

    public override LocString Description
    {
        get
        {
            var description = base.Description;
            description.Add(
                "Chance",
                Math.Clamp(Amount * FailureChancePerStack, 0, 100)
            );
            return description;
        }
    }

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount
    )
    {
        modifiedAmount = amount;
        if (target != Owner || canonicalPower is not GuShenDeYiPiePower || amount >= 0)
        {
            return false;
        }

        modifiedAmount = 0;
        Flash();
        return true;
    }

    public void FlashEffect() => Flash();
}

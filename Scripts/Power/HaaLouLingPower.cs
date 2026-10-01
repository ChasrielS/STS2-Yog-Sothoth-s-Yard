using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Scaffolding.Content;

namespace YogSothothsYard.Scripts;

public abstract class HaaLouLingPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string? CustomIconPath => "res://images/packed/combat_ui/food_pile.png";
    public override string? CustomBigIconPath => CustomIconPath;
    public void FlashEffect() => Flash();
}

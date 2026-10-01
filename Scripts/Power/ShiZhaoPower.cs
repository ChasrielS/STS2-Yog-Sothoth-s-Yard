using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Combat.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterPower]
public sealed class ShiZhaoPower : ModTemporaryAppliedPowerTemplate<ShiZhao, DexterityPower>
{
    public override string? CustomIconPath => "res://images/packed/combat_ui/food_pile.png";
    public override string? CustomBigIconPath => CustomIconPath;
}

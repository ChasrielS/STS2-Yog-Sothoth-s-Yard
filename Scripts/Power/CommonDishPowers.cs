using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace YogSothothsYard.Scripts;

public abstract class CommonDishPower : ModPowerTemplate
{
    protected abstract string IconFileName { get; }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string? CustomIconPath => $"res://images/powers/{IconFileName}_power_icon.png";
    public override string? CustomBigIconPath => $"res://images/powers/{IconFileName}_power.png";

    public void FlashEffect() => Flash();
}

[RegisterPower]
public sealed class ChangFenPower : CommonDishPower
{
    protected override string IconFileName => "chang_fen";
}

[RegisterPower]
public sealed class ChaoHeFenPower : CommonDishPower
{
    protected override string IconFileName => "chao_he_fen";
}

[RegisterPower]
public sealed class BoBoJiPower : CommonDishPower
{
    protected override string IconFileName => "bo_bo_ji";
}

[RegisterPower]
public sealed class XueHuaJiNaoPower : CommonDishPower
{
    protected override string IconFileName => "xue_hua_ji_nao";
}

[RegisterPower]
public sealed class MaYiShangShuPower : CommonDishPower
{
    protected override string IconFileName => "ma_yi_shang_shu";
}

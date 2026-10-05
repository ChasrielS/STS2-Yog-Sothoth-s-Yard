using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace YogSothothsYard.Scripts;

public abstract class CommonDishPower : DishPower
{
    protected abstract string IconFileName { get; }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string? CustomIconPath => $"res://images/powers/{IconFileName}_power_icon.png";
    public override string? CustomBigIconPath => $"res://images/powers/{IconFileName}_power.png";

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
public sealed class SuShiLengPanPower : CommonDishPower
{
    protected override string IconFileName => "su_shi_leng_pan";
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.ReplayDynamic, new DynamicVar("Times", 1))];
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

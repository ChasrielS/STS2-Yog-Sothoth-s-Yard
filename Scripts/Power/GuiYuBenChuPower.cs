using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace YogSothothsYard.Scripts;

[RegisterPower]
public sealed class GuiYuBenChuPower : HaaLouLingPower
{
    public override string? CustomIconPath => "res://images/powers/gui_yu_ben_chu_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/gui_yu_ben_chu_power.png";
    public override PowerStackType StackType => PowerStackType.Single;
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        new[] { HaaLouLingKeywords.KeywordId("GuiYuan") }.ToHoverTips();
}

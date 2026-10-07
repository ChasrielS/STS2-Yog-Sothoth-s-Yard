using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace YogSothothsYard.Scripts;

[RegisterPower]
public sealed class ZhouHaiWuYaPower : HaaLouLingPower
{
    public override string? CustomIconPath => "res://images/powers/zhou_hai_wu_ya_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/zhou_hai_wu_ya_power.png";
    public override PowerStackType StackType => PowerStackType.Single;
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        new[] { HaaLouLingKeywords.KeywordId("GuiYuan") }.ToHoverTips();
}

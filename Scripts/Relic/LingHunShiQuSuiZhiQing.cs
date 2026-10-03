using MegaCrit.Sts2.Core.Entities.Relics;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterRelic(typeof(HaaLouLingRelicPool))]
public sealed class LingHunShiQuSuiZhiQing : HaaLouLingRelic
{
    protected override string ImageName => "ling_hun_shi_qu_sui_zhi_qing";
    public override RelicRarity Rarity => RelicRarity.Starter;
}

using MegaCrit.Sts2.Core.Entities.Relics;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterRelic(typeof(HaaLouLingRelicPool))]
public sealed class DaNuoGuiMian : HaaLouLingRelic
{
    public override RelicRarity Rarity => RelicRarity.Shop;
}

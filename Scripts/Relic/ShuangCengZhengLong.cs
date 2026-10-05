using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterRelic(typeof(HaaLouLingRelicPool))]
public sealed class ShuangCengZhengLong : HaaLouLingRelic
{
    protected override string ImageName => "shuang_ceng_zheng_long";
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        new HoverTip(new LocString("static_hover_tips", "YOG_SOTHOTH_S_YARD_COOKING_SLOT.title"),
            new LocString("static_hover_tips", "YOG_SOTHOTH_S_YARD_COOKING_SLOT.description"))
    ];
    public override RelicRarity Rarity => RelicRarity.Rare;
    public override async Task BeforeCombatStart()
    {
        Flash();
        await PowerCmd.Apply<GuoWanPiaoPenPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, 1, Owner.Creature, null);
    }
}

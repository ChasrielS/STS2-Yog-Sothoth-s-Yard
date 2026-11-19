using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterRelic(typeof(HaaLouLingRelicPool))]
public sealed class LaoZhenBan : HaaLouLingRelic
{
    protected override string ImageName => "lao_zhen_ban";
    public override RelicRarity Rarity => RelicRarity.Rare;
    protected override IEnumerable<string> RegisteredKeywordIds => [HaaLouLingKeywords.KeywordId("DaoShi")];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Slots", 1), new IntVar("KnifeCap", 1)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        new HoverTip(new LocString("static_hover_tips", "YOG_SOTHOTH_S_YARD_COOKING_SLOT.title"),
            new LocString("static_hover_tips", "YOG_SOTHOTH_S_YARD_COOKING_SLOT.description"))
    ];

    public override async Task BeforeCombatStart()
    {
        Flash();
        var context = new ThrowingPlayerChoiceContext();
        await PowerCmd.Apply<GuoWanPiaoPenPower>(context, Owner.Creature, DynamicVars["Slots"].IntValue, Owner.Creature, null);
        await PowerCmd.Apply<DaoFaJingJinPower>(context, Owner.Creature, DynamicVars["KnifeCap"].IntValue, Owner.Creature, null);
    }
}

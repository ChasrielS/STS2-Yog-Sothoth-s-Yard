using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts.Shadow;

namespace YogSothothsYard.Scripts;

[RegisterRelic(typeof(HaaLouLingRelicPool))]
public sealed class TongBingChaoShao : HaaLouLingRelic
{
    protected override string ImageName => "tong_bing_chao_shao";
    protected override IEnumerable<string> RegisteredKeywordIds => [HaaLouLingKeywords.KeywordId("Ingredient")];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.Static(StaticHoverTip.Block)];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Threshold", 10), new BlockVar(5m, ValueProp.Unpowered)];
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    [SavedProperty]
    public int Progress { get; set; }
    public override bool ShowCounter => true;
    public override int DisplayAmount => Progress;
    internal async Task IngredientsUsed(PlayerChoiceContext context, int count)
    {
        var threshold = DynamicVars["Threshold"].IntValue;
        var erosion = ShadowRelicRules.TryGet(this, out var rule) ? rule.Amount : 0;
        var total = Progress + count;
        Progress = total % threshold;
        InvokeDisplayAmountChanged();
        for (var i = 0; i < total / threshold; i++)
        {
            Flash();
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, null);
            await ShadowRelicRules.Charge(this, erosion, context);
        }
    }
}

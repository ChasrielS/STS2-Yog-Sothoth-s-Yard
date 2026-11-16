using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts.Cooking;
using YogSothothsYard.Scripts.Shadow;

namespace YogSothothsYard.Scripts;

[RegisterRelic(typeof(HaaLouLingRelicPool))]
public sealed class TongBingChaoShao : HaaLouLingRelic
{
    protected override string ImageName => "tong_bing_chao_shao";
    protected override IEnumerable<string> RegisteredKeywordIds => [HaaLouLingKeywords.KeywordId("Ingredient")];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Threshold", 12), new IntVar("Damage", 6)];
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    [SavedProperty]
    public int Progress { get; set; }
    public override bool ShowCounter => true;
    public override int DisplayAmount => Progress;
    public async Task IngredientsGained(PlayerChoiceContext context, int count)
    {
        var threshold = DynamicVars["Threshold"].IntValue;
        var damage = DynamicVars["Damage"].IntValue;
        var erosion = ShadowRelicRules.TryGet(this, out var rule) ? rule.Amount : 0;
        var total = Progress + count;
        Progress = total % threshold;
        InvokeDisplayAmountChanged();
        for (var i = 0; i < total / threshold; i++)
        {
            Flash();
            await HaaLouLingCookingService.DamageAllEnemies(context, Owner, damage);
            await ShadowRelicRules.Charge(this, erosion, context);
        }
    }
}

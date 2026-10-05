using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterRelic(typeof(HaaLouLingRelicPool))]
public sealed class TongBingChaoShao : HaaLouLingRelic
{
    protected override string ImageName => "tong_bing_chao_shao";
    protected override IEnumerable<string> RegisteredKeywordIds => [HaaLouLingKeywords.KeywordId("Ingredient")];
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    [SavedProperty]
    public int Progress { get; set; }
    public override bool ShowCounter => true;
    public override int DisplayAmount => Progress;
    public async Task IngredientsGained(PlayerChoiceContext context, int count)
    {
        var total = Progress + count;
        Progress = total % 12;
        InvokeDisplayAmountChanged();
        for (var i = 0; i < total / 12; i++)
        {
            Flash();
            await HaaLouLingCookingService.DamageAllEnemies(context, Owner, 6);
        }
    }
}

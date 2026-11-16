using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterRelic(typeof(HaaLouLingRelicPool))]
public sealed class SuiShenCaiLan : HaaLouLingRelic
{
    protected override string ImageName => "sui_shen_cai_lan";
    protected override IEnumerable<string> RegisteredKeywordIds => [HaaLouLingKeywords.KeywordId("Ingredient")];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amount", 3)];
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    public override async Task BeforeCombatStart()
    {
        Flash();
        await HaaLouLingCookingService.AddRandomIngredients(new ThrowingPlayerChoiceContext(), Owner,
            DynamicVars["Amount"].IntValue, nonRare: true);
    }
}

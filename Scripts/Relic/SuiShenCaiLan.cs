using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts.Cooking;
using YogSothothsYard.Scripts.Shadow;

namespace YogSothothsYard.Scripts;

[RegisterRelic(typeof(HaaLouLingRelicPool))]
public sealed class SuiShenCaiLan : HaaLouLingRelic
{
    protected override string ImageName => "sui_shen_cai_lan";
    protected override IEnumerable<string> RegisteredKeywordIds => [HaaLouLingKeywords.KeywordId("Ingredient")];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amount", 2)];
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    public override async Task AfterSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || !Owner.Creature.IsAlive || !participants.Contains(Owner.Creature)) return;
        Flash();
        await HaaLouLingCookingService.AddRandomIngredients(context, Owner, DynamicVars["Amount"].IntValue, nonRare: true);
        if (ShadowRelicRules.TryGet(this, out var rule)) await ShadowRelicRules.Charge(this, rule.Amount, context);
    }
}

using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterPotion(typeof(HaaLouLingPotionPool))]
public sealed class XingShenTang : HaaLouLingPotion
{
    public override PotionRarity Rarity => PotionRarity.Common;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amount", 2)];
    protected override IEnumerable<string> RegisteredKeywordIds => [HaaLouLingKeywords.KeywordId("DaoShi")];
    protected override Task OnUse(PlayerChoiceContext context, Creature? target) =>
        HaaLouLingCombatRules.GainKnife(context, Owner, DynamicVars["Amount"].IntValue);
}

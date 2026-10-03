using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterPotion(typeof(HaaLouLingPotionPool))]
public sealed class GuiYuanFuShui : HaaLouLingPotion
{
    protected override string ImageName => "gui_yuan_fu_shui";
    public override PotionRarity Rarity => PotionRarity.Rare;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amount", 30)];
    protected override IEnumerable<string> RegisteredKeywordIds => [HaaLouLingKeywords.KeywordId("GuiYuan")];
    protected override Task OnUse(PlayerChoiceContext context, Creature? target) =>
        HaaLouLingCombatRules.GiveGuiYuanToEnemies(context, Owner, DynamicVars["Amount"].IntValue);
}

using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterPotion(typeof(HaaLouLingPotionPool))]
public sealed class GaoTangJingHua : HaaLouLingPotion
{
    protected override string ImageName => "gao_tang_jing_hua";
    public override PotionRarity Rarity => PotionRarity.Uncommon;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amount", 5)];
    protected override Task OnUse(PlayerChoiceContext context, Creature? target) =>
        HaaLouLingCookingService.AddRandomIngredients(context, Owner, DynamicVars["Amount"].IntValue, CardRarity.Uncommon);
}

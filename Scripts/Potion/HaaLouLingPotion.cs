using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Potions;
using STS2RitsuLib.Scaffolding.Content;

namespace YogSothothsYard.Scripts;

public abstract class HaaLouLingPotion : ModPotionTemplate
{
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;
    public override PotionAssetProfile AssetProfile => new(
        ImagePath: "res://images/packed/combat_ui/food_pile.png",
        OutlinePath: "res://images/packed/combat_ui/food_pile.png");
}

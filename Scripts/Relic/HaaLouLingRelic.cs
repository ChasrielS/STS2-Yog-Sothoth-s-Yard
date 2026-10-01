using STS2RitsuLib.Scaffolding.Content;

namespace YogSothothsYard.Scripts;

public abstract class HaaLouLingRelic : ModRelicTemplate
{
    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://images/packed/combat_ui/food_pile.png",
        IconOutlinePath: "res://images/packed/combat_ui/food_pile.png",
        BigIconPath: "res://images/packed/combat_ui/food_pile.png");
}

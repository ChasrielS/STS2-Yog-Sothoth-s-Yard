using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Potions;
using STS2RitsuLib.Scaffolding.Content;

namespace YogSothothsYard.Scripts;

public abstract class HaaLouLingPotion : ModPotionTemplate
{
    protected abstract string ImageName { get; }
    public string BigImagePath => $"res://images/potions/{ImageName}.png";
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;
    public override PotionAssetProfile AssetProfile => new(
        ImagePath: $"res://images/potions/{ImageName}_icon.png",
        OutlinePath: $"res://images/potions/{ImageName}_outline.png");
}

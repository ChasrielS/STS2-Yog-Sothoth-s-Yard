using STS2RitsuLib.Scaffolding.Content;

namespace YogSothothsYard.Scripts;

public abstract class HaaLouLingRelic : ModRelicTemplate
{
    protected abstract string ImageName { get; }
    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"res://images/relics/{ImageName}_icon.png",
        IconOutlinePath: $"res://images/relics/{ImageName}_outline.png",
        BigIconPath: $"res://images/relics/{ImageName}.png");
}

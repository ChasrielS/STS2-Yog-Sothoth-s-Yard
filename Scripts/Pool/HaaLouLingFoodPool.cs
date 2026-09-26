using Godot;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

namespace YogSothothsYard.Scripts;

[RegisterSharedCardPool]
public class HaaLouLingFoodPool : TypeListCardPoolModel
{
    public override string Title => "haa_lou_ling_food";
    public override string EnergyColorName => "haa_lou_ling";

    public override string? TextEnergyIconPath => "res://images/packed/sprite_fonts/haa_lou_ling_energy_icon.png";
    public override string? BigEnergyIconPath => "res://images/packed/sprite_fonts/haa_lou_ling_energy_icon_big.png";

    public override Color DeckEntryCardColor => new(0.18f, 0.70f, 0.62f);
    public override Color EnergyOutlineColor => new(0.03f, 0.33f, 0.29f);

    private static readonly Material? _poolFrameMaterial =
        MaterialUtils.CreateReplaceHueShaderMaterial(0.31f, 0.62f, 0.44f);

    public override Material? PoolFrameMaterial => _poolFrameMaterial;

    public override bool IsColorless => false;
}

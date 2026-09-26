using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class LuoRou : HaaLouLingIngredientCard
{
    protected override string PortraitFileName => "luo_rou";
    public LuoRou() : base(CardRarity.Uncommon) { }
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class Sun : HaaLouLingIngredientCard
{
    protected override string PortraitFileName => "sun";
    public Sun() : base(CardRarity.Uncommon) { }
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class HuaJiao : HaaLouLingIngredientCard
{
    protected override string PortraitFileName => "hua_jiao";
    public HuaJiao() : base(CardRarity.Uncommon) { }
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class YaRou : HaaLouLingIngredientCard
{
    protected override string PortraitFileName => "ya_rou";
    public YaRou() : base(CardRarity.Uncommon) { }
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class XieRou : HaaLouLingIngredientCard
{
    protected override string PortraitFileName => "xie_rou";
    public XieRou() : base(CardRarity.Uncommon) { }
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class DouFu : HaaLouLingIngredientCard
{
    protected override string PortraitFileName => "dou_fu";
    public DouFu() : base(CardRarity.Uncommon) { }
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class Tang : HaaLouLingIngredientCard
{
    protected override string PortraitFileName => "tang";
    public Tang() : base(CardRarity.Uncommon) { }
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class Yan : HaaLouLingIngredientCard
{
    protected override string PortraitFileName => "yan";
    public Yan() : base(CardRarity.Uncommon) { }
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class XiaRou : HaaLouLingIngredientCard
{
    protected override string PortraitFileName => "xia_rou";
    public XiaRou() : base(CardRarity.Uncommon) { }
}

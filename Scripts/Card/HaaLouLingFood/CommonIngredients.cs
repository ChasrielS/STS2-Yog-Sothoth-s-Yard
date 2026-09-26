using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class JiRou : HaaLouLingIngredientCard
{
    protected override string PortraitFileName => "ji_rou";
    public JiRou() : base(CardRarity.Common) { }
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class Mi : HaaLouLingIngredientCard
{
    protected override string PortraitFileName => "mi";
    public Mi() : base(CardRarity.Common) { }
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class QiGuaiDeRou : HaaLouLingIngredientCard
{
    protected override string PortraitFileName => "qi_guai_de_rou";
    public QiGuaiDeRou() : base(CardRarity.Common) { }
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class YuRou : HaaLouLingIngredientCard
{
    protected override string PortraitFileName => "yu_rou";
    public YuRou() : base(CardRarity.Common) { }
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class ZhuRou : HaaLouLingIngredientCard
{
    protected override string PortraitFileName => "zhu_rou";
    public ZhuRou() : base(CardRarity.Common) { }
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class LaJiao : HaaLouLingIngredientCard
{
    protected override string PortraitFileName => "la_jiao";
    public LaJiao() : base(CardRarity.Common) { }
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class LvYeCai : HaaLouLingIngredientCard
{
    protected override string PortraitFileName => "lv_ye_cai";
    public LvYeCai() : base(CardRarity.Common) { }
}

[RegisterCard(typeof(HaaLouLingFoodPool))]
public sealed class QingJiao : HaaLouLingIngredientCard
{
    protected override string PortraitFileName => "qing_jiao";
    public QingJiao() : base(CardRarity.Common) { }
}

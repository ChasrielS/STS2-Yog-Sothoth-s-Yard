using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class WanXiangGuiYuan : HaaLouLingCard
{
    public WanXiangGuiYuan() : base(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6, ValueProp.Move), new IntVar("GuiYuan", 6), new IntVar("Amount", 0)];
    protected override bool UsesGuiYuan => true;
    protected override bool HasEnergyCostX => true;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var count = ResolveEnergyXValue();
        await Attack(context, play, count);
        for (var i = 0; i < count + DynamicVars["Amount"].IntValue; i++) await GiveGuiYuan(context, play, DynamicVars["GuiYuan"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);
        DynamicVars["GuiYuan"].UpgradeValueBy(2);
        DynamicVars["Amount"].UpgradeValueBy(1);
    }
}

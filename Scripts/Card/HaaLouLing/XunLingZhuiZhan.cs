using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class XunLingZhuiZhan : HaaLouLingCard
{
    public XunLingZhuiZhan() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(16, ValueProp.Move)];
    protected override bool UsesKnife => true;
    protected override bool UsesGuiYuan => true;
    public override bool WantsGreenOutline => CombatState?.HittableEnemies.Any(enemy => HaaLouLingCombatRules.GuiYuan(enemy) > 0) == true;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var eligible = HaaLouLingCombatRules.GuiYuan(play.Target) > 0;
        await Attack(context, play);
        if (eligible) await GainKnife(context, 1);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5);
    }
}

using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class ChengXuErRu : HaaLouLingCard
{
    public ChengXuErRu() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(9, ValueProp.Move), new IntVar("Threshold", 20)];

    protected override bool UsesGuiYuan => true;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Attack(context, play, HaaLouLingCombatRules.GuiYuan(play.Target) >= DynamicVars["Threshold"].IntValue ? 2 : 1);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
    }
}

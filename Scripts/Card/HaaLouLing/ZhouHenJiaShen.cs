using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class ZhouHenJiaShen : HaaLouLingCard
{
    public ZhouHenJiaShen() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(5, ValueProp.Move), new IntVar("Amount", 0)];
    protected override bool UsesGuiYuan => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => FormKeywords.Concat([CardKeyword.Exhaust]);

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Attack(context, play);
        if (IsUpgraded) await GiveGuiYuan(context, play, DynamicVars["Amount"].IntValue);
        foreach (var power in play.Target!.Powers.OfType<GuiYuanBasePower>())
            power.SetAmount(Math.Min(100, power.Amount * 2));
    }

    protected override void OnUpgrade() => DynamicVars["Amount"].UpgradeValueBy(10);
}

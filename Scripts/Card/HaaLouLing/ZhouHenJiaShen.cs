using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class ZhouHenJiaShen : HaaLouLingCard
{
    public ZhouHenJiaShen() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(5, ValueProp.Move), new IntVar("Amount", 0)];
    protected override bool UsesGuiYuan => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => FormKeywords.Concat([CardKeyword.Exhaust]);

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Attack(context, play);
        if (IsUpgraded) await GiveGuiYuan(context, play, DynamicVars["Amount"].IntValue);
        await GiveGuiYuan(context, play, HaaLouLingCombatRules.GuiYuan(play.Target));
    }

    protected override void OnUpgrade() => DynamicVars["Amount"].UpgradeValueBy(10);
}

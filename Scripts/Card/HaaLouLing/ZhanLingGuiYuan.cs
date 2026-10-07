using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
[RegisterArchaicToothTranscendence(typeof(ZhanLingGuiYuanPoWang))]
public sealed class ZhanLingGuiYuan : HaaLouLingCard
{
    public ZhanLingGuiYuan() : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CalculationBaseVar(9), new ExtraDamageVar(6), new IntVar("Amount", 5), new CalculatedDamageVar(ValueProp.Move).WithMultiplier(static (card, _) => HaaLouLingCombatRules.KnifeForCard(card))];
    protected override bool UsesKnife => true;
    protected override bool UsesGuiYuan => true;
    public override KnifeForm Form => KnifeForm.Finish;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var spent = await FinishKnife(context);
        await Attack(context, play, calculated: true);
        await GiveGuiYuan(context, play, spent * DynamicVars["Amount"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.ExtraDamage.UpgradeValueBy(2);
        DynamicVars["Amount"].UpgradeValueBy(5);
    }
}

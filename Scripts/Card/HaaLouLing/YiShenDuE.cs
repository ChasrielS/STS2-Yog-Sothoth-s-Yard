using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class YiShenDuE : HaaLouLingCard
{
    public YiShenDuE() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CalculationBaseVar(0), new ExtraDamageVar(1), new CalculatedDamageVar(ValueProp.Move).WithMultiplier(static (_, target) => HaaLouLingCombatRules.GuiYuan(target))];
    protected override bool UsesGuiYuan => true;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var amount = HaaLouLingCombatRules.GuiYuan(play.Target);
        await Attack(context, play, calculated: true);
        await HaaLouLingCombatRules.GiveGuiYuan(context, Owner.Creature, amount, Owner.Creature, this);
        await HaaLouLingCombatRules.RemoveGuiYuan(play.Target!);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}

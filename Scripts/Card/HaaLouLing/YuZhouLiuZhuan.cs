using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class YuZhouLiuZhuan : HaaLouLingCard
{
    public YuZhouLiuZhuan() : base(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(8, ValueProp.Move)];
    protected override bool UsesGuiYuan => true;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var amount = HaaLouLingCombatRules.GuiYuan(play.Target);
        await Attack(context, play);
        foreach (var target in CombatState!.HittableEnemies.ToList())
            if (target != play.Target) await HaaLouLingCombatRules.GiveGuiYuan(context, target, amount, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1); AddKeyword(CardKeyword.Retain);
    }
}

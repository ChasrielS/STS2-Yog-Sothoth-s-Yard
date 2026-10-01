using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
[RegisterDustyTomeCard(typeof(HaaLouLing))]
public sealed class ZhanLingGuiYuanPoWang : HaaLouLingCard
{
    public ZhanLingGuiYuanPoWang() : base(1, CardType.Attack, CardRarity.Ancient, TargetType.AllEnemies) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CalculationBaseVar(10), new ExtraDamageVar(2), new CalculatedDamageVar(ValueProp.Move).WithMultiplier(static (card, _) => HaaLouLingCombatRules.KnifeForCard(card)), new IntVar("ExtraHit", 4)];
    protected override bool UsesKnife => true;
    public override KnifeForm Form => KnifeForm.Finish;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var count = await FinishKnife(context);
        await Attack(context, play, calculated: true);
        var target = CombatState!.HittableEnemies.OrderByDescending(enemy => enemy.CurrentHp).FirstOrDefault();
        if (target != null && count > 0)
            await DamageCmd.Attack(count * DynamicVars["ExtraHit"].BaseValue).FromCard(this, play).Targeting(target)
                .WithHitVfxNode(HaaLouLingStrikeSlashVfx.Create).Execute(context);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.ExtraDamage.UpgradeValueBy(1); DynamicVars["ExtraHit"].UpgradeValueBy(2);
    }
}

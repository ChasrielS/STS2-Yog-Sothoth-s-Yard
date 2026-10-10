using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class XianGuo : HaaLouLingCard
{
    public XianGuo() : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(5, ValueProp.Move), new DamageVar("DishDamage", 5, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Attack(context, play);
        var count = HaaLouLingCookingService.RemoveCookingSlots(Owner);
        for (var i = 0; i < count; i++)
            await DamageCmd.Attack(DynamicVars["DishDamage"].BaseValue).FromCard(this, play)
                .TargetingAllOpponents(CombatState!).WithHitVfxNode(HaaLouLingStrikeSlashVfx.Create).Execute(context);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
        DynamicVars["DishDamage"].UpgradeValueBy(3);
    }
}

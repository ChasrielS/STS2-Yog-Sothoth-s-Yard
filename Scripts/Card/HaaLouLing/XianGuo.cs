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
    public XianGuo() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(12, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var count = HaaLouLingCookingService.RemoveCookingSlots(Owner);
        for (var i = 0; i < count; i++) await Attack(context, play);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4);
    }
}

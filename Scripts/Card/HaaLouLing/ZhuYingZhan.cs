using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class ZhuYingZhan : HaaLouLingCard
{
    public ZhuYingZhan() : base(1, CardType.Attack, CardRarity.Common, TargetType.RandomEnemy) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(2, ValueProp.Move)];
    protected override bool UsesGuiYuan => true;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        for (var i = 0; i < 3; i++)
        {
            var attack = await Attack(context, play, random: true);
            foreach (var result in attack.Results.SelectMany(hit => hit))
                await HaaLouLingCombatRules.GiveGuiYuan(context, result.Receiver, (int)result.TotalDamage, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1);
    }
}

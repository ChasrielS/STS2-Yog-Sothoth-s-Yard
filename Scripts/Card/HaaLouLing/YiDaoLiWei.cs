using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class YiDaoLiWei : HaaLouLingCard
{
    public YiDaoLiWei() : base(3, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    protected override bool UsesKnife => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(30, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var attack = await Attack(context, play);
        if (attack.Results.SelectMany(hit => hit).Any(result => result.WasTargetKilled))
        {
            await Apply<DaoFaJingJinPower>(context, 1);
            await GainKnife(context, 3);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(10);
}

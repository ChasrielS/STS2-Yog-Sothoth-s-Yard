using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class LianHuanZhan : HaaLouLingCard
{
    public LianHuanZhan() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(4, ValueProp.Move)];
    protected override bool UsesKnife => true;
    public override KnifeForm Form => KnifeForm.Follow;
    public override bool WantsGreenOutline => Knife > 0;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var eligible = Knife > 0;
        await Attack(context, play, eligible ? 3 : 2);
        if (eligible) await GainKnife(context, 1);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);
    }
}

using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class HengDao : HaaLouLingCard
{
    public HengDao() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(8, ValueProp.Move), new BlockVar(6, ValueProp.Move)];
    protected override bool UsesKnife => true;
    public override KnifeForm Form => KnifeForm.Turn;
    public override bool WantsGreenOutline => Knife > 0;

    public override bool GainsBlock => true;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var eligible = Knife > 0;
        await Attack(context, play);
        if (eligible) { await SpendKnife(context, 1); await Block(play); }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3); DynamicVars.Block.UpgradeValueBy(2);
    }
}

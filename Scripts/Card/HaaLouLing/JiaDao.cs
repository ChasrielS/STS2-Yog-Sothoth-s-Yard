using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class JiaDao : HaaLouLingCard
{
    public JiaDao() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    protected override bool UsesKnife => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(7, ValueProp.Move)];

    public override bool GainsBlock => true;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Block(play);
        await GainKnife(context, 1);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3);
    }
}

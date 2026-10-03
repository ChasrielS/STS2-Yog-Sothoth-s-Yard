using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class TiQianBeiCai : HaaLouLingCard
{
    public TiQianBeiCai() : base(3, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(20, ValueProp.Move), new CardsVar(2), new IntVar("Ingredients", 3)];
    public override bool GainsBlock => true;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Block(play);
        if (IsUpgraded) await Apply<TiQianBeiCaiPlusPower>(context, 1);
        else await Apply<TiQianBeiCaiPower>(context, 1);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(4);
        DynamicVars.Cards.UpgradeValueBy(1);
        DynamicVars["Ingredients"].UpgradeValueBy(1);
    }
}

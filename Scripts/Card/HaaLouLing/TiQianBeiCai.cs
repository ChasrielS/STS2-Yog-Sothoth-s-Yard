using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class TiQianBeiCai : HaaLouLingCard
{
    public TiQianBeiCai() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(15, ValueProp.Move), new CardsVar(1), new IntVar("Ingredients", 2)];
    public override bool GainsBlock => true;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Block(play);
        await Apply<XiaHuiHeShiCaiAnyPower>(context, DynamicVars["Ingredients"].IntValue);
        await Apply<DrawCardsNextTurnPower>(context, DynamicVars.Cards.IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(5);
        DynamicVars.Cards.UpgradeValueBy(1);
        DynamicVars["Ingredients"].UpgradeValueBy(1);
    }
}

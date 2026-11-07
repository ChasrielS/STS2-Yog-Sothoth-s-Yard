using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Commands;
using YogSothothsYard.Scripts.Cooking;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class JiuDiQuCai : HaaLouLingCard
{
    public JiuDiQuCai() : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(21, ValueProp.Move), new CardsVar(2)];
    public override bool GainsBlock => true;
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.Static(StaticHoverTip.Transform)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Block(play);
        var cards = PileType.Hand.GetPile(Owner).Cards.ToList();
        await CardPileCmd.RemoveFromCombat(cards, false);
        await HaaLouLingCookingService.AddRandomIngredients(context, Owner, cards.Count * DynamicVars.Cards.IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3);
        DynamicVars.Cards.UpgradeValueBy(1);
    }
}

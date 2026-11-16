using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.HoverTips;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class XieXiaBaoFu : HaaLouLingCard
{
    public XieXiaBaoFu() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(7, ValueProp.Move), new CardsVar(1)];

    public override bool GainsBlock => true;
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Block(play);
        var prompt = new LocString("cards", Id.Entry + (ErosionRank > 0 ? ".optionalSelectionPrompt" : ".selectionScreenPrompt"));
        var count = DynamicVars.Cards.IntValue;
        var selected = await CardSelectCmd.FromHand(context, Owner,
            new CardSelectorPrefs(prompt, ErosionRank > 0 ? 0 : count, count), null, this);
        foreach (var card in selected) await CardCmd.Exhaust(context, card);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3);
    }
}

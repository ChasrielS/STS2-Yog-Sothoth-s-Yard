using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class QiShi : HaaLouLingCard
{
    public QiShi() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amount", 3)];
    protected override bool UsesKnife => true;
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        base.AdditionalHoverTips.Concat([HoverTipFactory.FromKeyword(HaaLouLingKeywords.HeShi)]);
    public override KnifeForm Form => KnifeForm.Open;
    protected override bool ShouldGlowRedInternal => Knife > 0;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (Knife > 0) return;
        await GainKnife(context, DynamicVars["Amount"].IntValue);
        var selected = (await CardSelectCmd.FromCombatPile(context, PileType.Discard.GetPile(Owner), Owner,
            new CardSelectorPrefs(new LocString("cards", Id.Entry + ".selectionScreenPrompt"), 1),
            card => card.Keywords.Contains(HaaLouLingKeywords.HeShi))).FirstOrDefault();
        if (selected != null) await CardPileCmd.Add(selected, PileType.Draw, CardPilePosition.Top);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}

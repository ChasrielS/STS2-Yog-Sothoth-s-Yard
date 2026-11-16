using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts.Shadow;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class QiChengZhuanHe : HaaLouLingCard
{
    public QiChengZhuanHe() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    private static IEnumerable<CardModel> Forms =>
        [ModelDb.Card<QiShi>(), ModelDb.Card<ChengShi>(), ModelDb.Card<ZhuanShi>(), ModelDb.Card<HeShi>()];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        Forms.Select(card => HoverTipFactory.FromCard(PreviewForm(card)));

    private CardModel PreviewForm(CardModel template)
    {
        if (ErosionRank == 0) return template;
        var preview = template.ToMutable();
        ShadowRanks.Set(preview, ErosionRank);
        return preview;
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var cards = Forms.Select(template => CombatState!.CreateCard(template, Owner)).ToList();
        foreach (var card in cards)
            if (ErosionRank > 0) ShadowRanks.Set(card, ErosionRank);
        var added = await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand, Owner);
        CardCmd.PreviewCardPileAdd(added);
    }

    protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Exhaust);
}

using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class JiRouZhuanGong : HaaLouLingCard
{
    public JiRouZhuanGong() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    public override IEnumerable<CardKeyword> CanonicalKeywords => FormKeywords.Concat([CardKeyword.Exhaust]);
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        base.AdditionalHoverTips.Concat([HoverTipFactory.FromCard<JiRou>()]);

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var cards = Entry.FoodPile.GetPile(Owner).Cards.Where(card => IsUpgraded ? card.Rarity != CardRarity.Rare : card.Rarity == CardRarity.Common).ToList();
        foreach (var card in cards) await CardPileCmd.RemoveFromCombat(card, false);
        await HaaLouLingCookingService.AddSpecificIngredients<JiRou>(context, Owner, cards.Count);
    }
}

using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class ChongXinPeiCai : HaaLouLingCard
{
    public ChongXinPeiCai() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    public override IEnumerable<CardKeyword> CanonicalKeywords => FormKeywords.Concat([CardKeyword.Exhaust]);
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        base.AdditionalHoverTips.Concat([HoverTipFactory.Static(StaticHoverTip.Transform)]);

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var selected = await HaaLouLingCardChoices.SelectOwnedIngredients(context, this,
            Entry.FoodPile.GetPile(Owner).Cards.Count, filter: card => card.Rarity != CardRarity.Rare);
        await HaaLouLingCardChoices.TransformIngredients(context, this, selected);
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}

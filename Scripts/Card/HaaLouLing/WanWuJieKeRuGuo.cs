using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class WanWuJieKeRuGuo : HaaLouLingCard
{
    public WanWuJieKeRuGuo() : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var cards = new[] { PileType.Hand, PileType.Draw, PileType.Discard }.SelectMany(pile => pile.GetPile(Owner).Cards).ToList();
        await HaaLouLingCardChoices.TransformCombatCards(context, this, cards);
        if (IsUpgraded) await HaaLouLingCookingService.AddSpecificIngredients<YuanGuXinXiSu>(context, Owner, 1);
    }
}

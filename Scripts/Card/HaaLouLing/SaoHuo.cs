using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class SaoHuo : HaaLouLingCard
{
    public SaoHuo() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var copies = ErosionRank == 3 ? 2 : 1;
        var cards = HaaLouLingCookingService.Ingredients(Owner, nonRare: true)
            .SelectMany(card => Enumerable.Range(0, copies).Select(_ => CombatState!.CreateCard(card, Owner)));
        await HaaLouLingCookingService.AddGeneratedIngredients(context, Owner, cards);
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}

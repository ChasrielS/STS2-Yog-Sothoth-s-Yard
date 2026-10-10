using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class ZaiLaiYiLan : HaaLouLingCard
{
    public ZaiLaiYiLan() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var copies = Entry.FoodPile.GetPile(Owner).Cards.Select(card => card.CreateClone()).ToList();
        await HaaLouLingCookingService.AddGeneratedIngredients(context, Owner, copies);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

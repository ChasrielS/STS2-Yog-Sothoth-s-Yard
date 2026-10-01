using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class YanHuoYuDaoGuang : HaaLouLingCard
{
    public YanHuoYuDaoGuang() : base(1, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3), new IntVar("Amount", 5)];
    protected override bool UsesKnife => true;
    protected override bool UsesGuiYuan => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => FormKeywords.Concat([CardKeyword.Exhaust]);

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await HaaLouLingCookingService.AddRandomIngredients(context, Owner, DynamicVars.Cards.IntValue);
        await GainKnife(context, 1);
        await GiveGuiYuan(context, play, DynamicVars["Amount"].IntValue);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}

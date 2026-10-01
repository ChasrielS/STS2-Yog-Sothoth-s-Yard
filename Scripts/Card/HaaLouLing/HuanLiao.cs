using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class HuanLiao : HaaLouLingCard
{
    public HuanLiao() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amount", 2)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var chosen = await HaaLouLingCardChoices.SelectOwnedIngredients(context, this, DynamicVars["Amount"].IntValue);
        await HaaLouLingCardChoices.TransformIngredients(context, this, chosen, sameRarity: true);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}

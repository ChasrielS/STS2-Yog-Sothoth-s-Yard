using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.HoverTips;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class JieZhou : HaaLouLingCard
{
    public JieZhou() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var prompt = new LocString("cards", Id.Entry + ".selectionScreenPrompt");
        DynamicVars.AddTo(prompt);
        var selected = await CardSelectCmd.FromCombatPile(context, PileType.Draw.GetPile(Owner), Owner,
            new CardSelectorPrefs(prompt, DynamicVars.Cards.IntValue), card => card.Type == CardType.Curse);
        foreach (var card in selected) await CardCmd.Exhaust(context, card);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1);
    }
}

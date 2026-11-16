using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using STS2RitsuLib.Patching.Models;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class JieZhou : HaaLouLingCard
{
    internal static string CombinedPilePrompt => ModelDb.GetId<JieZhou>().Entry + ".combinedPilePrompt";
    public JieZhou() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var selected = ErosionRank == 0
            ? await CardSelectCmd.FromCombatPile(context, PileType.Draw.GetPile(Owner), Owner,
                new CardSelectorPrefs(new LocString("cards", Id.Entry + ".selectionScreenPrompt"), DynamicVars.Cards.IntValue),
                card => card.Type == CardType.Curse)
            : await SelectFromBothPiles(context);
        foreach (var card in selected) await CardCmd.Exhaust(context, card);
    }

    private async Task<IEnumerable<CardModel>> SelectFromBothPiles(PlayerChoiceContext context)
    {
        var cards = CardPile.GetCards(Owner, PileType.Draw, PileType.Discard)
            .OrderBy(card => card.Rarity).ThenBy(card => card.Id.Entry, StringComparer.Ordinal).ToList();
        var count = Math.Min(DynamicVars.Cards.IntValue, cards.Count(card => card.Type == CardType.Curse));
        if (count == 0) return [];
        var prompt = new LocString("cards", CombinedPilePrompt);
        var prefs = new CardSelectorPrefs(prompt, count);
        var selected = await CardSelectCmd.FromSimpleGrid(context, cards, Owner, prefs);
        return selected.Where(card => card.Type == CardType.Curse);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1);
    }
}

public sealed class CombinedCursePileSelectionPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_combined_curse_pile_selection";
    public static ModPatchTarget[] GetTargets() =>
        [PatchTarget.Method<NSimpleCardSelectScreen>("OnCardClicked", typeof(CardModel))];

    public static bool Prefix(CardModel card, CardSelectorPrefs ____prefs) =>
        ____prefs.Prompt.LocEntryKey != JieZhou.CombinedPilePrompt || card.Type == CardType.Curse;
}

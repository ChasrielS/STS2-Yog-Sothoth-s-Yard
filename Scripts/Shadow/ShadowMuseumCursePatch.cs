using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;

namespace YogSothothsYard.Scripts.Shadow;

internal static partial class ShadowEvents
{
    internal static void SelectMuseumCurse(Player player, ref CardModel canonicalCard)
    {
        if (canonicalCard.Type != CardType.Curse ||
            ForPlayer(player, Effects.Card) is not { EventId: "MUSEUM_OF_TORTURE", OptionKey: "TAKE" } choice)
            return;

        var rank = RollRank(choice, 1);
        if (!ShadowCardRules.TryGet(canonicalCard, rank, out _))
        {
            var candidates = ModelDb.CardPool<CurseCardPool>()
                .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
                .Where(card => card.CanBeGeneratedByModifiers && ShadowCardRules.TryGet(card, rank, out _))
                .OrderBy(card => card.Id);
            if (choice.Rng.NextItem(candidates) is not { } replacement) return;
            canonicalCard = replacement;
        }
        choice.CardRanks[canonicalCard.Id] = rank;
    }
}

public sealed class ShadowMuseumCursePatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_museum_curse";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(RunState), nameof(RunState.CreateCard), [typeof(CardModel), typeof(Player)])];

    public static void Prefix(Player owner, ref CardModel canonicalCard) =>
        ShadowEvents.SelectMuseumCurse(owner, ref canonicalCard);
}

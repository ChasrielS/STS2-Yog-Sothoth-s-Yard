using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace YogSothothsYard.Scripts.Shadow;

internal static partial class ShadowMonsterRules
{
    private static int[]? StatusRanks(string id, string move, int rank, MonsterModel monster)
    {
        if (StatusType(id, move) == null) return null;
        return id switch
        {
            "LEAF_SLIME_S" or "TWIG_SLIME_M" => [rank],
            "LEAF_SLIME_M" or "MYTE" => rank switch
            {
                1 => [0, 1],
                2 => MonsterRng(monster).NextInt(2) == 0 ? [1, 1] : [0, 2],
                _ => MonsterRng(monster).NextInt(3) == 0 ? [0, 3] : [1, 2]
            },
            "EYE_WITH_TEETH" or "CHOMPER" => rank switch
            {
                1 => [0, 0, 1],
                2 => MonsterRng(monster).NextInt(2) == 0 ? [0, 1, 1] : id == "CHOMPER" ? [0, 0, 2] : [0, 2],
                _ => MonsterRng(monster).NextInt(3) switch { 0 => [1, 1, 1], 1 => [0, 1, 2], _ => [0, 0, 3] }
            },
            "HAUNTED_SHIP" => rank switch
            {
                1 => [0, 0, 0, 0, 1],
                2 => MonsterRng(monster).NextInt(2) == 0 ? [0, 0, 0, 1, 1] : [0, 0, 0, 0, 2],
                _ => MonsterRng(monster).NextInt(3) switch
                {
                    0 => [0, 0, 1, 1, 1], 1 => [0, 0, 0, 1, 2], _ => [0, 0, 0, 0, 3]
                }
            },
            "SLIMED_BERSERKER" => rank switch
            {
                1 => [0, 0, 0, 0, 0, 0, 0, 0, 0, 1],
                2 => MonsterRng(monster).NextInt(2) == 0
                    ? [0, 0, 0, 0, 0, 0, 0, 0, 0, 2] : [0, 0, 0, 0, 0, 0, 0, 0, 2, 2],
                _ => MonsterRng(monster).NextInt(3) switch
                {
                    0 => [0, 0, 0, 0, 0, 0, 0, 0, 0, 3],
                    1 => [0, 0, 0, 0, 0, 0, 0, 0, 1, 2],
                    _ => [0, 0, 0, 0, 0, 0, 0, 1, 1, 1]
                }
            },
            "NOISEBOT" => [rank, rank],
            _ => null
        };
    }

    private static Type? StatusType(string monster, string move) => (monster, move) switch
    {
        ("LEAF_SLIME_S", "GOOP_MOVE") or ("LEAF_SLIME_M", "STICKY_SHOT") or
        ("TWIG_SLIME_M", "STICKY_SHOT_MOVE") or ("SLIMED_BERSERKER", "VOMIT_ICHOR_MOVE") => typeof(Slimed),
        ("EYE_WITH_TEETH", "DISTRACT_MOVE") or ("HAUNTED_SHIP", "HAUNT_MOVE") or
        ("CHOMPER", "SCREECH_MOVE") or ("NOISEBOT", "NOISE_MOVE") => typeof(Dazed),
        ("MYTE", "TOXIC_MOVE") => typeof(Toxic),
        _ => null
    };

    internal static bool PrepareStatus(CardModel card)
    {
        if (Acting.Value is not { } action || Rank(action.Monster) == 0 ||
            card.GetType() != StatusType(Species(action.Monster), action.Move.Id))
            return true;
        if ((action.Kinds & MoveKind.Status) == 0) return false;
        if (action.StatusRanks is not { } ranks) return true;
        var owner = card.Owner;
        var index = action.Recipients.GetValueOrDefault(owner);
        if (index >= ranks.Length) return false;
        action.Recipients[owner] = index + 1;
        var rank = ranks[index];
        if (rank > 0 && ShadowCardRules.TryGet(card, rank, out _)) ShadowRanks.Set(card, rank);
        return true;
    }
}

using MegaCrit.Sts2.Core.Random;

namespace YogSothothsYard.Scripts.Shadow;

internal static class ShadowChances
{
    internal static readonly int[] Common = [20, 40, 60, 80, 100];
    internal static readonly int[] Uncommon = [0, 20, 40, 60, 80];
    internal static readonly int[] Rare = [0, 0, 20, 40, 60];
    internal static readonly int[] ShopAndAncient = [0, 0, 20, 35, 50];
    internal static readonly int[] Event = [15, 30, 45, 60, 75];

    private static readonly (int First, int Second)[] RankDistribution =
        [(100, 0), (60, 40), (35, 35), (20, 40), (10, 25)];

    internal static bool IsEroded(Rng rng, int level, int[] chances) =>
        level > 0 && chances[level - 1] is > 0 and var chance && rng.NextInt(100) < chance;

    internal static int RollRank(Rng rng, int level, int rarity)
    {
        var (first, second) = Distribution(level, rarity);
        if (first == 100) return 1;
        var roll = rng.NextInt(100);
        return roll < first ? 1 : roll < first + second ? 2 : 3;
    }

    internal static IEnumerable<int> PossibleRanks(int level, int rarity)
    {
        var (first, second) = Distribution(level, rarity);
        yield return 1;
        if (second > 0) yield return 2;
        if (first + second < 100) yield return 3;
    }

    private static (int First, int Second) Distribution(int level, int rarity) =>
        RankDistribution[Math.Max(0, level - rarity - 1)];
}

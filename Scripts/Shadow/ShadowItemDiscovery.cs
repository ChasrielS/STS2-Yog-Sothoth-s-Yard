using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib;
using STS2RitsuLib.Data;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils.Persistence;

namespace YogSothothsYard.Scripts.Shadow;

internal static class ShadowItemDiscovery
{
    private const string Key = "shadow_item_discovery";
    private static ModDataStore Store => RitsuLibFramework.GetDataStore(Entry.ModId);

    internal static void Register()
    {
        using (RitsuLibFramework.BeginModDataRegistration(Entry.ModId))
            Store.Register<ShadowItemDiscoveryData>(Key, "shadow_item_discovery.json", SaveScope.Profile,
                defaultFactory: static () => new ShadowItemDiscoveryData(), autoCreateIfMissing: false);
    }

    internal static bool HasSeen(AbstractModel item, int rank) => SaveManager.Instance.IsProfileInitialized &&
        Store.Get<ShadowItemDiscoveryData>(Key).Seen.Contains(ItemRankKey(item, rank));

    internal static void Observe(AbstractModel item)
    {
        var rank = item switch
        {
            RelicModel relic => ShadowRanks.Of(relic),
            PotionModel potion => ShadowRanks.Of(potion),
            _ => 0
        };
        if (rank == 0 || !SaveManager.Instance.IsProfileInitialized) return;
        var key = ItemRankKey(item, rank);
        if (Store.Get<ShadowItemDiscoveryData>(Key).Seen.Contains(key)) return;
        Store.Modify<ShadowItemDiscoveryData>(Key, data => data.Seen.Add(key));
        Store.Save(Key);
    }

    internal static void UnlockAll()
    {
        if (!SaveManager.Instance.IsProfileInitialized) return;
        Store.Modify<ShadowItemDiscoveryData>(Key, data =>
        {
            for (var rank = 1; rank <= 3; rank++)
            {
                foreach (var relic in ModelDb.AllRelics)
                    if (ShadowRelicRules.Supports(relic, rank)) data.Seen.Add(ItemRankKey(relic, rank));
                foreach (var potion in ModelDb.AllPotions)
                    if (ShadowPotionRules.Supports(potion, rank)) data.Seen.Add(ItemRankKey(potion, rank));
            }
        });
        Store.Save(Key);
    }

    private static string ItemRankKey(AbstractModel item, int rank) => $"{item.Id}:{rank}";
}

public sealed class ShadowItemDiscoveryData
{
    public HashSet<string> Seen { get; set; } = [];
}

public sealed class ShadowItemSeenPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_item_seen";
    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(SaveManager), nameof(SaveManager.MarkRelicAsSeen), [typeof(RelicModel)]),
        new(typeof(SaveManager), nameof(SaveManager.MarkPotionAsSeen), [typeof(PotionModel)])
    ];

    public static void Postfix(AbstractModel __0) => ShadowItemDiscovery.Observe(__0);
}
